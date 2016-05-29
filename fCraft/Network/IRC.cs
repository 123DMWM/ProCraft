﻿// Part of fCraft | fCraft is copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
/* Based, in part, on SmartIrc4net code. Original license is reproduced below.
 * 
 *
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2005 Mirco Bauer <meebey@meebey.net> <http://www.meebey.net>
 *
 * Full LGPL License: <http://www.gnu.org/licenses/lgpl.txt>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft
{
    /// <summary> IRC control class. </summary>
    public static class IRC
    {
        const string ResetReplacement = "\u0003\u000F",
                     BoldReplacement = "\u0002",
                     Reset = "\u211C",
                     Bold = "\u212C";

        static readonly Regex IrcNickRegex = new Regex(@"\A[a-z_\-\[\]\\^{}|`][a-z0-9_\-\[\]\\^{}|`]*\z",
                                                        RegexOptions.IgnoreCase),
                              UserHostRegex = new Regex(@"^[a-z0-9_\-\[\]\\^{}|`]+\*?=[+-]?(.+@.+)$",
                                                         RegexOptions.IgnoreCase),
                              MaxNickLengthRegex = new Regex(@"NICKLEN=(\d+)");

        static int userHostLength = 60,
                   maxNickLength = 30;

        const int MaxMessageSize = 510; // +2 bytes for CR-LF

        static DateTime lastIrcCommand;

        public static Dictionary<string, List<string>> Users = new Dictionary<string, List<string>>();

        /// <summary> Class represents an IRC connection/thread.
        /// There is an undocumented option (IRCThreads) to "load balance" the outgoing
        /// messages between multiple bots. If that's the case, several IrcThread objects
        /// are created. The bots grab messages from IRC.outputQueue whenever they are
        /// not on cooldown (a bit of an intentional race condition). </summary>
        sealed class IrcThread : IDisposable
        {
            readonly string desiredBotNick;
            TcpClient client;
            StreamReader reader;
            StreamWriter writer;
            Thread thread;
            bool isConnected;
            bool reconnect;
            DateTime lastMessageSent;
            DateTime lastNickAttempt;
            int nickTry;
            readonly ConcurrentQueue<string> localQueue = new ConcurrentQueue<string>();
            static readonly Encoding Encoding = new UTF8Encoding(false);

            public bool IsReady { get; private set; }
            public bool ResponsibleForInputParsing { get; set; }

            [NotNull]
            public string ActualBotNick { get; private set; }


            public IrcThread([NotNull] string botNick)
            {
                if (botNick == null) throw new ArgumentNullException("botNick");
                desiredBotNick = botNick;
                ActualBotNick = botNick;
            }

            public bool Start(bool parseInput)
            {
                ResponsibleForInputParsing = parseInput;
                try
                {
                    // start the machinery!
                    thread = new Thread(IoThread)
                    {
                        Name = "ProCraft.IRC",
                        IsBackground = true,
                        CurrentCulture = new CultureInfo("en-US")
                    };
                    thread.Start();
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error,
                                "IRC: Could not start the bot: {0}",
                                ex);
                    return false;
                }
            }


            void Connect()
            {
                // initialize the client
                IPAddress ipToBindTo = IPAddress.Parse(ConfigKey.IP.GetString());
                IPEndPoint localEndPoint = new IPEndPoint(ipToBindTo, 0);
                client = new TcpClient(localEndPoint)
                {
                    NoDelay = true,
                    ReceiveTimeout = (int)Timeout.TotalMilliseconds,
                    SendTimeout = (int)Timeout.TotalMilliseconds
                };
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);

                // connect
                client.Connect(hostName, port);

                // prepare to read/write
                reader = new StreamReader(client.GetStream(), Encoding, false);
                writer = new StreamWriter(client.GetStream(), Encoding, 512);
                isConnected = true;
            }


            void Send([NotNull] string msg)
            {
                if (msg == null) throw new ArgumentNullException("msg");
                localQueue.Enqueue(msg);
            }


            // runs in its own thread, started from Connect()
            void IoThread()
            {
                lastMessageSent = DateTime.UtcNow;

                do
                {
                    try
                    {
                        ActualBotNick = desiredBotNick;
                        reconnect = false;
                        Logger.Log(LogType.IrcStatus,
                                    "Connecting to {0}:{1} as {2}",
                                    hostName,
                                    port,
                                    ActualBotNick);
                        Connect();

                        // register
                        Send(IRCCommands.Nick(ActualBotNick));
                        Send(IRCCommands.User(ActualBotNick, 8, ConfigKey.ServerName.GetString()));
                        lastNickAttempt = DateTime.UtcNow;
                        nickTry = 0;

                        Send(IRCCommands.Names(channelNames));

                        while (isConnected && !reconnect)
                        {
                            Thread.Sleep(20);

                            DateTime now = DateTime.UtcNow;
                            if (now.Subtract(lastMessageSent) >= SendDelay)
                            {
                                string outputLine;
                                if (localQueue.TryDequeue(out outputLine))
                                {
#if DEBUG_IRC
                                    Logger.Log( LogType.IrcStatus, "[Out.Local] {0}", outputLine );
#endif
                                    writer.Write(outputLine);
                                    writer.Write('\r');
                                    writer.Write('\n');
                                    lastMessageSent = now;
                                    writer.Flush();
                                    if (outputLine.StartsWith("QUIT"))
                                    {
                                        isConnected = false;
                                        reconnect = false;
                                        break;
                                    }
                                }
                                else if (OutputQueue.TryDequeue(out outputLine))
                                {
#if DEBUG_IRC
                                    Logger.Log( LogType.IrcStatus, "[Out.Global] {0}", outputLine );
#endif
                                    writer.Write(outputLine);
                                    writer.Write('\r');
                                    writer.Write('\n');
                                    lastMessageSent = now;
                                    writer.Flush();
                                }
                                else if (ActualBotNick != desiredBotNick &&
                                         now.Subtract(lastNickAttempt) >= NickRetryDelay)
                                {
                                    RetryForDesiredNick();
                                }
                            }

                            if (client.Client.Available > 0)
                            {
                                string line = reader.ReadLine();
                                if (line == null)
                                {
                                    reconnect = true;
                                    break;
                                }
                                HandleMessage(line);
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        LogDisconnectWarning(ex);
                        reconnect = true;
                    }
                    catch (IOException ex)
                    {
                        LogDisconnectWarning(ex);
                        reconnect = true;
#if !DEBUG
                    }
                    catch (Exception ex)
                    {
                        Logger.LogAndReportCrash("IRC bot crashed", "ProCraft", ex, false);
                        reconnect = true;
#endif
                    }

                    if (reconnect) Thread.Sleep(ReconnectDelay);
                } while (reconnect);
            }

            void RetryForDesiredNick()
            {
                Logger.Log(LogType.IrcStatus,
                            "Retrying for desired IRC bot nick ({0} to {1})",
                            ActualBotNick,
                            desiredBotNick);
                Send(IRCCommands.Nick(desiredBotNick));
                lastNickAttempt = DateTime.UtcNow;
                lastMessageSent = lastNickAttempt;
            }


            static void LogDisconnectWarning([NotNull] Exception ex)
            {
                if (ex == null) throw new ArgumentNullException("ex");
                Logger.Log(LogType.Warning,
                            "IRC: Disconnected ({0}: {1}). Will retry in {2} seconds.",
                            ex.GetType().Name,
                            ex.Message,
                            ReconnectDelay.TotalSeconds);
            }

            void HandleMessage([NotNull] string message)
            {
                if (message == null) throw new ArgumentNullException("message");

                IRCMessage msg = MessageParser(message, ActualBotNick);
#if DEBUG_IRC
                Logger.Log( LogType.IrcStatus,
                            "[{0}.{1}] {2}",
                            msg.Type, msg.ReplyCode, msg.RawMessage );
#endif

                switch (msg.Type)
                {
                    case IRCMessageType.Login:
                        if (msg.ReplyCode == IRCReplyCode.Welcome)
                        {
                            AuthWithNickServ();
                            foreach (string channel in channelNames)
                            {
                                Send(IRCCommands.Join(channel));
                            }
                            IsReady = true;
                            Send(IRCCommands.Userhost(ActualBotNick));
                            AssignBotForInputParsing(); // bot should be ready to receive input after joining
                        }
                        else if (msg.ReplyCode == IRCReplyCode.Bounce)
                        {
                            Match nickLenMatch = MaxNickLengthRegex.Match(msg.Message);
                            int maxNickLengthTemp;
                            if (nickLenMatch.Success &&
                                Int32.TryParse(nickLenMatch.Groups[1].Value, out maxNickLengthTemp))
                            {
                                maxNickLength = maxNickLengthTemp;
                            }
                        }
                        return;


                    case IRCMessageType.Ping:
                        // ping-pong
                        Send(IRCCommands.Pong(msg.RawMessageArray[1].Substring(1)));
                        return;


                    case IRCMessageType.ChannelAction:
                    case IRCMessageType.ChannelMessage:
                        // channel chat
                        if (!ResponsibleForInputParsing) return;
                        if (!IsBotNick(msg.Nick)) {
                            string rawMessage = msg.Message;
                            if (msg.Type == IRCMessageType.ChannelAction) {
                                if (rawMessage.StartsWith("\u0001ACTION")) {
                                    rawMessage = rawMessage.Substring(8);
                                } else {
                                    return;
                                }
                            }

                            string processedMessage = ProcessMessageFromIRC(rawMessage);
                            if (processedMessage.Length > 0) {
                                if (ConfigKey.IRCBotForwardFromIRC.Enabled()) {
                                    if (msg.Type == IRCMessageType.ChannelAction) {
                                        foreach (Player player in Server.Players) {
                                            if (player.Info.ReadIRC) {
                                                player.Message("&i(IRC) * {0} {1}", msg.Nick, processedMessage);
                                            }
                                        }
                                        Logger.Log(LogType.IrcChat, "{0}: * {1} {2}", msg.Channel, msg.Nick,
                                            IRCColorsAndNonStandardCharsExceptEmotes.Replace(rawMessage, ""));
                            		} else if (HandleIrcCommand(msg, rawMessage)) {                            			
                                    } else if (rawMessage.ToLower().StartsWith("@") || rawMessage.ToLower().StartsWith(ActualBotNick.ToLower() + " @")) {
                                        if (DateTime.UtcNow.Subtract(lastIrcCommand).TotalSeconds > 5) {
                                            string otherPlayerName = rawMessage.Split()[(rawMessage.ToLower().StartsWith("@") ? 0 : 1)].Remove(0,1);
                                            string messageText = rawMessage.ToLower().StartsWith("@") ? rawMessage.Remove(0, rawMessage.Split()[0].Length + 1) : rawMessage.Remove(0, rawMessage.Split()[0].Length + rawMessage.Split()[1].Length + 2);

                                            // first, find ALL players (visible and hidden)
                                            Player[] allPlayers = Server.FindPlayers(otherPlayerName,
                                                SearchOptions.IncludeHidden);

                                            // if there is more than 1 target player, exclude hidden players
                                            if (allPlayers.Length > 1) {
                                                allPlayers = Server.FindPlayers(otherPlayerName,
                                                    SearchOptions.Default);
                                            }

                                            if (allPlayers.Length == 1) {
                                                Player target = allPlayers[0];
                                                if (target.Info.ReadIRC == true && !target.IsDeaf) {
                                                    Chat.IRCSendPM(msg.Nick, target, messageText);
                                                    lastIrcCommand = DateTime.UtcNow;
                                                }

                                                if (target.Info.IsHidden == true) {
                                                    // message was sent to a hidden player
													SendChannelMessage("No players found matching \"" +
																	   Bold + otherPlayerName + Reset + "\"");
                                                    lastIrcCommand = DateTime.UtcNow;

                                                } else {
                                                    // message was sent normally
                                                    if (target.Info.ReadIRC == false) {
                                                        if (target.Info.IsHidden == false) {
															SendChannelMessage("&WCannot PM " + Bold + 
                                                    		                   target.ClassyName + Reset +
																			   "&W: they have IRC ignored.");
                                                        }
                                                    } else if (target.IsDeaf) {
														SendChannelMessage("&WCannot PM " + Bold +
                                                                           target.ClassyName +
																		   Reset + "&W: they are currently deaf.");
                                                    } else {
														SendChannelMessage("to " + Bold + target.Name + Reset + ": " +
                                                                           messageText);
                                                    }
                                                    lastIrcCommand = DateTime.UtcNow;
                                                }

                                            } else if (allPlayers.Length == 0) {
												SendChannelMessage("No players found matching \"" +
																   Bold + otherPlayerName + Reset + "\"");

                                            } else {
                                                IClassy[] itemsEnumerated = allPlayers.ToArray();
                                                string nameList = itemsEnumerated.Take(15)
                                                    .JoinToString(", ", p => p.ClassyName);
                                                int count = itemsEnumerated.Length;
                                                if (count > 15) {
													SendChannelMessage("More than " + Bold + count + Reset +
																	   " players matched: " + nameList);
                                                } else {
                                                    SendChannelMessage("More than one player matched: " + nameList);
                                                }
                                                lastIrcCommand = DateTime.UtcNow;
                                            }
                                        }
                                    } else
                                        foreach (Player player in Server.Players.Where(player => player.Info.ReadIRC)) {
                                            player.Message("&i(IRC) {0}{1}: {2}", msg.Nick, Color.White,
                                                processedMessage);
                                        }
                                    Logger.Log(LogType.IrcChat, "{0}: {1}: {2}", msg.Channel, msg.Nick,
                                        IRCColorsAndNonStandardCharsExceptEmotes.Replace(rawMessage, ""));
                                } else if (msg.Message.StartsWith("#")) {
                                    foreach (Player player in Server.Players.Where(player => player.Info.ReadIRC)) {
                                        player.Message("&i(IRC) {0}{1}: {2}", msg.Nick, Color.White,
                                            processedMessage.Substring(1));
                                    }
                                    Logger.Log(LogType.IrcChat, "{0}: {1}: {2}", msg.Channel, msg.Nick,
                                        IRCColorsAndNonStandardCharsExceptEmotes.Replace(rawMessage, ""));
                                }
                            }
                        }
                        return;


                    case IRCMessageType.Join:
                        if (!ResponsibleForInputParsing) return;
                        if (ConfigKey.IRCBotAnnounceIRCJoins.Enabled())
                        {
                            Server.Message("&i(IRC) {0} joined the IRC channel",
                                            msg.Nick,
                                            msg.Channel);
                            Logger.Log(LogType.IrcChat,
                                        "{0} joined the IRC channel",
                                        msg.Nick,
                                        msg.Channel);
                        }
                        return;


                    case IRCMessageType.Kick:
                        string kicked = msg.RawMessageArray[3];
                        if (kicked == ActualBotNick)
                        {
                            // If we got kicked, attempt to rejoin
                            Logger.Log(LogType.IrcStatus,
                                        "IRC Bot was kicked from {0} by {1} ({2}), rejoining.",
                                        msg.Channel,
                                        msg.Nick,
                                        msg.Message);
                            Thread.Sleep(ReconnectDelay);
                            Send(IRCCommands.Join(msg.Channel));
                        }
                        else
                        {
                            if (!ResponsibleForInputParsing) return;
                            // Someone else got kicked -- announce it
                            string kickMessage = ProcessMessageFromIRC(msg.Message);
                            Server.Message("&i(IRC) {0} kicked {1} from the IRC channel ({3})",
                                            msg.Nick,
                                            kicked,
                                            msg.Channel,
                                            kickMessage);
                            Logger.Log(LogType.IrcChat,
                                        "{0} kicked {1} from {2} ({3})",
                                        msg.Nick,
                                        kicked,
                                        msg.Channel,
                                        IRCColorsAndNonStandardCharsExceptEmotes.Replace(kickMessage, ""));
                        }
                        return;


                    case IRCMessageType.Part:
                    case IRCMessageType.Quit:
                        // If someone using our desired nick just quit, retry for that nick
                        if (msg.Type == IRCMessageType.Quit &&
                            msg.Nick == desiredBotNick &&
                            ActualBotNick != desiredBotNick)
                        {
                            RetryForDesiredNick();
                            return;
                        }
                        if (!ResponsibleForInputParsing) return;
                        // Announce parts/quits of IRC people (except the bots)
                        if (ConfigKey.IRCBotAnnounceIRCJoins.Enabled() && !IsBotNick(msg.Nick))
                        {
                            Server.Message("&i(IRC) {0} left the IRC channel",
                                            msg.Nick,
                                            msg.Channel);
                            string quitMsg = (msg.Message == null)
                                                 ? "Quit"
                                                 : IRCColorsAndNonStandardCharsExceptEmotes.Replace(msg.Message, "");
                            Logger.Log(LogType.IrcChat,
                                        "{0} left {1} ({2})",
                                        msg.Nick,
                                        msg.Channel,
                                        quitMsg);
                        }
                        return;


                    case IRCMessageType.NickChange:
                        if (msg.Nick == ActualBotNick)
                        {
                            ActualBotNick = msg.Message;
                            nickTry = 0;
                            Logger.Log(LogType.IrcStatus,
                                        "Bot was renamed from {0} to {1}",
                                        msg.Nick,
                                        msg.Message);
                            AuthWithNickServ();
                        }
                        else
                        {
                            if (!ResponsibleForInputParsing) return;
                            Server.Message("&i(IRC) {0} is now known as {1}",
                                            msg.Nick,
                                            msg.Message);
                        }
                        return;


                    case IRCMessageType.ErrorMessage:
                    case IRCMessageType.Error:
                        bool die = false;
                        switch (msg.ReplyCode)
                        {
                            case IRCReplyCode.ErrorNicknameInUse:
                            case IRCReplyCode.ErrorNicknameCollision:
                                // Possibility 1: we tried to go for primary nick, but it's still taken
                                string currentName = msg.RawMessageArray[2];
                                string desiredName = msg.RawMessageArray[3];
                                if (currentName == ActualBotNick && desiredName == desiredBotNick)
                                {
                                    Logger.Log(LogType.IrcStatus,
                                                "Error: Desired nick \"{0}\" is still in use. Will retry shortly.",
                                                desiredBotNick);
                                    break;
                                }

                                // Possibility 2: We don't have any nick yet, the one we wanted is in use
                                string oldActualBotNick = ActualBotNick;
                                if (ActualBotNick.Length < maxNickLength)
                                {
                                    // append '_' to the end of desired nick, if we can
                                    ActualBotNick += "_";
                                }
                                else
                                {
                                    // if resulting nick is too long, add a number to the end instead
                                    nickTry++;
                                    if (desiredBotNick.Length + nickTry / 10 + 1 > maxNickLength)
                                    {
                                        ActualBotNick = desiredBotNick.Substring(0, maxNickLength - nickTry / 10 - 1) +
                                                        nickTry;
                                    }
                                    else
                                    {
                                        ActualBotNick = desiredBotNick + nickTry;
                                    }
                                }
                                Logger.Log(LogType.IrcStatus,
                                            "Error: Nickname \"{0}\" is already in use. Trying \"{1}\"",
                                            oldActualBotNick,
                                            ActualBotNick);
                                Send(IRCCommands.Nick(ActualBotNick));
                                Send(IRCCommands.Userhost(ActualBotNick));
                                break;

                            case IRCReplyCode.ErrorBannedFromChannel:
                            case IRCReplyCode.ErrorNoSuchChannel:
                                Logger.Log(LogType.IrcStatus,
                                            "Error: {0} ({1})",
                                            msg.ReplyCode,
                                            msg.Channel);
                                die = true;
                                break;

                            case IRCReplyCode.ErrorBadChannelKey:
                                Logger.Log(LogType.IrcStatus,
                                            "Error: Channel password required for {0}. " +
                                            "ProCraft does not currently support password-protected channels.",
                                            msg.Channel);
                                die = true;
                                break;

                            default:
                                Logger.Log(LogType.IrcStatus,
                                            "Error ({0}): {1}",
                                            msg.ReplyCode,
                                            msg.RawMessage);
                                break;
                        }

                        if (die)
                        {
                            Logger.Log(LogType.IrcStatus, "Error: Disconnecting.");
                            reconnect = false;
                            DisconnectThread(null);
                        }
                        return;


                    case IRCMessageType.QueryMessage:
                        // TODO: PMs
                        Logger.Log(LogType.IrcStatus, "QueryMessage: {0}", msg.RawMessage);
                        Server.Players.Where(p => p.IsStaff).Message("&i{0} -> {1}&f: {2}", msg.Nick, botNick, msg.Message);
                        break;

                    case IRCMessageType.Names:
                        Logger.Log(LogType.IrcStatus, "Name: {0}", msg.Message);
                        List<string> chanUsers;
                        if (!Users.TryGetValue(msg.Channel, out chanUsers)) {
                            chanUsers = new List<string>();
                            Users[msg.Channel] = chanUsers;
                        } else {
                            chanUsers.Clear();
                        }
                        foreach (string u in msg.Message.Split())                     
                            chanUsers.Add(u);
                        break;
                        
                    case IRCMessageType.Kill:
                        Logger.Log(LogType.IrcStatus,
                                    "Bot was killed from {0} by {1} ({2}), reconnecting.",
                                    hostName,
                                    msg.Nick,
                                    msg.Message);
                        reconnect = true;
                        isConnected = false;
                        return;

                    case IRCMessageType.Unknown:
                        if (msg.ReplyCode == IRCReplyCode.UserHost)
                        {
                            Match match = UserHostRegex.Match(msg.Message);
                            if (match.Success)
                            {
                                userHostLength = match.Groups[1].Length;
                            }
                        }
                        return;
                }
            }
            
            static string Formatter(Player p) {
                string value = p.Info.Rank.Color + p.Info.Name;
                if (p.World != null)
                    value += " &S[" + p.World.ClassyName + "&S]" + Reset;
                return value;
            }
            
            bool HandleIrcCommand(IRCMessage msg, string rawMessage) {
                if (!(rawMessage[0] == '!' || rawMessage.StartsWith(ActualBotNick, StringComparison.OrdinalIgnoreCase)))
                    return false;               
                string rawCmd = rawMessage.ToLower();
                string nick = ActualBotNick.ToLower();
                bool elapsed = DateTime.UtcNow.Subtract(lastIrcCommand).TotalSeconds > 5;
                
                if (rawCmd == "!players" || rawCmd == nick + " players") {
                    if (!elapsed) return true;
                    var visiblePlayers = Server.Players.Where(p => !p.Info.IsHidden)
                        .OrderBy(p => p, PlayerListSorter.Instance).ToArray();
                    
                    if (visiblePlayers.Any()) {
                        SendChannelMessage(Bold + "Players online: " + Reset +
                    	                   visiblePlayers.JoinToString(Formatter));
                        lastIrcCommand = DateTime.UtcNow;
                    } else {
                        SendChannelMessage(Bold + "There are no players online.");
                        lastIrcCommand = DateTime.UtcNow;
                    }
                    return true;
                } else if (rawCmd.StartsWith("!st") || rawCmd.StartsWith(nick + " st")) {
                    if (!elapsed) return true;
                    int messageStart = rawCmd[0] == '!' ? 4 : nick.Length + 4;
                    if (rawCmd.Length > messageStart) {
                        Chat.IRCSendStaff(msg.Nick, rawMessage.Remove(0, messageStart));
                        lastIrcCommand = DateTime.UtcNow;
                    }
                    return true;
                } else if (rawCmd.StartsWith("!seen") || rawCmd.StartsWith(nick + " seen")) {
                    if (!elapsed) return true;
                    int messageStart = rawCmd[0] == '!' ? 6 : nick.Length + 6;
                    if (rawCmd.Length > messageStart) {
                        string findPlayer = rawMessage.Remove(0, messageStart);
                        PlayerInfo info = PlayerDB.FindPlayerInfoExact(findPlayer);
                        if (info != null) {
                            Player target = info.PlayerObject;
                            if (target != null) {
                                SendChannelMessage("Player " + Bold + "{0}" + Reset + " has been " + 
                            	                   Bold + "&aOnline" + Reset + " for " + Bold + "{1}",
                                                   target.Info.Rank.Color + target.Name, target.Info.TimeSinceLastLogin.ToMiniNoColorString());
                                if (target.World != null) {
                                    SendChannelMessage("They are currently on world " + Bold + "{0}",
                                                       target.World.ClassyName);
                                }
                            } else {
                                SendChannelMessage("Player " + Bold + "{0}" + Reset + " is " + Bold + "&cOffline",
                                                   info.ClassyName);
                                SendChannelMessage(
                                    "They were last seen " + Bold + "{0}" + Reset + " ago on world " + Bold + "{1}",
                                    info.TimeSinceLastSeen.ToMiniNoColorString(),
                                    info.LastWorld);
                            }
                        } else {
                            SendChannelMessage("No player found with name \"" + Bold + findPlayer + Reset + "\"");
                        }
                    } else {
                        SendChannelMessage("Please specify a player name");
                    }
                    lastIrcCommand = DateTime.UtcNow;
                    return true;
                } else if (rawCmd.StartsWith("!bd") || rawCmd.StartsWith(nick + " bd")) {
                    if (!elapsed) return true;
                    int messageStart = rawCmd[0] == '!' ? 4 : nick.Length + 4;
                    if (rawCmd.Length > messageStart) {
                        string findPlayer = rawMessage.Remove(0, messageStart);
                        PlayerInfo info = PlayerDB.FindPlayerInfoExact(findPlayer);
                        if (info != null) {
                            SendChannelMessage("Player " + Bold + "{0}" + Reset + 
                        	                   " has Built: " + Bold + "{1}" + Reset +
                        	                   " blocks Deleted: " + Bold + "{2}" + Reset + " blocks{3}",
                                               info.ClassyName, Server.GetNumberString(info.BlocksBuilt), Server.GetNumberString(info.BlocksDeleted),
                                               (info.Can(Permission.Draw) ? " Drawn: " + Bold + Server.GetNumberString(info.BlocksDrawn) + Reset + " blocks." : ""));
                        } else {
                            SendChannelMessage("No player found with name \"" + Bold + findPlayer + Reset + "\"");
                        }
                    } else {
                        SendChannelMessage("Please specify a player name.");
                    }
                    lastIrcCommand = DateTime.UtcNow;
                    return true;
                } else if (rawCmd.StartsWith("!time") || rawCmd.StartsWith(nick + " time")) {
                    if (!elapsed) return true;
                    int messageStart = rawCmd[0] == '!' ? 6 : nick.Length + 6;
                    if (rawCmd.Length > messageStart) {
                        string findPlayer = rawMessage.Remove(0, messageStart);
                        PlayerInfo info = PlayerDB.FindPlayerInfoExact(findPlayer);
                        if (info != null && info.IsOnline) {
                            TimeSpan idle = info.PlayerObject.IdBotTime;
                            SendChannelMessage("Player " + Bold + "{0}" + Reset + " has spent a total of: " + Bold + "{1:F1}" + Reset +
                                               " hours (" + Bold + "{2:F1}" + Reset + " hours this session{3}",
                                               info.ClassyName,
                                               (info.TotalTime + info.TimeSinceLastLogin).TotalHours,
                                               info.TimeSinceLastLogin.TotalHours, 
                                               idle > TimeSpan.FromMinutes(1) ?  ", been idle for " + Bold + 
                                               string.Format("{0:F2}", idle.TotalMinutes) + Reset + " minutes)" : ")");
                        } else if (info != null) {
                            SendChannelMessage("Player " + Bold + "{0}" + Reset + " has spent a total of: " 
                        	                   + Bold + "{1:F1}" + Reset + " hours",
                                               info.ClassyName,
                                               info.TotalTime.TotalHours);
                        } else {
                            SendChannelMessage("No player found with name \"" + Bold + findPlayer + Reset + "\"");
                        }
                    } else {
                        SendChannelMessage("Please specify a player name.");
                    }
                    lastIrcCommand = DateTime.UtcNow;
                    return true;
                } else if (rawCmd.StartsWith("!clients") || rawCmd.StartsWith(nick + " clients")) {
                    if (!elapsed) return true;
                    
                    var visiblePlayers = Server.Players.Where(p => !p.Info.IsHidden)
                        .OrderBy(p => p, PlayerListSorter.Instance).ToArray();

                    Dictionary<string, List<Player>> clients = new Dictionary<string, List<Player>>();
                    foreach (var p in visiblePlayers) {
                        string appName = p.ClientName;
                        if (string.IsNullOrEmpty(appName))
                            appName = "(unknown)";

                        List<Player> usingClient;
                        if (!clients.TryGetValue(appName, out usingClient)) {
                            usingClient = new List<Player>();
                            clients[appName] = usingClient;
                        }
                        usingClient.Add(p);
                    }
                    SendChannelMessage(Bold + "Players using:");
                    foreach (var kvp in clients) {
                        SendChannelMessage("  " + Bold + "{0}" + Reset + ": {1}",
                                       kvp.Key, kvp.Value.JoinToClassyString());
                    }
                    return true;
                } else if (rawCmd == "!commands" || rawCmd == nick + " commands") {
                    if (!elapsed) return true;
                    SendChannelMessage(Bold + "List of commands: " + Reset + "BD, Commands, Clients, Players, Seen, St, Time");
                    lastIrcCommand = DateTime.UtcNow;
                    return true;
                }
                return rawCmd[0] == '!';
            }

            void AuthWithNickServ()
            {
                if (ConfigKey.IRCRegisteredNick.Enabled())
                {
                    Send(IRCCommands.Privmsg(ConfigKey.IRCNickServ.GetString(),
                                               ConfigKey.IRCNickServMessage.GetString()));
                }
            }


            public void DisconnectThread([CanBeNull] string quitMsg)
            {
                if (isConnected && quitMsg != null)
                {
                    ClearLocalQueue();
                    Send(IRCCommands.Quit(quitMsg));
                }
                else
                {
                    isConnected = false;
                }
                IsReady = false;
                AssignBotForInputParsing();
                if (thread != null && thread.IsAlive)
                {
                    thread.Join(1000);
                    if (thread.IsAlive)
                    {
                        thread.Abort();
                    }
                }
                try
                {
                    if (reader != null) reader.Close();
                }
                catch (ObjectDisposedException) { }
                try
                {
                    if (writer != null) writer.Close();
                }
                catch (ObjectDisposedException) { }
                try
                {
                    if (client != null) client.Close();
                }
                catch (ObjectDisposedException) { }
            }

            void ClearLocalQueue()
            {
                string ignored;
                while (localQueue.TryDequeue(out ignored)) { }
            }

            #region IDisposable members

            public void Dispose()
            {
                try
                {
                    if (reader != null) reader.Dispose();
                }
                catch (ObjectDisposedException) { }

                try
                {
                    if (reader != null) writer.Dispose();
                }
                catch (ObjectDisposedException) { }

                try
                {
                    if (client != null && client.Connected)
                    {
                        client.Close();
                    }
                }
                catch (ObjectDisposedException) { }
            }

            #endregion
        }


        /// <summary> Read/write timeout for IRC connections. Default is 15s. </summary>
        public static TimeSpan Timeout { get; set; }

        /// <summary> Delay between reconnect attempts,
        /// in case bot gets kicked or loses connection to IRC network. Default is 15s. </summary>
        public static TimeSpan ReconnectDelay { get; set; }

        /// <summary> Minimum delay between sending messages to IRC.
        /// Set by Config.ApplyConfig, based on value of IRCDelay config key. </summary>
        public static TimeSpan SendDelay { get; internal set; }

        /// <summary> Minimum delay between retrying for desired nick. </summary>
        public static TimeSpan NickRetryDelay { get; internal set; }

        static IRC()
        {
            Timeout = new TimeSpan(0, 0, 15);
            ReconnectDelay = new TimeSpan(0, 0, 15);
            NickRetryDelay = new TimeSpan(0, 0, 30);
        }

        static IrcThread[] threads;
        static string hostName;
        static int port;
        static string[] channelNames;
        static string botNick;

        static readonly ConcurrentQueue<string> OutputQueue = new ConcurrentQueue<string>();


        static void AssignBotForInputParsing()
        {
            bool needReassignment = false;
            for (int i = 0; i < threads.Length; i++)
            {
                if (threads[i].ResponsibleForInputParsing && !threads[i].IsReady)
                {
                    threads[i].ResponsibleForInputParsing = false;
                    needReassignment = true;
                }
            }
            if (needReassignment)
            {
                for (int i = 0; i < threads.Length; i++)
                {
                    if (threads[i].IsReady)
                    {
                        threads[i].ResponsibleForInputParsing = true;
                        Logger.Log(LogType.IrcStatus,
                                    "Bot \"{0}\" is now responsible for parsing input.",
                                    threads[i].ActualBotNick);
                        return;
                    }
                }
                Logger.Log(LogType.IrcStatus, "All IRC bots have disconnected.");
            }
        }

        // includes IRC color codes and non-printable ASCII
        public static readonly Regex NonPrintableChars = new Regex( "\x03\\d{1,2}(,\\d{1,2})?|[\x00-\x1F\x7E-\xFF]", RegexOptions.Compiled );


        public static void Init()
        {
            if (!ConfigKey.IRCBotEnabled.Enabled()) return;

            hostName = ConfigKey.IRCBotNetwork.GetString();
            port = ConfigKey.IRCBotPort.GetInt();
            channelNames = ConfigKey.IRCBotChannels.GetString().Split(',');
            for (int i = 0; i < channelNames.Length; i++)
            {
                channelNames[i] = channelNames[i].Trim();
                if (!channelNames[i].StartsWith("#"))
                {
                    channelNames[i] = '#' + channelNames[i].Trim();
                }
            }
            botNick = ConfigKey.IRCBotNick.GetString();
        }


        public static bool Start()
        {
            if (!IrcNickRegex.IsMatch(botNick))
            {
                Logger.Log(LogType.Error, "IRC: Unacceptable bot nick.");
                return false;
            }

            int threadCount = ConfigKey.IRCThreads.GetInt();

            if (threadCount == 1)
            {
                IrcThread thread = new IrcThread(botNick);
                if (thread.Start(true))
                {
                    threads = new[] {
                        thread
                    };
                }
            }
            else
            {
                List<IrcThread> threadTemp = new List<IrcThread>();
                for (int i = 0; i < threadCount; i++)
                {
                    IrcThread temp = new IrcThread(botNick + (i + 1));
                    if (temp.Start((threadTemp.Count == 0)))
                    {
                        threadTemp.Add(temp);
                    }
                }
                threads = threadTemp.ToArray();
            }

            if (threads.Length > 0)
            {
                HookUpHandlers();
                return true;
            }
            else
            {
                Logger.Log(LogType.IrcStatus, "IRC: Set IRCThreads to 1.");
                return false;
            }
        }


        public static void SendChannelMessage([NotNull] string line, [NotNull] params object[] args)
        {
            if (line == null) throw new ArgumentNullException("line");
            if (args == null) throw new ArgumentNullException("args");
            if (args.Length > 0)
            {
                line = String.Format(line, args);
            }
            if (channelNames == null) return; // in case IRC bot is disabled.
            line = ProcessMessageToIRC(line);
            for (int i = 0; i < channelNames.Length; i++)
            {
                SendRawMessage(IRCCommands.Privmsg(channelNames[i], ""), line, "");
            }
        }


        public static void SendAction([NotNull] string line)
        {
            if (line == null) throw new ArgumentNullException("line");
            if (channelNames == null) return; // in case IRC bot is disabled.
            line = ProcessMessageToIRC(line);
            for (int i = 0; i < channelNames.Length; i++)
            {
                SendRawMessage(IRCCommands.Privmsg(channelNames[i], "\u0001ACTION "), line, "\u0001");
            }
        }


        static string[] split = new string[] { "&N", "&n", "\n" };
        public static void SendRawMessage(string prefix, [NotNull] string line, string suffix)
        {
            if (line == null) throw new ArgumentNullException("line");
            // handle newlines
            if (line.Contains("&N") || line.Contains("&n") || line.Contains("\n"))
            {
                string[] segments = line.Split(split, StringSplitOptions.RemoveEmptyEntries);
                SendRawMessage(prefix, segments[0], suffix);
                for (int i = 1; i < segments.Length; i++)
                {
                    SendRawMessage(prefix, "> " + segments[i], suffix);
                }
                return;
            }

            // handle line wrapping
            int maxContentLength = MaxMessageSize - prefix.Length - suffix.Length - userHostLength - 3 - maxNickLength;
            if (line.Length > maxContentLength)
            {
                SendRawMessage(prefix, line.Substring(0, maxContentLength), suffix);
                int offset = maxContentLength;
                while (offset < line.Length)
                {
                    int length = Math.Min(line.Length - offset, maxContentLength - 2);
                    SendRawMessage(prefix, "> " + line.Substring(offset, length), suffix);
                    offset += length;
                }
                return;
            }

            // actually send
            OutputQueue.Enqueue(prefix + line + suffix);
        }


        static bool IsBotNick([NotNull] string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            return threads.Any(t => t.ActualBotNick == str);
        }


        internal static void Disconnect([CanBeNull] string quitMsg)
        {
            if (threads != null && threads.Length > 0)
            {
                foreach (IrcThread thread in threads)
                {
                    thread.DisconnectThread(quitMsg);
                }
            }
        }


        // includes IRC color codes and non-printable ASCII
        static readonly Regex
            IRCColorsAndNonStandardChars = new Regex("\x03\\d{1,2}(,\\d{1,2})?|[^\x0A\x20-\x7E]"),
            IRCColorsAndNonStandardCharsExceptEmotes =
                new Regex("\x03\\d{1,2}(,\\d{1,2})?|[^\x0A\x20-\x7F☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼⌂]");

        [NotNull]
        static string ProcessMessageFromIRC([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            bool useColor = ConfigKey.IRCShowColorsFromIRC.Enabled();
            bool useEmotes = ConfigKey.IRCShowEmotesFromIRC.Enabled();

            if (useColor && useEmotes)
            {
                message = Color.IrcToMinecraftColors(message);
                message = Chat.ReplaceUnicodeWithEmotes(message);
                message = Chat.ReplaceEmoteKeywords(message);
                message = Chat.ReplacePercentColorCodes(message, false);
                message = Chat.StripNewlines(message);
            }
            else if (useColor)
            {
                message = Color.IrcToMinecraftColors(message);
                message = Chat.StripEmotes(message);
                message = Chat.ReplacePercentColorCodes(message, false);
                message = Chat.StripNewlines(message);
            }
            else if (useEmotes)
            {
                message = IRCColorsAndNonStandardCharsExceptEmotes.Replace(message, "");
                message = Chat.ReplaceUnicodeWithEmotes(message);
                message = Chat.ReplaceEmoteKeywords(message);
                // strips minecraft colors and newlines
                message = Color.StripColors(message);
            }
            else
            {
                // strips emotes
                message = IRCColorsAndNonStandardChars.Replace(message, "");
                // strips minecraft colors and newlines
                message = Color.StripColors(message);
            }

            message = Chat.UnescapeBackslashes(message);
            return message.Trim();
        }


        [NotNull]
        static string ProcessMessageToIRC([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            bool useColor = ConfigKey.IRCShowColorsFromServer.Enabled();
            bool useEmotes = ConfigKey.IRCShowEmotesFromServer.Enabled();

            if (useEmotes)
                message = Chat.ReplaceEmotesWithUnicode(message);
            else
                message = Chat.StripEmotes(message);

            message = Chat.ReplaceNewlines(message);

            if (useColor) {
				message = message.Replace("&t", ResetReplacement);
				message = message.Replace("&T", ResetReplacement);
				message = Color.MinecraftToIrcColors(message);
                message = message.Replace(Bold, BoldReplacement);
                message = message.Replace(Reset, ResetReplacement);
            }
            else
            {
                message = message.Replace(Bold, "");
                message = message.Replace(Reset, "");
                message = Color.StripColors(message);
            }
            return message.Trim();
        }

        #region Server Event Handlers

        static void HookUpHandlers()
        {
            Chat.Sent += ChatSentHandler;
            Player.Ready += PlayerReadyHandler;
            Player.HideChanged += OnPlayerHideChanged;
            Player.Disconnected += PlayerDisconnectedHandler;
            Player.Kicked += PlayerKickedHandler;
            PlayerInfo.BanChanged += PlayerInfoBanChangedHandler;
            PlayerInfo.RankChanged += PlayerInfoRankChangedHandler;
        }


        static void OnPlayerHideChanged([CanBeNull] object sender, [NotNull] PlayerHideChangedEventArgs e)
        {
            if (!ConfigKey.IRCBotAnnounceServerJoins.Enabled() || e.Silent)
            {
                return;
            }
            if (e.IsNowHidden)
            {
                if (ConfigKey.IRCBotAnnounceServerJoins.Enabled())
                {
                    ShowPlayerDisconnectedMsg(e.Player, LeaveReason.ClientQuit);
                }
            }
            else
            {
                PlayerReadyHandler(null, new PlayerEventArgs(e.Player));
            }
        }


        private static void ChatSentHandler([CanBeNull] object sender, [NotNull] ChatSentEventArgs args) {
            bool enabled = ConfigKey.IRCBotForwardFromServer.Enabled();
            switch (args.MessageType) {
                case ChatMessageType.Global:
                    string ignoreIRC = "";
                    if (args.Player.Info.ReadIRC == false) {
                        if (args.Player != Player.Console) {
                            ignoreIRC = "&7[Ignoring IRC]";
                        }
                    }
                    if (enabled) {
                        string formattedMessage = String.Format("{0}{1}: {2}{3}", args.Player.ClassyName, Reset,
                            args.Message, ignoreIRC);
                        SendChannelMessage(formattedMessage);
                    } else if (args.Message.StartsWith("#")) {
                        string formattedMessage = String.Format("{0}{1}: {2}{3}", args.Player.ClassyName, Reset,
                            args.Message.Substring(1), ignoreIRC);
                        SendChannelMessage(formattedMessage);
                    }
                    break;

                case ChatMessageType.Me:
                    if (enabled) {
                        SendChannelMessage(Bold + "&M*" + args.Player.Name + " " + Reset + args.Message);
                    }
                    break;

                case ChatMessageType.Say:
                    if (enabled) {
                        SendChannelMessage(Bold + "&S[&YSay&S] " + Reset + args.Message);
                    }
                    break;
            }
        }


        static void PlayerReadyHandler([CanBeNull] object sender, [NotNull] IPlayerEvent e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (ConfigKey.IRCBotAnnounceServerJoins.Enabled() && !e.Player.Info.IsHidden)
            {
                string message = string.Format("{0}&2+(&f{1}&2) Connected{2}.", Bold, e.Player.ClassyName, e.Player.Info.TimesVisited == 1 ? " for their first time" : "");
                SendChannelMessage(message);
            }
        }


        static void PlayerDisconnectedHandler([CanBeNull] object sender, [NotNull] PlayerDisconnectedEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (e.Player.HasFullyConnected && ConfigKey.IRCBotAnnounceServerJoins.Enabled() && !e.Player.Info.IsHidden)
            {
                ShowPlayerDisconnectedMsg(e.Player, e.LeaveReason);
            }
        }


        static void PlayerKickedHandler([CanBeNull] object sender, [NotNull] PlayerKickedEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (e.Announce && e.Context == LeaveReason.Kick)
            {
                PlayerSomethingMessage(e.Kicker, "kicked", e.Player.Info, e.Reason);
            }
        }


        static void PlayerInfoBanChangedHandler([CanBeNull] object sender, [NotNull] PlayerInfoBanChangedEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (e.Announce)
            {
                if (e.WasUnbanned)
                {
                    PlayerSomethingMessage(e.Banner, "unbanned", e.PlayerInfo, e.Reason);
                }
                else
                {
                    PlayerSomethingMessage(e.Banner, "banned", e.PlayerInfo, e.Reason);
                }
            }
        }


        static void PlayerInfoRankChangedHandler([CanBeNull] object sender, [NotNull] PlayerInfoRankChangedEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (e.Announce)
            {
                string actionString = String.Format("{0} from {1}&W to {2}&W",
                                                     e.RankChangeType,
                                                     e.OldRank.ClassyName,
                                                     e.NewRank.ClassyName);
                PlayerSomethingMessage(e.RankChanger, actionString, e.PlayerInfo, e.Reason);
            }
        }


        static void ShowPlayerDisconnectedMsg([NotNull] Player player, LeaveReason leaveReason)
        {
            if (player == null) throw new ArgumentNullException("player");
            string message = string.Format("{0}{1} &c({2})",
                                            Bold,
                                            Server.MakePlayerDisconnectedMessage(player),
                                            (player.usedquit ? player.quitmessage : leaveReason.ToString()));
            SendChannelMessage(message);
        }


        static void PlayerSomethingMessage([NotNull] IClassy player, [NotNull] string action, [NotNull] IClassy target,
                                           [CanBeNull] string reason)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (action == null) throw new ArgumentNullException("action");
            if (target == null) throw new ArgumentNullException("target");
            if (!ConfigKey.IRCBotAnnounceServerEvents.Enabled()) return;
            string message = String.Format("{0}&WPlayer {1}&W was {2} by {3}&W",
                                            Bold,
                                            target.ClassyName,
                                            action,
                                            player.ClassyName);
            if (!reason.NullOrWhiteSpace())
            {
                message += " Reason: " + reason;
            }
            SendChannelMessage(message);
        }

        #endregion

        #region Parsing

        static readonly IRCReplyCode[] ReplyCodes = (IRCReplyCode[])Enum.GetValues(typeof(IRCReplyCode));


        static IRCMessageType GetMessageType([NotNull] string rawLine, [NotNull] string actualBotNick)
        {
            if (rawLine == null) throw new ArgumentNullException("rawLine");
            if (actualBotNick == null) throw new ArgumentNullException("actualBotNick");

            Match found = ReplyCodeRegex.Match(rawLine);
            if (found.Success)
            {
                string code = found.Groups[1].Value;
                IRCReplyCode replyCode = (IRCReplyCode)int.Parse(code);

                // check if this replyCode is known in the RFC
                if (Array.IndexOf(ReplyCodes, replyCode) == -1)
                {
                    return IRCMessageType.Unknown;
                }

                switch (replyCode)
                {
                    case IRCReplyCode.Welcome:
                    case IRCReplyCode.YourHost:
                    case IRCReplyCode.Created:
                    case IRCReplyCode.MyInfo:
                    case IRCReplyCode.Bounce:
                        return IRCMessageType.Login;
                    case IRCReplyCode.StatsConn:
                    case IRCReplyCode.LocalUsers:
                    case IRCReplyCode.GlobalUsers:
                    case IRCReplyCode.LuserClient:
                    case IRCReplyCode.LuserOp:
                    case IRCReplyCode.LuserUnknown:
                    case IRCReplyCode.LuserMe:
                    case IRCReplyCode.LuserChannels:
                        return IRCMessageType.Info;
                    case IRCReplyCode.MotdStart:
                    case IRCReplyCode.Motd:
                    case IRCReplyCode.EndOfMotd:
                        return IRCMessageType.Motd;
                    case IRCReplyCode.NamesReply:
                        return IRCMessageType.Names;
                    case IRCReplyCode.EndOfNames:
                        return IRCMessageType.EndOfNames;
                    case IRCReplyCode.WhoReply:
                    case IRCReplyCode.EndOfWho:
                        return IRCMessageType.Who;
                    case IRCReplyCode.ListStart:
                    case IRCReplyCode.List:
                    case IRCReplyCode.ListEnd:
                        return IRCMessageType.List;
                    case IRCReplyCode.BanList:
                    case IRCReplyCode.EndOfBanList:
                        return IRCMessageType.BanList;
                    case IRCReplyCode.Topic:
                    case IRCReplyCode.TopicSetBy:
                    case IRCReplyCode.NoTopic:
                        return IRCMessageType.Topic;
                    case IRCReplyCode.WhoIsUser:
                    case IRCReplyCode.WhoIsServer:
                    case IRCReplyCode.WhoIsOperator:
                    case IRCReplyCode.WhoIsIdle:
                    case IRCReplyCode.WhoIsChannels:
                    case IRCReplyCode.EndOfWhoIs:
                        return IRCMessageType.WhoIs;
                    case IRCReplyCode.WhoWasUser:
                    case IRCReplyCode.EndOfWhoWas:
                        return IRCMessageType.WhoWas;
                    case IRCReplyCode.UserModeIs:
                        return IRCMessageType.UserMode;
                    case IRCReplyCode.ChannelModeIs:
                        return IRCMessageType.ChannelMode;
                    default:
                        if (((int)replyCode >= 400) &&
                            ((int)replyCode <= 599))
                        {
                            return IRCMessageType.ErrorMessage;
                        }
                        else
                        {
                            return IRCMessageType.Unknown;
                        }
                }
            }

            found = PingRegex.Match(rawLine);
            if (found.Success)
            {
                return IRCMessageType.Ping;
            }

            found = ErrorRegex.Match(rawLine);
            if (found.Success)
            {
                return IRCMessageType.Error;
            }

            found = ActionRegex.Match(rawLine);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return IRCMessageType.ChannelAction;
                    default:
                        return IRCMessageType.QueryAction;
                }
            }

            found = CtcpRequestRegex.Match(rawLine);
            if (found.Success)
            {
                return IRCMessageType.CtcpRequest;
            }

            found = MessageRegex.Match(rawLine);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return IRCMessageType.ChannelMessage;
                    default:
                        return IRCMessageType.QueryMessage;
                }
            }

            found = CtcpReplyRegex.Match(rawLine);
            if (found.Success)
            {
                return IRCMessageType.CtcpReply;
            }

            found = NoticeRegex.Match(rawLine);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return IRCMessageType.ChannelNotice;
                    default:
                        return IRCMessageType.QueryNotice;
                }
            }

            found = InviteRegex.Match(rawLine);
            if (found.Success)
            {
                return IRCMessageType.Invite;
            }

            found = JoinRegex.Match(rawLine);
            if (found.Success)
            {
                return IRCMessageType.Join;
            }

            found = TopicRegex.Match(rawLine);
            if (found.Success)
            {
                return IRCMessageType.TopicChange;
            }

            found = NickRegex.Match(rawLine);
            if (found.Success)
            {
                return IRCMessageType.NickChange;
            }

            found = KickRegex.Match(rawLine);
            if (found.Success)
            {
                return IRCMessageType.Kick;
            }

            found = PartRegex.Match(rawLine);
            if (found.Success)
            {
                return IRCMessageType.Part;
            }

            found = ModeRegex.Match(rawLine);
            if (found.Success)
            {
                if (found.Groups[1].Value == actualBotNick)
                {
                    return IRCMessageType.UserModeChange;
                }
                else
                {
                    return IRCMessageType.ChannelModeChange;
                }
            }

            found = QuitRegex.Match(rawLine);
            if (found.Success)
            {
                return IRCMessageType.Quit;
            }

            found = KillRegex.Match(rawLine);
            return found.Success ? IRCMessageType.Kill : IRCMessageType.Unknown;
        }


        [NotNull]
        public static IRCMessage MessageParser([NotNull] string rawLine, [NotNull] string actualBotNick)
        {
            if (rawLine == null) throw new ArgumentNullException("rawLine");
            if (actualBotNick == null) throw new ArgumentNullException("actualBotNick");

            string line;
            string nick = null;
            string ident = null;
            string host = null;
            string channel = null;
            string message = null;
            IRCReplyCode replyCode;

            if (rawLine[0] == ':')
            {
                line = rawLine.Substring(1);
            }
            else
            {
                line = rawLine;
            }

            string[] linear = line.Split(new[] {
                ' '
            });

            // conform to RFC 2812
            string from = linear[0];
            string messageCode = linear[1];
            int exclamationPos = from.IndexOf('!');
            int atPos = from.IndexOf('@');
            int colonPos = line.IndexOfOrdinal(" :");
            if (colonPos != -1)
            {
                // we want the exact position of ":" not beginning from the space
                colonPos += 1;
            }
            if (exclamationPos != -1)
            {
                nick = from.Substring(0, exclamationPos);
            }
            if ((atPos != -1) &&
                (exclamationPos != -1))
            {
                ident = from.Substring(exclamationPos + 1, (atPos - exclamationPos) - 1);
            }
            if (atPos != -1)
            {
                host = from.Substring(atPos + 1);
            }

            int messageCodeInt;
            if (Int32.TryParse(messageCode, out messageCodeInt))
            {
                replyCode = (IRCReplyCode)messageCodeInt;
            }
            else
            {
                replyCode = IRCReplyCode.Null;
            }
            IRCMessageType type = GetMessageType(rawLine, actualBotNick);
            if (colonPos != -1)
            {
                message = line.Substring(colonPos + 1);
            }

            switch (type)
            {
                case IRCMessageType.Join:
                case IRCMessageType.Kick:
                case IRCMessageType.Part:
                case IRCMessageType.TopicChange:
                case IRCMessageType.ChannelModeChange:
                case IRCMessageType.ChannelMessage:
                case IRCMessageType.ChannelAction:
                case IRCMessageType.ChannelNotice:
                    channel = linear[2];
                    break;
                case IRCMessageType.Who:
                case IRCMessageType.Topic:
                case IRCMessageType.Invite:
                case IRCMessageType.BanList:
                case IRCMessageType.ChannelMode:
                    channel = linear[3];
                    break;
                case IRCMessageType.Names:
                case IRCMessageType.EndOfNames:
                    channel = linear[4];
                    break;
            }

            if ((channel != null) &&
                (channel[0] == ':'))
            {
                channel = channel.Substring(1);
            }

            return new IRCMessage(from, nick, ident, host, channel, message, rawLine, type, replyCode);
        }


        static readonly Regex ReplyCodeRegex = new Regex("^:[^ ]+? ([0-9]{3}) .+$", RegexOptions.Compiled);
        static readonly Regex PingRegex = new Regex("^PING :.*", RegexOptions.Compiled);
        static readonly Regex ErrorRegex = new Regex("^ERROR :.*", RegexOptions.Compiled);

        static readonly Regex ActionRegex = new Regex("^:.*? PRIVMSG (.).* :" + "\x1" + "ACTION .*" + "\x1" + "$",
                                                       RegexOptions.Compiled);

        static readonly Regex CtcpRequestRegex = new Regex("^:.*? PRIVMSG .* :" + "\x1" + ".*" + "\x1" + "$",
                                                            RegexOptions.Compiled);

        static readonly Regex MessageRegex = new Regex("^:.*? PRIVMSG (.).* :.*$", RegexOptions.Compiled);

        static readonly Regex CtcpReplyRegex = new Regex("^:.*? NOTICE .* :" + "\x1" + ".*" + "\x1" + "$",
                                                          RegexOptions.Compiled);

        static readonly Regex NoticeRegex = new Regex("^:.*? NOTICE (.).* :.*$", RegexOptions.Compiled);
        static readonly Regex InviteRegex = new Regex("^:.*? INVITE .* .*$", RegexOptions.Compiled);
        static readonly Regex JoinRegex = new Regex("^:.*? JOIN .*$", RegexOptions.Compiled);
        static readonly Regex TopicRegex = new Regex("^:.*? TOPIC .* :.*$", RegexOptions.Compiled);
        static readonly Regex NickRegex = new Regex("^:.*? NICK .*$", RegexOptions.Compiled);
        static readonly Regex KickRegex = new Regex("^:.*? KICK .* .*$", RegexOptions.Compiled);
        static readonly Regex PartRegex = new Regex("^:.*? PART .*$", RegexOptions.Compiled);
        static readonly Regex ModeRegex = new Regex("^:.*? MODE (.*) .*$", RegexOptions.Compiled);
        static readonly Regex QuitRegex = new Regex("^:.*? QUIT :.*$", RegexOptions.Compiled);
        static readonly Regex KillRegex = new Regex("^:.*? KILL (.*) :.*$", RegexOptions.Compiled);

        #endregion
    }


#pragma warning disable 1591
    // ReSharper disable UnusedMember.Global

    /// <summary> IRC protocol reply codes. </summary>
    public enum IRCReplyCode
    {
        Null = 000,
        Welcome = 001,
        YourHost = 002,
        Created = 003,
        MyInfo = 004,
        Bounce = 005,
        TraceLink = 200,
        TraceConnecting = 201,
        TraceHandshake = 202,
        TraceUnknown = 203,
        TraceOperator = 204,
        TraceUser = 205,
        TraceServer = 206,
        TraceService = 207,
        TraceNewType = 208,
        TraceClass = 209,
        TraceReconnect = 210,
        StatsLinkInfo = 211,
        StatsCommands = 212,
        EndOfStats = 219,
        UserModeIs = 221,
        ServiceList = 234,
        ServiceListEnd = 235,
        StatsUptime = 242,
        StatsOLine = 243,
        StatsConn = 250,
        LuserClient = 251,
        LuserOp = 252,
        LuserUnknown = 253,
        LuserChannels = 254,
        LuserMe = 255,
        AdminMe = 256,
        AdminLocation1 = 257,
        AdminLocation2 = 258,
        AdminEmail = 259,
        TraceLog = 261,
        TraceEnd = 262,
        TryAgain = 263,
        LocalUsers = 265,
        GlobalUsers = 266,
        Away = 301,
        UserHost = 302,
        IsOn = 303,
        UnAway = 305,
        NowAway = 306,
        WhoIsUser = 311,
        WhoIsServer = 312,
        WhoIsOperator = 313,
        WhoWasUser = 314,
        EndOfWho = 315,
        WhoIsIdle = 317,
        EndOfWhoIs = 318,
        WhoIsChannels = 319,
        ListStart = 321,
        List = 322,
        ListEnd = 323,
        ChannelModeIs = 324,
        UniqueOpIs = 325,
        NoTopic = 331,
        Topic = 332,
        TopicSetBy = 333,
        Inviting = 341,
        Summoning = 342,
        InviteList = 346,
        EndOfInviteList = 347,
        ExceptionList = 348,
        EndOfExceptionList = 349,
        Version = 351,
        WhoReply = 352,
        NamesReply = 353,
        Links = 364,
        EndOfLinks = 365,
        EndOfNames = 366,
        BanList = 367,
        EndOfBanList = 368,
        EndOfWhoWas = 369,
        Info = 371,
        Motd = 372,
        EndOfInfo = 374,
        MotdStart = 375,
        EndOfMotd = 376,
        YouAreOper = 381,
        Rehashing = 382,
        YouAreService = 383,
        Time = 391,
        UsersStart = 392,
        Users = 393,
        EndOfUsers = 394,
        NoUsers = 395,
        ErrorNoSuchNickname = 401,
        ErrorNoSuchServer = 402,
        ErrorNoSuchChannel = 403,
        ErrorCannotSendToChannel = 404,
        ErrorTooManyChannels = 405,
        ErrorWasNoSuchNickname = 406,
        ErrorTooManyTargets = 407,
        ErrorNoSuchService = 408,
        ErrorNoOrigin = 409,
        ErrorNoRecipient = 411,
        ErrorNoTextToSend = 412,
        ErrorNoTopLevel = 413,
        ErrorWildTopLevel = 414,
        ErrorBadMask = 415,
        ErrorUnknownCommand = 421,
        ErrorNoMotd = 422,
        ErrorNoAdminInfo = 423,
        ErrorFileError = 424,
        ErrorNoNicknameGiven = 431,
        ErrorErroneousNickname = 432,
        ErrorNicknameInUse = 433,
        ErrorNicknameCollision = 436,
        ErrorUnavailableResource = 437,
        ErrorUserNotInChannel = 441,
        ErrorNotOnChannel = 442,
        ErrorUserOnChannel = 443,
        ErrorNoLogin = 444,
        ErrorSummonDisabled = 445,
        ErrorUsersDisabled = 446,
        ErrorNotRegistered = 451,
        ErrorNeedMoreParams = 461,
        ErrorAlreadyRegistered = 462,
        ErrorNoPermissionForHost = 463,
        ErrorPasswordMismatch = 464,
        ErrorYouAreBannedCreep = 465,
        ErrorYouWillBeBanned = 466,
        ErrorKeySet = 467,
        ErrorChannelIsFull = 471,
        ErrorUnknownMode = 472,
        ErrorInviteOnlyChannel = 473,
        ErrorBannedFromChannel = 474,
        ErrorBadChannelKey = 475,
        ErrorBadChannelMask = 476,
        ErrorNoChannelModes = 477,
        ErrorBanListFull = 478,
        ErrorNoPrivileges = 481,
        ErrorChannelOpPrivilegesNeeded = 482,
        ErrorCannotKillServer = 483,
        ErrorRestricted = 484,
        ErrorUniqueOpPrivilegesNeeded = 485,
        ErrorNoOperHost = 491,
        ErrorUserModeUnknownFlag = 501,
        ErrorUsersDoNotMatch = 502
    }


    /// <summary> IRC message types. </summary>
    public enum IRCMessageType
    {
        Ping,
        Info,
        Login,
        Motd,
        List,
        Join,
        Kick,
        Part,
        Invite,
        Quit,
        Kill,
        Who,
        WhoIs,
        WhoWas,
        Names,
        EndOfNames,
        Topic,
        BanList,
        NickChange,
        TopicChange,
        UserMode,
        UserModeChange,
        ChannelMode,
        ChannelModeChange,
        ChannelMessage,
        ChannelAction,
        ChannelNotice,
        QueryMessage,
        QueryAction,
        QueryNotice,
        CtcpReply,
        CtcpRequest,
        Error,
        ErrorMessage,
        Unknown
    }
}

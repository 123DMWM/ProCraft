// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using fCraft.Events;
using JetBrains.Annotations;
using System.IO;
using Microsoft.SqlServer.Server;

namespace fCraft {
    /// <summary> Helper class for handling player-generated chat. </summary>
    public static class Chat
    {
        static readonly Regex RegexIPMatcher = new Regex(@"\d{1,3}(\.\d{1,3}){3}(:?(\d{0,5})?)");
        public static char newPlayerPrefix = '+';
        #region Filters/Reports
        public static List<Filter> Filters = new List<Filter>();
        public static List<Report> Reports = new List<Report>();

        /// <summary>
        /// Saves the Filter data to be used when restarting the server
        /// </summary>
        /// <param name="filter">Filter being saved</param>
        public static void SaveFilter(Filter filter) {
            try {
                String[] filterData = {
                    filter.Word, filter.Replacement
                };
                if (!Directory.Exists("./Filters")) {
                    Directory.CreateDirectory("./Filters");
                }
                File.WriteAllLines("./Filters/" + filter.Id + ".txt", filterData);
            } catch (Exception ex) {
                Player.Console.Message("Filter Saver Has Crashed: {0}", ex);
            }
        }
        /// <summary>
        /// Saves the report to be read by the owner with /reports
        /// </summary>
        /// <param name="report">Report being saved</param>
        public static void SaveReport(Report report) {
            try {
                String[] reportData = {report.Sender, report.Datesent.ToBinary().ToString(), report.Message
                };
                if (!Directory.Exists("./Reports")) {
                    Directory.CreateDirectory("./Reports");
                }
                File.WriteAllLines("./Reports/" + report.Id + "-" + report.Sender + ".txt", reportData);
            } catch (Exception ex) {
                Player.Console.Message("Report Saver Has Crashed: {0}", ex);
            }
        }

        #endregion
        /// <summary> Sends a global (white) chat. </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendGlobal([NotNull] Player player, [NotNull] string rawMessage) {
            if (player == null) throw new ArgumentNullException("player");
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");
            foreach (Filter Swear in Filters) {
                if (rawMessage.ToLower().Contains(Swear.Word.ToLower())) {
                    rawMessage = rawMessage.ReplaceString(Swear.Word, Swear.Replacement, StringComparison.InvariantCultureIgnoreCase);
                }
            }
            if (!player.IsStaff) {
                rawMessage = RegexIPMatcher.Replace(rawMessage, "<Redacted IP>");
            }
            if (rawMessage.Length >= 10 && player.Info.Rank.MaxCaps > 0) {
                int caps = 0;
                for (int i = 0; i < rawMessage.Length; i++) {
                    if (char.IsUpper(rawMessage[i])) {
                        caps++;
                    }
                }
                if (player.Info.Rank.MaxCaps == 1) {
                    if (caps > (rawMessage.Length / 2)) {
                        rawMessage = rawMessage.ToLower().UppercaseFirst();
                        player.Message("Max uppercase letters reached. Message set to lowercase");
                    }
                } else if (caps > player.Info.Rank.MaxCaps) {
                    rawMessage = rawMessage.ToLower().UppercaseFirst();
                    player.Message("Max uppercase letters reached. Message set to lowercase");
                }
            }
            if (player.Info.ChatRainbows) {
                rawMessage = Rainbow.Rainbowize(rawMessage);
            } else if (player.Info.ChatBWRainbows) {
                rawMessage = Rainbow.BWRainbowize(rawMessage);
            }

            var recipientList = Server.Players.NotIgnoring(player);

            string formattedMessage = string.Format("{0}&F: {1}",
                                                     player.ClassyName,
                                                     rawMessage);

            var e = new ChatSendingEventArgs(player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Global,
                                              recipientList);


            if (!SendInternal(e)) return false;
            rawMessage = Color.StripColors(rawMessage);

            checkBotResponses(player, rawMessage);

            Logger.Log(LogType.GlobalChat,
                        "(global){0}: {1}", player.Info.Rank.Color + player.Name + Color.White, rawMessage);
            return true;
        }

        static void checkBotResponses(Player player, string rawMessage) {
            if (player.Can(Permission.UseBot)) {
                if (rawMessage.StartsWith("Bot ", StringComparison.OrdinalIgnoreCase) && rawMessage.Length < 17) {
                    player.ParseMessage("/bot <CalledFromChat> " + rawMessage.Remove(0, 4), false);
                }
                double BotTime = player.TimeSinceLastServerMessage.TotalSeconds;
                if (LDistance(rawMessage.ToLower(), "how do i rank up?") <= 0.25
                    || LDistance(rawMessage.ToLower(), "how do we rank up") <= 0.25) {
                    if (BotTime > 5) {
                        Server.BotMessage("You rank up by building something nice, preferably big. Then ask a staff member for a review.");
                        player.LastServerMessageDate = DateTime.UtcNow;
                        player.Info.TimesUsedBot++;
                    }
                }
                if (LDistance(rawMessage.ToLower(), "who is the owner?") <= 0.25) {
                    if (BotTime > 5) {
                        PlayerInfo owner;
                        if (PlayerDB.FindPlayerInfo(ConfigKey.ServerOwner.GetString(), out owner) && owner != null) {
                            Server.BotMessage("The owner is {0}", RankManager.HighestRank.Color + owner.Name);
                        } else {
                            Server.BotMessage("The owner is {0}", RankManager.HighestRank.Color + ConfigKey.ServerOwner.GetString());
                        }
                        player.LastServerMessageDate = DateTime.UtcNow;
                        player.Info.TimesUsedBot++;
                    }
                }
                if (LDistance(rawMessage.ToLower(), "what is this server called?") <= 0.25
                    || LDistance(rawMessage.ToLower(), "what is the name of this server?") <= 0.25) {
                    if (BotTime > 5) {
                        Server.BotMessage("The server name is: " + ConfigKey.ServerName.GetString());
                        player.LastServerMessageDate = DateTime.UtcNow;
                        player.Info.TimesUsedBot++;
                    }
                }
                if (LDistance(rawMessage.ToLower(), "where can i build?") <= 0.25
                    || LDistance(rawMessage.ToLower(), "where do we build") <= 0.25) {
                    if (BotTime > 5) {
                        Server.BotMessage("You can build anywhere outside of spawn. Just not on another persons build unless they say you can. ");
                        player.LastServerMessageDate = DateTime.UtcNow;
                        player.Info.TimesUsedBot++;
                    }
                }
                if (LDistance(rawMessage.ToLower(), "what is my next rank?") <= 0.25 ||
                    LDistance(rawMessage.ToLower(), "what rank is after this one?") <= 0.25) {
                    Rank meh = player.Info.Rank.NextRankUp;
                    if (BotTime > 5 && player.Info.Rank != RankManager.HighestRank) {
                    tryagain:
                        if (meh.IsDonor) {
                            meh = meh.NextRankUp;
                            goto tryagain;
                        }
                        Server.BotMessage("Your next rank is: " + meh.ClassyName);
                        player.LastServerMessageDate = DateTime.UtcNow;
                        player.Info.TimesUsedBot++;
                    } else if (player.Info.Rank == RankManager.HighestRank) {
                        Server.BotMessage("You are already the highest rank.");
                        player.LastServerMessageDate = DateTime.UtcNow;
                        player.Info.TimesUsedBot++;
                    }
                }
            }
        }

        public static float LDistance( string s, string t ) {
            // degenerate cases
            if (s == t)
                return 0;
            if (s.Length == 0)
                return 1;
            if (t.Length == 0)
                return 1;

            // create two work vectors of integer distances
            int[] v0 = new int[t.Length + 1];
            int[] v1 = new int[t.Length + 1];

            // initialize v0 (the previous row of distances)
            // this row is A[0][i]: edit distance for an empty s
            // the distance is just the number of characters to delete from t
            for (int i = 0; i < v0.Length; i++)
                v0[i] = i;

            for (int i = 0; i < s.Length; i++) {
                // calculate v1 (current row distances) from the previous row v0

                // first element of v1 is A[i+1][0]
                //   edit distance is delete (i+1) chars from s to match empty t
                v1[0] = i + 1;

                // use formula to fill in the rest of the row
                for (int j = 0; j < t.Length; j++) {
                    var cost = (s[i] == t[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min( v1[j] + 1, v0[j + 1] + 1 );
                    v1[j + 1] = Math.Min( v1[j + 1], v0[j] + cost );
                }

                // copy v1 (current row) to v0 (previous row) for next iteration
                for (int j = 0; j < v0.Length; j++)
                    v0[j] = v1[j];
            }
            float percent = (((float)v1[t.Length]) / ((float)(s.Length + t.Length) / 2));
            if (percent < 0)
                percent = 0;
            if (percent > 1)
                percent = 1;
            return percent;
        }

        /// <summary> Sends an action message (/Me). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendMe([NotNull] Player player, [NotNull] string rawMessage)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");

            var recepientList = Server.Players.NotIgnoring(player);

            string formattedMessage = String.Format("&M*{0} {1}",
                                                     player.Name,
                                                     rawMessage);

            var e = new ChatSendingEventArgs(player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Me,
                                              recepientList);

            if (!SendInternal(e)) return false;

            Logger.Log(LogType.GlobalChat,
                        "(me){0}: {1}", player.Name, rawMessage);
            return true;
        }


        /// <summary> Sends a private message (PM). Does NOT send a copy of the message to the sender. </summary>
        /// <param name="from"> Sender player. </param>
        /// <param name="to"> Recepient player. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendPM([NotNull] Player from, [NotNull] Player to, [NotNull] string rawMessage)
        {
            if (from == null) throw new ArgumentNullException("from");
            if (to == null) throw new ArgumentNullException("to");
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");
            var recepientList = new[] { to };

            string formattedMessage = String.Format("&Pfrom {0}: {1}",
                                                     from.Name, rawMessage);

            var e = new ChatSendingEventArgs(from,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.PM,
                                              recepientList);

            if (!SendInternal(e)) return false;

            Logger.Log(LogType.PrivateChat,
                        "{0} to {1}: {2}",
                        from.Name, to.Name, rawMessage);
            return true;
        }

        /// <summary> Sends a private message (PM). Does NOT send a copy of the message to the sender. </summary>
        /// <param name="from"> Sender player. </param>
        /// <param name="to"> Recepient player. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool IRCSendPM([NotNull] string from, [NotNull] Player to, [NotNull] string rawMessage)
        {
            if (from == null) throw new ArgumentNullException("from");
            if (to == null) throw new ArgumentNullException("to");
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");
            var recepientList = new[] { to };

            string formattedMessage = String.Format("&Ifrom (IRC){0}: &P{1}",
                                                     from, rawMessage);

            var e = new ChatSendingEventArgs(Player.Console,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.PM,
                                              recepientList);

            if (!SendInternal(e)) return false;

            Logger.Log(LogType.PrivateChat,
                        "{0} to {1}: {2}",
                        from, to.Name, rawMessage);
            return true;
        }


        /// <summary> Sends a rank-wide message (@@Rank message). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rank"> Target rank. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendRank([NotNull] Player player, [NotNull] Rank rank, [NotNull] string rawMessage)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (rank == null) throw new ArgumentNullException("rank");
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");

            var recepientList = rank.Players.NotIgnoring(player).Union(player);

            string formattedMessage = String.Format("&P({0}&P){1}: {2}",
                                                     rank.ClassyName,
                                                     player.Name,
                                                     rawMessage);

            var e = new ChatSendingEventArgs(player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Rank,
                                              recepientList);

            if (!SendInternal(e)) return false;

            Logger.Log(LogType.RankChat,
                        "(rank {0}){1}: {2}",
                        rank.Name, player.Name, rawMessage);
            return true;
        }


        /// <summary> Sends a global announcement (/Say). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
		public static bool SendSay([NotNull] Player player, [NotNull] string rawMessage) {
			if (player == null)
				throw new ArgumentNullException("player");
			if (rawMessage == null)
				throw new ArgumentNullException("rawMessage");

			var recepientList = Server.Players.Where(p => !p.IsStaff);
			string formattedMessage = Color.Say + rawMessage;
			var e = new ChatSendingEventArgs(player, rawMessage, formattedMessage, ChatMessageType.Say, recepientList);
			if (!SendInternal(e))
				return false;

			var recepientListStaff = Server.Players.Can(Permission.ReadStaffChat);
			string formattedMessageStaff = "&s[&YSay&s][&f" + player.Name + "&s] &Y" + rawMessage;
			var es = new ChatSendingEventArgs(player, rawMessage, formattedMessageStaff, ChatMessageType.SayStaff, recepientListStaff);
			if (!SendInternal(es))
				return false;


			Logger.Log(LogType.GlobalChat,
						"(say){0}: {1}", player.Name, rawMessage);
			return true;
		}

		/// <summary> Sends a global announcement to staff (/StaffSay). </summary>
		/// <param name="player"> Player writing the message. </param>
		/// <param name="rawMessage"> Message text. </param>
		/// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
		public static bool SendStaffSay([NotNull] Player player, [NotNull] string rawMessage) {
			if (player == null)
				throw new ArgumentNullException("player");
			if (rawMessage == null)
				throw new ArgumentNullException("rawMessage");

			var recepientList = Server.Players.Where(p => p.Info.Rank != RankManager.HighestRank).Can(Permission.ReadStaffChat);
			string formattedMessage = Color.Say + rawMessage;
			var e = new ChatSendingEventArgs(player, rawMessage, formattedMessage, ChatMessageType.Staff, recepientList);
			if (!SendInternal(e))
				return false;

			var recepientListOwner = Server.Players.Where(p => p.Info.Rank == RankManager.HighestRank);
			string formattedMessageOwner = "&s[&yStaffSay&s][&f" + player.Name + "&s] &Y" + rawMessage;
			var eo = new ChatSendingEventArgs(player, rawMessage, formattedMessageOwner, ChatMessageType.StaffSayOwner, recepientListOwner);
			if (!SendInternal(eo))
				return false;

			Logger.Log(LogType.GlobalChat, "(staff_say){0}: {1}", player.Name, rawMessage);
			return true;
		}

        public static bool SendIRC([NotNull] string rawMessage, [NotNull] params object[] formatArgs)
        {
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");

            var recepientList = Server.Players;

            foreach (Player player in recepientList)
            {
                if (player.Info.ReadIRC)
                {
                    player.Message(rawMessage, formatArgs);
                }
            }

            return true;
        }

        /// <summary> Sends a staff message (/Staff). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendStaff([NotNull] Player player, [NotNull] string rawMessage)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");

            var recipientList = Server.Players.Can(Permission.ReadStaffChat)
                                      .NotIgnoring(player)
                                      .Union(player);

            string formattedMessage = String.Format("&P(staff){0}&P: {1}",
                                                     player.Name,
                                                     rawMessage);

            var e = new ChatSendingEventArgs(player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Staff,
                                              recipientList);

            if (!SendInternal(e)) return false;

            Logger.Log(LogType.GlobalChat,
                        "(staff){0}: {1}",
                        player.Name,
                        rawMessage);
            return true;
        }
        /// <summary> Sends a staff message (/IRCStaff). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendIRCStaff([NotNull] Player player, [NotNull] string rawMessage)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");

            var recipientList = Server.Players.Can(Permission.ReadStaffChat)
                                      .NotIgnoring(player)
                                      .Union(player);

            string formattedMessage = String.Format("&P(IRC+staff){0}&P: {1}",
                                                     player.Name,
                                                     rawMessage);

            var e = new ChatSendingEventArgs(player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Staff,
                                              recipientList);

            IRC.SendChannelMessage("\u212C(IRC+Staff)\u211C" + player.Name + ": " + rawMessage);

            if (!SendInternal(e)) return false;

            Logger.Log(LogType.GlobalChat,
                        "(IRC+staff){0}: {1}",
                        player.Name,
                        rawMessage);
            return true;
        }
        /// <summary> Sends a staff message from irc (!St). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool IRCSendStaff([NotNull] string player, [NotNull] string rawMessage)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");

            var recipientList = Server.Players.Can(Permission.ReadStaffChat).Where(p => p.Info.ReadIRC == true);

            string formattedMessage = String.Format("&P(IRC+staff)&5(IRC){0}&P: {1}",
                                                     player,
                                                     rawMessage);

            var e = new ChatSendingEventArgs( Player.Console,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Staff,
                                              recipientList);

            if (!SendInternal(e)) return false;
			IRC.SendChannelMessage("\u211C\u212C(IRC+Staff)(IRC)\u211C" + player + ": " + rawMessage);

            Logger.Log(LogType.GlobalChat,
                        "(IRC+staff)(IRC){0}: {1}",
                        player,
                        rawMessage);
            return true;
        }


        static bool SendInternal([NotNull] ChatSendingEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (RaiseSendingEvent(e)) return false;

            Player[] players = e.RecepientList.ToArray();
            int packets = players.Message(e.FormattedMessage);

            // Only increment the MessagesWritten count if someone other than
            // the player was on the recepient list.
            if (players.Length > 1 || (players.Length == 1 && players[0] != e.Player))
            {
                e.Player.Info.ProcessMessageWritten();
            }

			if (e.MessageType != ChatMessageType.SayStaff && e.MessageType != ChatMessageType.StaffSayOwner) {
				RaiseSentEvent(e, packets);
			}
            return true;
        }

        /// <summary> Replaces newline codes (&amp;n and &amp;N) with actual newlines (\n). </summary>
        /// <param name="message"> Message to process. </param>
        /// <returns> Processed message. </returns>
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        [NotNull, Pure]
        public static string ReplaceNewlines( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            message = message.Replace( "&n", "\n" );
            message = message.Replace( "&N", "\n" );
            return message;
        }

        static bool SendInternalIRC([NotNull] ChatSendingEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (RaiseSendingEvent(e)) return false;

            Player[] players = e.RecepientList.ToArray();
            int packets = players.Message(e.FormattedMessage);

            //RaiseSentEvent(e, packets);
            return true;
        }


        /// <summary> Checks for unprintable or illegal characters in a message. </summary>
        /// <param name="message"> Message to check. </param>
        /// <returns> True if message contains invalid chars. False if message is clean. </returns>
        public static bool ContainsInvalidChars(string message)
        {
            return message.Any(t => t < ' ' || t == '&' || t > 255);
        }


        /// <summary> Determines the type of player-supplies message based on its syntax. </summary>
        internal static RawMessageType GetRawMessageType(string message)
        {
            if (string.IsNullOrEmpty(message)) return RawMessageType.Invalid;
            if (message == "/") return RawMessageType.RepeatCommand;
            if (message.Equals("/ok", StringComparison.OrdinalIgnoreCase)) return RawMessageType.Command;
            if (message.EndsWith(" /")) return RawMessageType.PartialMessage;
            if (message.EndsWith(" //")) message = message.Substring(0, message.Length - 1);
			if (message.EndsWith(@" \")) return RawMessageType.PartialMessageNoSpace;
			if (message.EndsWith(@" /\")) message = message.Substring(0, message.Length  - 2) + @"\";
            if (message.EndsWith("λ")) return RawMessageType.LongerMessage;

            switch (message[0])
            {
                case '/':
                    if (message.Length < 2)
                    {
                        // message too short to be a command
                        return RawMessageType.Invalid;
                    }
                    if (message[1] == '/')
                    {
                        // escaped slash in the beginning: "//blah"
                        return RawMessageType.Chat;
                    }
                    if (message[1] != ' ')
                    {
                        // normal command: "/cmd"
                        return RawMessageType.Command;
                    }
                    return RawMessageType.Invalid;

                case '@':
                    if (message.Length < 4 || message.IndexOf(' ') == -1)
                    {
                        // message too short to be a PM or rank chat
                        return RawMessageType.Invalid;
                    }
                    if (message[1] == '@')
                    {
                        return RawMessageType.RankChat;
                    }
                    if (message[1] == '-' && message[2] == ' ')
                    {
                        // name shortcut: "@- blah"
                        return RawMessageType.PrivateChat;
                    }
                    if (message[1] == ' ' && message.IndexOf(' ', 2) != -1)
                    {
                        // alternative PM notation: "@ name blah"
                        return RawMessageType.PrivateChat;
                    }
                    if (message[1] != ' ')
                    {
                        // primary PM notation: "@name blah"
                        return RawMessageType.PrivateChat;
                    }
                    return RawMessageType.Invalid;
            }
            return RawMessageType.Chat;
        }

        /// <summary> Replaces keywords with appropriate values.
        /// See http://www.fcraft.net/wiki/Constants </summary>
        [NotNull]
        public static string ReplaceTextKeywords([NotNull] Player player, [NotNull] string input)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (input == null) throw new ArgumentNullException("input");
            StringBuilder sb = new StringBuilder(input);
            sb.Replace("{SERVER_NAME}", ConfigKey.ServerName.GetString());
            sb.Replace("{RANK}", player.Info.Rank.ClassyName);
			sb.Replace("{TIME}", DateTime.Now.ToShortTimeString()); // localized
            if (player.World == null)
            {
                sb.Replace("{WORLD}", "(No World)");
            }
            else
            {
                sb.Replace("{WORLD}", player.World.ClassyName);
            }
            sb.Replace("{WORLDS}", WorldManager.Worlds.Length.ToStringInvariant());
            sb.Replace("{MOTD}", ConfigKey.MOTD.GetString());
            sb.Replace("{VERSION}", "1.23");
            sb.Replace("{TIMESPLAYED}", player.Info.TimesVisited.ToString());
            sb.Replace("{TOTALTIME}", player.Info.TotalTime.ToMiniString());
            if (input.IndexOfOrdinal("{PLAYER") != -1)
            {
                Player[] playerList = Server.Players.CanBeSeen(player).Union(player).ToArray();
                sb.Replace("{PLAYER_NAME}", player.ClassyName);
                sb.Replace("{PLAYER_LIST}", playerList.JoinToClassyString());
				sb.Replace("{PLAYERS}", Server.CountPlayers(false).ToStringInvariant());
            }
            return sb.ToString();
        }


        #region Events

        static bool RaiseSendingEvent(ChatSendingEventArgs args)
        {
            var h = Sending;
            if (h == null) return false;
            h(null, args);
            return args.Cancel;
        }


        static void RaiseSentEvent(ChatSendingEventArgs args, int count)
        {
            var h = Sent;
            if (h != null) h(null, new ChatSentEventArgs(args.Player, args.Message, args.FormattedMessage,
                                                           args.MessageType, args.RecepientList, count));
        }


        /// <summary> Occurs when a chat message is about to be sent. Cancelable. </summary>
        public static event EventHandler<ChatSendingEventArgs> Sending;

        /// <summary> Occurs after a chat message has been sent. </summary>
        public static event EventHandler<ChatSentEventArgs> Sent;

        #endregion

        #region Emotes

        /// <summary> Conversion for code page 437 characters from index 0 to 31 to unicode. </summary>
		public const string ControlCharReplacements = "\0☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼";
		
		/// <summary> Conversion for code page 437 characters from index 127 to 255 to unicode. </summary>
		public const string ExtendedCharReplacements = "⌂ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»" +
			"░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌" +
			"█▄▌▐▀αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■\u00a0";
        static readonly char[] UnicodeReplacements = " ☺☻♥♦♣♠•◘○\n♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼".ToCharArray();

        /// <summary> List of chat keywords, and emotes that they stand for. </summary>
        public static readonly Dictionary<string, char> EmoteKeywords = new Dictionary<string, char> {
            {":)", '☺'}, {"smile", '☺'},

            {"smile2", '☻'},

            {"heart", '♥'},{"hearts", '♥'},
            {"<3", '♥'},

            {"diamond", '♦'}, {"diamonds", '♦'},
            {"rhombus", '♦'},

            {"club", '♣'},  {"clubs", '♣'},
            {"clover", '♣'}, {"shamrock", '♣'},

            {"spade", '♠'}, {"spades", '♠'},

            {"*", '•'}, {"bullet", '•'},
            {"dot", '•'}, {"point", '•'},

            {"hole", '◘'}, 

            {"circle", '○'}, {"o", '○'},

            {"male", '♂'}, {"mars", '♂'},

            {"female", '♀'}, {"venus", '♀'},

            {"8", '♪'}, {"note", '♪'},
            {"quaver", '♪'},

            {"notes", '♫'}, {"music", '♫'},

            {"sun", '☼'}, {"celestia", '☼'},

            {">>", '►'}, {"right2", '►'},

            {"<<", '◄'}, {"left2", '◄'},

            {"updown", '↕'}, {"^v", '↕'},

            {"!!", '‼'},

            {"p", '¶'}, {"para", '¶'},
            {"pilcrow", '¶'}, {"paragraph", '¶'},

            {"s", '§'}, {"sect", '§'},
            {"section", '§'},

            {"-", '▬'}, {"_", '▬'},
            {"bar", '▬'}, {"half", '▬'},

            {"updown2", '↨'}, {"^v_", '↨'},

            {"^", '↑'}, {"up", '↑'},

            {"v", '↓'}, {"down", '↓'},

            {">", '→'}, {"->", '→'},
            {"right", '→'},

            {"<", '←'}, {"<-", '←'},
            {"left", '←'},

            {"l", '∟'}, {"angle", '∟'},
            {"corner", '∟'},

            {"<>", '↔'}, {"<->", '↔'},
            {"leftright", '↔'},

            {"^^", '▲'}, {"up2", '▲'},

            {"vv", '▼'}, {"down2", '▼'},

            {"house", '⌂'},
            
            {"caret", '^'}, {"hat", '^'},

            {"tilde", '~'}, {"wave", '~'},

            {"grave", '`'}, {"'", '`'}
        };

        /// <summary> Removes newlines (\n) and newline codes (&amp;n and &amp;N). </summary>
        /// <param name="message"> Message to process. </param>
        /// <returns> Processed message. </returns>
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        [NotNull, Pure]
        public static string StripNewlines([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            message = message.Replace("\n", "");
            message = message.Replace("&n", "");
            message = message.Replace("&N", "");
            return message;
        }


        static readonly Regex EmoteSymbols = new Regex("[\x00-\x1F\x7F☺☻♥♦♣♠•◘○\n♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼⌂]");
        /// <summary> Strips all emote symbols (ASCII control characters and their UTF-8 equivalents).
        /// Does not strip emote keywords (e.g. {:)}). </summary>
        /// <param name="message"> Message to strip emotes from. </param>
        /// <returns> Message with its emotes stripped. </returns>
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        [NotNull, Pure]
        public static string StripEmotes([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            return EmoteSymbols.Replace(message, "");
        }


        /// <summary> Replaces emote keywords with actual emotes, using Chat.EmoteKeywords mapping. 
        /// Keywords are enclosed in curly braces, and are case-insensitive. </summary>
        /// <param name="message"> String to process. </param>
        /// <returns> Processed string. </returns>
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        [NotNull, Pure]
        public static string ReplaceEmoteKeywords([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            int startIndex = message.IndexOf('{');
            if (startIndex == -1)
            {
                return message; // break out early if there are no opening braces
            }

            StringBuilder output = new StringBuilder(message.Length);
            int lastAppendedIndex = 0;
            while (startIndex != -1)
            {
                int endIndex = message.IndexOf('}', startIndex + 1);
                if (endIndex == -1)
                {
                    break; // abort if there are no more closing braces
                }

                // see if emote was escaped (if odd number of backslashes precede it)
                bool escaped = false;
                for (int i = startIndex - 1; i >= 0 && message[i] == '\\'; i--)
                {
                    escaped = !escaped;
                }
                // extract the keyword
                string keyword = message.Substring(startIndex + 1, endIndex - startIndex - 1);
                char substitute;
                if (EmoteKeywords.TryGetValue(keyword.ToLowerInvariant(), out substitute))
                {
                    if (escaped)
                    {
                        // it was escaped; remove escaping character
                        startIndex++;
                        output.Append(message, lastAppendedIndex, startIndex - lastAppendedIndex - 2);
                        lastAppendedIndex = startIndex - 1;
                    }
                    else
                    {
                        // it was not escaped; insert substitute character
                        output.Append(message, lastAppendedIndex, startIndex - lastAppendedIndex);
                        output.Append(substitute);
                        startIndex = endIndex + 1;
                        lastAppendedIndex = startIndex;
                    }
                }
                else
                {
                    startIndex++; // unrecognized macro, keep going
                }
                startIndex = message.IndexOf('{', startIndex);
            }
            // append the leftovers
            output.Append(message, lastAppendedIndex, message.Length - lastAppendedIndex);
            return output.ToString();
        }


        /// <summary> Substitutes percent color codes (e.g. %C) with equivalent ampersand color codes (&amp;C).
        /// Also replaces newline codes (%N) with actual newlines (\n). </summary>
        /// <param name="message"> Message to process. </param>
        /// <param name="allowNewlines"> Whether newlines are allowed. </param>
        /// <returns> Processed string. </returns>
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        [NotNull, Pure]
        public static string ReplacePercentColorCodes([NotNull] string message, bool allowNewlines)
        {
            if (message == null) throw new ArgumentNullException("message");
            int startIndex = message.IndexOf('%');
            if (startIndex == -1)
                return message; // break out early if there are no percent marks

            StringBuilder output = new StringBuilder(message.Length);
            int lastAppendedIndex = 0;
            while (startIndex != -1 && startIndex < message.Length - 1)
            {
                // see if colorcode was escaped (if odd number of backslashes precede it)
                bool escaped = false;
                for (int i = startIndex - 1; i >= 0 && message[i] == '\\'; i--)
                {
                    escaped = !escaped;
                }
                // extract the colorcode
                char colorCode = message[startIndex + 1];
                if (Color.IsColorCode(colorCode) || allowNewlines && (colorCode == 'n' || colorCode == 'N'))
                {
                    if (escaped)
                    {
                        // it was escaped; remove escaping character
                        startIndex++;
                        output.Append(message, lastAppendedIndex, startIndex - lastAppendedIndex - 2);
                        lastAppendedIndex = startIndex - 1;
                    }
                    else
                    {
                        // it was not escaped; insert substitute character
                        output.Append(message, lastAppendedIndex, startIndex - lastAppendedIndex);
                        output.Append('&');
                        lastAppendedIndex = startIndex + 1;
                        startIndex += 2;
                    }
                }
                else
                {
                    startIndex++; // unrecognized colorcode, keep going
                }
                startIndex = message.IndexOf('%', startIndex);
            }
            // append the leftovers
            output.Append(message, lastAppendedIndex, message.Length - lastAppendedIndex);
            return output.ToString();
        }


        /// <summary> Unescapes backslashes. Any paid of backslashes (\\) is converted to a single one (\). </summary>
        /// <param name="message"> String to process. </param>
        /// <returns> Processed string. </returns>
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        [NotNull, Pure]
        public static string UnescapeBackslashes([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (message.IndexOf('\\') != -1)
            {
                return message.Replace(@"\\", @"\");
            }
            else
            {
                return message;
            }
        }


        /// <summary> Replaces UTF-8 symbol characters with ASCII control characters, matching Code Page 437.
        /// Opposite of ReplaceEmotesWithUnicode. </summary>
        /// <param name="input"> String to process. </param>
        /// <returns> Processed string, with its UTF-8 symbol characters replaced. </returns>
        /// <exception cref="ArgumentNullException"> input is null. </exception>
        [NotNull]
        public static string ReplaceUnicodeWithEmotes([NotNull] string input)
        {
            if (input == null) throw new ArgumentNullException("input");
            StringBuilder sb = new StringBuilder(input);
            for (int i = 1; i < UnicodeReplacements.Length; i++)
            {
                sb.Replace(UnicodeReplacements[i], (char)i);
            }
            sb.Replace('⌂', '\u007F');
            return sb.ToString();
        }


        /// <summary> Replaces ASCII control characters with UTF-8 symbol characters, matching Code Page 437. 
        /// Opposite of ReplaceUnicodeWithEmotes. </summary>
        /// <param name="input"> String to process. </param>
        /// <returns> Processed string, with its ASCII control characters replaced. </returns>
        /// <exception cref="ArgumentNullException"> input is null. </exception>
        [NotNull]
        public static string ReplaceEmotesWithUnicode([NotNull] string input)
        {
            if (input == null) throw new ArgumentNullException("input");
            StringBuilder sb = new StringBuilder(input);
            for (int i = 1; i < UnicodeReplacements.Length; i++)
            {
                sb.Replace((char)i, UnicodeReplacements[i]);
            }
            sb.Replace('\u007F', '⌂');
            return sb.ToString();
        }

        #endregion
    }


    /// <summary> Type of a broadcast chat message. </summary>
    public enum ChatMessageType {
        /// <summary> Unknown or custom chat message type. </summary>
        Other,

        /// <summary> Global (white) chat message. </summary>
        Global,

        /// <summary> Message directed to or from IRC. </summary>
        IRC,

        /// <summary> Message produced by /Me command (action). </summary>
        Me,

        /// <summary> Private message (@Player message). </summary>
        PM,

        /// <summary> Rank-wide message (@@Rank message). </summary>
        Rank,

        /// <summary> Message produced by /Say command (global announcement). </summary>
        Say,

        /// <summary> Message produced by /Staff command. </summary>
        Staff,

        /// <summary> Local (world) chat message. </summary>
		World,

		/// <summary> Message produced by /say command shown to staff members. </summary>
		SayStaff,

		/// <summary> Message produced by /staffsay command shown to highest rank. </summary>
		StaffSayOwner
    }


    /// <summary> Type of message sent by the player. Determined by CommandManager.GetMessageType() </summary>
    public enum RawMessageType {
        /// <summary> Unparseable chat syntax (rare). </summary>
        Invalid,

        /// <summary> Normal (white) chat. </summary>
        Chat,

        /// <summary> Command. </summary>
        Command,

        /// <summary> Confirmation (/ok) for a previous command. </summary>
        Confirmation,

        /// <summary> Partial message (ends with " /"). </summary>
        PartialMessage,

        /// <summary> Private message. </summary>
        PrivateChat,

        /// <summary> Rank chat. </summary>
        RankChat,

        /// <summary> Repeat of the last command ("/"). </summary>
		RepeatCommand,

		/// <summary> Partial message (ends with " \"). </summary>
		PartialMessageNoSpace,

        /// <summary> LongerMessage CPE Support. </summary>
        LongerMessage,
    }
}


namespace fCraft.Events {
    /// <summary> Provides data for Chat.Sending event. Cancelable.
    /// FormattedMessage and recipientList properties may be changed. </summary>
    public sealed class ChatSendingEventArgs : EventArgs, IPlayerEvent, ICancelableEvent {
        internal ChatSendingEventArgs( Player player, string message, string formattedMessage,
                                       ChatMessageType messageType, IEnumerable<Player> recepientList ) {
            Player = player;
            Message = message;
            MessageType = messageType;
            RecepientList = recepientList;
            FormattedMessage = formattedMessage;
        }

        /// <summary> Player who is sending the message. </summary>
        public Player Player { get; private set; }

        /// <summary> Raw text of the message. </summary>
        public string Message { get; private set; }

        /// <summary> Formatted message, as it will appear to the recepients. </summary>
        public string FormattedMessage { get; set; }

        /// <summary> Type of the message that's being sent. </summary>
        public ChatMessageType MessageType { get; private set; }

        /// <summary> List of intended recepients. </summary>
        public readonly IEnumerable<Player> RecepientList;

        public bool Cancel { get; set; }
    }
        
    /// <summary> Provides data for Chat.Sent event. Immutable. </summary>
    public sealed class ChatSentEventArgs : EventArgs, IPlayerEvent {
        internal ChatSentEventArgs( Player player, string message, string formattedMessage,
                                    ChatMessageType messageType, IEnumerable<Player> recepientList, int packetCount ) {
            Player = player;
            Message = message;
            MessageType = messageType;
            RecepientList = recepientList;
            FormattedMessage = formattedMessage;
            PacketCount = packetCount;
        }

        /// <summary> Player who sent the message. </summary>
        public Player Player { get; private set; }

        /// <summary> Raw text of the message. </summary>
        public string Message { get; private set; }

        /// <summary> Formatted message, as it appeared to the recepients. </summary>
        public string FormattedMessage { get; private set; }

        /// <summary> Type of message that was sent. </summary>
        public ChatMessageType MessageType { get; private set; }

        /// <summary> List of players who received the message. </summary>
        public IEnumerable<Player> RecepientList { get; private set; }

        /// <summary> Number of message packets that were sent out. </summary>
        public int PacketCount { get; private set; }
    }
}
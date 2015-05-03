// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.ExceptionServices;
using fCraft;
using fCraft.Drawing;
using fCraft.Events;
using Microsoft.Win32;

namespace fCraft
{
    static class ChatCommands
    {

        public static void Init()
        {
            CommandManager.RegisterCommand(CdSay);
            CommandManager.RegisterCommand(CdStaff);
            CommandManager.RegisterCommand(CdStaffSay);
            CommandManager.RegisterCommand(CdReview);
            CommandManager.RegisterCommand(CdAFK);
            CommandManager.RegisterCommand(CdIgnore);
            CommandManager.RegisterCommand(CdUnignore);
            CommandManager.RegisterCommand(CdMe);
            CommandManager.RegisterCommand(CdRoll);
            CommandManager.RegisterCommand(CdDeafen);
            CommandManager.RegisterCommand(CdClear);
            CommandManager.RegisterCommand(CdTimer);
            CommandManager.RegisterCommand(CdSwear);
            CommandManager.RegisterCommand(CdSwears);
            CommandManager.RegisterCommand(CdIRC);
            CommandManager.RegisterCommand(CdQuit);
            CommandManager.RegisterCommand(CdReport);
            CommandManager.RegisterCommand(CdReports);
            CommandManager.RegisterCommand(CdRBChat);
            CommandManager.RegisterCommand(CdGreeting);
            CommandManager.RegisterCommand(CdIRCStaff);
            CommandManager.RegisterCommand(Cdchat);
            CommandManager.RegisterCommand(CdLdis);
            CommandManager.RegisterCommand(CdtextHotKey);
            CommandManager.RegisterCommand(Cdbrushes);
            CommandManager.RegisterCommand(CdIdea);
            CommandManager.RegisterCommand(CdAction);
            CommandManager.RegisterCommand(CdWarn);


            Player.Moved += new EventHandler<Events.PlayerMovedEventArgs>(Player_IsBack);
        }

        #region Say

        static readonly CommandDescriptor CdSay = new CommandDescriptor
        {
            Name = "Say",
            Aliases = new[] { "broadcast" },
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            NotRepeatable = true,
            DisableLogging = true,
            UsableByFrozenPlayers = true,
            Permissions = new[] { Permission.Chat, Permission.Say },
            Usage = "/Say Message",
            Help = "Shows a message in special color, without the player name prefix. " +
                   "Can be used for making announcements.",
            Handler = SayHandler
        };

        static void SayHandler(Player player, CommandReader cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.DetectChatSpam()) return;

            if (player.Can(Permission.Say))
            {
                string msg = cmd.NextAll().Trim();
                if (msg.Length > 0)
                {
                    Chat.SendSay(player, msg);
                }
                else
                {
                    CdSay.PrintUsage(player);
                }
            }
            else
            {
                player.MessageNoAccess(Permission.Say);
            }
        }

        #endregion
        #region Staff

        static readonly CommandDescriptor CdStaff = new CommandDescriptor
        {
            Name = "Staff",
            Aliases = new[] { "st" },
            Category = CommandCategory.Chat | CommandCategory.Moderation,
            Permissions = new[] { Permission.Chat },
            NotRepeatable = true,
            IsConsoleSafe = true,
            DisableLogging = true,
            UsableByFrozenPlayers = true,
            Usage = "/Staff Message",
            Help = "Broadcasts your message to all operators/moderators on the server at once.",
            Handler = StaffHandler
        };

        static void StaffHandler(Player player, CommandReader cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.DetectChatSpam()) return;

            string message = cmd.NextAll().Trim(' ');
            if (message.Length > 0)
            {
                Chat.SendStaff(player, message);
            }
        }

        #endregion
        #region IRCStaff

        static readonly CommandDescriptor CdIRCStaff = new CommandDescriptor
        {
            Name = "IRCStaff",
            Aliases = new[] { "ist" },
            Category = CommandCategory.Chat | CommandCategory.Moderation,
            Permissions = new[] { Permission.Chat },
            NotRepeatable = true,
            IsConsoleSafe = true,
            DisableLogging = true,
            UsableByFrozenPlayers = true,
            Usage = "/IRCStaff Message",
            Help = "Broadcasts your message to all operators/moderators on the server at once and also to everyone on IRC.",
            Handler = IRCStaffHandler
        };

        static void IRCStaffHandler(Player player, CommandReader cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.DetectChatSpam()) return;

            string message = cmd.NextAll().Trim(' ');
            if (message.Length > 0)
            {
                Chat.SendIRCStaff(player, message);
            }
        }

        #endregion
        #region StaffSay

        static readonly CommandDescriptor CdStaffSay = new CommandDescriptor
        {
            Name = "StaffSay",
            Aliases = new[] { "sts" },
            Category = CommandCategory.New | CommandCategory.Chat,
            IsConsoleSafe = true,
            NotRepeatable = true,
            DisableLogging = true,
            UsableByFrozenPlayers = true,
            Permissions = new[] { Permission.Chat, Permission.Say },
            Usage = "/StaffSay Message",
            Help = "Shows a message in special color, without the player name prefix, to staff only." +
                   "Can be used for making announcements to staff members.",
            Handler = StaffSayHandler
        };

        static void StaffSayHandler(Player player, CommandReader cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.DetectChatSpam()) return;

            if (player.Can(Permission.Say))
            {
                string msg = cmd.NextAll().Trim();
                if (msg.Length > 0)
                {
                    Chat.SendStaffSay(player, msg);
                }
                else
                {
                    CdStaffSay.PrintUsage(player);
                }
            }
            else
            {
                player.MessageNoAccess(Permission.Say);
            }
        }

        #endregion
        #region Review

        static readonly CommandDescriptor CdReview = new CommandDescriptor
        {
            Name = "Review",
            Aliases = new[] { "rvw" },
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            NotRepeatable = true,
            DisableLogging = true,
            UsableByFrozenPlayers = false,
            Usage = "/Review",
            Help = "Asks available Moderators for a review of your build.",
            Handler = ReviewHandler
        };

        static void ReviewHandler(Player player, CommandReader cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.DetectChatSpam()) return;
            var staff = Server.Players.Where(p => p.Info.Rank.Can(Permission.ReadStaffChat));
            if (staff != null && staff.Any()) {
                player.Message("&SYour review request has been sent to the Moderators. They will be with you shortly");
                Chat.SendStaffSay(player, "&SPlayer " + player.ClassyName + " &Srequests a building review.");
            } else {
                player.Message("&SThere are no staff on! Sorry!");
            }
        }

        #endregion
        #region chat

        static readonly CommandDescriptor Cdchat = new CommandDescriptor
        {
            Name = "chat",
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.ShutdownServer},
            NotRepeatable = true,
            DisableLogging = true,
            IsHidden = true,
            UsableByFrozenPlayers = false,
            Usage = "/chat [type] [message]",
            Help = "debug for message types",
            Handler = chatHandler
        };

        static void chatHandler(Player player, CommandReader cmd)
        {
            byte type;
            byte.TryParse(cmd.Next(), out type);
            string message = cmd.NextAll();
            player.Send(Packet.Message(type, message));
        }

        #endregion
        #region AFK

        public static void Player_IsBack(object sender, Events.PlayerMovedEventArgs e) {
            // We need to have block positions, so we divide by 32
            Vector3I oldPos = new Vector3I(e.OldPosition.X/32, e.OldPosition.Y/32, e.OldPosition.Z/32);
            Vector3I newPos = new Vector3I(e.NewPosition.X/32, e.NewPosition.Y/32, e.NewPosition.Z/32);

            // Check if the player actually moved and not just rotated
            if ((oldPos.X != newPos.X) || (oldPos.Y != newPos.Y) || (oldPos.Z != newPos.Z)) {
                if (e.Player.Info.IsAFK) {
                    Server.Players.CanSee(e.Player).Message("&S{0} is no longer AFK", e.Player.Name);
                    e.Player.Message("&SYou are no longer AFK");
                    e.Player.Info.IsAFK = false;
                    e.Player.Info.oldafkMob = e.Player.Info.afkMob;
                    e.Player.Info.afkMob = e.Player.Info.Mob;
                }
                Server.UpdateTabList();
                e.Player.ResetIdBotTimer();
            }
        }

        static readonly CommandDescriptor CdAFK = new CommandDescriptor
        {
            Name = "AFK",
            Category = CommandCategory.New | CommandCategory.Chat,
            Aliases = new[] { "away", "awayfromkeyboard" },
            Usage = "/afk [optional message]",
            Help = "Shows an AFK message.",
            Handler = AFKHandler
        };

        private static void AFKHandler(Player player, CommandReader cmd) {
            string msg = cmd.NextAll();
            if (player.Info.IsMuted) {
                player.MessageMuted();
                return;
            }
            Server.Players.CanSee(player)
                .Message("&S{0} is {1} AFK{2}", player.Name, player.Info.IsAFK ? "no longer" : "now",
                msg.Length > 0 ? " (" + (msg.Length > 32 ? msg.Remove(32) : msg) + ")" : "");
            player.Message("&SYou are {0} AFK {1}", player.Info.IsAFK ? "no longer" : "now",
                msg.Length > 0 ? " (" + (msg.Length > 32 ? msg.Remove(32) : msg) + ")" : "");
            player.Info.IsAFK = !player.Info.IsAFK;
            player.Info.oldafkMob = player.Info.afkMob;
            player.Info.afkMob = player.Info.IsAFK ? "chicken" : player.Info.Mob;
            Server.UpdateTabList();
            player.ResetIdBotTimer();
        }

        #endregion
        #region Ignore / Unignore

        static readonly CommandDescriptor CdIgnore = new CommandDescriptor
        {
            Name = "Ignore",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Usage = "/Ignore [PlayerName] or IRC",
            Help = "Temporarily blocks the other player from messaging you. " +
                   "If no player name is given, lists all ignored players.",
            Handler = IgnoreHandler
        };

        static void IgnoreHandler(Player player, CommandReader cmd)
        {
            string name = cmd.Next();
            if (name.ToLower() == "irc")
            {
                if (player.Info.ReadIRC == true)
                {
                    player.Info.ReadIRC = false;
                    player.Message("You are now ignoring &iIRC");
                    string message = String.Format("\u212C&SPlayer {0}&S is now Ignoring IRC", player.ClassyName);
                    if (!player.Info.IsHidden)
                    {
                        IRC.SendChannelMessage(message);
                    }
                }
                else
                {
                    player.Message("You are already ignoring &iIRC");
                }
                return;
            }
            if (name != "")
            {
                if (cmd.HasNext)
                {
                    CdIgnore.PrintUsage(player);
                    return;
                }
                PlayerInfo targetInfo = PlayerDB.FindPlayerInfoOrPrintMatches(player, name, SearchOptions.ReturnSelfIfOnlyMatch);
                if (targetInfo == null) return;

                if (player.Ignore(targetInfo))
                {
                    player.Message("You are now ignoring {0}", targetInfo.ClassyName);
                }
                else
                {
                    player.Message("You are already ignoring {0}", targetInfo.ClassyName);
                }

            }
            else
            {
                PlayerInfo[] ignoreList = player.IgnoreList;
                if (ignoreList.Length > 0)
                {
                    player.Message("Ignored players: {0}", ignoreList.JoinToClassyString());
                }
                else
                {
                    player.Message("You are not currently ignoring anyone.");
                }
            }
        }


        static readonly CommandDescriptor CdUnignore = new CommandDescriptor
        {
            Name = "Unignore",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Usage = "/Unignore [PlayerName] or [IRC]",
            Help = "Unblocks the other player from messaging you.",
            Handler = UnignoreHandler
        };

        static void UnignoreHandler(Player player, CommandReader cmd)
        {
            string name = cmd.Next();
            if (name.ToLower() == "irc")
            {
                if (player.Info.ReadIRC == false)
                {
                    player.Info.ReadIRC = true;
                    player.Message("You are no longer ignoring &iIRC");
                    string message = String.Format("\u212C&SPlayer {0}&S is no longer Ignoring IRC", player.ClassyName);
                    if (!player.Info.IsHidden)
                    {
                        IRC.SendChannelMessage(message);
                    }
                }
                else
                {
                    player.Message("You are not currently ignoring &iIRC");
                }
                return;
            }
            if (name != "")
            {
                if (cmd.HasNext)
                {
                    CdUnignore.PrintUsage(player);
                    return;
                }
                PlayerInfo targetInfo = PlayerDB.FindPlayerInfoOrPrintMatches(player, name, SearchOptions.ReturnSelfIfOnlyMatch);
                if (targetInfo == null) return;

                if (player.Unignore(targetInfo))
                {
                    player.Message("You are no longer ignoring {0}", targetInfo.ClassyName);
                }
                else
                {
                    player.Message("You are not currently ignoring {0}", targetInfo.ClassyName);
                }
            }
            else
            {
                PlayerInfo[] ignoreList = player.IgnoreList;
                if (ignoreList.Length > 0)
                {
                    player.Message("Ignored players: {0}", ignoreList.JoinToClassyString());
                }
                else
                {
                    player.Message("You are not currently ignoring anyone.");
                }
            }
        }

        #endregion
        #region Me

        static readonly CommandDescriptor CdMe = new CommandDescriptor
        {
            Name = "Me",
            Category = CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            NotRepeatable = true,
            DisableLogging = true,
            UsableByFrozenPlayers = true,
            Usage = "/Me Message",
            Help = "Sends IRC-style action message prefixed with your name.",
            Handler = MeHandler
        };

        static void MeHandler(Player player, CommandReader cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.DetectChatSpam()) return;

            string msg = cmd.NextAll().Trim();
            if (msg.Length > 0)
            {
                Chat.SendMe(player, msg);
            }
            else
            {
                CdMe.PrintUsage(player);
            }
        }

        #endregion
        #region Roll

        static readonly CommandDescriptor CdRoll = new CommandDescriptor
        {
            Name = "Roll",
            Category = CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            Help = "Gives random number between 1 and 100.\n" +
                   "&H/Roll MaxNumber\n" +
                   "&S  Gives number between 1 and max.\n" +
                   "&H/Roll MinNumber MaxNumber\n" +
                   "&S  Gives number between min and max.",
            Handler = RollHandler
        };

        static void RollHandler(Player player, CommandReader cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }
            if (player.Info.TimeSinceLastServerMessage.TotalSeconds < 5) {
                player.Info.getLeftOverTime(5, cmd);
                return;
            }

            if (player.DetectChatSpam()) return;

            Random rand = new Random();
            int n1;
            int min, max;
            if (cmd.NextInt(out n1))
            {
                int n2;
                if (!cmd.NextInt(out n2))
                {
                    n2 = 1;
                }
                min = Math.Min(n1, n2);
                max = Math.Max(n1, n2);
            }
            else
            {
                min = 1;
                max = 100;
            }

            int num = rand.Next(min, max + 1);
            Server.Message(player,
                            "{0}{1} rolled {2} ({3}...{4})",
                            player.ClassyName, Color.Silver, num, min, max);            
            player.Message("{0}You rolled {1} ({2}...{3})",
                            Color.Silver, num, min, max);
            player.Info.LastServerMessageDate = DateTime.Now;
            if (min == 1 && max == 100)
            {
                if (num == 69)
                {
                    Server.Players.Message( "&6Bot&f: Tehe....69" );
                    Logger.Log( LogType.UserActivity, "&6Bot&f: Tehe....69" );
                    IRC.SendChannelMessage("\u212C&6Bot\u211C: Tehe....69");
                }
                if (num == Server.CountPlayers(false))
                {
                    Server.Players.Message( "&6Bot&f: That's how many players are online :D" );
                    Logger.Log( LogType.UserActivity, "&6Bot&f: That's how many players are online :D" );
                    IRC.SendChannelMessage("\u212C&6Bot\u211C: That's how many players are online :D");
                }
            }
        }

        #endregion
        #region IRC

        static readonly CommandDescriptor CdIRC = new CommandDescriptor
        {
            Name = "IRC",
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            Help = "Turns IRC On or Off.",
            Usage = "/IRC (On/Off)",
            Handler = IRCHandler
        };

        private static void IRCHandler(Player player, CommandReader cmd) {
            string IRCFlag = cmd.Next();
            if (IRCFlag != null) {
                if (IRCFlag.ToLower() == "on" || IRCFlag.ToLower() == "true" || IRCFlag.ToLower() == "1" ||
                    IRCFlag.ToLower() == "yes") {
                    if (player.Info.ReadIRC == false) {
                        player.Info.ReadIRC = true;
                        player.Message("&sYou are now receiving IRC Messages. To disable, type &h/IRC Off&s.");
                        string message = String.Format("\u212C&SPlayer {0}&S is no longer Ignoring IRC",
                            player.ClassyName);
                        if (!player.Info.IsHidden) {
                            IRC.SendChannelMessage(message);
                        }
                    } else {
                        player.Message("&sYou are already receiving IRC messages.");
                    }
                } else if (IRCFlag.ToLower() == "off" || IRCFlag.ToLower() == "false" || IRCFlag.ToLower() == "0" ||
                    IRCFlag.ToLower() == "no") {
                    if (player.Info.ReadIRC == true) {
                        player.Info.ReadIRC = false;
                        player.Message("&sYou are no longer receiving IRC Messages. To enable, type &h/IRC On&s.");
                        string message = String.Format("\u212C&SPlayer {0}&S is now Ignoring IRC", player.ClassyName);
                        if (!player.Info.IsHidden) {
                            IRC.SendChannelMessage(message);
                        }
                    } else {
                        player.Message("&sYou are already not receiving IRC messages.");
                    }
                } else CdIRC.PrintUsage(player);
            } else {
                if (player.Info.ReadIRC) player.Message("&S  IRC Receive: &AON");
                else player.Message("&S  IRC Receive: &COFF");
            }
        }

        #endregion
        #region Deafen

        static readonly CommandDescriptor CdDeafen = new CommandDescriptor
        {
            Name = "Deafen",
            Aliases = new[] { "deaf" },
            Category = CommandCategory.Chat,
            Help = "Blocks all chat messages from being sent to you.",
            Handler = DeafenHandler
        };

        static void DeafenHandler(Player player, CommandReader cmd)
        {
            if (cmd.HasNext)
            {
                CdDeafen.PrintUsage(player);
                return;
            }
            if (!player.IsDeaf)
            {
                for (int i = 0; i < LinesToClear; i++)
                {
                    player.Message("");
                }
                player.Message("Deafened mode: &2ON");
                player.Message("You will not see ANY messages until you type &H/Deafen&S again.");
                player.IsDeaf = true;
            }
            else
            {
                player.IsDeaf = false;
                player.Message("Deafened mode: &4OFF");
            }
        }

        #endregion
        #region Clear

        const int LinesToClear = 30;
        static readonly CommandDescriptor CdClear = new CommandDescriptor
        {
            Name = "Clear",
            UsableByFrozenPlayers = true,
            Category = CommandCategory.Chat,
            Help = "Clears the chat screen.",
            Handler = ClearHandler
        };

        static void ClearHandler(Player player, CommandReader cmd)
        {
            if (cmd.HasNext)
            {
                CdClear.PrintUsage(player);
                return;
            }
            for (int i = 0; i < LinesToClear; i++)
            {
                player.Message("");
            }
        }

        #endregion
        #region Report
        static readonly CommandDescriptor CdReport = new CommandDescriptor
        {
            Name = "Report",
            Category = CommandCategory.New | CommandCategory.Chat,
            Usage = "/Report <Message>",
            Help = "Used to leave a report message only the Highest Rank can read.\n" +
                   "Things to talk about: \n"+
                   "  &fGriefers, Spammers, Trenchers\n" +
                   "  &fAbusive Players, Abusive Admins\n" +
                   "  &fBugs, Suggestions\n" +
                   "  &fOr just a friendly message\n" +
                   "&sRemember, everything is kept a secret unless stated otherwise.",

            Handler = reportHandler
        };

        private static void reportHandler(Player player, CommandReader cmd) {
            if (player.DetectChatSpam()) return;
            string message = cmd.NextAll();
            Report rCreate = new Report();
            if (cmd.IsConfirmed) {
                rCreate.addReport(getNewReportId(), player.Name, DateTime.Now, message);
                player.Message("Report sent!");
				foreach (Player p in Server.Players.Where(q => q.Info.Rank == RankManager.HighestRank)) {
					p.Message((byte)MessageType.Announcement, "Player {0} has sent in a report!", player.Name);
					p.Message("Player {0} has sent in a report!", player.Name);
				}
                return;
            }
            if (message.Length < 1) {
                CdReport.PrintUsage(player);
            } else {
                player.Confirm(cmd,
                    "&sYour message will show up like this: \n" + "&s[&1Report&s]\n" + "  &sFrom:&f {0}\n" +
                    "  &sDate: &7{1} at {2}\n" + "  &sMessage:&f {3}", player.Name, DateTime.Now.ToShortDateString(),
                    DateTime.Now.ToLongTimeString(), message);
            }

        }

        static readonly CommandDescriptor CdReports = new CommandDescriptor
        {
            Name = "Reports",
            Aliases = new[] { "mail" },
            Permissions = new[] { Permission.ShutdownServer },
            IsConsoleSafe = true,
            Category = CommandCategory.New | CommandCategory.Chat,
            Usage = "/Reports",
            Help = "Use this command to list/remove reports from players",

            Handler = MOLHandler
        };

        private static void MOLHandler(Player player, CommandReader cmd) {
            string param = cmd.Next();
            int reportId;

            // List Reports
            switch (param) {
                case "abort":
                case "r":
                case "d":
                case "remove":
                case "delete":
                    bool removed = false;
                    Report rRemove = null;
                    if (cmd.NextInt(out reportId)) {
                        foreach (Report r in Chat.Reports)
                            if (r.Id == reportId) {
                                player.Message("  #{0} has been removed", r.Id);
                                rRemove = r;
                                removed = true;
                            }
                        if (rRemove != null) {
                            rRemove.removeFilter();
                        }
                        if (!removed) {
                            player.Message("Given Report (#{0}) does not exist.", reportId);
                        }

                    } else {
                        CdReports.PrintUsage(player);
                    }
                    break;
                case "read":
				case "open":
				case "view":
                    bool read = false;
                    if (cmd.NextInt(out reportId)) {
                        foreach (Report r in Chat.Reports) {
                            if (r.Id == reportId) {
                                player.Message(
                                    "&s[&1Report&s] #&f{0}\n" + "  &sFrom:&f {1}\n" + "  &sDate: &7{2} at {3}\n" +
                                    "  &sMessage:&f {4}", r.Id, r.Sender, r.Datesent.ToShortDateString(),
                                    r.Datesent.ToLongTimeString(), r.Message);
                                read = true;
                            }
                        }
                        if (!read) {
                            player.Message("Given Report (#{0}) does not exist.", reportId);
                        }
                    } else {
                        CdReports.PrintUsage(player);
                    }
                    break;
                default:
                    if (Chat.Reports.Count == 0) {
                        player.Message("There are no reports.");
                    } else {
                        player.Message("There are {0} reports:", Chat.Reports.Count);
                        foreach (Report r in Chat.Reports.OrderBy(r => r.Datesent)) {
                            player.Message("&s[&1Report&s] #&f" + r.Id + " &sFrom:&f " + r.Sender);
                        }
                    }
                    break;
            }
        }

        public static int getNewReportId() {
            int i = 1;
        go:
            foreach (Report r in Chat.Reports) {
                if (r.Id == i) {
                    i++;
                    goto go;
                }
            }
            return i;
        }

        #endregion
        #region Timer

        static readonly CommandDescriptor CdTimer = new CommandDescriptor
        {
            Name = "Timer",
            Permissions = new[] { Permission.UseTimers },
            IsConsoleSafe = true,
            Category = CommandCategory.Chat,
            Usage = "/Timer <Duration> <Message>",
            Help = "Starts a timer with a given duration and message. " +
                   "As the timer counts down, announcements are shown globally. See also: &H/Help Timer Abort",
            HelpSections = new Dictionary<string, string> {
                { "abort",  "&H/Timer Abort <TimerID>\n&S" +
                            "Aborts a timer with the given ID number. " +
                            "To see a list of timers and their IDs, type &H/Timer&S (without any parameters)." }
            },
            Handler = TimerHandler
        };

        static void TimerHandler(Player player, CommandReader cmd)
        {
            string param = cmd.Next();

            // List timers
            if (param == null)
            {
                ChatTimer[] list = ChatTimer.TimerList.OrderBy(timer => timer.TimeLeft).ToArray();
                if (list.Length == 0)
                {
                    player.Message("No timers running.");
                }
                else
                {
                    player.Message("There are {0} timers running:", list.Length);
                    foreach (ChatTimer timer in list)
                    {
                        if (timer.Message.Equals(""))
                        {
                            player.Message("  #{0} \"&7*CountDown*&s\" (started by {2}, {3} left)",
                                            timer.ID, timer.Message, timer.StartedBy, timer.TimeLeft.ToMiniString());
                        }
                        else
                        {
                            player.Message("  #{0} \"{1}&s\" (started by {2}, {3} left)",
                                            timer.ID, timer.Message, timer.StartedBy, timer.TimeLeft.ToMiniString());
                        }
                    }
                }
                return;
            }

            // Abort a timer
            if (param.Equals("abort", StringComparison.OrdinalIgnoreCase))
            {
                int timerId;
                if (cmd.NextInt(out timerId))
                {
                    ChatTimer timer = ChatTimer.FindTimerById(timerId);
                    if (timer == null || !timer.IsRunning)
                    {
                        player.Message("Given timer (#{0}) does not exist.", timerId);
                    }
                    else
                    {
                        timer.Abort();
                        string abortMsg = "";
                        string abortircMsg = "";
                        if (timer.Message.Equals(""))
                        {
                            abortMsg = String.Format("&S{0}&S aborted a &7CountDown&S with {1} left",
                                                             player.ClassyName, timer.TimeLeft.ToMiniString());
                            abortircMsg = String.Format("\u212C&S{0}&S aborted a &7CountDown&S with {1} left",
                                                             player.ClassyName, timer.TimeLeft.ToMiniString());
                        }
                        else
                        {
                            abortMsg = String.Format("&S{0}&S aborted a &7Timer&S with {1} left: &7{2}",
                                                             player.ClassyName, timer.TimeLeft.ToMiniString(), timer.Message);
                            abortircMsg = String.Format( "\u212C&S{0}&S aborted a &7Timer&S with {1} left: \u211C{2}",
                                                             player.ClassyName, timer.TimeLeft.ToMiniString(), timer.Message);
                        }
                        Server.Players.Message(abortMsg);
                        IRC.SendChannelMessage(abortircMsg);
                    }
                }
                else
                {
                    CdTimer.PrintUsage(player);
                }
                return;
            }

            // Start a timer
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }
            if (player.DetectChatSpam()) return;
            TimeSpan duration;
            if (!param.TryParseMiniTimespan(out duration))
            {
                CdTimer.PrintUsage(player);
                return;
            }
            if (duration > DateTimeUtil.MaxTimeSpan)
            {
                player.MessageMaxTimeSpan();
                return;
            }
            if (duration < ChatTimer.MinDuration)
            {
                player.Message("Timer: Must be at least 1 second.");
                return;
            }

            string sayMessage;
            string ircMessage;
            string message = cmd.NextAll();
            if (String.IsNullOrEmpty(message))
            {
                sayMessage = String.Format("&2[&7CountDown Started&2][&7{1}&2] &2-&7{0}",
                                            player.Name,
                                            duration.ToMiniString());
                ircMessage = String.Format( "\u212C&2[&7{1} CountDown Started&2] -\u211C{0}",
                                            player.Name,
                                            duration.ToMiniString());
            }
            else
            {
                sayMessage = String.Format("&2[&7Timer Started&2][&7{1}&2] &7{2} &2-&7{0}",
                                            player.Name,
                                            duration.ToMiniString(),
                                            message);
                ircMessage = String.Format( "\u212C&2[&7{1} Timer Started&2][&7{0}&2] \u211C{2}",
                                            player.Name,
                                            duration.ToMiniString(),
                                            message);
            }
            Server.Players.Message(sayMessage);
            IRC.SendChannelMessage(ircMessage);
            ChatTimer.Start(duration, message, player.Name);
        }
        #endregion
        #region Filters

        static readonly CommandDescriptor CdSwears = new CommandDescriptor
        {
            Name = "filters",
            Aliases = new[] { "filterlist", "fl" },
            IsConsoleSafe = true,
            Category = CommandCategory.New | CommandCategory.Chat,
            Usage = "/filters",
            Help = "Lists all words that are replaced with their replacment word.",
            Handler = SwearsHandler
        };

        static void SwearsHandler(Player player, CommandReader cmd)
        {
            if (Chat.Filters.Count == 0)
            {
                player.Message("There are no filters.");
            }
            else
            {
                player.Message("There are {0} filters:", Chat.Filters.Count);
                foreach (Filter filter in Chat.Filters.OrderBy(f => f.Id))
                {
                    player.Message("  #{0} \"{1}\" --> \"{2}&S\"",
                                    filter.Id, filter.Word, filter.Replacement);
                }
            }
        }
        static readonly CommandDescriptor CdSwear = new CommandDescriptor
        {
            Name = "filter",
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ShutdownServer },
            Category = CommandCategory.Chat,
            Usage = "/Filter <(add|create)|(remove|delete)> <Word> <Replacement>",
            Help = "Adds or removes a word and it's replacement to the filter",
            HelpSections = new Dictionary<string, string> {
                { "add",  "&H/Filter add <Word> <Replacement>\n&S" +
                            "Adds a Word and it's replacement to the filter list. " },
                { "remove",  "&H/Filter remove <filterID>\n&S" +
                            "Removes a filter with the given ID number. " +
                            "To see a list of filters and their IDs, type &H/filterlist" }
            },
            Handler = SwearHandler
        };

        private static void SwearHandler(Player player, CommandReader cmd) {
            string param = cmd.Next();
            if (param == null) {
                CdSwear.PrintUsage(player);
                return;
            }
            switch (param.ToLower()) {
                case "r":
                case "d":
                case "remove":
                case "delete":
                    int fId;
                    bool removed = false;
                    Filter fRemove = null;
                    if (cmd.NextInt(out fId)) {
                        foreach (Filter f in Chat.Filters) {
                            if (f.Id == fId) {
                                Server.Message("&Y[Filters] {0}&Y removed the filter \"{1}\" -> \"{2}\"",
                                    player.ClassyName, f.Word, f.Replacement);
                                fRemove = f;
                                removed = true;
                            }
                        }
                        if (fRemove != null) {
                            fRemove.removeFilter();
                        }
                        if (!removed) {
                            player.Message("Given filter (#{0}) does not exist.", fId);
                        }
                    } else {
                        CdSwear.PrintUsage(player);
                    }
                    break;
                case "a":
                case "add":
                case "create":
                    Filter fCreate = new Filter();
                    if (player.Info.IsMuted) {
                        player.MessageMuted();
                        return;
                    }
                    string word = cmd.Next();
                    string replacement = cmd.NextAll();
                    if ("".Equals(word) || "".Equals(replacement)) {
                        CdSwear.PrintUsage(player);
                        break;
                    }
                    bool exists = false;
                    foreach (Filter f in Chat.Filters) {
                        if (f.Word.ToLower().Equals(word.ToLower())) {
                            exists = true;
                        }
                    }
                    if (!exists) {
                        Server.Message("&Y[Filters] \"{0}\" is now replaced by \"{1}\"", word, replacement);
                        fCreate.addFilter(getNewFilterId(), word, replacement);
                    } else {
                        player.Message("A filter with that world already exists!");
                    }
                    break;
                default:
                    CdSwear.PrintUsage(player);
                    break;
            }
        }


        public static int getNewFilterId() {
            int i = 1;
            go:
            foreach (Filter filter in Chat.Filters) {
                if (filter.Id == i) {
                    i++;
                    goto go;
                }
            }
            return i;
        }

        #endregion
        #region Quit

        static readonly CommandDescriptor CdQuit = new CommandDescriptor
        {
            Name = "Quit",
            Aliases = new[] { "quitmessage", "quitmsg", "qm" },
            Category = CommandCategory.New | CommandCategory.Chat,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.Chat },
            Usage = "/Quit [message]",
            Help = "Quits the server with a reason",
            Handler = QuitHandler
        };

        static void QuitHandler(Player player, CommandReader cmd) {
            string Msg = "/Quit" + (cmd.HasNext ? " " + cmd.NextAll() : "");
            player.usedquit = true;
            player.quitmessage = (Msg.Length > 70 ? Msg.Remove(70) : Msg);
            player.Kick(Msg, LeaveReason.ClientQuit);
            Logger.Log(LogType.UserActivity,
                        "{0} left the server. Reason: {1}", player.Name, player.quitmessage);
        }
        #endregion
        #region RBChat
        static readonly CommandDescriptor CdRBChat = new CommandDescriptor
        {
            Name = "RainbowChat",
            Category = CommandCategory.New | CommandCategory.Chat,
            Aliases = new[] { "rainbowch", "rbchat", "rbch", "rc" },
            Permissions = new[] { Permission.UseColorCodes },
            IsConsoleSafe = true,
            Usage = "/RainbowChat \"bw\"(black and white optional)",
            Help = "Determines if you speak in rainbow or not.",
            Handler = RBChatHandler
        };

		static void RBChatHandler(Player player, CommandReader cmd) {
			if ("bw".Equals(cmd.Next().ToLower())) {
				if (player.Info.ChatBWRainbows == true) {
					player.Info.ChatRainbows = false;
					player.Info.ChatBWRainbows = false;
					player.Message("BWRainbow Chat: &4Off");
					player.Message("Your messages will now show up normally.");
					return;
				} else {
					player.Info.ChatBWRainbows = true;
					player.Info.ChatRainbows = false;
					player.Message("BWRainbow Chat: &2On");
					player.Message("Your messages will now show up as &0R&8A&7I&fN&7B&8O&0W&8S&7!&f.");
					return;
				}
			}
			if (player.Info.ChatRainbows == true) {
				player.Info.ChatRainbows = false;
				player.Info.ChatBWRainbows = false;
				player.Message("Rainbow Chat: &4Off");
				player.Message("Your messages will now show up normally.");
				return;
			} else {
				player.Info.ChatRainbows = true;
				player.Info.ChatBWRainbows = false;
				player.Message("Rainbow Chat: &2On");
				player.Message("Your messages will now show up as &cR&4A&6I&eN&aB&2O&bW&3S&9!&s.");
				return;
			}
		}
        #endregion
        #region Greet

        static readonly CommandDescriptor CdGreeting = new CommandDescriptor
        {
            Name = "Greet",
            Aliases = new[] { "greeting", "welcome" },
            Permissions = new[] { Permission.Chat },
            Category = CommandCategory.New | CommandCategory.Chat,
            Help = "Sends a message welcoming the last player to join the server.",
            Handler = greetHandler
        };

        private static void greetHandler(Player player, CommandReader cmd) {
            if (player.Info.TimeSinceLastServerMessage.TotalSeconds < 5) {
                player.Info.getLeftOverTime(5, cmd);
                return;
            }
            var all = Server.Players.Where(p => !p.Info.IsHidden).OrderBy(p => p.Info.TimeSinceLastLogin.ToMilliSeconds());
            Player last = all.First();

            if (last == player) {
                player.Message("You were the last player to join silly");
                return;
            }
            if (all.Any()) {
				string message = "Welcome to " + Color.StripColors(ConfigKey.ServerName.GetString()) + ", " + last.Name + "!";
                player.ParseMessage(message, false);
                player.Info.LastServerMessageDate = DateTime.Now;
            } else {
                player.Message("Error: No one else on!");
            }
        }

        #endregion
        #region difference

        static readonly CommandDescriptor CdLdis = new CommandDescriptor {
            Name = "Compare",
            Aliases = new[] { "similarity", "similar", "sim" },
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            DisableLogging = true,
            UsableByFrozenPlayers = true,
            Usage = "/Compare [Word1] [Word2]",
            Help = "Tells you how similar two words are as a percentage",
            Handler = LdisHandler
        };

        static void LdisHandler( Player player, CommandReader cmd ) {
            string first = cmd.Next();
            string second = cmd.Next();
            if (first == null) 
                first = "";
            if (second == null) 
                second = "";
            float percent = (1 - Chat.LDistance(first, second)) * 100;
            player.Message( percent + "&s% similarity between:" );
            player.Message( "  &7{0} &sand &7{1}", first, second );
        }

        #endregion
        #region textHotKey

        static readonly CommandDescriptor CdtextHotKey = new CommandDescriptor {
            Name = "TextHotKey",
            Aliases = new[] { "HotKey", "thk", "hk" },
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.ReadStaffChat },
            Usage = "/TextHotKey [Label] [Action] [KeyCode] [KeyMods]",
            Help = "Sets up TextHotKeys. Use http://minecraftwiki.net/Key_Codes for keycodes",
            Handler = textHotKeyHandler
        };

        private static void textHotKeyHandler(Player player, CommandReader cmd) {
            string Label = cmd.Next();
            string Action = cmd.Next();
            string third = cmd.Next();
            string fourth = cmd.Next();
            if (Label == null || Action == null || third == null || fourth == null) {
                CdtextHotKey.PrintUsage(player);
                return;
            }

            int KeyCode;
            if (!int.TryParse(third, out KeyCode)) {
                player.Message("Error: Invalid Integer ({0})", third);
                return;
            }
            byte KeyMod = 0;
            if (cmd.HasNext) {
                if (!Byte.TryParse(fourth, out KeyMod)) {
                    player.Message("Error: Invalid Byte ({0})", fourth);
                    return;
                }
            }
            if (player.Supports(CpeExtension.TextHotKey)) {
                player.Send(Packet.MakeSetTextHotKey(Label, Action, KeyCode, KeyMod));
            } else {
                player.Message("You do not support TextHotKey");
                return;
            }
        }

        #endregion
        #region brushes

        static readonly CommandDescriptor Cdbrushes = new CommandDescriptor {
            Name = "Brushes",
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.DrawAdvanced },
            Usage = "/brushes",
            Help = "Lists all available brushes",
            Handler = brushesHandler
        };

        private static void brushesHandler( Player player, CommandReader cmd ) {
            player.Message( BrushManager.CdBrush.Help.Replace( "Gets or sets the current brush. ", "" ) );
        }

        #endregion
        #region Idea

        static readonly CommandDescriptor CdIdea = new CommandDescriptor {
            Name = "Idea",
            Aliases = new[] { "buildideas", "ideas"  },
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            Help = "Gives random building idea",
            Usage = "/Idea [number of ideas 1-10]",
            Handler = IdeaHandler
        };

        private static void IdeaHandler(Player player, CommandReader cmd) {
            string[] adjectiveStrings;
            string[] nounStrings;
            if (File.Exists("./Bot/Adjectives.txt") && File.Exists("./Bot/Nouns.txt")) {
                adjectiveStrings = File.ReadAllLines("./Bot/Adjectives.txt");
                nounStrings = File.ReadAllLines("./Bot/Nouns.txt");
            } else {
                player.Message(
                    "&cError: No idea files! This should not happen! Yell at the host for deleting Adjectives.txt and Nouns.txt in the bot file.");
                return;
            }
            Random randAdjectiveString = new Random();
            Random randNounString = new Random();
            string adjective;
            string noun;
            int amount;
            string ana = "a";
            if (player.Info.TimeSinceLastServerMessage.TotalSeconds < 5) {
                player.Info.getLeftOverTime(5, cmd);
                return;
            }

            if (cmd.NextInt(out amount)) {
                if (amount > 10)
                    amount = 10;
                if (amount < 1)
                    amount = 1;
                player.Message("{0} random building ideas", amount);
                for (int i = 1; i <= amount;) {
                    adjective = adjectiveStrings[randAdjectiveString.Next(0, adjectiveStrings.Length)];
                    noun = nounStrings[randNounString.Next(0, nounStrings.Length)];
                    if (adjective.StartsWith("a") || adjective.StartsWith("e") || adjective.StartsWith("i") ||
                        adjective.StartsWith("o") || adjective.StartsWith("u")) {
                        ana = "an";
                    } else if (noun.EndsWith("s")) {
                        ana = "some";
                    }
                    player.Message("&sIdea #{0}&f: Build " + ana + " " + adjective + " " + noun, i);
                    i++;
                    ana = "a";
                }
            } else {
                adjective = adjectiveStrings[randAdjectiveString.Next(0, adjectiveStrings.Length)];
                noun = nounStrings[randNounString.Next(0, nounStrings.Length)];
                if (adjective.StartsWith("a") || adjective.StartsWith("e") || adjective.StartsWith("i") ||
                    adjective.StartsWith("o") || adjective.StartsWith("u")) {
                    ana = "an";
                } else if (noun.EndsWith("s")) {
                    ana = "some";
                }
                player.Message("&sIdea&f: Build " + ana + " " + adjective + " " + noun);
            }
            player.Info.LastServerMessageDate = DateTime.Now;
        }

        #endregion
        #region Action

        static readonly CommandDescriptor CdAction = new CommandDescriptor {
            Name = "Action",
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            NotRepeatable = true,
            DisableLogging = true,
            UsableByFrozenPlayers = true,
            Usage = "/Action [Player] [Action]",
            Help = "Displays you doing an action to a player. Example: \"/action Facepalmed high fived\" would show up as \"123DontMessWitMe has high fived Facepalmed",
            Handler = ActionHandler
        };

        private static void ActionHandler(Player player, CommandReader cmd) {
            if (player.Info.IsMuted) {
                player.MessageMuted();
                return;
            }
            if (player.DetectChatSpam())
                return;
            string searchplayer = cmd.Next();
            string action = cmd.NextAll().Trim();
            Player other = Server.FindPlayerOrPrintMatches(player, searchplayer, SearchOptions.Default);
            if (other == player) {
                player.Message("Cannot action yourself");
                return;
            }
            if (!(cmd.Count <= 2) && cmd.IsConfirmed) {
                Server.Players.Message("{0} &s{1} {2}", player.ClassyName, action, other.ClassyName);
                return;
            }
            if (other != null) {
                player.Confirm(cmd, "Your messege will show up as: {0} &s{1} {2}", player.ClassyName, action,
                    other.ClassyName);
            }
        }

        #endregion
        #region Warn

        static readonly CommandDescriptor CdWarn = new CommandDescriptor {
            Name = "Warn",
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.Kick },
            IsConsoleSafe = true,
            NotRepeatable = true,
            DisableLogging = true,
            Usage = "/Warn [Player] [warning]",
            Help = "Warns a player about a specified message. Example: \"/Warn Facepalmed stop griefing\" would show up as \"123DontMessWitMe has warned Facepalmed to stop griefing",
            Handler = WarningHandler
        };

        private static void WarningHandler(Player player, CommandReader cmd) {
            if (player.Info.IsMuted) {
                player.MessageMuted();
                return;
            }
            if (player.DetectChatSpam())
                return;
            string searchplayer = cmd.Next();
            string warning = cmd.NextAll().Trim();
            Player other = Server.FindPlayerOrPrintMatches(player, searchplayer, SearchOptions.Default);
            if (other == player) {
                player.Message("Cannot warn yourself");
                return;
            }
            if (!(cmd.Count <= 2) && cmd.IsConfirmed) {
                Server.Players.Message("&f{0} &chas warned &f{1} &cto &4{2}", player.ClassyName, other.ClassyName, warning);
                return;
            }
            if (other != null) {
                player.Confirm(cmd, "Your warning will display as: \"&f{0} &chas warned &f{1} &cto &4{2}\"", player.ClassyName, other.ClassyName, warning);
            }
        }

        #endregion
    }
}

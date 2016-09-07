// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fCraft;
using fCraft.Drawing;

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
            CommandManager.RegisterCommand(CdFilters);
            CommandManager.RegisterCommand(CdIRC);
            CommandManager.RegisterCommand(CdQuit);
            CommandManager.RegisterCommand(CdReport);
            CommandManager.RegisterCommand(CdReports);
            CommandManager.RegisterCommand(CdRBChat);
            CommandManager.RegisterCommand(CdGreeting);
            CommandManager.RegisterCommand(CdIRCStaff);
            CommandManager.RegisterCommand(CdLdis);
            CommandManager.RegisterCommand(Cdbrushes);
            CommandManager.RegisterCommand(CdIdea);
            CommandManager.RegisterCommand(CdAction);
            CommandManager.RegisterCommand(CdWarn);
            CommandManager.RegisterCommand(CdCapColor);


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
            var staff = Server.Players.Where(p => p.IsStaff);
            if (staff != null && staff.Any()) {
                player.Message("Your review request has been sent to the Moderators. They will be with you shortly");
                Server.Players.Where(p => p.IsStaff).Message("Player " + player.ClassyName + " &Srequests a building review.");
            } else {
                player.Message("There are no staff on! Sorry!");
            }
        }

        #endregion
        #region AFK

        public static void Player_IsBack(object sender, Events.PlayerMovedEventArgs e) {
            // We need to have block positions, so we divide by 32
            Vector3I oldPos = new Vector3I(e.OldPosition.X/32, e.OldPosition.Y/32, e.OldPosition.Z/32);
            Vector3I newPos = new Vector3I(e.NewPosition.X/32, e.NewPosition.Y/32, e.NewPosition.Z/32);

            // Check if the player actually moved and not just rotated
            if ((oldPos.X != newPos.X) || (oldPos.Y != newPos.Y) || (oldPos.Z != newPos.Z)) {
                if (e.Player.IsAFK) {
                    Server.Players.CanSee(e.Player).Message("{0} is no longer AFK", e.Player.Name);
                    e.Player.Message("You are no longer AFK");
                    e.Player.IsAFK = false;
                    e.Player.oldafkMob = e.Player.afkMob;
                    e.Player.afkMob = e.Player.Info.Mob;
                    Server.UpdateTabList(true);
                }
                e.Player.ResetIdleTimer();
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
                .Message("{0} is {1} AFK{2}", player.Name, player.IsAFK ? "no longer" : "now",
                msg.Length > 0 ? " (" + (msg.Length > 32 ? msg.Remove(32) : msg) + ")" : "");
            player.Message("You are {0} AFK {1}", player.IsAFK ? "no longer" : "now",
                msg.Length > 0 ? " (" + (msg.Length > 32 ? msg.Remove(32) : msg) + ")" : "");
            player.IsAFK = !player.IsAFK;
            player.oldafkMob = player.afkMob;
            player.afkMob = player.IsAFK ? player.AFKModel : player.Info.Mob;
            Server.UpdateTabList(true);
            player.ResetIdleTimer();
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

        static void IgnoreHandler(Player player, CommandReader cmd) {
            string name = cmd.Next();
            if (!string.IsNullOrEmpty(name)) {
            	if (name.CaselessEquals("irc")) {
                    if (player.Info.ReadIRC) {
                        player.Info.ReadIRC = false;
                        player.Message("You are now ignoring &iIRC");
                        string message = String.Format("\u212C&SPlayer {0}&S is now Ignoring IRC", player.ClassyName);
                        if (!player.Info.IsHidden) {
                            IRC.SendChannelMessage(message);
                        }
                    } else {
                        player.Message("You are already ignoring &iIRC");
                    }
                    return;
                }
                if (cmd.HasNext) {
                    CdIgnore.PrintUsage(player);
                    return;
                }
                PlayerInfo targetInfo = PlayerDB.FindPlayerInfoOrPrintMatches(player, name, SearchOptions.ReturnSelfIfOnlyMatch);
                if (targetInfo == null) return;

                if (player.Ignore(targetInfo)) {
                    player.Message("You are now ignoring {0}", targetInfo.ClassyName);
                } else {
                    player.Message("You are already ignoring {0}", targetInfo.ClassyName);
                }

            } else {
                PlayerInfo[] ignoreList = player.IgnoreList;
                if (ignoreList.Length > 0) {
                    player.Message("Ignored players: {0}", ignoreList.JoinToClassyString());
                } else {
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

        static void UnignoreHandler(Player player, CommandReader cmd) {
            string name = cmd.Next();
            if (!string.IsNullOrEmpty(name)) {
            	if (name.CaselessEquals("irc")) {
                    if (!player.Info.ReadIRC) {
                        player.Info.ReadIRC = true;
                        player.Message("You are no longer ignoring &iIRC");
                        string message = String.Format("\u212C&SPlayer {0}&S is no longer Ignoring IRC", player.ClassyName);
                        if (!player.Info.IsHidden) {
                            IRC.SendChannelMessage(message);
                        }
                    } else {
                        player.Message("You are not currently ignoring &iIRC");
                    }
                    return;
                }
                if (cmd.HasNext) {
                    CdUnignore.PrintUsage(player);
                    return;
                }
                PlayerInfo targetInfo = PlayerDB.FindPlayerInfoOrPrintMatches(player, name, SearchOptions.ReturnSelfIfOnlyMatch);
                if (targetInfo == null) return;

                if (player.Unignore(targetInfo)) {
                    player.Message("You are no longer ignoring {0}", targetInfo.ClassyName);
                } else {
                    player.Message("You are not currently ignoring {0}", targetInfo.ClassyName);
                }
            } else {
                PlayerInfo[] ignoreList = player.IgnoreList;
                if (ignoreList.Length > 0) {
                    player.Message("Ignored players: {0}", ignoreList.JoinToClassyString());
                } else {
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
            Help = "Gives random number between 1 and 100.&n" +
                   "&H/Roll MaxNumber&n" +
                   "  Gives number between 1 and max.&n" +
                   "&H/Roll MinNumber MaxNumber&n" +
                   "  Gives number between min and max.",
            Handler = RollHandler
        };

        static void RollHandler(Player player, CommandReader cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }
            if (player.TimeSinceLastServerMessage.TotalSeconds < 5) {
                player.getLeftOverTime(5, cmd);
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
            player.LastServerMessageDate = DateTime.UtcNow;
            if (min == 1 && max == 100)
            {
                if (num == 69)
                {
                    Server.BotMessage("Tehe....69");
                }
                if (num == Server.CountPlayers(false))
                {
                    Server.BotMessage("That's how many players are online :D");
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
            Help = "Tells you the IRC channel.",
            Usage = "/IRC",
            Handler = IRCHandler
        };

        private static void IRCHandler(Player player, CommandReader cmd) {
            if (!ConfigKey.IRCBotEnabled.Enabled() || ConfigKey.IRCBotChannels.GetString().Length <= 1 || ConfigKey.IRCBotNetwork.GetString().Length <= 1) {
                player.Message("This server has IRC disabled or does not have IRC settings setup correctly");
                return;
            } else {
                player.Message("&IInternet Relay Chat information");
                player.Message(" IRC Network: &f{0}", ConfigKey.IRCBotNetwork.GetString());
                player.Message(" Network Channel/s: (please wait 1 second for updates)");
                IRC.SendRawMessage(IRCCommands.Names(ConfigKey.IRCBotChannels.GetString()),"","");
                Scheduler.NewTask(t => sendChannelUsers(player)).RunOnce(TimeSpan.FromSeconds(1));
            }
        }

        private static void sendChannelUsers(Player p) {
            foreach (var chan in IRC.Users) {
                p.Message(" -&s{0}: &f{1}",
                               chan.Key, chan.Value.JoinToString(", "));
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

        static void DeafenHandler(Player player, CommandReader cmd) {
            if (cmd.HasNext) {
                CdDeafen.PrintUsage(player);
                return;
            }
            
            if (!player.IsDeaf) {
                for (int i = 0; i < LinesToClear; i++) {
                    player.Message("");
                }
                  
                bool fallback = !player.Supports(CpeExt.TextColors);
                player.SendNow(Packet.Message(0, "Deafened mode: &2ON", fallback));
                player.SendNow(Packet.Message(0, "You will not see ANY messages until you type &H/Deafen&S again.", fallback));
                player.IsDeaf = true;
            } else {
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
            Help = "Used to leave a report message only the Highest Rank can read.&n" +
                   "Things to talk about: &n"+
                   "  &fGriefers, Spammers, Trenchers&n" +
                   "  &fAbusive Players, Abusive Admins&n" +
                   "  &fBugs, Suggestions&n" +
                   "  &fOr just a friendly message&n" +
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
					if (p.Supports(CpeExt.MessageType)) {
						p.Send(Packet.Message((byte)MessageType.Announcement, 
                		                      String.Format("Player {0} has sent in a report!", player.Name), true));
					}
					p.Message("Player {0} has sent in a report!", player.Name);
				}
                return;
            }
            if (message.Length < 1) {
                CdReport.PrintUsage(player);
            } else {
                player.Confirm(cmd,
                    "&sYour message will show up like this: &n" + "&s[&1Report&s]&n" + "  &sFrom:&f {0}&n" +
                    "  &sDate: &7{1} at {2}&n" + "  &sMessage:&f {3}", player.Name, DateTime.Now.ToShortDateString(),
                    DateTime.Now.ToLongTimeString(), message);
            }

        }

        static readonly CommandDescriptor CdReports = new CommandDescriptor
        {
            Name = "Reports",
            Aliases = new[] { "mail" },
            Permissions = new[] { Permission.ShutdownServer, Permission.EditPlayerDB, Permission.ReloadConfig },
            IsConsoleSafe = true,
            Category = CommandCategory.New | CommandCategory.Chat,
            Usage = "/Reports [option] {args}",
            Help = "Use this command to list/remove reports from players",

            Handler = ReportsHandler
        };

        private static void ReportsHandler(Player player, CommandReader cmd) {
            string param = cmd.Next() ?? "n/a";
            int reportId;
            // List Reports
            switch (param.ToLower()) {
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
                                    "&s[&1Report&s] #&f{0}&n" + "  &sFrom:&f {1}&n" + "  &sDate: &7{2} at {3}&n" +
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
                            player.Message("[&1Report&s] #&f" + r.Id + " &sFrom:&f " + r.Sender);
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
                { "abort",  "&H/Timer Abort <TimerID>&n&S" +
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
            if (param.CaselessEquals("abort"))
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

        static readonly CommandDescriptor CdFilters = new CommandDescriptor {
            Name = "filter",
            Aliases = new[] { "filterlist", "fl", "filters" },
            IsConsoleSafe = true,
            Category = CommandCategory.Chat,
            Usage = "/Filter {option} {args}",
            Help = "Adds or removes a word and it's replacement to the chat filters&n" +
                "Options: Add, Edit, Remove&n" + 
                "Writing nothing will display all filters.",
            HelpSections = new Dictionary<string, string> {
                { "add",  "&H/Filter add <Word> <Replacement>&n&S" +
                            "Adds a Word and it's replacement to the filter list. " },
                { "edit",  "&H/Filter edit <filterID> [word/replacement] [new string]&n&S" +
                            "Edits a filter with the given ID number. " +
                            "To see a list of filters and their IDs, type &H/filters" },
                { "remove",  "&H/Filter remove <filterID>&n&S" +
                            "Removes a filter with the given ID number. " +
                            "To see a list of filters and their IDs, type &H/filters" }
            },
            Handler = SwearHandler
        };

        private static void SwearHandler(Player player, CommandReader cmd) {
            if (!player.IsStaff || cmd.CountRemaining == 0) {
                if (ChatFilter.Filters.Count == 0) {
                    player.Message("There are no filters.");
                } else {
                    player.Message("There are {0} filters:", ChatFilter.Filters.Count);
                    foreach (ChatFilter filter in ChatFilter.Filters.OrderBy(f => f.Id)) {
                        player.Message("  #{0} \"{1}\" --> \"{2}&S\"",
                                        filter.Id, filter.Word, filter.Replacement);
                    }
                }
                return;
            }
            string param = cmd.Next();
            if (string.IsNullOrEmpty(param)) {
                CdFilters.PrintUsage(player); return;
            }
            
            switch (param.ToLower()) {
                case "r":
                case "d":
                case "remove":
                case "delete":
                    string dID = cmd.Next();
                    if (string.IsNullOrEmpty(dID)) {
                        CdFilters.PrintUsage(player);
                        break;
                    }
                    ChatFilter dFilter = ChatFilter.Find(dID);
                    if (dFilter != null) {
                        if (cmd.IsConfirmed) {
                            ChatFilter.RemoveFilter(dID);
                            Server.Message("&Y[Filters] {0}&Y removed the filter \"{1}\" -> \"{2}\"",
                                           player.ClassyName, dFilter.Word, dFilter.Replacement);
                            break;
                        } else {
                            player.Confirm(cmd, "This will delete the filter permanently");
                            break;
                        }
                    } else {
                        player.Message("Given filter (#{0}) does not exist.", dID);
                        break;
                    }
                case "a":
                case "add":
                case "create":
                    ChatFilter aFilter = new ChatFilter();
                    string word = cmd.Next();
                    string replacement = cmd.NextAll();
                    if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(replacement)) {
                        CdFilters.PrintUsage(player); return;
                    }
                    if (!ChatFilter.exists(word)) {
                        Server.Message("&Y[Filters] \"{0}\" is now replaced by \"{1}\"", word, replacement);
                        ChatFilter.CreateFilter(getNewFilterId(), word, replacement);
                    } else {
                        player.Message("A filter with that word already exists!");
                    }
                    break;
                case "edit":
                case "change":
                case "update":
                    int eID;
                    if (!cmd.NextInt(out eID)) {
                        CdFilters.PrintUsage(player); return;
                    }
                    string option = cmd.Next() ?? "n/a";
                    
                    ChatFilter[] matches = ChatFilter.Filters.Where(f => f.Id == eID).ToArray();
                    if (matches.Length == 0) {
                        player.Message("No filters have the ID \"" + eID + "\"."); return;
                    }
                    ChatFilter eFilter = matches[0];
                    string oldWord = eFilter.Word, oldReplacement = eFilter.Replacement;
                    string value = cmd.NextAll();
                    if (string.IsNullOrEmpty(value)) {
                        CdFilters.PrintUsage(player); return;
                    }
                    
                    switch (option.ToLower()) {
                        case "word":
                        case "w":
                            eFilter.Word = value; break;
                        case "replacement":
                        case "r":
                            eFilter.Replacement = value; break;
                        default:
                            CdFilters.PrintUsage(player); return;
                    }
                    Server.Message("&Y[Filters] {0}&Y edited a filter from &n(\"{1}\" -> \"{2}\") &nto (\"{3}\" -> \"{2}\")",
                                   player.ClassyName, oldWord, oldReplacement, eFilter.Word, eFilter.Replacement);
                    ChatFilter.RemoveFilter(eID.ToString());
                    ChatFilter.Filters.Add(eFilter);
                    break;
                case "reload":
                    if (player.Info.Rank == RankManager.HighestRank) {
                        ChatFilter.ReloadAll();
                        player.Message("Reloaded filters from file");
                    }
                    break;
                default:
                    CdFilters.PrintUsage(player);
                    break;
            }
        }

        public static int getNewFilterId() {
            int i = 1;
            go:
            foreach (ChatFilter filter in ChatFilter.Filters) {
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
            Aliases = new[] { "quitmessage", "quitmsg", "qm", "rq", "ragequit" },
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
            player.Send(Packet.MakeKick(Msg));
            Logger.Log(LogType.UserActivity,
                        "{0} disconnected. Reason: {1}", player.Name, player.quitmessage);
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
			string s = cmd.Next();
			if (s != null && s.CaselessEquals("bw")) {
				if (player.ChatBWRainbows) {
					player.ChatRainbows = false;
					player.ChatBWRainbows = false;
					player.Message("BWRainbow Chat: &4Off");
					player.Message("Your messages will now show up normally.");
				} else {
					player.ChatBWRainbows = true;
					player.ChatRainbows = false;
					player.Message("BWRainbow Chat: &2On");
					player.Message("Your messages will now show up as &0R&8A&7I&fN&7B&8O&0W&8S&7!&f.");
				}
			} else if (player.ChatRainbows) {
				player.ChatRainbows = false;
				player.ChatBWRainbows = false;
				player.Message("Rainbow Chat: &4Off");
				player.Message("Your messages will now show up normally.");
			} else {
				player.ChatRainbows = true;
				player.ChatBWRainbows = false;
				player.Message("Rainbow Chat: &2On");
				player.Message("Your messages will now show up as &cR&4A&6I&eN&aB&2O&bW&3S&9!&s.");
			}
		}
        #endregion
        #region Greet

        static readonly CommandDescriptor CdGreeting = new CommandDescriptor
        {
            Name = "Greet",
            Aliases = new[] { "greeting", "welcome" },
            Permissions = new[] { Permission.Chat },
			IsConsoleSafe = true,
            Category = CommandCategory.New | CommandCategory.Chat,
            Help = "Sends a message welcoming the last player to join the server.",
            Handler = greetHandler
        };

        private static void greetHandler(Player player, CommandReader cmd) {
            var all = Server.Players.Where(p => !p.Info.IsHidden).OrderBy(p => p.Info.TimeSinceLastLogin.ToMilliSeconds());
            if (all.Any() && all != null) {
                Player last = all.First();
                if (last == player) {
                    player.Message("You were the last player to join silly");
                    return;
                }
                if (player.LastPlayerGreeted == last) {
                    player.Message("You have to greet someone else before you can greet {0} again.", last.Name);
                    return;
                }
                string message = "Welcome to " + Color.StripColors(ConfigKey.ServerName.GetString()) + ", " + last.Name + "!";
                player.ParseMessage(message, false);
                player.LastServerMessageDate = DateTime.UtcNow;
                player.LastPlayerGreeted = last;
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
        #region brushes

        static readonly CommandDescriptor Cdbrushes = new CommandDescriptor {
            Name = "Brushes",
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.DrawAdvanced },
            Usage = "/brushes",
            Help = "Lists all available brushes",
            IsConsoleSafe = true,
            Handler = brushesHandler
        };

        private static void brushesHandler( Player player, CommandReader cmd ) {
            string brushes = null;
            foreach (IBrushFactory brush in BrushManager.RegisteredFactories) {
                if (string.IsNullOrEmpty(brushes)) {
                    brushes = brush.Name;
                } else {
                    brushes = brushes + ", " + brush.Name;
                }
            }
            player.Message("Available brushes: " + brushes);
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
            if (player.TimeSinceLastServerMessage.TotalSeconds < 5) {
                player.getLeftOverTime(5, cmd);
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
                    player.Message("Idea #{0}&f: Build " + ana + " " + adjective + " " + noun, i);
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
                player.Message("Idea&f: Build " + ana + " " + adjective + " " + noun);
            }
            player.LastServerMessageDate = DateTime.UtcNow;
        }

        #endregion
        #region Action

        static readonly CommandDescriptor CdAction = new CommandDescriptor {
            Name = "Action",
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            NotRepeatable = true,
            Usage = "/Action [Player] [Action]",
            Help = "Displays you doing an action to a player. " +
            	"Example: \"/action Facepalmed high fived\" would show up as \"123DontMessWitMe has high fived Facepalmed",
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
			if ("".Equals(searchplayer) || "".Equals(action)) {
				CdAction.PrintUsage(player);
				return;
			}
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
                Server.Players.Message("{0} &chas warned {1}&c: &4{2}", player.ClassyName, other.ClassyName, warning);
                return;
            }
            if (other != null) {
                player.Confirm(cmd, "Your warning will display as: \"{0} &chas warned {1}&c: &4{2}\"", player.ClassyName, other.ClassyName, warning);
            }
        }

        #endregion
        #region CapColor
        static readonly CommandDescriptor CdCapColor = new CommandDescriptor
        {
            Name = "CapColor",
            Aliases = new[] { "capcol", "cc" },
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new[] { Permission.ChangeNameCaps },
            Help = "Quick changes your displayed name capitalization or color.",
            Usage = "/CapColor <NewName>",
            Handler = CapColHandler
        };

        static void CapColHandler(Player player, CommandReader cmd)
        {
            string targetName = player.Name;
            string valName = cmd.NextAll();

            PlayerInfo info = player.Info;
            if (info == null) return;

            //Quickchanges nickname.
            string oldDisplayedName = info.DisplayedName;
            if (valName.Length == 0) valName = null;
            if (valName == null)
            {
                valName = info.Name;
            }
            if (valName == info.DisplayedName)
            {
                player.Message("CapColor: Your DisplayedName is already set to \"{1}&S\"",
                                    info.Name,
                                    valName);
                return;
            }
            if (valName != null && !Color.StripColors(Chat.ReplacePercentColorCodes(valName, false)).CaselessEquals(info.Name))
            {
                player.Message("CapColor: You may not change your name to something else");
                return;
            }
            if (!player.Can(Permission.ChangeNameColor))
            {
                valName = Color.StripColors(Chat.ReplacePercentColorCodes(valName, false));
            }
            if (cmd.IsConfirmed)
            {
                info.DisplayedName = valName;

                if (oldDisplayedName == null)
                {
                    player.Message("CapColor: Your DisplayedName was set to \"{1}&S\"",
                                    info.Name,
                                    valName);
                }
                else if (valName == info.DisplayedName)
                {
                    player.Message("CapColor: Your DisplayedName was reset (was \"{1}&S\")",
                                    info.Name,
                                    oldDisplayedName);
                }
                else
                {
                    player.Message("CapColor: Your DisplayedName was changed from \"{1}&S\" to \"{2}&S\"",
                                    info.Name,
                                    oldDisplayedName,
                                    valName);
                }
                return;
            }
            player.Confirm(cmd, "This will change your displayed name to " + valName);
        }
        #endregion
    }
}

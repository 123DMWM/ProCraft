// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.ExceptionServices;
using fCraft;

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
            CommandManager.RegisterCommand(CdMail);
            CommandManager.RegisterCommand(CdMailList);
            CommandManager.RegisterCommand(CdRBChat);
            CommandManager.RegisterCommand(CdGreeting);
            CommandManager.RegisterCommand(CdIRCStaff);
            CommandManager.RegisterCommand(Cdchat);
            CommandManager.RegisterCommand(CdLdis);
            CommandManager.RegisterCommand(CdtextHotKey);


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
            Category = CommandCategory.New,
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
            Category = CommandCategory.New,
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
            
            {
                player.Message("&SYour review request has been sent to the Moderators. They will be with you shortly");
                Chat.SendStaffSay(player, "&SPlayer " + player.ClassyName + " &Srequests a building review.");
            }
        }

        #endregion
        #region chat

        static readonly CommandDescriptor Cdchat = new CommandDescriptor
        {
            Name = "chat",
            Category = CommandCategory.New,
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

        public static void Player_IsBack(object sender, Events.PlayerMovedEventArgs e)
        {
            if (e.Player.Info.IsAFK)
            {
                // We need to have block positions, so we divide by 32
                Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32);
                Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);

                // Check if the player actually moved and not just rotated
                if ((oldPos.X != newPos.X) || (oldPos.Y != newPos.Y) || (oldPos.Z != newPos.Z))
                {
                    Server.Players.CanSee(e.Player).Message("&S{0} is no longer AFK", e.Player.Name);
                    e.Player.Message("&SYou are no longer AFK");
                    e.Player.Info.Mob = e.Player.Info.TempMob;
                    e.Player.Info.IsAFK = false;
                    Server.UpdateTabList();
                    e.Player.ResetIdBotTimer();
                }
            }
            //InfoCommands.NearestPlayerTo(e.Player);
        }

        static readonly CommandDescriptor CdAFK = new CommandDescriptor
        {
            Name = "AFK",
            Category = CommandCategory.New,
            Aliases = new[] { "away" },
            Usage = "/afk [optional message]",
            Help = "Shows an AFK message.",
            Handler = AFKHandler
        };

        static void AFKHandler(Player player, CommandReader cmd)
        {
            string msg = cmd.NextAll().Trim();
            PlayerInfo p = PlayerDB.FindPlayerInfoOrPrintMatches(player, player.Name, SearchOptions.IncludeSelf);
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }
            if (player.Info.IsAFK) {
                Server.Players.CanSee(player).Message("&S{0} is no longer AFK", player.Name);
                Server.UpdateTabList();
                player.Message("&SYou are no longer AFK");
                p.Mob = p.TempMob;
                player.Info.IsAFK = false;
                Server.UpdateTabList();
                player.ResetIdBotTimer();
            }
            else
                if (msg.Length > 0)
                    if (msg.Length <= 32)
                    {
                        Server.Players.CanSee(player).Message("&S{0} is now AFK ({1})", player.Name, msg);
                        player.Message("&SYou are now AFK ({0})", msg);
                        p.TempMob = p.Mob;
                        p.Mob = "chicken";
                        player.Info.IsAFK = true;
                        Server.UpdateTabList();
                    }
                    else
                    {
                        player.Message("Message cannot be more than 32 spaces. Yours was: " + cmd.NextAll().Length);
                    }
                else
                {
                    Server.Players.CanSee(player).Message("&S{0} is now AFK", player.Name);
                    player.Message("&SYou are now AFK");
                    p.TempMob = p.Mob;
                    p.Mob = "chicken";
                    player.Info.IsAFK = true;
                    Server.UpdateTabList();
                }     
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
                    player.MessageNow("You are now ignoring &iIRC");
                    string message = String.Format("\u212C&SPlayer {0}&S is now Ignoring IRC", player.ClassyName);
                    if (!player.Info.IsHidden)
                    {
                        IRC.SendChannelMessage(message);
                    }
                }
                else
                {
                    player.MessageNow("You are already ignoring &iIRC");
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
                    player.MessageNow("You are now ignoring {0}", targetInfo.ClassyName);
                }
                else
                {
                    player.MessageNow("You are already ignoring {0}", targetInfo.ClassyName);
                }

            }
            else
            {
                PlayerInfo[] ignoreList = player.IgnoreList;
                if (ignoreList.Length > 0)
                {
                    player.MessageNow("Ignored players: {0}", ignoreList.JoinToClassyString());
                }
                else
                {
                    player.MessageNow("You are not currently ignoring anyone.");
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
                    player.MessageNow("You are no longer ignoring &iIRC");
                    string message = String.Format("\u212C&SPlayer {0}&S is no longer Ignoring IRC", player.ClassyName);
                    if (!player.Info.IsHidden)
                    {
                        IRC.SendChannelMessage(message);
                    }
                }
                else
                {
                    player.MessageNow("You are not currently ignoring &iIRC");
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
                    player.MessageNow("You are no longer ignoring {0}", targetInfo.ClassyName);
                }
                else
                {
                    player.MessageNow("You are not currently ignoring {0}", targetInfo.ClassyName);
                }
            }
            else
            {
                PlayerInfo[] ignoreList = player.IgnoreList;
                if (ignoreList.Length > 0)
                {
                    player.MessageNow("Ignored players: {0}", ignoreList.JoinToClassyString());
                }
                else
                {
                    player.MessageNow("You are not currently ignoring anyone.");
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
            if (min == 1 && max == 100)
            {
                if (num == 69)
                {
                    Server.Players.Message("&6Bot&f: Tehe....69");
                    IRC.SendChannelMessage("\u212C&6Bot\u211C: Tehe....69");
                }
                if (num == Server.CountPlayers(false))
                {
                    Server.Players.Message("&6Bot&f: That's how many players are online :D");
                    IRC.SendChannelMessage("\u212C&6Bot\u211C: That's how many players are online :D");
                }
            }
        }

        #endregion
        #region IRC

        static readonly CommandDescriptor CdIRC = new CommandDescriptor
        {
            Name = "IRC",
            Category = CommandCategory.New,
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
                    player.MessageNow("");
                }
                player.MessageNow("Deafened mode: &2ON");
                player.MessageNow("You will not see ANY messages until you type &H/Deafen&S again.");
                player.IsDeaf = true;
            }
            else
            {
                player.IsDeaf = false;
                player.MessageNow("Deafened mode: &4OFF");
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
        #region mailer
        static readonly CommandDescriptor CdMail = new CommandDescriptor
        {
            Name = "Mail",
            Aliases = new[] { "MailOwners" },
            Category = CommandCategory.New,
            Usage = "/Mail <Message>",
            Help = "Used to leave a message only the Highest Rank can read.\n" +
                   "Things to talk about: \n"+
                   "  &fGriefers, Spammers, Trenchers\n" +
                   "  &fAbusive Players, Abusive Admins\n" +
                   "  &fBugs, Suggestions\n" +
                   "  &fOr just a friendly message\n" +
                   "&sRemember, everything is kept a secret unless stated otherwise.",

            Handler = MOHandler
        };

        static void MOHandler(Player player, CommandReader cmd)
        {
            if (player.DetectChatSpam()) return;
            string message = cmd.NextAll();
            if (cmd.IsConfirmed)
            {
                ChatMailer.Start(message, player.Name);
                player.Message("Mail sent!");
                return;
            }
            else if (message.Length < 1)
            {
                CdMail.PrintUsage(player);
                return;
            }
            else
            {
                player.Confirm(cmd, "&sYour message will show up like this: \n" +
                                    "&s[&1Mail&s]\n" + 
                                    "  &sFrom:&f {0}\n" + 
                                    "  &sDate: &7{1} {2}\n" + 
                                    "  &sMessage:&f {3}", 
                                    player.Name, 
                                    DateTime.Now.ToShortDateString(), 
                                    DateTime.Now.ToLongTimeString(), 
                                    message);
                return;
            }

        }
        static readonly CommandDescriptor CdMailList = new CommandDescriptor
        {
            Name = "ListMail",
            Aliases = new[] { "lm" },
            Permissions = new[] { Permission.ShutdownServer },
            IsConsoleSafe = true,
            Category = CommandCategory.New,
            Usage = "/ListMail",
            Help = "Use this command to list/remove reports from players",

            Handler = MOLHandler
        };

        static void MOLHandler(Player player, CommandReader cmd)
        {
            string param = cmd.Next();

            // List Mails
            if (param == null)
            {
                ChatMailer[] list = ChatMailer.MailerList.OrderBy(Mail => Mail.TimeLeft).ToArray();
                if (list.Length == 0)
                {
                    player.Message("There is no Mail.");
                }
                else
                {
                    player.Message("There are {0} Messages:", list.Length);
                    foreach (ChatMailer Mail in list)
                    {
                        player.Message("&s[&1Mail ID#" + Mail.ID + "&s] From:&f " + Mail.StartedBy);
                    }
                }
                return;
            }
            // Abort a Mail
            if (param.Equals("abort", StringComparison.OrdinalIgnoreCase))
            {
                int MailId;
                if (cmd.NextInt(out MailId))
                {
                    ChatMailer Mail = ChatMailer.FindMailerById(MailId);
                    if (Mail == null || !Mail.IsRunning)
                    {
                        player.Message("Given Mail (#{0}) does not exist.", MailId);
                    }
                    else
                    {
                        Mail.Abort(Mail.ID);
                        player.Message("  #{0} has been closed", Mail.ID);
                    }
                }
                return;
            }
            if (param.Equals("read", StringComparison.OrdinalIgnoreCase))
            {
                int MailId;
                if (cmd.NextInt(out MailId))
                {
                    ChatMailer Mail = ChatMailer.FindMailerById(MailId);
                    if (Mail == null || !Mail.IsRunning)
                    {
                        player.Message("Given Mail (#{0}) does not exist.", MailId);
                    }
                    else
                    {
                        player.Message("&s[&1Mail #{0}&s]\n" +
                                       "  &sFrom:&f {1}\n" + 
                                       "  &sDate: &7{2} {3}\n" + 
                                       "  &sMessage:&f {4}", 
                                       Mail.ID, 
                                       Mail.StartedBy, 
                                       Mail.StartTime.ToShortDateString(), 
                                       Mail.StartTime.ToLongTimeString(), 
                                       Mail.Message);
                    }
                }
                return;
            }
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
        #region Swears

        static readonly CommandDescriptor CdSwears = new CommandDescriptor
        {
            Name = "filters",
            Aliases = new[] { "filterlist", "fl" },
            IsConsoleSafe = true,
            Category = CommandCategory.New,
            Usage = "/filters",
            Help = "Lists all words that are replaced with their replacment word.",
            Handler = SwearsHandler
        };

        static void SwearsHandler(Player player, CommandReader cmd)
        {
            ChatSwears[] list = ChatSwears.SwearList.OrderBy(swear => swear.ID).ToArray();
            if (list.Length == 0)
            {
                player.Message("No filters.");
            }
            else
            {
                player.Message("There are {0} filters:", list.Length);
                foreach (ChatSwears Swear in list)
                {
                    player.Message("  #{0} \"{1}\" --> \"{2}&S\"",
                                    Swear.ID, Swear.Swear, Swear.Replacement);
                }
            }
        }
        static readonly CommandDescriptor CdSwear = new CommandDescriptor
        {
            Name = "filter",
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ShutdownServer },
            Category = CommandCategory.Chat,
            Usage = "/Filter <add/remove> <Word> <Replacement>",
            Help = "Adds or removes a word and it's replacement to the filter",
            HelpSections = new Dictionary<string, string> {
                { "add",  "&H/Filter add <Word> <Replacement>\n&S" +
                            "Adds a Word and it's replacement to the filter. " },
                { "remove",  "&H/Filter remove <filterID>\n&S" +
                            "Removes a filter with the given ID number. " +
                            "To see a list of filters and their IDs, type &H/filterlist" }
            },
            Handler = SwearHandler
        };

        static void SwearHandler(Player player, CommandReader cmd)
        {
            string param = cmd.Next();

            // Abort a timer
            if (param == null)
            {
                CdSwear.PrintUsage(player);
                return;
            }
            else if (param.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                int swearId;
                if (cmd.NextInt(out swearId))
                {
                    ChatSwears swear = ChatSwears.FindSwearById(swearId);
                    if (swear == null || !swear.IsRunning)
                    {
                        player.Message("Given filter (#{0}) does not exist.", swearId);
                    }
                    else
                    {
                        swear.Abort();
                        Server.Message("&Y[Filters] {0}&Y removed the filter \"{1}\"",
                                                         player.ClassyName, swear.Swear);
                    }
                }
                else
                {
                    CdSwear.PrintUsage(player);
                }
                return;
            }
            // Start a timer
            else if (param.Equals("add", StringComparison.OrdinalIgnoreCase))
            {
                if (player.Info.IsMuted)
                {
                    player.MessageMuted();
                    return;
                }
                if (player.DetectChatSpam()) return;
                string swear = cmd.Next();
                string replacement = cmd.Next();
                Server.Message("&Y[Filters] \"{0}\" is now replaced by \"{1}\"", swear, replacement);
                ChatSwears.Start(swear.ToLower(), replacement);
            }
            else
            {
                CdSwear.PrintUsage(player);
                return;
            }
        }
        #endregion
        #region Quit

        static readonly CommandDescriptor CdQuit = new CommandDescriptor
        {
            Name = "Quit",
            Aliases = new[] { "quitmessage", "quitmsg", "qm" },
            Category = CommandCategory.New,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.Chat },
            Usage = "/Quitmsg [message]",
            Help = "Quits the server with a reason",
            Handler = QuitHandler
        };

        static void QuitHandler(Player player, CommandReader cmd)
        {
            string Msg = "/Quit " + cmd.NextAll();

            if (Msg.Length < 1)
            {
                Msg = "/Quit";
            }
            if (Msg.Length > 64)
            {
                player.Message("Message length must be at most 64 digits long. Not: {0}", Msg.Length);
                return;
            }
            player.usedquit = true;
            player.quitmessage = Msg;
            player.Kick(Msg, LeaveReason.ClientQuit);            

            Logger.Log(LogType.UserActivity,
                        "{0} left the server. Reason: {1}", player.Name, Msg);
        }
        #endregion
        #region RBChat
        static readonly CommandDescriptor CdRBChat = new CommandDescriptor
        {
            Name = "RainbowChat",
            Category = CommandCategory.New,
            Aliases = new[] { "rainbowch", "rbchat", "rbch", "rc" },
            Permissions = new[] { Permission.UseColorCodes },
            IsConsoleSafe = true,
            Usage = "/RainbowChat On/Off",
            Help = "Determines if you speak in rainbow or not.",
            Handler = RBChatHandler
        };

        static void RBChatHandler(Player player, CommandReader cmd)
        {
            if (cmd.HasNext)
            {
                string state = cmd.Next();
                if (state.ToLower() == "on" || state.ToLower() == "yes")
                {
                    player.Info.ChatRainbows = true;
                    player.Message("Rainbow Chat: &2On");
                    player.Message("Your messages will now show up as &cR&4A&6I&eN&aB&2O&bW&3S&9!&s.");
                    return;
                }
                if (state.ToLower() == "off" || state.ToLower() == "no")
                {
                    player.Info.ChatRainbows = false;
                    player.Message("Rainbow Chat: &4Off");
                    player.Message("Your messages will now show up normally.");
                    return;
                }
                if (state.ToLower() == "state" || state.ToLower() == "what" || state.ToLower() == "current")
                {
                    if (player.Info.ChatRainbows == false)
                    {
                        player.Message("Rainbow Chat: &4Off");
                    }
                    if (player.Info.ChatRainbows == true)
                    {
                        player.Message("Rainbow Chat: &2On");
                    }
                    return;
                }
                else
                {
                    CdRBChat.PrintUsage(player);
                    return;
                }
            }
            if (player.Info.ChatRainbows == true)
            {
                player.Info.ChatRainbows = false;
                player.Message("Rainbow Chat: &4Off");
                player.Message("Your messages will now show up normally.");
                return;
            }
            else
            {
                player.Info.ChatRainbows = true;
                player.Message("Rainbow Chat: &2On");
                player.Message("Your messages will now show up as &cR&4A&6I&eN&aB&2O&bW&3S&9!&s.");
                return;
            }
        }
        #endregion
        #region Greet

        static readonly CommandDescriptor CdGreeting = new CommandDescriptor
        {
            Name = "Greeting",
            Aliases = new[] { "greet", "welcome" },
            Permissions = new[] { Permission.Chat },
            Category = CommandCategory.New,
            Help = "Sends a message welcoming the last player to join the server.",
            Handler = greetHandler
        };

        static void greetHandler(Player player, CommandReader cmd)
        {
            string message;
            double GreetTime = (DateTime.Now - player.Info.LastTimeGreeted).TotalSeconds;
            if (GreetTime < 10)
            {
                double LeftOverTime = Math.Round(10 - GreetTime);
                if (LeftOverTime == 1)
                {
                    player.Message("&WYou can use /Greet again in 1 second.");
                    return;
                }
                else
                {
                    player.Message("&WYou can use /Greet again in " + LeftOverTime + " seconds");
                    return;
                }
            }
            int count = 1;
            var all = Server.Players.Where(z => !z.Info.IsHidden).OrderBy(p => player.Info.TimeSinceLastLogin.ToMilliSeconds());
            if (cmd.HasNext && player.Can(Permission.ReadStaffChat))
            {
                cmd.NextInt(out count);
            }
            var closest = all.Take(count).ToArray();
            Player test = closest[0];
            if (test == player)
            {
                player.Message("You were the last player to join silly");
                return;
            }
            if (player.CanSee(test) && test.Info.IsHidden)
            {
                player.Message("Don't Blow their cover!");
                return;
            }
            string serverName = ConfigKey.ServerName.GetString();
            if (closest.Length == 1)
            {
                message = "Welcome to " + serverName + ", " + closest.JoinToString(r => r.Name + "!");
                player.ParseMessage(message, false);
                player.Info.LastTimeGreeted = DateTime.Now;
            }
            else if (closest.Length > 1)
            {
                message = "Welcome to " + serverName + ", " + closest.JoinToString(", ", r => r.Name) + "!";
                player.ParseMessage(message, false);
                player.Info.LastTimeGreeted = DateTime.Now;
            }
            else if (closest.Length == 0)
            {
                player.Message("Error: LastPlayer == null");
            }
            return;
        }

        #endregion
        #region difference

        static readonly CommandDescriptor CdLdis = new CommandDescriptor {
            Name = "Compare",
            Aliases = new[] { "similarity", "similar", "sim" },
            Category = CommandCategory.New,
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
            Category = CommandCategory.New,
            Permissions = new[] { Permission.Chat },
            Usage = "/TextHotKey [Label] [Action] [KeyCode] [KeyMods]",
            Help = "Sets up TextHotKeys. Use http://minecraftwiki.net/Key_Codes for keycodes",
            Handler = textHotKeyHandler
        };

        private static void textHotKeyHandler(Player player, CommandReader cmd) {
            if (cmd.Count < 3) {
                CdtextHotKey.PrintUsage(player);
                return;
            }
            string Label = cmd.Next();
            string Action = cmd.Next();
            int KeyCode;
            if (!cmd.NextInt(out KeyCode)) {
                player.Message("Error: Invalid Integer");
                return;
            }
            byte KeyMod = 0;
            if (cmd.HasNext) {
                if (!Byte.TryParse(cmd.Next(), out KeyMod)) {
                    player.Message("Error: Invalid Byte");
                    return;
                }
            }
            player.Send(Packet.MakeSetTextHotKey(Label, Action, KeyCode, KeyMod));
        }

        #endregion
    }
}

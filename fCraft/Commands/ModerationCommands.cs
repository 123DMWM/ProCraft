// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using JetBrains.Annotations;
using ServiceStack.Text;

namespace fCraft {
    /// <summary> Most commands for server moderation - kick, ban, rank change, etc - are here. </summary>
    static class ModerationCommands {
        const string BanCommonHelp = "Ban information can be viewed with &H/BanInfo";

        internal static void Init() {
            CdBan.Help += BanCommonHelp;
            CdBanIP.Help += BanCommonHelp;
            CdBanAll.Help += BanCommonHelp;
            CdUnban.Help += BanCommonHelp;
            CdUnbanIP.Help += BanCommonHelp;
            CdUnbanAll.Help += BanCommonHelp;

            CommandManager.RegisterCommand( CdBan );
            CommandManager.RegisterCommand( CdBanIP );
            CommandManager.RegisterCommand( CdBanAll );
            CommandManager.RegisterCommand( CdUnban );
            CommandManager.RegisterCommand( CdUnbanIP );
            CommandManager.RegisterCommand( CdUnbanAll );
            CommandManager.RegisterCommand( CdBot );
            CommandManager.RegisterCommand( CdCalculator );
            CommandManager.RegisterCommand( CdKick );
            CommandManager.RegisterCommand( CdRank );
            CommandManager.RegisterCommand( CdHide );
            CommandManager.RegisterCommand( CdUnhide );
            CommandManager.RegisterCommand( CdSetSpawn );
            CommandManager.RegisterCommand( CdFreeze );
            CommandManager.RegisterCommand( CdUnfreeze );
            CommandManager.RegisterCommand( CdTeleport );
            CommandManager.RegisterCommand( CdTeleportP );
            CommandManager.RegisterCommand( CdTop );
            CommandManager.RegisterCommand( CdBring );
            CommandManager.RegisterCommand( CdWorldBring );
            CommandManager.RegisterCommand( CdBringAll );
            CommandManager.RegisterCommand( CdPatrol );
            CommandManager.RegisterCommand( CdSpecPatrol );
            CommandManager.RegisterCommand( CdMute );
            CommandManager.RegisterCommand( CdUnmute );
            CommandManager.RegisterCommand( CdSpectate );
            CommandManager.RegisterCommand( CdUnspectate );
            CommandManager.RegisterCommand( CdSudo );
            CommandManager.RegisterCommand( CdTPDeny );
            CommandManager.RegisterCommand( CdJORW );
            CommandManager.RegisterCommand( CdMaxCaps );
            CommandManager.RegisterCommand( CdBack );
            CommandManager.RegisterCommand( CdVerify );
        }
        
        #region Calculator

        static readonly CommandDescriptor CdCalculator = new CommandDescriptor
        {
            Name = "Calculator",
            Aliases = new[] { "Calc" },
            Category = CommandCategory.New | CommandCategory.Info,
            Permissions = new Permission[] { Permission.Chat },
            IsConsoleSafe = true,
            Usage = "/Calculator [number] [+, -, *, /, sqrt, sqr] [(for +,-,*, or /)number]",
            Help = "Lets you use a simple calculator in minecraft. Valid options are [ + , - , * ,  / , sqrt, and sqr].",
            NotRepeatable = false,
            Handler = CalcHandler,
        };

        static void CalcHandler(Player player, CommandReader cmd)
        {
            String numberone = cmd.Next();
            String op = cmd.Next();
            String numbertwo = cmd.Next();
            double no1 = 1;
            double no2 = 1;

            if (numberone == null || op == null)
            {
                CdCalculator.PrintUsage(player);
                return;
            }

            if (!double.TryParse(numberone, out no1))
            {
                player.Message("Please choose from a whole number.");
                return;
            }
            if (numbertwo != null)
            {
                if (!double.TryParse(numbertwo, out no2))
                {
                    player.Message("Please choose from a whole number.");
                    return;
                }
            }


            if (player.Can(Permission.Chat))
            {

                if (numberone != null || op != null)
                {
                    if (op == "+" | op == "-" | op == "*" | op == "/" | op == "sqrt" | op == "sqr")
                    {

                        if (op == "+")
                        {
                            if (numbertwo == null)
                            {
                                player.Message("You must select a second number!");
                                return;
                            }
                            double add = no1 + no2;
                            if (add < 0 | no1 < 0 | no2 < 0)
                            {
                                player.Message("Negative Number Detected, please choose from a whole number.");
                                return;
                            }
                            else
                            {
                                player.Message("&0Calculator&f: {0} + {1} = {2}", no1, no2, add);
                            }
                        }
                        if (op == "-")
                        {
                            if (numbertwo == null)
                            {
                                player.Message("You must select a second number!");
                                return;
                            }
                            double subtr = no1 - no2;
                            if (subtr < 0 | no1 < 0 | no2 < 0)
                            {
                                player.Message("Negative Number Detected, please choose from a whole number.");
                                return;
                            }
                            else
                            {
                                player.Message("&0Calculator&f: {0} - {1} = {2}", no1, no2, subtr);
                            }
                        }
                        if (op == "*")
                        {
                            if (numbertwo == null)
                            {
                                player.Message("You must select a second number!");
                                return;
                            }
                            double mult = no1 * no2;
                            if (mult < 0 | no1 < 0 | no2 < 0)
                            {
                                player.Message("Negative Number Detected, please choose from a whole number.");
                                return;
                            }
                            else
                            {
                                player.Message("&0Calculator&f: {0} * {1} = {2}", no1, no2, mult);
                            }
                        }
                        if (op == "/")
                        {
                            if (numbertwo == null)
                            {
                                player.Message("You must select a second number!");
                                return;
                            }
                            double div = no1 / no2;
                            if (div < 0 | no1 < 0 | no2 < 0)
                            {
                                player.Message("Negative Number Detected, please choose from a whole number.");
                                return;
                            }
                            else
                            {
                                player.Message("&0Calculator&f: {0} / {1} = {2}", no1, no2, div);
                                return;
                            }
                        }
                        if (op == "sqrt")
                        {
                            double sqrt = Math.Round(Math.Sqrt(no1), 2);
                            if (no1 < 0)
                            {
                                player.Message("Negative Number Detected, please choose from a whole number.");
                                return;
                            }
                            else
                            {
                                player.Message("&0Calculator&f: Square Root of {0} = {1}", no1, sqrt);
                                return;
                            }
                        }
                        if (op == "sqr")
                        {
                            double sqr = no1 * no1;
                            if (no1 < 0)
                            {
                                player.Message("Negative Number Detected, please choose from a whole number.");
                                return;
                            }
                            else
                            {
                                player.Message("&0Calculator&f: Square of {0} = {1}", no1, sqr);
                                return;
                            }
                        }
                    }
                    else
                    {
                        player.Message("&cInvalid Operator. Please choose from '+' , '-' , '*' , '/' , 'sqrt' , or 'sqr'");
                        return;
                    }
                }
                else
                {
                    CdCalculator.PrintUsage(player);
                }
            }

        }
        #endregion
        #region Sudo
        static readonly CommandDescriptor CdSudo = new CommandDescriptor
        {
            Name = "Sudo",
            Category = CommandCategory.New | CommandCategory.Moderation | CommandCategory.Chat,
            Permissions = new[] { Permission.EditPlayerDB },
            Usage = "/Sudo [Player] [What to type in]",
            IsConsoleSafe = true,
            Help = "Forces the player to execute chat/commands",
            Handler = SudoHandler
        };

        static void SudoHandler(Player player, CommandReader cmd)
        {
            string ply = cmd.Next();
            if (!cmd.HasNext)
            {
                CdSudo.PrintUsage(player);
                return;
            }
            string sudocmd = cmd.NextAll();
            PlayerInfo p = PlayerDB.FindPlayerInfoOrPrintMatches(player, ply, SearchOptions.IncludeSelf);
            if (p == null)
            {
                return;
            }
            if (p.PlayerObject == null)
            {
                player.Message("This player is offline!");
                return;
            }
            try {
                p.PlayerObject.ParseMessage(sudocmd, false);
                player.Message("Forced {0} to type in \"{1}\"", p.Name, sudocmd);
            } catch {
                    player.Message("Cannot use that command with /sudo");
            }

        }
        #endregion
        #region Bot
        static readonly CommandDescriptor CdBot = new CommandDescriptor
        {
            Name = "Bot",
            Category = CommandCategory.New | CommandCategory.Chat,
            Permissions = new Permission[] { Permission.UseBot },
            Usage = "Bot [Option]",
            Help = "Bot options are &hGo&s, &hServer&s, &hJoke&s, &hTime&s, &hPromos&s, &hBans&s, &hKicks&s, &hBlocks&s, &hProtip&s, &hFunfact&s, and &hIdea&s.&n" +
                   "Type in &h/help bot [option] &sfor more information.&n" +
                   "&6Bot&s is our Automated response system, so please don't abuse it.",
            NotRepeatable = true,
            IsConsoleSafe = true,
            HelpSections = new Dictionary<string, string>{
                { "go",             "&sType: &f!Bot Go&n&S" +
                                    "Sets a 5 second timer to be used for games." +
                                    "Most useful for when there are no staff on." },
                { "server",         "&sType: &f!Bot Server&n&S" +
                                    "Displays the server name."},
                { "joke",           "&sType: &f!Bot Joke&n&S" +
                                    "Displays a joke."},
                { "time",           "&sType: &f!Bot Time&n&S" +
                                    "Displays the time you spent this game session." +
                                    "&sType: &f!Bot Time Total&n&S" +
                                    "Displays your Total Time spent on the server." },
                { "promos",         "&sType: &f!Bot Promos&n&S" +
                                    "Displays the amount of players you have promoted."},
                { "bans",           "&sType: &f!Bot Bans&n&S" +
                                    "Displays the amount of players you have banned."},
                { "kicks",          "&sType: &f!Bot Kicks&n&S" +
                                    "Displays the amount of players you have kicked."},
                { "blocks",         "&sType: &f!Bot Blocks&n&S" +
                                    "Displays the amount of blocks you have modified this session." +
                                    "&sType: &f!Bot Blocks Total&n&S" +
                                    "Displays the amount of blocks you have modified in Total." },
                { "protip",         "&sType: &f!Bot Protip&n&S" +
                                    "Displays a life changing tip.*" +
                                    "*May or may not change your life" },
                { "funfact",        "&sType: &f!Bot Funfact&n&S" +
                                    "Displays a funfact."},
                { "idea",           "&sType: &f!Bot Idea&n&S" +
                                    "Displays a random building idea"}
                
                
            },
            Handler = LeBotHandler,
        };

		private static void LeBotHandler(Player player, CommandReader cmd) {
			string cmdchat = cmd.Next();
			string option = cmd.Next();
			string helper = cmd.Next();
			if (cmdchat == null) {
				player.Message(CdBot.Help);
				return;
			}
			if (option != null)
				option = option.ToLower();
			if (cmdchat != "<CalledFromChat>") {
				cmd.Rewind();
				option = cmd.Next().ToLower();
				helper = cmd.Next();
				player.ParseMessage(string.Format("Bot {0} {1}", option ?? "", helper ?? ""), (player == Player.Console));
                return;
			}
			if (player.Info.TimeSinceLastServerMessage.TotalSeconds < 5) {
				player.Info.getLeftOverTime(5, cmd);
				return;
			}
			if (option == null) {
				player.Message(CdBot.Help);
				return;
			}
            if (player.Info.TimesUsedBot == 0) {
                player.Message(
                    "&6Bot&f: This is your first time using &6Bot&s, I suggest you use \"/Help Bot\" to further understand how I work.");
            }
            bool sentMessage = true;
            switch (option) {
				case "go":
					Scheduler.NewTask(t => Server.BotMessage("5")).RunManual(TimeSpan.FromSeconds(0));
					Scheduler.NewTask(t => Server.BotMessage("4")).RunManual(TimeSpan.FromSeconds(1));
					Scheduler.NewTask(t => Server.BotMessage("3")).RunManual(TimeSpan.FromSeconds(2));
					Scheduler.NewTask(t => Server.BotMessage("2")).RunManual(TimeSpan.FromSeconds(3));
					Scheduler.NewTask(t => Server.BotMessage("1")).RunManual(TimeSpan.FromSeconds(4));
					Scheduler.NewTask(t => Server.BotMessage("Go!")).RunManual(TimeSpan.FromSeconds(5));
					break;
				case "server":
					Server.BotMessage("The name of this server is " + ConfigKey.ServerName.GetString() + ".");
					break;
				case "joke":
					FileInfo jokeList = new FileInfo("./Bot/Jokes.txt");
					string[] jokeStrings;
					if (jokeList.Exists) {
						jokeStrings = File.ReadAllLines("./Bot/Jokes.txt");
					} else {
						Server.BotMessage("I cannot tell a joke at this time!");
						return;
					}
					Random RandjokeString = new Random();
					string joker = jokeStrings[RandjokeString.Next(0, jokeStrings.Length)];
					Server.BotMessage(joker);
					break;
				case "idea":
					FileInfo adjectiveList = new FileInfo("./Bot/Adjectives.txt");
					FileInfo nounList = new FileInfo("./Bot/Nouns.txt");
					string[] adjectiveStrings;
					string[] nounStrings;
					if (adjectiveList.Exists && nounList.Exists) {
						adjectiveStrings = File.ReadAllLines("./Bot/Adjectives.txt");
						nounStrings = File.ReadAllLines("./Bot/Nouns.txt");
					} else {
						Server.BotMessage("I cannot tell you a build idea at this time!");
						return;
					}
					Random randAdjectiveString = new Random();
					Random randNounString = new Random();
					string adjective = adjectiveStrings[randAdjectiveString.Next(0, adjectiveStrings.Length)];
					string noun = nounStrings[randNounString.Next(0, nounStrings.Length)];
					string ana = "a";
					if (adjective.StartsWith("a") || adjective.StartsWith("e") || adjective.StartsWith("i") ||
						adjective.StartsWith("o") || adjective.StartsWith("u")) {
						ana = "an";
					} else if (noun.EndsWith("s")) {
						ana = "some";
					}
					Server.BotMessage("Build " + ana + " " + adjective + " " + noun);
					break;
				case "protip":
					FileInfo tipList = new FileInfo("./Bot/Protips.txt");
					string[] tipStrings;
					if (tipList.Exists) {
						tipStrings = File.ReadAllLines("./Bot/Protips.txt");
					} else {
						Server.BotMessage("I cannot tell a protip at this time!");
						return;
					}
					Random RandtipString = new Random();
					string tipper = tipStrings[RandtipString.Next(0, tipStrings.Length)];
					Server.BotMessage(tipper);
					break;
				case "time":
					TimeSpan time = player.Info.TotalTime;
					TimeSpan timenow = player.Info.TimeSinceLastLogin;
					if (helper == "total") {
						Server.BotMessage(player.ClassyName + "&f has spent a total of {0:F2} hours on this server.", time.TotalHours);
					} else {
						Server.BotMessage(player.ClassyName + "&f has played a total of {0:F2} minutes this session.", timenow.TotalMinutes);
					}
					break;
				case "promos":
					if (player.Info.Rank.Can(Permission.Promote) || player.Info.PromoCount != 0) {
						Server.BotMessage(player.ClassyName + "&f has promoted " + player.Info.PromoCount + " players.");
					} else {
						Server.BotMessage(player.ClassyName + "&f cannot promote players yet");
					}
					break;
				case "bans":
					if (player.Info.Rank.Can(Permission.Ban) || player.Info.TimesBannedOthers != 0) {
						Server.BotMessage(player.ClassyName + "&f has banned " + player.Info.TimesBannedOthers + " players.");
					} else {
						Server.BotMessage(player.ClassyName + "&f cannot ban yet");
					}
					break;
				case "kicks":
					if (player.Info.Rank.Can(Permission.Kick) || player.Info.TimesKickedOthers != 0) {
						Server.BotMessage(player.ClassyName + "&f has kicked " +
											   player.Info.TimesKickedOthers + " players.");
					} else {
						Server.BotMessage(player.ClassyName + "&f cannot kick yet");
					}
					break;
				case "blocks":
					if (helper == "total") {
						Server.BotMessage(player.ClassyName + "&f has built " + player.Info.BlocksBuilt +
											   " blocks, deleted " + player.Info.BlocksDeleted + " and drew " +
											   player.Info.BlocksDrawn + ".");
					} else {
						Server.BotMessage(player.ClassyName + "&f has built " +
											   player.Info.BlocksBuiltThisGame + " blocks and deleted " +
											   player.Info.BlocksDeletedThisGame + " blocks this session.");
					}
					break;
				case "funfact":
					FileInfo factList = new FileInfo("./Bot/Funfacts.txt");
					string[] factStrings;
					if (factList.Exists) {
						factStrings = File.ReadAllLines("./Bot/Funfacts.txt");
					} else {
						Server.BotMessage("I cannot tell a funfact at this time!");
						return;
					}
					Random RandfactString = new Random();
					string facter = factStrings[RandfactString.Next(0, factStrings.Length)];
					Server.BotMessage(facter);
					break;
				default:
                    player.Message(CdBot.Help);
                    sentMessage = false;
					break;
			}
            if (sentMessage) {
                player.Info.LastServerMessageDate = DateTime.Now;
                player.Info.TimesUsedBot = (player.Info.TimesUsedBot + 1);
            }
		}

        #endregion        
        #region Ban / Unban

        static readonly CommandDescriptor CdBan = new CommandDescriptor
        {
            Name = "Ban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/Ban PlayerName [Reason]",
            Help = "Bans a specified player by name. Note: Does NOT ban IP. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = BanHandler
        };

        static void BanHandler(Player player, CommandReader cmd)
        {
            string targetName = cmd.Next();
            if (targetName == null)
            {
                CdBan.PrintUsage(player);
                return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player,
                                                                       targetName,
                                                                       SearchOptions.ReturnSelfIfOnlyMatch);
            if (target == null) return;
            if (target == player.Info)
            {
                player.Message("You cannot &H/Ban&S yourself.");
                return;
            }
            string reason = cmd.NextAll();
            try
            {
                Player targetPlayer = target.PlayerObject;
                target.Ban(player, reason, true, true);
                WarnIfOtherPlayersOnIP(player, target, targetPlayer);
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
                if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                {
                    FreezeIfAllowed(player, target);
                }
            }
        }



        static readonly CommandDescriptor CdBanIP = new CommandDescriptor
        {
            Name = "BanIP",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/BanIP PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name and IP. If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanIPHandler
        };

        static void BanIPHandler(Player player, CommandReader cmd)
        {
            string targetNameOrIP = cmd.Next();
            if (targetNameOrIP == null)
            {
                CdBanIP.PrintUsage(player);
                return;
            }
            string reason = cmd.NextAll();

            IPAddress targetAddress;
            if (IPAddressUtil.IsIP(targetNameOrIP) && IPAddress.TryParse(targetNameOrIP, out targetAddress))
            {
                try
                {
                    targetAddress.BanIP(player, reason, true, true);
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                }
            }
            else
            {
                PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player,
                                                                           targetNameOrIP,
                                                                           SearchOptions.ReturnSelfIfOnlyMatch);
                if (target == null) return;
                if (target == player.Info)
                {
                    player.Message("You cannot &H/BanIP&S yourself.");
                    return;
                }
                try
                {
                    if (target.LastIP.Equals(IPAddress.Any) || target.LastIP.Equals(IPAddress.None))
                    {
                        target.Ban(player, reason, true, true);
                    }
                    else
                    {
                        target.BanIP(player, reason, true, true);
                    }
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                    if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                    {
                        FreezeIfAllowed(player, target);
                    }
                }
            }
        }



        static readonly CommandDescriptor CdBanAll = new CommandDescriptor
        {
            Name = "BanAll",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/BanAll PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name, IP, and all other names associated with the IP. " +
                   "If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanAllHandler
        };

        static void BanAllHandler(Player player, CommandReader cmd)
        {
            string targetNameOrIP = cmd.Next();
            if (targetNameOrIP == null)
            {
                CdBanAll.PrintUsage(player);
                return;
            }
            string reason = cmd.NextAll();

            IPAddress targetAddress;
            if (IPAddressUtil.IsIP(targetNameOrIP) && IPAddress.TryParse(targetNameOrIP, out targetAddress))
            {
                try
                {
                    targetAddress.BanAll(player, reason, true, true);
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                }
            }
            else
            {
                PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player,
                                                                           targetNameOrIP,
                                                                           SearchOptions.ReturnSelfIfOnlyMatch);
                if (target == null) return;
                if (target == player.Info)
                {
                    player.Message("You cannot &H/BanAll&S yourself.");
                    return;
                }
                try
                {
                    if (target.LastIP.Equals(IPAddress.Any) || target.LastIP.Equals(IPAddress.None))
                    {
                        target.Ban(player, reason, true, true);
                    }
                    else
                    {
                        target.BanAll(player, reason, true, true);
                    }
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                    if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                    {
                        FreezeIfAllowed(player, target);
                    }
                }
            }
        }



        static readonly CommandDescriptor CdUnban = new CommandDescriptor
        {
            Name = "Unban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/Unban PlayerName [Reason]",
            Help = "Removes ban for a specified player. Does NOT remove associated IP bans. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanHandler
        };

        static void UnbanHandler(Player player, CommandReader cmd)
        {
            string targetName = cmd.Next();
            if (targetName == null)
            {
                CdUnban.PrintUsage(player);
                return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player,
                                                                       targetName,
                                                                       SearchOptions.ReturnSelfIfOnlyMatch);
            if (target == null) return;
            if (target == player.Info)
            {
                player.Message("You cannot &H/Unban&S yourself.");
                return;
            }
            string reason = cmd.NextAll();
            try
            {
                target.Unban(player, reason, true, true);
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
            }
        }



        static readonly CommandDescriptor CdUnbanIP = new CommandDescriptor
        {
            Name = "UnbanIP",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/UnbanIP PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name and last known IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanIPHandler
        };

        static void UnbanIPHandler(Player player, CommandReader cmd)
        {
            string targetNameOrIP = cmd.Next();
            if (targetNameOrIP == null)
            {
                CdUnbanIP.PrintUsage(player);
                return;
            }
            string reason = cmd.NextAll();

            try
            {
                IPAddress targetAddress;
                if (IPAddressUtil.IsIP(targetNameOrIP) && IPAddress.TryParse(targetNameOrIP, out targetAddress))
                {
                    targetAddress.UnbanIP(player, reason, true, true);
                }
                else
                {
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player,
                                                                               targetNameOrIP,
                                                                               SearchOptions.ReturnSelfIfOnlyMatch);
                    if (target == null) return;
                    if (target == player.Info)
                    {
                        player.Message("You cannot &H/UnbanIP&S yourself.");
                        return;
                    }
                    if (target.LastIP.Equals(IPAddress.Any) || target.LastIP.Equals(IPAddress.None))
                    {
                        target.Unban(player, reason, true, true);
                    }
                    else
                    {
                        target.UnbanIP(player, reason, true, true);
                    }
                }
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
            }
        }



        static readonly CommandDescriptor CdUnbanAll = new CommandDescriptor
        {
            Name = "UnbanAll",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/UnbanAll PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name, last known IP, and all other names associated with the IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanAllHandler
        };

        static void UnbanAllHandler(Player player, CommandReader cmd)
        {
            string targetNameOrIP = cmd.Next();
            if (targetNameOrIP == null)
            {
                CdUnbanAll.PrintUsage(player);
                return;
            }
            string reason = cmd.NextAll();

            try
            {
                IPAddress targetAddress;
                if (IPAddressUtil.IsIP(targetNameOrIP) && IPAddress.TryParse(targetNameOrIP, out targetAddress))
                {
                    targetAddress.UnbanAll(player, reason, true, true);
                }
                else
                {
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player,
                                                                               targetNameOrIP,
                                                                               SearchOptions.ReturnSelfIfOnlyMatch);
                    if (target == null) return;
                    if (target == player.Info)
                    {
                        player.Message("You cannot &H/UnbanAll&S yourself.");
                        return;
                    }
                    if (target.LastIP.Equals(IPAddress.Any) || target.LastIP.Equals(IPAddress.None))
                    {
                        target.Unban(player, reason, true, true);
                    }
                    else
                    {
                        target.UnbanAll(player, reason, true, true);
                    }
                }
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
            }
        }


        static readonly CommandDescriptor CdBanEx = new CommandDescriptor
        {
            Name = "BanEx",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/BanEx +PlayerName&S or &H/BanEx -PlayerName",
            Help = "Adds or removes an IP-ban exemption for an account. " +
                   "Exempt accounts can log in from any IP, including banned ones.",
            Handler = BanExHandler
        };

        static void BanExHandler(Player player, CommandReader cmd)
        {
            string playerName = cmd.Next();
            if (playerName == null || playerName.Length < 2 || (playerName[0] != '-' && playerName[0] != '+'))
            {
                CdBanEx.PrintUsage(player);
                return;
            }
            bool addExemption = (playerName[0] == '+');
            string targetName = playerName.Substring(1);
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, targetName, SearchOptions.Default);
            if (target == null) return;

            switch (target.BanStatus)
            {
                case BanStatus.Banned:
                    if (addExemption)
                    {
                        player.Message("Player {0}&S is currently banned. Unban before adding an exemption.",
                                        target.ClassyName);
                    }
                    else
                    {
                        player.Message("Player {0}&S is already banned. There is no exemption to remove.",
                                        target.ClassyName);
                    }
                    break;
                case BanStatus.IPBanExempt:
                    if (addExemption)
                    {
                        player.Message("IP-Ban exemption already exists for player {0}", target.ClassyName);
                    }
                    else
                    {
                        player.Message("IP-Ban exemption removed for player {0}",
                                        target.ClassyName);
                        target.BanStatus = BanStatus.NotBanned;
                        Logger.Log(LogType.UserActivity,
                                    "{0} removed a ban exemption for {1}", player.Name, target.Name);

                    }
                    break;
                case BanStatus.NotBanned:
                    if (addExemption)
                    {
                        player.Message("IP-Ban exemption added for player {0}",
                                        target.ClassyName);
                        target.BanStatus = BanStatus.IPBanExempt;
                        Logger.Log(LogType.UserActivity,
                                    "{0} added a ban exemption for {1}", player.Name, target.Name);
                    }
                    else
                    {
                        player.Message("No IP-Ban exemption exists for player {0}",
                                        target.ClassyName);
                    }
                    break;
            }
        }

        #endregion
        #region Kick

        static readonly CommandDescriptor CdKick = new CommandDescriptor {
            Name = "Kick",
            Aliases = new[] { "k" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Kick },
            Usage = "/Kick PlayerName [Reason]",
            Help = "Kicks the specified player from the server. " +
                   "Optional kick reason/message is shown to the kicked player and logged." +
                   "Kicking will also make them spawn in &fTutorial &sworld next time they log in." +
                   "(In the mean time it will show up as them not having read the rules in their info)",
            Handler = KickHandler
        };

        static void KickHandler( Player player, CommandReader cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                player.Message( "Usage: &H/Kick PlayerName [Message]" );
                return;
            }
            // find the target
            Player target = Server.FindPlayerOrPrintMatches(player, name, SearchOptions.Default);
            if( target == null ) return;

            string reason = cmd.NextAll();
            DateTime previousKickDate = target.Info.LastKickDate;
            string previousKickedBy = target.Info.LastKickByClassy;
            string previousKickReason = target.Info.LastKickReason;

            // do the kick
            try {
                Player targetPlayer = target;
                target.Kick( player, reason, LeaveReason.Kick, true, true, true );
                target.Info.HasRTR = false;
                WarnIfOtherPlayersOnIP( player, target.Info, targetPlayer );

            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
                if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                    FreezeIfAllowed( player, target.Info );
                }
                return;
            }

            // warn player if target has been kicked before
            if( target.Info.TimesKicked > 1 ) {
                player.Message( "Warning: {0}&S has been kicked {1} times before.",
                                target.ClassyName, target.Info.TimesKicked - 1 );
                if( previousKickDate != DateTime.MinValue ) {
                    player.Message( "Most recent kick was {0} ago, by {1}",
                                    DateTime.UtcNow.Subtract( previousKickDate ).ToMiniString(),
                                    previousKickedBy );
                }
                if( !String.IsNullOrEmpty( previousKickReason ) ) {
                    player.Message( "Most recent kick reason was: {0}",
                                    previousKickReason );
                }
            }
        }

        #endregion
        #region Changing Rank (Promotion / Demotion)

        static readonly CommandDescriptor CdRank = new CommandDescriptor
        {
            Name = "Rank",
            Aliases = new[] { "user", "promote", "demote" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Promote, Permission.Demote },
            AnyPermission = true,
            IsConsoleSafe = true,
            Usage = "/Rank PlayerName RankName [Reason]",
            Help = "Changes the rank of a player to a specified rank. " +
                   "Any text specified after the RankName will be saved as a memo.",
            Handler = RankHandler
        };

        static void RankHandler([NotNull] Player player, [NotNull] CommandReader cmd)
        {
            string name = cmd.Next();
            string newRankName = cmd.Next();

            // Check arguments
            if (name == null || newRankName == null)
            {
                CdRank.PrintUsage(player);
                player.Message("See &H/Ranks&S for list of ranks.");
                return;
            }

            // Parse rank name
            Rank newRank = RankManager.FindRank(newRankName);
            if (newRank == null)
            {
                player.MessageNoRank(newRankName);
                return;
            }

            if (name == "-")
            {
                if (player.LastUsedPlayerName != null)
                {
                    name = player.LastUsedPlayerName;
                }
                else
                {
                    player.Message("Cannot repeat player name: you haven't used any names yet.");
                    return;
                }
            }
            PlayerInfo targetInfo;

            // Find player by name
            if (name.StartsWith("!"))
            {
                name = name.Substring(1);
                Player target = Server.FindPlayerExact(player, name, SearchOptions.IncludeSelf);
                if (target == null)
                {
                    player.MessageNoPlayer(name);
                    return;
                }
                targetInfo = target.Info;
            }
            else
            {
                targetInfo = PlayerDB.FindPlayerInfoExact(name);
            }

            // Handle non-existent players
            if (targetInfo == null)
            {
                if (!player.Can(Permission.EditPlayerDB))
                {
                    player.MessageNoPlayer(name);
                    return;
                }
                if (!Player.IsValidPlayerName(name))
                {
                    player.MessageInvalidPlayerName(name);
                    CdRank.PrintUsage(player);
                    return;
                }
                if (cmd.IsConfirmed)
                {
                    if (newRank > RankManager.DefaultRank)
                    {
                        targetInfo = PlayerDB.AddFakeEntry(name, RankChangeType.Promoted);
                    }
                    else
                    {
                        targetInfo = PlayerDB.AddFakeEntry(name, RankChangeType.Demoted);
                    }
                }
                else
                {
                    Logger.Log(LogType.UserActivity,
                                "Rank: Asked {0} to confirm adding unrecognized name \"{1}\" to the database.",
                                player.Name,
                                name);
                    player.Confirm(cmd,
                                    "Warning: Player \"{0}\" is not in the database (possible typo). Type the full name or",
                                    name);
                    return;
                }
            }
            try
            {
                player.LastUsedPlayerName = targetInfo.Name;
                Rank oldrank = targetInfo.Rank;
                targetInfo.ChangeRank(player, newRank, cmd.NextAll(), true, true, false);
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
            }            
        }

        #endregion
        #region Hide

        static readonly CommandDescriptor CdHide = new CommandDescriptor
        {
            Name = "Hide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/Hide [silent]",
            Help = "Enables invisible mode. It looks to other players like you left the server, " +
                   "but you can still do anything - chat, build, delete, type commands - as usual. " +
                   "Great way to spy on griefers and scare newbies. " +
                   "Call &H/Unhide&S to reveal yourself.",
            Handler = HideHandler
        };

		static void HideHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
			if (player.Info.IsHidden) {
				player.Message("You are already hidden.");
				return;
			}

			string silentString = cmd.NextAll();
			bool silent = (silentString.ToLower().Equals("silent") || silentString.ToLower().Equals("s"));

			player.Info.IsHidden = true;
			player.Message(silent ? "&8You are now hidden (silent)." : "&8You are now hidden.");

			// to make it look like player just logged out in /Info
			player.Info.LastSeen = DateTime.UtcNow;
			String quitMessage = (silentString.Length < 1 ? "" : "/Quit " + (silentString.Length > 64 ? silentString.Remove(64) : silentString));

			if (!silent && ConfigKey.ShowConnectionMessages.Enabled()) {
				if (!quitMessage.Equals("")) {
					player.quitmessage = quitMessage;
					quitMessage = String.Format(" (Reason: {0})", player.quitmessage);
					player.usedquit = true;
				}
				Server.Players.CantSee(player).Message("{0}&s left the server.{1}", player.ClassyName, quitMessage);
			}

			// for aware players: notify
			Server.Players.CanSee(player).Message("&SPlayer {0}&S is now hidden. {1}", player.ClassyName, quitMessage);
			Player.RaisePlayerHideChangedEvent(player, true, silent);
			foreach (Player p1 in Server.Players) {
				if (p1.Supports(CpeExtension.ExtPlayerList) || p1.Supports(CpeExtension.ExtPlayerList2)) {
					p1.Send(Packet.MakeExtRemovePlayerName(player.NameID));
				}
			}
			Server.UpdateTabList();
		}


        static readonly CommandDescriptor CdUnhide = new CommandDescriptor
        {
            Name = "Unhide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/Unhide [silent]",
            Help = "Disables the &H/Hide&S invisible mode. " +
                   "It looks to other players like you just joined the server.",
            Handler = UnhideHandler
        };

		static void UnhideHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
			World playerWorld = player.World;
			if (playerWorld == null)
				PlayerOpException.ThrowNoWorld(player);

			if (!player.Info.IsHidden) {
				player.Message("You are not currently hidden.");
				return;
			}
			bool silent = cmd.HasNext;

			// for aware players: notify
			Server.Players
				  .CanSee(player)
				  .Message("&SPlayer {0}&S is no longer hidden.",
							player.ClassyName);

			// for unaware players: fake a join message
			if (!silent) {
				if (ConfigKey.ShowConnectionMessages.Enabled()) {
					player.Info.GeoipLogin();
					string msg = Server.MakePlayerConnectedMessage(player, false, playerWorld);
					Server.Players.CantSee(player).Message(msg);
				}
			}
			player.Info.IsHidden = false;
			player.quitmessage = "/Quit";
			player.usedquit = false;
			if (silent) {
				player.Message("&8You are no longer hidden (silent).");
			} else {
				player.Message("&8You are no longer hidden.");
			}

			Player.RaisePlayerHideChangedEvent(player, false, silent);
			foreach (Player p1 in Server.Players) {
				if (p1.Supports(CpeExtension.ExtPlayerList) || p1.Supports(CpeExtension.ExtPlayerList2)) {
					p1.Send(Packet.MakeExtRemovePlayerName(player.NameID));
				}
			}
			Server.UpdateTabList();
		}

        #endregion
        #region Set Spawn

        static readonly CommandDescriptor CdSetSpawn = new CommandDescriptor {
            Name = "SetSpawn",
            Category = CommandCategory.Moderation | CommandCategory.World,
            Permissions = new[] { Permission.SetSpawn },
            Help = "Assigns your current location to be the spawn point of the map/world. " +
                   "If an optional PlayerName param is given, the spawn point of only that player is changed instead.",
            Usage = "/SetSpawn [PlayerName]",
            Handler = SetSpawnHandler
        };

        static void SetSpawnHandler( Player player, CommandReader cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );


            string playerName = cmd.Next();
            if( playerName == null ) {
                Map map = player.WorldMap;
                map.Spawn = player.Position;
                player.TeleportTo( map.Spawn );
                player.Send( Packet.MakeAddEntity( Packet.SelfId, player.ListName, player.Position ) );
                player.Message( "New spawn point saved." );
                Logger.Log( LogType.UserActivity,
                            "{0} changed the spawned point.",
                            player.Name );

            } else if( player.Can( Permission.Bring ) ) {
                Player[] infos = playerWorld.FindPlayers( player, playerName );
                if( infos.Length == 1 ) {
                    Player target = infos[0];
                    player.LastUsedPlayerName = target.Name;
                    if( player.Can( Permission.Bring, target.Info.Rank ) ) {
                        target.Send( Packet.MakeAddEntity( Packet.SelfId, target.ListName, player.Position ) );
                    } else {
                        player.Message( "You may only set spawn of players ranked {0}&S or lower.",
                                        player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                        player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
                    }

                } else if( infos.Length > 0 ) {
                    player.MessageManyMatches( "player", infos );

                } else {
                    infos = Server.FindPlayers(playerName, SearchOptions.IncludeSelf);
                    if( infos.Length > 0 ) {
                        player.Message( "You may only set spawn of players on the same world as you." );
                    } else {
                        player.MessageNoPlayer( playerName );
                    }
                }
            } else {
                player.MessageNoAccess( CdSetSpawn );
            }
        }

        #endregion
        #region Freeze

        static readonly CommandDescriptor CdFreeze = new CommandDescriptor
        {
            Name = "Freeze",
            Aliases = new[] { "f" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/Freeze PlayerName",
            Help = "Freezes the specified player in place. " +
                   "This is usually effective, but not hacking-proof. " +
                   "To release the player, use &H/unfreeze PlayerName",
            Handler = FreezeHandler
        };

        static void FreezeHandler(Player player, CommandReader cmd)
        {
            string targetName = cmd.Next();
            if (targetName == null)
            {
                CdFreeze.PrintUsage(player);
                return;
            }

            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player,
                                                                       targetName,
                                                                       SearchOptions.ReturnSelfIfOnlyMatch);
            if (target == null) return;
            if (target == player.Info)
            {
                player.Message("You cannot &H/Freeze&S yourself.");
                return;
            }

            try
            {
                target.Freeze(player, true, true);
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
            }
        }


        static readonly CommandDescriptor CdUnfreeze = new CommandDescriptor
        {
            Name = "Unfreeze",
            Aliases = new[] { "uf" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/Unfreeze PlayerName",
            Help = "Releases the player from a frozen state. See &H/Help Freeze&S for more information.",
            Handler = UnfreezeHandler
        };

        static void UnfreezeHandler(Player player, CommandReader cmd)
        {
            string targetName = cmd.Next();
            if (targetName == null)
            {
                CdFreeze.PrintUsage(player);
                return;
            }

            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player,
                                                                       targetName,
                                                                       SearchOptions.ReturnSelfIfOnlyMatch);
            if (target == null) return;
            if (target == player.Info)
            {
                player.Message("You cannot &H/Unfreeze&S yourself.");
                return;
            }

            try
            {
                target.Unfreeze(player, true, true);
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
            }
        }

        #endregion
        #region TPDeny
        static readonly CommandDescriptor CdTPDeny = new CommandDescriptor
        {
            Name = "TPDeny",
            Category = CommandCategory.New | CommandCategory.Moderation,
            Aliases = new[] { "tpd", "teleportdeny", "tptoggle" },
            Permissions = new[] { Permission.ReadStaffChat },
            Usage = "/TPDeny On/Off",
            Help = "Determines if lower ranks can teleport to you or not.",
            Handler = TPDenyHandler
        };

        static void TPDenyHandler(Player player, CommandReader cmd)
        {
            if (cmd.HasNext)
            {
                string state = cmd.Next();
                if (state.ToLower() == "on" || state.ToLower() == "yes")
                {
                    player.Info.TPDeny = true;
                    player.Message("TPDeny: &2On");
                    player.Message("Lower ranks can no longer teleport to you.");
                    return;
                }
                if (state.ToLower() == "off" || state.ToLower() == "no")
                {
                    player.Info.TPDeny = false;
                    player.Message("TPDeny: &4Off");
                    player.Message("Lower ranks can now teleport to you.");
                    return;
                }
                if (state.ToLower() == "state" || state.ToLower() == "what" || state.ToLower() == "current")
                {
                    if (player.Info.TPDeny == false)
                    {
                        player.Message("TPDeny: &4Off");
                    }
                    if (player.Info.TPDeny == true)
                    {
                        player.Message("TPDeny: &2On");
                    }
                    return;
                }
                else
                {
                    CdTPDeny.PrintUsage(player);
                    return;
                }
            }
            if (player.Info.TPDeny == true)
            {
                player.Info.TPDeny = false;
                player.Message("TPDeny: &4Off");
                player.Message("Lower ranks can now teleport to you.");
                return;
            }
            else
            {
                player.Info.TPDeny = true;
                player.Message("TPDeny: &2On");
                player.Message("Lower ranks can no longer teleport to you.");
                return;
            }
        }
        #endregion
        #region TP

        static readonly CommandDescriptor CdTeleport = new CommandDescriptor {
            Name = "TP",
            Aliases = new[] { "teleport", "to" },
            Category = CommandCategory.New | CommandCategory.Moderation,
            Permissions = new[] { Permission.Teleport },
            Usage = "/TP PlayerName&S or &H/TP X Y Z [R L]&s or &h/TP Random",
            Help = "Teleports you to a specified player's location. " +
                   "If coordinates are given, teleports to that location." +
                   "Or teleports you to a random location at your own height level.",
            Handler = TeleportHandler
        };

        private static void TeleportHandler(Player player, CommandReader cmd) {
            string name = cmd.Next();
            if (name == null) {
                CdTeleport.PrintUsage(player);
                return;
            }
            if (player.World.Name.ToLower() == "maze") {
                player.Message("Hey no cheating!");
                return;
            }
            if (name == "zone") {
                string zoneName = cmd.Next();
                if (zoneName == null) {
                    player.Message("No zone name specified. See &H/Help tpzone");
                    return;
                } else {
                    Zone zone = player.World.Map.Zones.Find(zoneName);
                    if (zone == null) {
                        player.MessageNoZone(zoneName);
                        return;
                    }
                    int zoneX = (zone.Bounds.XMin + zone.Bounds.XMax)/2;
                    int zoneY = (zone.Bounds.YMin + zone.Bounds.YMax)/2;
                    int zoneZ = (zone.Bounds.ZMin + zone.Bounds.ZMax)/2;
                    retry2:
                    if (player.World.map.GetBlock(zoneX, zoneY, zoneZ - 1) == Block.Air) {
                        zoneZ = zoneZ - 1;
                        goto retry2;
                    }
                    retry:
                    if (player.World.map.GetBlock(zoneX, zoneY, zoneZ) != Block.Air ||
                        player.World.map.GetBlock(zoneX, zoneY, zoneZ + 1) != Block.Air) {
                        zoneZ = zoneZ + 1;
                        goto retry;
                    }
					Position zPos = new Position((zoneX) * 32 + 16, (zoneY) * 32 + 16, (zoneZ) * 32 + 64);
					if (player.World != null) {
						player.LastWorld = player.World;
						player.LastPosition = player.Position;
					}
                    player.TeleportTo((zPos));
                    player.Message("&sTeleporting you to zone " + zone.ClassyName);
                    return;
                }
            }
            if (name == "random" || name == "rand") {
                Random rand = new Random();
                int x = rand.Next(0, player.WorldMap.Width);
                int y = rand.Next(0, player.WorldMap.Length);
                int z = player.Position.Z/32 + 1;
                retry2:
                if (player.World.map.GetBlock(x, y, z - 3) == Block.Air) {
                    z = z - 1;
                    goto retry2;
                }
                retry:
                if (player.World.map.GetBlock(x, y, z - 2) != Block.Air ||
                    player.World.map.GetBlock(x, y, z - 1) != Block.Air) {
                    z = z + 1;
                    goto retry;
                }

				if (player.World != null) {
					player.LastWorld = player.World;
					player.LastPosition = player.Position;
				}
                player.TeleportTo(new Position {
                    X = (short) (x*32 + 16),
                    Y = (short) (y*32 + 16),
                    Z = (short) (z*32 + 16),
                    R = player.Position.R,
                    L = player.Position.L
                });
                player.Message("Teleported to: ({0}, {1}, {2})", x, y, z);
                return;
            }

            if (cmd.Next() != null) {
                cmd.Rewind();
                int x, y, z, rot, lot;
                rot = player.Position.R;
                lot = player.Position.L;
                if (cmd.NextInt(out x) && cmd.NextInt(out y) && cmd.NextInt(out z)) {
                    if (cmd.HasNext) {
                        if (cmd.HasNext) {
                            if (cmd.NextInt(out rot) && cmd.NextInt(out lot)) {
                                if (rot > 255 || rot < 0) {
                                    player.Message("R must be inbetween 0 and 255. Set to player R");
                                }
                                if (lot > 255 || lot < 0) {
                                    player.Message("L must be inbetween 0 and 255. Set to player L");
                                }
                            }
                        }
                    }
                    if (x <= -1024 || x >= 1024 || y <= -1024 || y >= 1024 || z <= -1024 || z >= 1024) {
                        player.Message("Coordinates are outside the valid range!");

					} else {
						if (player.World != null) {
							player.LastWorld = player.World;
							player.LastPosition = player.Position;
						}
                        player.TeleportTo(new Position {
                            X = (short) (x*32 + 16),
                            Y = (short) (y*32 + 16),
                            Z = (short) (z*32 + 48),
                            R = (byte) rot,
                            L = (byte) lot
                        });
                    }
                } else {
                    CdTeleport.PrintUsage(player);
                }

            } else {
                if (name == "-") {
                    if (player.LastUsedPlayerName != null) {
                        name = player.LastUsedPlayerName;
                    } else {
                        player.Message("Cannot repeat player name: you haven't used any names yet.");
                        return;
                    }
                }
                Player[] matches = Server.FindPlayers(player, name, SearchOptions.Default);
                if (matches.Length == 1) {
                    Player target = matches[0];
                    World targetWorld = target.World;
                    if (targetWorld == null) PlayerOpException.ThrowNoWorld(target);
                    if (target.Info.TPDeny && target.Info.Rank >= player.Info.Rank) {
                        player.Message("&CThis player does not want people teleporting to them");
                        player.Message("Cannot teleport to {0}&S.", target.ClassyName, targetWorld.ClassyName,
                            targetWorld.AccessSecurity.MinRank.ClassyName);
                        return;
                    }

					if (targetWorld == player.World) {
						if (player.World != null) {
							player.LastWorld = player.World;
							player.LastPosition = player.Position;
						}
                        player.TeleportTo(target.Position);

                    } else {
                        if (targetWorld.Name.StartsWith("PW_") &&
                            !targetWorld.AccessSecurity.ExceptionList.Included.Contains(player.Info)) {
                            player.Message(
                                "You cannot join due to that player being in a personal world that you cannot access.");
                            return;
                        }
                        switch (targetWorld.AccessSecurity.CheckDetailed(player.Info)) {
                            case SecurityCheckResult.Allowed:
                            case SecurityCheckResult.WhiteListed:
                                if (player.Info.Rank.Name == "Banned") {
                                    player.Message("&CYou can not change worlds while banned.");
                                    player.Message("Cannot teleport to {0}&S.", target.ClassyName,
                                        targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                    break;
                                }
                                if (targetWorld.IsFull) {
                                    player.Message("Cannot teleport to {0}&S because world {1}&S is full.",
                                        target.ClassyName, targetWorld.ClassyName);
                                    player.Message("Cannot teleport to {0}&S.", target.ClassyName,
                                        targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                    break;
                                }
                                player.StopSpectating();
                                player.JoinWorld(targetWorld, WorldChangeReason.Tp, target.Position);
                                break;
                            case SecurityCheckResult.BlackListed:
                                player.Message("Cannot teleport to {0}&S because you are blacklisted on world {1}",
                                    target.ClassyName, targetWorld.ClassyName);
                                break;
                            case SecurityCheckResult.RankTooLow:
                                if (player.Info.Rank.Name == "Banned") {
                                    player.Message("&CYou can not change worlds while banned.");
                                    player.Message("Cannot teleport to {0}&S.", target.ClassyName,
                                        targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                    break;
                                }

                                if (targetWorld.IsFull) {
                                    if (targetWorld.IsFull) {
                                        player.Message("Cannot teleport to {0}&S because world {1}&S is full.",
                                            target.ClassyName, targetWorld.ClassyName);
                                        player.Message("Cannot teleport to {0}&S.", target.ClassyName,
                                            targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                        break;
                                    }
                                    player.StopSpectating();
                                    player.JoinWorld(targetWorld, WorldChangeReason.Tp, target.Position);
                                    break;
                                }
                                player.Message("Cannot teleport to {0}&S because world {1}&S requires {2}+&S to join.",
                                    target.ClassyName, targetWorld.ClassyName,
                                    targetWorld.AccessSecurity.MinRank.ClassyName);
                                break;
                        }
                    }

                } else if (matches.Length > 1) {
                    player.MessageManyMatches("player", matches);

                }
            }
        }

        #endregion
        #region TPP

        static readonly CommandDescriptor CdTeleportP = new CommandDescriptor
        {
            Name = "TPP",
            Aliases = new[] { "teleportprecise", "tppos"},
            Category = CommandCategory.New | CommandCategory.Moderation,
            Permissions = new[] { Permission.Teleport },
            IsHidden = true,
            Usage = "/TPP X Y Z [R L]",
            Help = "Teleports to precise location, one block is 32 units.",
            Handler = TeleportPHandler
        };

        static void TeleportPHandler(Player player, CommandReader cmd)
        {
            int x, y, z;
            int rot = player.Position.R;
            int lot = player.Position.L;
            
            if (cmd.NextInt(out x) && cmd.NextInt(out y) && cmd.NextInt(out z)) {
                if (cmd.NextInt(out rot) && cmd.NextInt(out lot)) {
                    if (rot < 0 || rot > 255) {
                        player.Message("R must be inbetween 0 and 255, using player R");
                        rot = player.Position.R;
                    }
                    if (lot < 0 || lot > 255) {
                        player.Message("L must be inbetween 0 and 255, using player L");
                        lot = player.Position.L;
                    }
                }
                
                if (x < short.MinValue || x > short.MaxValue || y < short.MinValue ||
                    y > short.MaxValue || z < short.MinValue || z > short.MaxValue) {
                    player.Message("Coordinates are outside the valid range!");
                } else {
                    if (player.World != null) {
                        player.LastWorld = player.World;
                        player.LastPosition = player.Position;
                    }
                    player.TeleportTo(new Position((short)x, (short)y, (short)z, 
                                                   (byte)rot, (byte)lot));
                }
            } else {
                CdTeleportP.PrintUsage(player);
            }
        }

        #endregion
        #region Top

        static readonly CommandDescriptor CdTop = new CommandDescriptor {
            Name = "Top",
            Category = CommandCategory.New | CommandCategory.Moderation,
            Permissions = new[] { Permission.Teleport },
            Usage = "/Top",
            Help = "Teleports you to the top block where you are standing",
            Handler = TopHandler
        };

        private static void TopHandler(Player player, CommandReader cmd) {
            int x = player.Position.X/32;
            int y = player.Position.Y/32;
            int z = player.WorldMap.Height;
            retry2:
            if (player.World.map.GetBlock(x, y, z - 3) == Block.Air) {
                z = z - 1;
                goto retry2;
            }
            retry:
            if (player.World.map.GetBlock(x, y, z - 2) != Block.Air ||
                player.World.map.GetBlock(x, y, z - 1) != Block.Air) {
                z = z + 1;
                goto retry;
            }

            player.TeleportTo(new Position {
                X = (short) (x*32 + 16),
                Y = (short) (y*32 + 16),
                Z = (short) (z*32 + 16),
                R = player.Position.R,
                L = player.Position.L
            });
            player.Message("Teleported to top");
        }

        #endregion
        #region JoinOnRankWorld
        static readonly CommandDescriptor CdJORW = new CommandDescriptor
        {
            Name = "JoinOnRankWorld",
            Category = CommandCategory.New | CommandCategory.Moderation,
            Aliases = new[] { "jorw", "joinonrank", "jor"},
            Usage = "/JoinOnRank [On/Off/State] ",
            Help = "Determines if you spawn on your designated rank world or not.",
            Handler = JoinOnRankWorldHandler
        };

        static void JoinOnRankWorldHandler(Player player, CommandReader cmd)
        {
            if (!WorldManager.Worlds.Contains(player.Info.Rank.MainWorld)) {
                player.Message("Sorry, there is no main world for your rank at the moment.");
                return;                    
            }
            
            if (cmd.HasNext) {
                string state = cmd.Next().ToLower();
                if (state == "on" || state == "yes") {
                    SetAndPrintJoinOnWorldState(player, true);
                } else if (state == "off" || state == "no") {
                    SetAndPrintJoinOnWorldState(player, false);
                } else if (state == "state" || state == "what" || state == "current") {
                    string message = player.Info.JoinOnRankWorld ? "&2On" : "&4Off";
                    player.Message("JoinOnRankWorld: " + message );
                } else {
                    CdJORW.PrintUsage(player);
                }
            } else {
                SetAndPrintJoinOnWorldState(player, !player.Info.JoinOnRankWorld);
            }
        }
        
        static void SetAndPrintJoinOnWorldState(Player player, bool joinRankWorld) {
            player.Info.JoinOnRankWorld = joinRankWorld;
            player.Message("JoinOnRankWorld: Set to " + (joinRankWorld ? "&2On" : "&4Off"));
            World world = joinRankWorld ? player.Info.Rank.MainWorld : WorldManager.MainWorld;
            player.Message("You will now spawn on world {0}&s when you logon to the server.", world.ClassyName);
        }
        
        #endregion
        #region MaxCaps

        static readonly CommandDescriptor CdMaxCaps = new CommandDescriptor
        {
            Name = "MaxCaps",
            Aliases = new[] { "caps" },
            Permissions = new[] { Permission.Chat },
            Category = CommandCategory.New | CommandCategory.Moderation | CommandCategory.Chat,
            Help = "Changes/Displays the max amount of uppercase letters a rank can use in a message.",
            Usage = "/MaxCaps <Rank> <Amount>",
            Handler = MaxCapsHandler
        };

        static void MaxCapsHandler(Player player, CommandReader cmd)
        {
            string rname = cmd.Next();
            string rmax = cmd.Next();
            Rank prank = player.Info.Rank;
            if (player.Info.Rank.Can(Permission.ShutdownServer))
            {
                if (rname == null)
                {
                    if (prank.MaxCaps != 0 && prank.MaxCaps != 1)
                    {
                        player.Message("Rank ({0}&s) has a max of {1} uppercase letters/message.", prank.ClassyName, prank.MaxCaps);
                        return;
                    }
                    else if (prank.MaxCaps == 0)
                    {
                        player.Message("Rank ({0}&s) has no max.", prank.ClassyName);
                        return;
                    }
                    else if (prank.MaxCaps == 1)
                    {
                        player.Message("Rank ({0}&s) has a max of (RawMessage.Length / 2) uppercase letters/message.", prank.ClassyName);
                        return;
                    }
                }
                Rank rank = RankManager.FindRank(rname);
                if (rank == null)
                {
                    player.MessageNoRank(rname);
                    return;
                }
                if (rmax == null)
                {
                    if (rank.MaxCaps != 0 && rank.MaxCaps != 1)
                    {
                        player.Message("Rank ({0}&s) has a max of {1} uppercase letters/message.", rank.ClassyName, rank.MaxCaps);
                        return;
                    }
                    else if (rank.MaxCaps == 0)
                    {
                        player.Message("Rank ({0}&s) has no max.", rank.ClassyName);
                        return;
                    }
                    else if (rank.MaxCaps == 1)
                    {
                        player.Message("Rank ({0}&s) has a max of (RawMessage.Length / 2) uppercase letters/message.", rank.ClassyName);
                        return;
                    }
                }
                int mcaps;
                if (!int.TryParse(rmax, out mcaps))
                {
                    player.Message(CdMaxCaps.Usage);
                    return;
                }
                if (rank != null)
                {
                    rank.MaxCaps = mcaps;
                    player.Message("Set MaxCaps for rank ({0}&s) to {1} uppercase letters/message.", rank.ClassyName, rank.MaxCaps);
                    return;
                }
            }
            else
            {
                if (prank.MaxCaps != 0 && prank.MaxCaps != 1)
                {
                    player.Message("Rank ({0}&s) has a max of {1} uppercase letters/message.", prank.ClassyName, prank.MaxCaps);
                    return;
                }
                else if (prank.MaxCaps == 0)
                {
                    player.Message("Rank ({0}&s) has no max.", prank.ClassyName);
                    return;
                }
                else if (prank.MaxCaps == 1)
                {
                    player.Message("Rank ({0}&s) has a max of (RawMessage.Length / 2) uppercase letters/message.", prank.ClassyName);
                    return;
                }
                return;
            }
        }

        #endregion
        #region Bring / WorldBring / BringAll

        static readonly CommandDescriptor CdBring = new CommandDescriptor {
            Name = "Bring",
            IsConsoleSafe = true,
            Aliases = new[] { "summon", "fetch" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/Bring PlayerName [ToPlayer]",
            Help = "Teleports another player to your location. " +
                   "If the optional second parameter is given, teleports player to another player.",
            Handler = BringHandler
        };

        static void BringHandler( Player player, CommandReader cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdBring.PrintUsage( player );
                return;
            }

            // bringing someone to another player (instead of to self)
            string toName = cmd.Next();
            Player toPlayer = player;
            if( toName != null ) {
                toPlayer = Server.FindPlayerOrPrintMatches(player, toName, SearchOptions.IncludeSelf);
                if( toPlayer == null ) return;
            } else if( toPlayer.World == null ) {
                player.Message( "When used from console, /Bring requires both names to be given." );
                return;
            }

            World world = toPlayer.World;
            if( world == null ) PlayerOpException.ThrowNoWorld( toPlayer );

            Player target = Server.FindPlayerOrPrintMatches(player, name, SearchOptions.Default);
            if( target == null ) return;

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You may only bring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( target.World == world ) {
				// teleport within the same world

				if (target.World != null) {
					target.LastWorld = target.World;
					target.LastPosition = target.Position;
				}
                target.TeleportTo( toPlayer.Position );

            } else {
                // teleport to a different world
                SecurityCheckResult check = world.AccessSecurity.CheckDetailed( target.Info );
                if( check == SecurityCheckResult.RankTooLow ) {
                    if( player.CanJoin( world ) ) {
                        if( cmd.IsConfirmed ) {
                            BringPlayerToWorld( player, target, world, true, true );
                        } else {
                            //Allow banned players to be moved about...
                            if (target.Info.Rank.Name == "Banned")
                            {
                                player.Message("&sYou CAN move banned players about... It is considered bad form though...");
                            }
                            Logger.Log( LogType.UserActivity,
                                        "Bring: Asked {0} to confirm overriding world permissions to bring player {1} to world {2}",
                                        player.Name, target.Name, world.Name );
                            player.Confirm( cmd,
                                            "{0} {1}&S is ranked too low to join {2}&S. Override world permissions?",
                                            target.Info.Rank.ClassyName,
                                            target.ClassyName,
                                            world.ClassyName );
                        }
                    } else {
                        player.Message( "Neither you nor {0}&S are allowed to join world {1}",
                                        target.ClassyName, world.ClassyName );
                    }
                } else {
                    BringPlayerToWorld( player, target, world, false, true );
                }
            }
        }


        static readonly CommandDescriptor CdWorldBring = new CommandDescriptor {
            Name = "WBring",
            IsConsoleSafe = true,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/WBring PlayerName WorldName",
            Help = "Teleports a player to the given world's spawn.",
            Handler = WorldBringHandler
        };

        static void WorldBringHandler( Player player, CommandReader cmd ) {
            string playerName = cmd.Next();
            string worldName = cmd.Next();
            if( playerName == null || worldName == null ) {
                CdWorldBring.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, playerName, SearchOptions.Default);
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );

            if( target == null || world == null ) return;

            if (target == player)
            {
                player.Message( "&WYou cannot &H/WBring&W yourself." );
                return;
            }

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You may only bring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( world == target.World ) {
                player.Message( "{0}&S is already in world {1}&S. They were brought to spawn.",
								target.ClassyName, world.ClassyName);

				if (target.World != null) {
					target.LastWorld = target.World;
					target.LastPosition = target.Position;
				}
                target.TeleportTo( target.WorldMap.Spawn );
                return;
            }

            SecurityCheckResult check = world.AccessSecurity.CheckDetailed( target.Info );
            if( check == SecurityCheckResult.RankTooLow ) {
                if( player.CanJoin( world ) ) {
                    if( cmd.IsConfirmed ) {
                        BringPlayerToWorld( player, target, world, true, false );
                    } else {
                        //Allow banned players to be moved about...
                        if (target.Info.Rank.Name == "Banned")
                        {
                            player.Message("&sYou CAN move banned players about... It is considered bad form though...");
                        }
                        Logger.Log( LogType.UserActivity,
                                    "WBring: Asked {0} to confirm overriding world permissions to bring player {1} to world {2}",
                                    player.Name, target.Name, world.Name );
                        player.Confirm( cmd,
                                        "{0} {1}&S is ranked too low to join {2}&S. Override world permissions?",
                                        target.Info.Rank.ClassyName,
                                        target.ClassyName,
                                        world.ClassyName );
                    }
                } else {
                    player.Message( "Neither you nor {0}&S are allowed to join world {1}",
                                    target.ClassyName, world.ClassyName );
                }
            } else {
                //Allow banned players to be moved about...
                if (target.Info.Rank.Name == "Banned")
                {
                    player.Message("&sYou CAN move banned players about... It is considered bad form though...");
                }
                BringPlayerToWorld( player, target, world, false, false );
            }
        }


        static readonly CommandDescriptor CdBringAll = new CommandDescriptor {
            Name = "BringAll",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring, Permission.BringAll },
            Usage = "/BringAll [@Rank [@AnotherRank]] [*|World [AnotherWorld]]",
            Help = "Teleports all players from your world to you. " +
                   "If any world names are given, only teleports players from those worlds. " +
                   "If any rank names are given, only teleports players of those ranks.",
            Handler = BringAllHandler
        };

        static void BringAllHandler( Player player, CommandReader cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );

            List<World> targetWorlds = new List<World>();
            List<Rank> targetRanks = new List<Rank>();
            bool allWorlds = false;
            bool allRanks = true;

            // Parse the list of worlds and ranks
            string arg;
            while( (arg = cmd.Next()) != null ) {
                if( arg.StartsWith( "@" ) ) {
                    Rank rank = RankManager.FindRank( arg.Substring( 1 ) );
                    if( rank == null ) {
                        player.MessageNoRank( arg.Substring( 1 ) );
                        return;
                    } else {
                        if( player.Can( Permission.Bring, rank ) ) {
                            targetRanks.Add( rank );
                        } else {
                            player.Message( "&WYou are not allowed to bring players of rank {0}",
                                            rank.ClassyName );
                        }
                        allRanks = false;
                    }
                } else if( arg == "*" ) {
                    allWorlds = true;
                } else {
                    World world = WorldManager.FindWorldOrPrintMatches( player, arg );
                    if( world == null ) return;
                    targetWorlds.Add( world );
                }
            }

            // If no worlds were specified, use player's current world
            if( !allWorlds && targetWorlds.Count == 0 ) {
                targetWorlds.Add( player.World );
            }

            // Apply all the rank and world options
            HashSet<Player> targetPlayers;
            if( allRanks && allWorlds ) {
                targetPlayers = new HashSet<Player>( Server.Players );
            } else if( allWorlds ) {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( Player rankPlayer in Server.Players.Ranked( rank ) ) {
                        targetPlayers.Add( rankPlayer );
                    }
                }
            } else if( allRanks ) {
                targetPlayers = new HashSet<Player>();
                foreach( World world in targetWorlds ) {
                    foreach( Player worldPlayer in world.Players ) {
                        targetPlayers.Add( worldPlayer );
                    }
                }
            } else {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( World world in targetWorlds ) {
                        foreach( Player rankWorldPlayer in world.Players.Ranked( rank ) ) {
                            targetPlayers.Add( rankWorldPlayer );
                        }
                    }
                }
            }

            Rank bringLimit = player.Info.Rank.GetLimit( Permission.Bring );

            // Remove the player him/herself
            targetPlayers.Remove( player );

            int count = 0;


            // Actually bring all the players
            foreach( Player targetPlayer in targetPlayers.CanBeSeen( player )
                                                         .RankedAtMost( bringLimit ) ) {
                if( targetPlayer.World == player.World ) {
					// teleport within the same world
					if (targetPlayer.World != null) {
						targetPlayer.LastWorld = targetPlayer.World;
						targetPlayer.LastPosition = targetPlayer.Position;
					}
                    targetPlayer.TeleportTo( player.Position );
                    targetPlayer.Position = player.Position;
                    if( targetPlayer.Info.IsFrozen ) {
                        targetPlayer.Position = player.Position;
                    }

                } else {
                    // teleport to a different world
                    BringPlayerToWorld( player, targetPlayer, player.World, false, true );
                }
                count++;
            }

            // Check if there's anyone to bring
            if( count == 0 ) {
                player.Message( "No players to bring!" );
            } else {
                player.Message( "Bringing {0} players...", count );
            }
        }



        static void BringPlayerToWorld( [NotNull] Player player, [NotNull] Player target, [NotNull] World world,
                                        bool overridePermissions, bool usePlayerPosition ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( target == null ) throw new ArgumentNullException( "target" );
            if( world == null ) throw new ArgumentNullException( "world" );
            switch( world.AccessSecurity.CheckDetailed( target.Info ) ) {
                case SecurityCheckResult.Allowed:
                case SecurityCheckResult.WhiteListed:
                    //Allow banned players to be moved about...
                    if( world.IsFull ) {
                        player.Message( "Cannot bring {0}&S because world {1}&S is full.",
                                        target.ClassyName,
                                        world.ClassyName );
                        return;
                    }
                    target.StopSpectating();
                    if( usePlayerPosition ) {
                        target.JoinWorld( world, WorldChangeReason.Bring, player.Position );
                    } else {
                        target.JoinWorld( world, WorldChangeReason.Bring );
                    }
                    break;

                case SecurityCheckResult.BlackListed:
                    player.Message( "Cannot bring {0}&S because he/she is blacklisted on world {1}",
                                    target.ClassyName,
                                    world.ClassyName );
                    break;

                case SecurityCheckResult.RankTooLow:
                    if( overridePermissions ) {
                        //Allow banned players to be moved about...
                        if (target.Info.Rank.Name == "Banned")
                        {
                            player.Message("&sYou CAN move banned players about... It is considered bad form though...");
                        }
                        target.StopSpectating();
                        if( usePlayerPosition ) {
                            target.JoinWorld( world, WorldChangeReason.Bring, player.Position );
                        } else {
                            target.JoinWorld( world, WorldChangeReason.Bring );
                        }
                    } else {
                        player.Message( "Cannot bring {0}&S because world {1}&S requires {2}+&S to join.",
                                        target.ClassyName,
                                        world.ClassyName,
                                        world.AccessSecurity.MinRank.ClassyName );
                    }
                    break;
            }
        }

        #endregion
        #region Patrol & SpecPatrol

        static readonly CommandDescriptor CdPatrol = new CommandDescriptor {
            Name = "Patrol",
            Aliases = new[] { "pat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol },
            Help = "Teleports you to the next player in need of checking.",
            Handler = PatrolHandler
        };

        static void PatrolHandler( Player player, CommandReader cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            Player target = playerWorld.GetNextPatrolTarget( player );
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

			if (player.World != null) {
				player.LastWorld = player.World;
				player.LastPosition = player.Position;
			}
            player.TeleportTo( target.Position );
            player.Message( "Patrol: Teleporting to {0}", target.ClassyName );
        }


        static readonly CommandDescriptor CdSpecPatrol = new CommandDescriptor {
            Name = "SpecPatrol",
            Aliases = new[] { "spat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol, Permission.Spectate },
            Help = "Teleports you to the next player in need of checking.",
            Handler = SpecPatrolHandler
        };

        static void SpecPatrolHandler( Player player, CommandReader cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            Player target = playerWorld.GetNextPatrolTarget( player,
                                                             p => player.Can( Permission.Spectate, p.Info.Rank ),
                                                             true );
            if( target == null ) {
                player.Message( "Patrol: No one to spec-patrol in this world." );
                return;
            }

            target.LastPatrolTime = DateTime.UtcNow;
            player.Spectate( target );
        }

        #endregion
        #region Mute / Unmute

        static readonly TimeSpan MaxMuteDuration = TimeSpan.FromDays(700); // 100w0d

        static readonly CommandDescriptor CdMute = new CommandDescriptor
        {
            Name = "Mute",
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Mutes a player for a specified length of time.",
            Usage = "/Mute PlayerName Duration",
            Handler = MuteHandler
        };

        static void MuteHandler(Player player, CommandReader cmd)
        {
            string targetName = cmd.Next();
            string timeString = cmd.Next();
            TimeSpan duration;
           
            // validate command parameters
            if (targetName == null || timeString == null ||
                !timeString.TryParseMiniTimeSpan(out duration) || duration <= TimeSpan.Zero)
            {
                CdMute.PrintUsage(player);
                return;
            }

            // check if given time exceeds maximum (700 days)
            if (duration > MaxMuteDuration)
            {
                player.Message("Maximum mute duration is {0}.", MaxMuteDuration.ToMiniString());
                duration = MaxMuteDuration;
            }

            // find the target
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player,
                                                                       targetName,
                                                                       SearchOptions.ReturnSelfIfOnlyMatch);
            if (target == null) return;
            if (target == player.Info)
            {
                player.Message("You cannot &H/Mute&S yourself.");
                return;
            }

            // actually mute
            try
            {
                target.Mute(player, duration, true, true);
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
            }
        }


        static readonly CommandDescriptor CdUnmute = new CommandDescriptor
        {
            Name = "Unmute",
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Unmutes a player.",
            Usage = "/Unmute PlayerName",
            Handler = UnmuteHandler
        };

        static void UnmuteHandler(Player player, CommandReader cmd)
        {
            string targetName = cmd.Next();
            if (String.IsNullOrEmpty(targetName))
            {
                CdUnmute.PrintUsage(player);
                return;
            }

            // find target
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player,
                                                                       targetName,
                                                                       SearchOptions.ReturnSelfIfOnlyMatch);
            if (target == null) return;
            if (target == player.Info)
            {
                player.Message("You cannot &H/Unmute&S yourself.");
                return;
            }

            try
            {
                target.Unmute(player, true, true);
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
            }
        }

        #endregion
        #region Spectate / Unspectate

        static readonly CommandDescriptor CdSpectate = new CommandDescriptor {
            Name = "Spectate",
            Aliases = new[] { "follow", "spec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Usage = "/Spectate PlayerName",
            Handler = SpectateHandler
        };

        static void SpectateHandler( Player player, CommandReader cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                PlayerInfo lastSpec = player.LastSpectatedPlayer;
                if( lastSpec != null ) {
                    Player spec = player.SpectatedPlayer;
                    if( spec != null ) {
                        player.Message( "Now spectating {0}", spec.ClassyName );
                    } else {
                        player.Message( "Last spectated {0}", lastSpec.ClassyName );
                    }
                } else {
                    CdSpectate.PrintUsage( player );
                }
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, targetName, SearchOptions.Default);
            if( target == null ) return;

            if( target == player ) {
                player.Message( "You cannot spectate yourself." );
                return;
            }

            if( !player.Can( Permission.Spectate, target.Info.Rank ) ) {
                player.Message( "You may only spectate players ranked {0}&S or lower.",
                player.Info.Rank.GetLimit( Permission.Spectate ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( !player.Spectate( target ) ) {
                player.Message( "Already spectating {0}", target.ClassyName );
            }
        }


        static readonly CommandDescriptor CdUnspectate = new CommandDescriptor {
            Name = "Unspectate",
            Aliases = new[] { "unfollow", "unspec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            NotRepeatable = true,
            Handler = UnspectateHandler
        };

        static void UnspectateHandler( Player player, CommandReader cmd ) {
            if( !player.StopSpectating() ) {
                player.Message( "You are not currently spectating anyone." );
            }
        }

        #endregion
		#region Back

		static readonly CommandDescriptor CdBack = new CommandDescriptor {
			Name = "Back",
			Category = CommandCategory.New | CommandCategory.Moderation,
			Permissions = new[] { Permission.Teleport },
			Usage = "/Back",
			Help = "Teleports you to the last place you were before teleporting.",
			Handler = BackHandler
		};

		private static void BackHandler(Player player, CommandReader cmd) {
			if (player.LastPosition == null || player.LastWorld == null) {
				player.Message("Unknown last location!");
				return;
			}
			if (player.LastWorld != player.World) {
				player.JoinWorld(player.LastWorld, WorldChangeReason.ManualJoin, player.LastPosition);
			} else {
				player.TeleportTo(player.LastPosition);
				player.Message("Teleported to last location!");
			}
		}

		#endregion
        #region tempfreeze

        // freeze target if player is allowed to do so
        static void FreezeIfAllowed( Player player, PlayerInfo targetInfo ) {
            if( targetInfo.IsOnline && !targetInfo.IsFrozen && player.Can( Permission.Freeze, targetInfo.Rank ) ) {
                try {
                    targetInfo.Freeze( player, true, true );
                    player.Message( "{0}&S has been frozen while you retry.", targetInfo.ClassyName );
                } catch( PlayerOpException ) { }
            }
        }


        // warn player if others are still online from target's IP
        static void WarnIfOtherPlayersOnIP( Player player, PlayerInfo targetInfo, Player except ) {
            Player[] otherPlayers = Server.Players.FromIP( targetInfo.LastIP )
                                                  .Except( except )
                                                  .ToArray();
            if( otherPlayers.Length > 0 ) {
                player.Message( "&WWarning: Other player(s) share IP with {0}&W: {1}",
                                targetInfo.ClassyName,
                                otherPlayers.JoinToClassyString() );
            }
        }
        #endregion
        
        #region Verify
        
        static readonly CommandDescriptor CdVerify = new CommandDescriptor {
            Name = "Verify",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Aliases = new[] { "ver" },
            Permissions = new[] { Permission.ReadStaffChat },
            Usage = "/verify [PlayerName]",
            Help = "Used for when players with a last login date before Jan 1 2014 join with a classicube.net account, " +
                   "marks the account as verified as belonging to the person and allows them to use non-guest commands.&N" +
                   "&cNote:&s This should only be used after you have contacted the old account owner and " +
            	   "verified that they created a classicube.net account.&N" +
            	   "&cNote:&s This command can only be used by players of the highest rank.",
            Handler = VerifyHandler
        };

        static void VerifyHandler(Player player, CommandReader cmd) {
            string targetName = cmd.Next();
            if (targetName == null) {
                CdVerify.PrintUsage(player); return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player,
                                                                       targetName,
                                                                       SearchOptions.ReturnSelfIfOnlyMatch);
            if (target == null) return;
            if (target == player.Info) {
                player.Message("You cannot &H/verify&S yourself."); return;
            }
            
            if (player.Info.Rank != RankManager.HighestRank) {
                player.Message("You can only use &H/verify&S if you are the highest rank."); return;
            }
            target.ClassicubeVerified = true;
            player.Message(target.ClassyName + "&S is marked as having their account verified.");
        }
        
        #endregion
    }
}
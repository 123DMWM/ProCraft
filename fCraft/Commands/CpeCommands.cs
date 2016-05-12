// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace fCraft {
    
    /// <summary> Most commands for CPE extensions are here. </summary>
    static class CpeCommands {

        internal static void Init() {
            CommandManager.RegisterCommand(CdAFKModel);
            CommandManager.RegisterCommand(CdChangeModel);
            CommandManager.RegisterCommand(CdChangeSkin);
            CommandManager.RegisterCommand(Cdclickdistance);
            CommandManager.RegisterCommand(CdCustomColors);
            CommandManager.RegisterCommand(CdEntity);
            CommandManager.RegisterCommand(CdEnv);
            CommandManager.RegisterCommand(CdEnvPreset);
            CommandManager.RegisterCommand(CdGlobalBlock);
            CommandManager.RegisterCommand(CdLevelBlock);
            CommandManager.RegisterCommand(CdHackControl);
            CommandManager.RegisterCommand(CdListClients);
            CommandManager.RegisterCommand(CdMRD);
            CommandManager.RegisterCommand(Cdtex);
            CommandManager.RegisterCommand(CdtextHotKey);
            CommandManager.RegisterCommand(Cdweather);
            CommandManager.RegisterCommand(CdZoneShow);
        }

        internal static string[] validEntities =  { "chibi", "chicken", "creeper", "giant", "humanoid", 
        "human", "pig", "sheep", "skeleton", "spider", "zombie"
        };

        /// <summary> Ensures that the hex color has the correct length (1-6 characters)
        /// and character set (alphanumeric chars allowed). </summary>
        public static bool IsValidHex(string hex) {
            if (hex == null) throw new ArgumentNullException("hex");
            if (hex.StartsWith("#")) hex = hex.Remove(0, 1);
            if (hex.Length < 1 || hex.Length > 6) return false;
            for (int i = 0; i < hex.Length; i++) {
                char ch = hex[i];
                if (ch < '0' || ch > '9' &&
                    ch < 'A' || ch > 'F' &&
                    ch < 'a' || ch > 'f') {
                    return false;
                }
            }
            return true;
        }

        #region AddEntity

        static readonly CommandDescriptor CdEntity = new CommandDescriptor {
            Name = "Entity",
            Aliases = new[] { "AddEntity", "AddEnt", "Ent" },
            Permissions = new[] { Permission.BringAll },
            Category = CommandCategory.CPE | CommandCategory.World,
            Usage = "/ent <create / remove / removeAll / model / list / bring / skin>",
            Help = "Commands for manipulating entities. For help and usage for the individual options, use /help ent <option>.",
            HelpSections = new Dictionary<string, string>{
                { "create", "&H/Ent create <entity name> <model> <skin>&n&S" +
                                "Creates a new entity with the given name. Valid models are chibi, chicken, creeper, giant, human, pig, sheep, skeleton, spider, zombie, or any block ID/Name." },
                { "remove", "&H/Ent remove <entity name>&n&S" +
                                "Removes the given entity." },
                { "removeall", "&H/Ent removeAll&n&S" +
                                "Removes all entities from the server."},
                { "model", "&H/Ent model <entity name> <model>&n&S" +
                                "Changes the model of an entity to the given model. Valid models are chibi, chicken, creeper, giant, human, pig, sheep, skeleton, spider, zombie, or any block ID/Name."},
                { "list", "&H/Ent list&n&S" +
                                "Prints out a list of all the entites on the server."},
                 { "bring", "&H/Ent bring <entity name>&n&S" +
                                "Brings the given entity to you."},
                 { "skin", "&H/Ent skin <entity name> <skin url or name>&n&S" +
                                "Changes the skin of a bot."}
            },
            Handler = BotHandler,
        };

        private static void BotHandler(Player player, CommandReader cmd) {
            string option = cmd.Next();
            if (string.IsNullOrEmpty(option)) {
                CdEntity.PrintUsage(player);
                return;
            }

            if (option.ToLower() == "list") {
                player.Message("_Entities on {0}_", ConfigKey.ServerName.GetString());
                foreach (Bot botCheck in World.Bots) {
                    player.Message(botCheck.Name + " on " + botCheck.World.Name);
                }
                return;
            }
            if (option.ToLower() == "removeall") {
                if (cmd.IsConfirmed) {
                    foreach (Bot b in World.Bots) {
                        b.World.Players.Send(Packet.MakeRemoveEntity(b.ID));
                        if (File.Exists("./Entities/" + b.Name.ToLower() + ".txt")) {
                            File.Delete("./Entities/" + b.Name.ToLower() + ".txt");
                        }
                    }
                    World.Bots.Clear();
                    player.Message("All entities removed.");
                } else {
                    player.Confirm(cmd, "This will remove all the entites everywhere, are you sure?");
                }
                return;
            }

            //finally away from the special cases
            string botName = cmd.Next();
            if (string.IsNullOrEmpty(botName)) {
                CdEntity.PrintUsage(player);
                return;
            }

            Bot bot = new Bot();
            if (option != "create" && option != "add") {
                bot = World.FindBot(botName.ToLower());
                if (bot == null) {
                    player.Message(
                        "Could not find {0}! Please make sure you spelled the entities name correctly. To view all the entities, type /ent list.",
                        botName);
                    return;
                }
            }

            switch (option.ToLower()) {
                case "create":
                case "add":
                    string requestedModel = "humanoid";
                    if (cmd.HasNext) {
                        requestedModel = cmd.Next().ToLower();
                        requestedModel = ParseModel(player, requestedModel);
                        if (requestedModel == null) {
                            player.Message(
                                "That wasn't a valid entity model! Valid models are chibi, chicken, creeper, giant, human, pig, sheep, skeleton, spider, zombie, or any block ID/Name.");
                            return;
                        }
                    }

                    //if a botname has already been chosen, ask player for a new name
                    var matchingNames = from b in World.Bots where b.Name.ToLower() == botName.ToLower() select b;

                    if (matchingNames.Count() > 0) {
                        player.Message("An entity with that name already exists! To view all entities, type /ent list.");
                        return;
                    }

                    string skinString1 = (cmd.Next() ?? botName);
                    if (skinString1 != null) {
                        if (skinString1.StartsWith("--")) {
                            skinString1 = string.Format("http://minecraft.net/skin/{0}.png", skinString1.Replace("--", ""));
                        }
                        if (skinString1.StartsWith("-+")) {
                            skinString1 = string.Format("http://skins.minecraft.net/MinecraftSkins/{0}.png", skinString1.Replace("-+", ""));
                        }
                        if (skinString1.StartsWith("++")) {
                            skinString1 = string.Format("http://i.imgur.com/{0}.png", skinString1.Replace("++", ""));
                        }
                    }
                    Bot botCreate = new Bot();
                    botCreate.setBot(botName, skinString1, requestedModel, player.World, player.Position, getNewID());
                    botCreate.createBot();
                    player.Message("Successfully created entity {0}&s with id:{1} and skin {2}.", botCreate.Name, botCreate.ID, skinString1 ?? bot.Name);
                    break;
                case "remove":
                    player.Message("{0} was removed from the server.", bot.Name);
                    bot.removeBot();
                    break;
                case "model":
                    if (cmd.HasNext) {
                        string model = cmd.Next().ToLower();
                        if (string.IsNullOrEmpty(model)) {
                            player.Message(
                                "Usage is /Ent model <bot> <model>. Valid models are chibi, chicken, creeper, giant, human, pig, sheep, skeleton, spider, zombie, or any block ID/Name.");
                            break;
                        }
                        model = ParseModel(player, model);
                        if (model == null) {
                            player.Message(
                                "That wasn't a valid entity model! Valid models are chibi, chicken, creeper, giant, human, pig, sheep, skeleton, spider, zombie, or any block ID/Name.");
                            break;
                        }
                        player.Message("Changed entity model to {0}.", model);
                        bot.changeBotModel(model);
                    } else {
                        player.Message(
                            "Usage is /Ent model <bot> <model>. Valid models are chibi, chicken, creeper, giant, human, pig, sheep, skeleton, spider, zombie, or any block ID/Name.");
                    }
                    break;
                case "bring":
                    bot.teleportBot(player.Position);
                    break;
                case "tp":
                case "teleport":
                    World targetWorld = bot.World;
                    Bot target = bot;
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
                                "You cannot join due to that Bot being in a personal world that you cannot access.");
                            break;
                        }
                        switch (targetWorld.AccessSecurity.CheckDetailed(player.Info)) {
                            case SecurityCheckResult.Allowed:
                            case SecurityCheckResult.WhiteListed:
                                if (player.Info.Rank.Name == "Banned") {
                                    player.Message("&CYou can not change worlds while banned.");
                                    player.Message("Cannot teleport to {0}&S.", target.Name,
                                        targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                    break;
                                }
                                if (targetWorld.IsFull) {
                                    player.Message("Cannot teleport to {0}&S because world {1}&S is full.",
                                        target.Name, targetWorld.ClassyName);
                                    player.Message("Cannot teleport to {0}&S.", target.Name,
                                        targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                    break;
                                }
                                player.StopSpectating();
                                player.JoinWorld(targetWorld, WorldChangeReason.Tp, target.Position);
                                break;
                            case SecurityCheckResult.BlackListed:
                                player.Message("Cannot teleport to {0}&S because you are blacklisted on world {1}",
                                    target.Name, targetWorld.ClassyName);
                                break;
                            case SecurityCheckResult.RankTooLow:
                                if (player.Info.Rank.Name == "Banned") {
                                    player.Message("&CYou can not change worlds while banned.");
                                    player.Message("Cannot teleport to {0}&S.", target.Name,
                                        targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                    break;
                                }

                                if (targetWorld.IsFull) {
                                    if (targetWorld.IsFull) {
                                        player.Message("Cannot teleport to {0}&S because world {1}&S is full.",
                                            target.Name, targetWorld.ClassyName);
                                        player.Message("Cannot teleport to {0}&S.", target.Name,
                                            targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                        break;
                                    }
                                    player.StopSpectating();
                                    player.JoinWorld(targetWorld, WorldChangeReason.Tp, target.Position);
                                    break;
                                }
                                player.Message("Cannot teleport to {0}&S because world {1}&S requires {2}+&S to join.",
                                    target.Name, targetWorld.ClassyName,
                                    targetWorld.AccessSecurity.MinRank.ClassyName);
                                break;
                        }
                    }
                    break;
                case "skin":
                    string skinString3 = cmd.Next();
                    if (string.IsNullOrEmpty(skinString3)) {
                        player.Message("Please specify a skin URL/Name.");
                        break;
                    } else { 
                        if (skinString3.StartsWith("--")) {
                            skinString3 = string.Format("http://minecraft.net/skin/{0}.png", skinString3.Replace("--", ""));
                        }
                        if (skinString3.StartsWith("-+")) {
                            skinString3 = string.Format("http://skins.minecraft.net/MinecraftSkins/{0}.png", skinString3.Replace("-+", ""));
                        }
                        if (skinString3.StartsWith("++")) {
                            skinString3 = string.Format("http://i.imgur.com/{0}.png", skinString3.Replace("++", ""));
                        }
                    }
                    player.Message("Changed entity skin to {0}.", skinString3 ?? bot.Name);
                    bot.changeBotSkin(skinString3);
                    break;
                default:
                    CdEntity.PrintUsage(player);
                    break;
            }
        }

        public static sbyte getNewID() {
            sbyte i = 1;
        go:
            foreach (Bot bot in World.Bots) {
                if (bot.ID == i) {
                    i++;
                    goto go;
                }
            }
            return i;
        }

        #endregion

        #region ChangeModel

        static readonly CommandDescriptor CdChangeModel = new CommandDescriptor {
            Name = "Model",
            Aliases = new[] { "ChangeModel", "cm" },
            Category = CommandCategory.CPE | CommandCategory.Moderation,
            Permissions = new[] { Permission.ReadStaffChat },
            Usage = "/Model [Player] [Model]",
            IsConsoleSafe = true,
            Help = "Change the Model or Skin of [Player]!&n" +
            "Valid models: &s[Any Block Name or ID#], Chibi, Chicken, Creeper, Giant, Humanoid, Pig, Sheep, Skeleton, Spider, Zombie",
            Handler = ModelHandler
        };

        private static void ModelHandler(Player player, CommandReader cmd) {
            SetModel(player, cmd, "", p => p.Mob, (p, value) => p.Mob = value);
        }

        static readonly CommandDescriptor CdAFKModel = new CommandDescriptor {
            Name = "AFKModel",
            Category = CommandCategory.New,
            Permissions = new[] { Permission.Chat },
            Usage = "/AFKModel [Player] [Model]",
            IsConsoleSafe = true,
            Help = "Change your own model for when you are AFK!&n" +
    "Valid models: &s [Any Block Name or ID#], Chibi, Chicken, Creeper, Giant, Humanoid, Pig, Sheep, Skeleton, Spider, Zombie!",
            Handler = AFKModelHandler
        };

        private static void AFKModelHandler(Player player, CommandReader cmd) {
            SetModel(player, cmd, "AFK ", 
                    p => p.PlayerObject.AFKModel, 
                    (p, value) => p.PlayerObject.AFKModel = value);
        }
        
        static void SetModel(Player player, CommandReader cmd, string prefix,
                             Func<PlayerInfo, string> getter, Action<PlayerInfo, string> setter) {
            PlayerInfo target = InfoCommands.FindPlayerInfo(player, cmd);
            if (target == null) return;

            if (!player.IsStaff && target != player.Info) {
                Rank staffRank = RankManager.GetMinRankWithAnyPermission(Permission.ReadStaffChat);
                if (staffRank != null) {
                    player.Message("You must be {0}&s+ to change another player's {1}Model", 
                                  staffRank.ClassyName, prefix);
                } else {
                    player.Message("No ranks have the ReadStaffChat permission," +
                                  "so no one can change other player's {0}Model, yell at the owner.", prefix);
                }
                return;
            }
            if (target.Rank.Index < player.Info.Rank.Index) {
                player.Message("Cannot change the {0}Model of someone higher rank than you.", prefix); return;
            }
            if (target != null && !target.IsOnline) {
                player.Message("Player is not currently online."); return;
            }
            if (target == null) {
               player.Message("Your current {0}Model: &f{1}", prefix, getter(player.Info)); return;
            }
            
            string model = cmd.Next();
            if (string.IsNullOrEmpty(model)) {
               player.Message("Current {2}Model for {0}: &f{1}", target.Name, getter(target), prefix); 
               return;
            }
            model = ParseModel(player, model);
            if (model == null) {
                player.Message("Model not valid, see &h/Help {0}Model&s.", prefix.TrimEnd());
                return;
            }
            if (getter(target).ToLower() == model.ToLower()) {
               player.Message("&f{0}&s's {0}model is already set to &f{1}", target.Name, model, prefix); 
               return;
            }
            
            if (target.IsOnline) {
               target.PlayerObject.Message("&f{0}&shanged your {3}model from &f{1} &sto &f{2}", 
                                           (target.PlayerObject == player ? "&sC" : player.Name + " &sC"), 
                                           getter(target), model, prefix);
            }
            if (target.PlayerObject != player) {
               player.Message("&sChanged {3}model of &f{0} &sfrom &f{1} &sto &f{2}", 
                              target.Name, getter(target), model, prefix);
            }
            target.oldMob = target.Mob;
            setter(target, model);
        }
        
        internal static string ParseModel(Player player, string model) {
            float scale = 0.0f;
            string scalestr = "";
            int sepIndex = model.IndexOf('|');
            if (sepIndex >= 0) {
                scalestr = model.Substring(sepIndex + 1);
                model = model.Substring(0, sepIndex);
            }
            if (float.TryParse(scalestr, out scale)) {
                if (scale < 0.25f) scale = 0.25f;
                if (scale > 3f) scale = 3f;
            }
            
            if (!validEntities.Contains(model.ToLower()) && !model.ToLower().StartsWith("dev:")) {
                byte blockId;
                Block block;
                if (byte.TryParse(model, out blockId)) {
                } else if (Map.GetBlockByName(model, false, out block)) {
                    model = block.GetHashCode().ToString();
                } else {
                    return null;
                }
            }
            
            if (model.ToLower().StartsWith("dev:")) {
                if (player != null)
                    player.Message("&cBe careful with development models, as they could crash others.");
                model = model.Remove(0, 4);
            }
            return scale == 0 ? model : model + "|" + scale;
        }

        static readonly CommandDescriptor CdChangeSkin = new CommandDescriptor {
            Name = "Skin",
            Aliases = new[] { "ChageSkin", "chs" },
            Category = CommandCategory.New | CommandCategory.Moderation,
            Permissions = new[] { Permission.EditPlayerDB },
            Usage = "/Skin [Player] [SkinName]",
            IsConsoleSafe = true,
            Help = "Change the Skin of [Player]!",
            Handler = SkinHandler
        };

        private static void SkinHandler(Player player, CommandReader cmd) {
            if (!cmd.HasNext) {
                CdChangeSkin.PrintUsage(player);
                return;
            }

            PlayerInfo p = InfoCommands.FindPlayerInfo(player, cmd);
            if (p == null) return;

            if (!cmd.HasNext) {
                CdChangeSkin.PrintUsage(player);
                return;
            }
            string skinString = cmd.Next();
            if (skinString != null) {
                if (skinString.StartsWith("--")) {
                    skinString = string.Format("http://minecraft.net/skin/{0}.png", skinString.Replace("--", ""));
                }
                if (skinString.StartsWith("-+")) {
                    skinString = string.Format("http://skins.minecraft.net/MinecraftSkins/{0}.png", skinString.Replace("-+", ""));
                }
                if (skinString.StartsWith("++")) {
                    skinString = string.Format("http://i.imgur.com/{0}.png", skinString.Replace("++", ""));
                }
            }

            if (p.skinName == skinString) {
                player.Message("&f{0}&s's skin is already set to &f{1}", p.Name, skinString);
                return;
            }
            if (p.IsOnline) {
                p.PlayerObject.Message("&f{0}&shanged your skin from &f{1} &sto &f{2}", (p.PlayerObject == player ? "&sC" : player.Name + " &sc"), p.oldskinName, skinString);
            }
            if (p.PlayerObject != player) {
                player.Message("&sChanged skin of &f{0} &sfrom &f{1} &sto &f{2}", p.Name, p.oldskinName, skinString);
            }
            p.oldskinName = p.skinName;
            p.skinName = skinString;
        }

        #endregion 

        #region ClickDistance

        static readonly CommandDescriptor Cdclickdistance = new CommandDescriptor {
            Name = "ReachDistance",
            Aliases = new[] { "Reach", "rd" },
            Permissions = new[] { Permission.DrawAdvanced },
            IsConsoleSafe = true,
            Category = CommandCategory.CPE | CommandCategory.World,
            Help = "Changes player reach distance. Every 32 is one block. Default: 160",
            Usage = "/reach [Player] [distance or reset]",
            Handler = ClickDistanceHandler
        };

        static void ClickDistanceHandler(Player player, CommandReader cmd) {
            PlayerInfo otherPlayer = InfoCommands.FindPlayerInfo(player, cmd);
            if (otherPlayer == null) return;

            if (!player.IsStaff && otherPlayer != player.Info) {
                Rank staffRank = RankManager.GetMinRankWithAnyPermission(Permission.ReadStaffChat);
                if (staffRank != null) {
                    player.Message("You must be {0}&s+ to change another players reach distance", staffRank.ClassyName);
                } else {
                    player.Message("No ranks have the ReadStaffChat permission so no one can change other players reachdistance, yell at the owner.");
                }
                return;
            }
            if (otherPlayer.Rank.Index < player.Info.Rank.Index) {
                player.Message("Cannot change the Reach Distance of someone higher rank than you.");
                return;
            }
            string second = cmd.Next();
            if (string.IsNullOrEmpty(second)) {
                if (otherPlayer == player.Info) {
                    player.Message("Your current ReachDistance: {0} blocks [Units: {1}]", player.Info.ReachDistance / 32, player.Info.ReachDistance);
                } else {
                    player.Message("Current ReachDistance for {2}: {0} blocks [Units: {1}]", otherPlayer.ReachDistance / 32, otherPlayer.ReachDistance, otherPlayer.Name);
                }
                return;
            }
            short distance;
            if (!short.TryParse(second, out distance)) {
                if (second != "reset") {
                    player.Message("Please try something inbetween 0 and 32767");
                    return;
                } else {
                    distance = 160;
                }
            }
            if (distance < 0 || distance > 32767) {
                player.Message("Reach distance must be between 0 and 32767");
                return;
            }

            if (distance != otherPlayer.ReachDistance) {
                if (otherPlayer != player.Info) {
                    if (otherPlayer.IsOnline == true) {
                        if (otherPlayer.PlayerObject.Supports(CpeExt.ClickDistance)) {
                            otherPlayer.PlayerObject.Message("{0} set your reach distance from {1} to {2} blocks [Units: {3}]", player.Name, otherPlayer.ReachDistance / 32, distance / 32, distance);
                            player.Message("Set reach distance for {0} from {1} to {2} blocks [Units: {3}]", otherPlayer.Name, otherPlayer.ReachDistance / 32, distance / 32, distance);
                            otherPlayer.ReachDistance = distance;
                            otherPlayer.PlayerObject.Send(Packet.MakeSetClickDistance(distance));
                        } else {
                            player.Message("This player does not support ReachDistance packet");
                        }
                    } else {
                        player.Message("Set reach distance for {0} from {1} to {2} blocks [Units: {3}]", otherPlayer.Name, otherPlayer.ReachDistance / 32, distance / 32, distance);
                        otherPlayer.ReachDistance = distance;
                    }
                } else {
                    if (player.Supports(CpeExt.ClickDistance)) {
                        player.Message("Set own reach distance from {0} to {1} blocks [Units: {2}]", player.Info.ReachDistance / 32, distance / 32, distance);
                        player.Info.ReachDistance = distance;
                        player.Send(Packet.MakeSetClickDistance(distance));
                    } else {
                        player.Message("You don't support ReachDistance packet");
                    }
                }
            } else {
                if (otherPlayer != player.Info) {
                    player.Message("{0}'s reach distance is already set to {1}", otherPlayer.ClassyName, otherPlayer.ReachDistance);
                } else {
                    player.Message("Your reach distance is already set to {0}", otherPlayer.ReachDistance);
                }
                return;
            }
        }

        #endregion

        #region CustomColors

        static readonly CommandDescriptor CdCustomColors = new CommandDescriptor {
            Name = "CustomColors",
            Aliases = new[] { "ccols" },
            Category = CommandCategory.CPE | CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            Usage = "/ccols [type] [args]",
            IsConsoleSafe = true,
            Help = "&sModifies the custom colors, or prints information about them.&n" +
                "&sTypes are: add, free, list, remove&n" +
                "&sSee &h/help ccols <type>&s for details about each type.",
            HelpSections = new Dictionary<string, string>{
                { "add",     "&h/ccols add [code] [name] [fallback] [hex]&n" +
                        "&scode is in ASCII. You cannot replace the standard color codes.&n" +
                        "&sfallback is a standard color code, shown to non-supporting clients.&n" },
                { "free",    "&h/ccols free&n" +
                        "&sPrints a list of free/unused available color codes." },
                { "list",    "&h/ccols list [offset]&n" +
                        "&sPrints a list of the codes, names, and fallback codes of the custom colors. " },
                { "remove",  "&h/ccols remove [code]&n" +
                        "&sRemoves the custom color which has the given color code." }
            },
            Handler = CustomColorsHandler,
        };

        static void CustomColorsHandler(Player p, CommandReader cmd) {
            string type = cmd.Next();
            if (type == null) { CdCustomColors.PrintUsage(p); return; }
            type = type.ToLower();

            if (type == "add" || type == "create" || type == "new") {
                AddCustomColorsHandler(p, cmd);
            } else if (type == "delete" || type == "remove") {
                RemoveCustomColorsHandler(p, cmd);
            } else if (type == "list") {
                ListCustomColorsHandler(p, cmd);
            } else if (type == "free" || type == "available") {
                FreeCustomColorsHandler(p);
            } else {
                CdCustomColors.PrintUsage(p);
            }
        }

        static void AddCustomColorsHandler(Player p, CommandReader cmd) {
            if (cmd.Count < 4) { p.Message("Usage: &H/ccols add [code] [name] [fallback] [hex]"); return; }
            if (!p.Can(Permission.DefineCustomBlocks)) {
                p.MessageNoAccess(Permission.DefineCustomBlocks); return;
            }

            string rawCode = cmd.Next();
            if (rawCode.Length > 1) {
                p.Message("Color code must only be one character long."); return;
            }
            char code = rawCode[0];
            if (Color.IsStandardColorCode(code)) {
                p.Message(code + " is a standard color code, and thus cannot be replaced."); return;
            }
            if (code <= ' ' || code > '~' || code == '%' || code == '&' || code == '\\' || code == '"' || code == '@') {
                p.Message(code + " must be a standard ASCII character.");
                p.Message("It also cannot be a space, percentage, ampersand, backslash, at symbol, or double quotes.");
                return;
            }
            if (Color.IsColorCode(code)) {
                p.Message("There is already a custom or server defined color with the code " + code +
                                   ", you must either use a different code or use \"&h/ccols remove " + code + "&s\"");
                return;
            }

            string name = cmd.Next();
            if (Color.Parse(name) != null) {
                p.Message("There is already an existing standard or " +
                                   "custom color with the name \"" + name + "\"."); return;
            }

            string rawFallback = cmd.Next();
            if (rawFallback.Length > 1) {
                p.Message("Fallback color code must only be one character long."); return;
            }
            char fallback = rawFallback[0];
            if (!Color.IsStandardColorCode(fallback)) {
                p.Message(fallback + " must be a standard color code."); return;
            }

            string hex = cmd.Next();
            if (hex.Length > 0 && hex[0] == '#')
                hex = hex.Substring(1);
            if (hex.Length != 6 || !IsValidHex(hex)) {
                p.Message("\"#" + hex + "\" is not a valid hex color."); return;
            }

            CustomColor col = default(CustomColor);
            col.Code = code; col.Fallback = fallback; col.A = 255;
            col.Name = name;
            System.Drawing.Color rgb = System.Drawing.ColorTranslator.FromHtml("#" + hex);
            col.R = rgb.R; col.G = rgb.G; col.B = rgb.B;
            Color.AddExtColor(col);
            p.Message("Successfully added a custom color. &{0} %{0} {1}", col.Code, col.Name.ToLower().UppercaseFirst());
        }

        static void RemoveCustomColorsHandler(Player p, CommandReader cmd) {
            if (cmd.Count < 2) { p.Message("Usage: &H/ccols remove [code]"); return; }
            if (cmd.RawMessage.Split()[2].Contains("\"")) {
                p.Message("Color code cannot be \"");
                return;
            }
            if (!p.Can(Permission.DefineCustomBlocks)) {
                p.MessageNoAccess(Permission.DefineCustomBlocks);
                return;
            }

            char code = cmd.Next()[0];
            if (Color.IsStandardColorCode(code)) {
                p.Message(code + " is a standard color, and thus cannot be removed."); return;
            }

            if ((int)code >= 256 || Color.ExtColors[code].Undefined) {
                p.Message("There is no custom color with the code " + code + ".");
                p.Message("Use \"&h/ccols list\" &Sto see a list of custom colors.");
                return;
            }
            Color.RemoveExtColor(code);
            p.Message("Successfully removed a custom color. {0}", code);
        }

        static void ListCustomColorsHandler(Player p, CommandReader cmd) {
            int offset = 0, index = 0, count = 0;
            cmd.NextInt(out offset);
            CustomColor[] cols = Color.ExtColors;

            for (int i = 0; i < cols.Length; i++) {
                CustomColor col = cols[i];
                if (col.Undefined) continue;

                if (index >= offset) {
                    count++;
                    const string format = "{0} - %{1} displays as &{1}{2}&s, and falls back to {3}.";
                    p.Message(format, col.Name, col.Code, Hex(col), col.Fallback);

                    if (count >= 8) {
                        const string helpFormat = "To see the next set of custom colors, type &h/ccols list {0}";
                        p.Message(helpFormat, offset + 8);
                        return;
                    }
                }
                index++;
            }
        }

        static void FreeCustomColorsHandler(Player p) {
            StringBuilder codes = new StringBuilder();
            for (char c = '!'; c <= '~'; c++) {
                if (IsValidCode(c)) codes.Append(c).Append(' ');
            }
            p.Message("Free codes: &f" + codes.ToString());
        }

        static bool IsValidCode(char code) {
            if (code == '%' || code == '&' || code == '\\' || code == '"' || code == '@')
                return false;
            return !Color.IsColorCode(code);
        }

        static string Hex(CustomColor c) {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
        #endregion

        #region Env

        static readonly CommandDescriptor CdEnv = new CommandDescriptor {
            Name = "Env",
            Category = CommandCategory.CPE | CommandCategory.World,
            Permissions = new[] { Permission.ManageWorlds },
            Help = "Prints or changes the environmental variables for a given world. " +
                   "Variables are: border, clouds, edge, fog, level, cloudsheight, shadow, sky, sunlight, texture, weather, maxfog. " +
                   "See &H/Help env <Variable>&S for details about each variable. " +
                   "Type &H/Env <WorldName> normal&S to reset everything for a world.",
            HelpSections = new Dictionary<string, string>{
                { "normal",     "&H/Env <WorldName> normal&n&S" +
                                "Resets all environment settings to their defaults for the given world." },
                { "clouds",     "&H/Env <WorldName> clouds <Color>&n&S" +
                                "Sets color of the clouds. Use \"normal\" instead of color to reset." },
                { "fog",        "&H/Env <WorldName> fog <Color>&n&S" +
                                "Sets color of the fog. Sky color blends with fog color in the distance. " +
                                "Use \"normal\" instead of color to reset." },
                { "shadow",     "&H/Env <WorldName> shadow <Color>&n&S" +
                                "Sets color of the shadowed areas. Use \"normal\" instead of color to reset." },
                { "sunlight",   "&H/Env <WorldName> sunlight <Color>&n&S" +
                                "Sets color of the lighted areas. Use \"normal\" instead of color to reset." },
                { "sky",        "&H/Env <WorldName> sky <Color>&n&S" +
                                "Sets color of the sky. Sky color blends with fog color in the distance. " +
                                "Use \"normal\" instead of color to reset." },
                { "level",      "&H/Env <WorldName> level <#>&n&S" +
                                "Sets height of the map edges/water level, in terms of blocks from the bottom of the map. " +
                                "Use \"normal\" instead of a number to reset to default (middle of the map)." },
                { "cloudsheight","&H/Env <WorldName> cloudsheight <#>&n&S" +
                                "Sets height of the clouds, in terms of blocks from the bottom of the map. " +
                                "Use \"normal\" instead of a number to reset to default (map height + 2)." },
                { "edge",       "&H/Env <WorldName> edge <BlockType>&n&S" +
                                "Changes the type of block that's visible beyond the map boundaries. "+
                                "Use \"normal\" instead of a number to reset to default (water)." },
                { "border",     "&H/Env <WorldName> border <BlockType>&n&S" +
                                "Changes the type of block that's visible on sides the map boundaries. "+
                                "Use \"normal\" instead of a number to reset to default (bedrock)." },
                { "texture",    "&H/Env <WorldName> texture <Texture .PNG Url>&n&S" +
                                "Changes the texture for all visible blocks on a map. "+
                                "Use \"normal\" instead of a web link to reset to default (" + Server.DefaultTerrain + ")" },
                { "weather",    "&H/Env <WorldName> weather <0,1,2/sun,rain,snow>&n&S" +
                                "Changes the weather on a specified map. "+
                                "Use \"normal\" instead to use default (0/sun)" },
                { "maxfog",     "&H/Env <WorldName> maxfog <#>&n&S" +
                                "Sets the maximum distance clients can see around them. " +
                                "Use \"normal\" instead of a number to reset to default (0)." }
            },
            Usage = "/Env <WorldName> <Variable>",
            IsConsoleSafe = true,
            Handler = EnvHandler
        };

        static void EnvHandler(Player player, CommandReader cmd) {
            string worldName = cmd.Next();
            World world;
            if (worldName == null) {
                world = player.World;
                if (world == null) {
                    player.Message("When used from console, /Env requires a world name.");
                    return;
                }
            } else {
                world = WorldManager.FindWorldOrPrintMatches(player, worldName);
                if (world == null) return;
            }

            string variable = cmd.Next(), value = cmd.Next();
            if (variable == null) {
                ShowEnvSettings(player, world);
                return;
            }

            if (variable.Equals("normal", StringComparison.OrdinalIgnoreCase)) {
                if (cmd.IsConfirmed) {
            		ResetEnv(player, world);
                } else {
                    Logger.Log(LogType.UserActivity,
                                "Env: Asked {0} to confirm resetting enviroment settings for world {1}",
                                player.Name, world.Name);
                    player.Confirm(cmd, "Reset enviroment settings for world {0}&S?", world.ClassyName);
                }
                return;
            }

            if (value == null) {
                CdEnv.PrintUsage(player);
                return;
            }
            if (value.StartsWith("#"))
                value = value.Remove(0, 1);

            switch (variable.ToLower()) {
                case "fog":
                    SetEnvColor(player, world, value, "fog color", EnvVariable.FogColor, ref world.FogColor);
                    break;
                case "cloud":
                case "clouds":
                    SetEnvColor(player, world, value, "cloud color", EnvVariable.CloudColor, ref world.CloudColor);
                    break;
                case "sky":
                    SetEnvColor(player, world, value, "sky color", EnvVariable.SkyColor, ref world.SkyColor);
                    break;
                case "dark":
                case "shadow":
                    SetEnvColor(player, world, value, "shadow color", EnvVariable.Shadow, ref world.ShadowColor);
                    break;
                case "sun":
                case "light":
                case "sunlight":
                    SetEnvColor(player, world, value, "sunlight color", EnvVariable.Sunlight, ref world.LightColor);
                    break;
                case "level":
                case "edgelevel":
                case "waterlevel":
                    SetEnvAppearanceShort(player, world, value, EnvProp.EdgeLevel,
                                          "water level", 0, ref world.EdgeLevel);
                    break;
                case "cloudheight":
                case "cloudsheight":
                    SetEnvAppearanceShort(player, world, value, EnvProp.CloudsLevel,
                                          "clouds height", 0, ref world.CloudsHeight);
                    break;
                case "fogdist":
                case "maxfog":
                case "maxdist":
                    SetEnvAppearanceShort(player, world, value, EnvProp.MaxFog,
                                          "max fog distance", 0, ref world.MaxFogDistance);
                    break;
                case "weatherspeed":
                    SetEnvAppearanceShort(player, world, value, EnvProp.WeatherSpeed,
                                          "weather speed", 256, ref world.WeatherSpeed);
                    break;
                case "cloudspeed":
                case "cloudsspeed":
                    SetEnvAppearanceShort(player, world, value, EnvProp.CloudsSpeed,
                                          "clouds speed", 256, ref world.CloudsSpeed);
                    break;
                case "horizon":
                case "edge":
                case "water":
                    SetEnvAppearanceBlock(player, world, value, EnvProp.EdgeBlock,
                                          "water block", Block.StillWater, ref world.HorizonBlock);
                    break;
                case "side":
                case "border":
                case "bedrock":
                    SetEnvAppearanceBlock(player, world, value, EnvProp.SidesBlock,
                                          "bedrock block", Block.Admincrete, ref world.EdgeBlock);
                    break;
                case "tex":
                case "terrain":
                case "texture":
                    if (value.ToLower() == "default") {
                        player.Message("Reset texture for {0}&S to {1}", world.ClassyName, Server.DefaultTerrain);
                        value = "Default";
                    } else if (!value.EndsWith(".png") && !value.EndsWith(".zip")) {
                        player.Message("Env Texture: Invalid image type. Please use a \".png\" or \".zip\"", world.ClassyName);
                        return;
                    } else if (!(value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                 value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))) {
                        player.Message("Env Texture: Invalid URL. Please use a \"http://\" or \"https://\" type url.", world.ClassyName);
                        return;
                    } else {
                        player.Message("Set texture for {0}&S to {1}", world.ClassyName, value);
                    }
                    world.Texture = value;
                    foreach (Player p in world.Players) {
                    	if (p.Supports(CpeExt.EnvMapAspect))
                            p.Send(Packet.MakeEnvSetMapUrl(world.GetTexture()));
                        else if (p.Supports(CpeExt.EnvMapAppearance) || p.Supports(CpeExt.EnvMapAppearance2))
                            p.SendEnvSettings();
                    }
                    break;

                case "weather":
                    byte weather = 0;
                    if (value.Equals("normal", StringComparison.OrdinalIgnoreCase)) {
                        player.Message("Reset weather for {0}&S to normal(0) ", world.ClassyName);
                        world.Weather = 0;
                    } else {
                        if (!byte.TryParse(value, out weather)) {
                            if (value.Equals("sun", StringComparison.OrdinalIgnoreCase)) {
                                weather = 0;
                            } else if (value.Equals("rain", StringComparison.OrdinalIgnoreCase)) {
                                weather = 1;
                            } else if (value.Equals("snow", StringComparison.OrdinalIgnoreCase)) {
                                weather = 2;
                            }
                        }
                        if (weather < 0 || weather > 2) {
                            player.Message("Please use a valid integer(0,1,2) or string(sun,rain,snow)");
                            return;
                        }
                        world.Weather = weather;
                        player.Message("&aSet weather for {0}&a to {1} ({2}&a)", world.ClassyName, weather, weather == 0 ? "&sSun" : (weather == 1 ? "&1Rain" : "&fSnow"));
                    }
                    foreach (Player p in world.Players) {
                        if (p.Supports(CpeExt.EnvWeatherType)) {
                            p.Send(Packet.SetWeather(world.Weather));
                        }
                    }
                    break;

                default:
                    CdEnv.PrintUsage(player);
                    return;
            }
            WorldManager.SaveWorldList();
        }
        
        static void ShowEnvSettings(Player player, World world) {
            player.Message("Environment settings for world {0}&S:", world.ClassyName);
            player.Message("  Cloud: {0}   Fog: {1}   Sky: {2}",
                           world.CloudColor == null ? "normal" : '#' + world.CloudColor,
                           world.FogColor == null ? "normal" : '#' + world.FogColor,
                           world.SkyColor == null ? "normal" : '#' + world.SkyColor);
            player.Message("  Shadow: {0}   Sunlight: {1}  Edge level: {2}",
                           world.ShadowColor == null ? "normal" : '#' + world.ShadowColor,
                           world.LightColor == null ? "normal" : '#' + world.LightColor,
                           world.GetEdgeLevel() + " blocks");
            player.Message("  Clouds height: {0}  Max fog distance: {1}",
                           world.GetCloudsHeight() + " blocks",
                           world.MaxFogDistance <= 0 ? "(no limit)" : world.MaxFogDistance.ToString());
            player.Message("  Cloud speed: {0}  Weather speed: {1}",
                           (world.CloudsSpeed / 256f) + " %",
                           (world.WeatherSpeed / 256f) + " %");
            player.Message("  Water block: {1}  Bedrock block: {0}",
                           world.EdgeBlock, world.HorizonBlock);
            player.Message("  Texture: {0}", world.GetTexture());
            if (!player.IsUsingWoM) {
                player.Message("  You need ClassiCube or ClassicalSharp client to see the changes.");
            }
        }
        
        static void ResetEnv(Player player, World world) {
            world.FogColor = null;
            world.CloudColor = null;
            world.SkyColor = null;
            world.ShadowColor = null;
            world.LightColor = null;
            world.EdgeLevel = -1;
            world.CloudsHeight = short.MinValue;
            world.MaxFogDistance = 0;
            world.EdgeBlock = (byte)Block.Admincrete;
            world.HorizonBlock = (byte)Block.Water;
            world.Texture = "Default";
            world.WeatherSpeed = 256;
            world.CloudsSpeed = 256;
            
            Logger.Log(LogType.UserActivity,
                       "Env: {0} {1} reset environment settings for world {2}",
                       player.Info.Rank.Name, player.Name, world.Name);
            player.Message("Enviroment settings for world {0} &swere reset.", world.ClassyName);
            WorldManager.SaveWorldList();
            foreach (Player p in world.Players) {
                if (p.Supports(CpeExt.EnvMapAppearance) || p.Supports(CpeExt.EnvMapAppearance2)
                    || p.Supports(CpeExt.EnvMapAspect))
                    p.SendEnvSettings();
            }
        }

        static void SetEnvColor(Player player, World world, string value, string name, EnvVariable variable, ref string target) {
            if (value.Equals("-1") || value.Equals("normal", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("reset", StringComparison.OrdinalIgnoreCase) || value.Equals("default", StringComparison.OrdinalIgnoreCase)) {
                player.Message("Reset {0} for {1}&S to normal", name, world.ClassyName);
                target = null;
            } else {
                if (!IsValidHex(value)) {
                    player.Message("Env: \"#{0}\" is not a valid HEX color code.", value);
                    return;
                } else {
                    target = value;
                    player.Message("Set {0} for {1}&S to #{2}", name, world.ClassyName, value);
                }
            }

            foreach (Player p in world.Players) {
                if (p.Supports(CpeExt.EnvColors))
                    p.Send(Packet.MakeEnvSetColor((byte)variable, target));
            }
        }

        static void SetEnvAppearanceShort(Player player, World world, string value, EnvProp prop,
                                          string name, short defValue, ref short target) {
            short amount;
            if (value.Equals("-1") || value.Equals("normal", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("reset", StringComparison.OrdinalIgnoreCase) 
                || value.Equals("default", StringComparison.OrdinalIgnoreCase)) {
                player.Message("Reset {0} for {1}&S to normal", name, world.ClassyName);
                target = defValue;
            } else {
                if (!short.TryParse(value, out amount)) {
                    player.Message("Env: \"{0}\" is not a valid integer.", value);
                    return;
                } else {
                    target = amount;
                    player.Message("Set {0} for {1}&S to {2}", name, world.ClassyName, amount);
                }
            }

            foreach (Player p in world.Players) {
            	if (p.Supports(CpeExt.EnvMapAspect))
                    p.Send(Packet.MakeEnvSetMapProperty(prop, target));
                else if (p.Supports(CpeExt.EnvMapAppearance) || p.Supports(CpeExt.EnvMapAppearance2))
                    p.SendEnvSettings();
            }
        }

        static void SetEnvAppearanceBlock(Player player, World world, string value, EnvProp prop,
                                          string name, Block defValue, ref byte target) {
            if (value.Equals("normal", StringComparison.OrdinalIgnoreCase) 
                || value.Equals("default", StringComparison.OrdinalIgnoreCase)) {
                player.Message("Reset {0} for {1}&S to normal ({2})", name, world.ClassyName, defValue);
                target = (byte)defValue;
            } else {
                Block block;
                if (!Map.GetBlockByName(world, value, false, out block)) {
                    CdEnv.PrintUsage(player);
                    return;
                }
                target = (byte)block;
                player.Message("Set {0} for {1}&S to {2}", name, world.ClassyName, block);
            }

            foreach (Player p in world.Players) {
        		if (p.Supports(CpeExt.EnvMapAspect))
                    p.Send(Packet.MakeEnvSetMapProperty(prop, target));
                else if (p.Supports(CpeExt.EnvMapAppearance) || p.Supports(CpeExt.EnvMapAppearance2))
                    p.SendEnvSettings();
            }
        }

        #endregion

        #region EnvPreset

        static readonly CommandDescriptor CdEnvPreset = new CommandDescriptor {
            Name = "EnvPreset",
            Aliases = new[] { "EnvPresets", "EnvP" },
            Category = CommandCategory.CPE | CommandCategory.World,
            Permissions = new[] { Permission.ManageWorlds },
            Help = "Environment preset commands" +
                   "Options are: Delete, Edit, Info, List, Load, Save" +
                   "See &H/Help EnvPreset <option>&S for details about each variable. ",
            HelpSections = new Dictionary<string, string>{
                { "save",   "&H/EnvPreset Save <PresetName> &n&S" +
                            "Saves Env settings to a defined preset name." },
                { "load",   "&H/EnvPreset Load <PresetName>&n&S" +
                            "Loads an Env preset to a specified world." },
                { "delete", "&H/EnvPreset Delete <PresetName>&n&S" +
                            "Deleted a defined Env preset." },
                { "info",   "&H/EnvPreset Info <PresetName>&n&S" +
                            "Displays Env settings of a defined Preset." },
                { "list",   "&H/EnvPreset List&n&S" +
                            "Lists all Env presets by name."},
                { "update", "&H/EnvPreset Update <PresetName>&n&S" +
                            "Updates an Env preset with the current world settings."}
            },
            Usage = "/EnvPreset <Option> [Args]",
            Handler = EnvPresetHandler
        };

        static void EnvPresetHandler(Player player, CommandReader cmd) {
            string option = cmd.Next();
            string args = cmd.NextAll();
            World world = player.World;
            EnvPresets preset;
            if (string.IsNullOrEmpty(option)) {
                CdEnvPreset.PrintUsage(player);
                return;
            }
            if (!option.ToLower().Equals("list") && !option.ToLower().Equals("reload") && string.IsNullOrEmpty(args)) {
                CdEnvPreset.PrintUsage(player);
                return;
            }

            string name = args.Split()[0];
            switch (option.ToLower()) {
                case "save":
                    if (!EnvPresets.exists(name)) {
                        EnvPresets.CreateEnvPreset(world, name);
                        player.Message("Saved Env settings from world \"{0}\" to preset named \"{1}\"", world.Name, name);
                        break;
                    } else {
                        player.Message("A preset with the name \"{0}\" already exists", name);
                        break;
                    }
                case "load":
                    if (EnvPresets.exists(name)) {
                        EnvPresets.LoadupEnvPreset(world, name);
                        player.Message("Loaded Env settings from preset named \"{0}\"", name);
                    } else {
                        player.Message("A preset with the name \"{0}\" does not exist", name);
                        break;
                    }
                    break;
                case "remove":
                    if (cmd.IsConfirmed) {
                        if (EnvPresets.exists(name)) {
                            EnvPresets.RemoveEnvPreset(name);
                            player.Message("Deleted Env preset named \"{0}\"", name);
                        } else {
                            player.Message("A preset with the name \"{0}\" does not exist", name);
                            break;
                        }
                    } else {
                        player.Confirm(cmd, "This will delete the preset permanently");
                        break;
                    }
                    break;
                case "update":
                    if (cmd.IsConfirmed) {
                        if (EnvPresets.exists(name)) {
                            EnvPresets.RemoveEnvPreset(name);
                            EnvPresets.CreateEnvPreset(world, name);
                            player.Message("Updated the preset \"{1}\" to the current worlds env settings", world.Name, name);
                        } else {
                            player.Message("A preset with the name \"{0}\" does not exist", name);
                            break;
                        }
                    } else {
                        player.Confirm(cmd, "This will erase and update the specified Env Preset");
                        break;
                    }
                    break;
                case "info":
                    if ((preset = EnvPresets.Find(args)) != null) {
                        player.Message("Environment settings for Preset {0}&S:", preset.Name);
                        player.Message("  Cloud: {0}   Fog: {1}   Sky: {2}",
                                        preset.CloudColor == null ? "normal" : '#' + preset.CloudColor,
                                        preset.FogColor == null ? "normal" : '#' + preset.FogColor,
                                        preset.SkyColor == null ? "normal" : '#' + preset.SkyColor);
                        player.Message("  Shadow: {0}   Light: {1}  Horizon level: {2}",
                                        preset.ShadowColor == null ? "normal" : '#' + preset.ShadowColor,
                                        preset.LightColor == null ? "normal" : '#' + preset.LightColor,
                                        preset.HorizonLevel <= 0 ? "normal" : preset.HorizonLevel + " blocks");
                        player.Message("  Clouds height: {0}  Max fog distance: {1}",
                                        preset.CloudLevel == short.MinValue ? "normal" : preset.CloudLevel + " blocks",
                                        preset.MaxViewDistance <= 0 ? "(no limit)" : preset.MaxViewDistance.ToString());
                        player.Message("  Horizon  block: {0}  Border block: {1}",
                                        preset.HorizonBlock, preset.BorderBlock);
                        player.Message("  Texture: {0}", preset.TextureURL);
                    }
                    break;
                case "list":
                    string list = "Presets: &n";
                    foreach (EnvPresets env in EnvPresets.Presets.OrderBy(p => p.Name)) {
                        list = list + env.Name + ", ";
                    }
                    player.Message(list.Remove(list.Length - 2, 2));
                    break;
                case "reload":
                    if (player.Info.Rank == RankManager.HighestRank) {
                        EnvPresets.ReloadAll();
                        player.Message("Reloaded presets from file");
                    } else {
                        player.Message(CdEnvPreset.Help);
                    }
                    break;
                default:
                    player.Message(CdEnvPreset.Help);
                    break;
            }
        }

        #endregion

        #region CustomBlocks

        static readonly CommandDescriptor CdGlobalBlock = new CommandDescriptor {
            Name = "GlobalBlock",
            Aliases = new string[] { "global", "block", "gb" },
            Category = CommandCategory.CPE | CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.DefineCustomBlocks },
            Usage = "/gb [type/value] {args}",
            Help = MakeHelp("global", "gb"),
            HelpSections = MakeHelpSections("global", "/gb"),
            Handler = GlobalBlockHandler
        };
        
        static void GlobalBlockHandler(Player player, CommandReader cmd) {
            CustomBlockHandler(player, cmd, true);
        }
        
        static readonly CommandDescriptor CdLevelBlock = new CommandDescriptor {
            Name = "LevelBlock",
            Aliases = new string[] { "lb" },
            Category = CommandCategory.CPE | CommandCategory.World,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.DefineLevelCustomBlocks },
            Usage = "/lb [type/value] {args}",
            Help = MakeHelp("level's", "lb"),
            HelpSections = MakeHelpSections("level", "/lb"),
            Handler = LevelBlockHandler
        };
        
        static void LevelBlockHandler(Player player, CommandReader cmd) {
            CustomBlockHandler(player, cmd, false);
        }
        
        static string MakeHelp(string scope, string name) {
            return "&sModifies the " + scope + " custom blocks, or prints information about them.&n" +
                   "&sTypes are: add, abort, duplicate, edit, info, list, remove, texture&n" +
                   "&sSee &h/help " + name + " <type>&s for details about each type.";
        }
        
        static Dictionary<string, string> MakeHelpSections(string scope, string name) {
            return new Dictionary<string, string>{
                { "add",     "&h" + name + " add [id]&n" +
                        "&sBegins the process of defining a " + scope + " custom block with the given block id." },
                { "abort",   "&h" + name + " abort&n" +
                        "&sAborts the custom block that was currently in the process of being " +
                        "defined from the last &h" + name + " add &scall." },
                { "duplicate",     "&h" + name + " duplicate [source id] [new id]&n" +
                        "&sCreates a new custom block, using all the data of the given existing " + scope + " custom block. " },
                { "edit",     "&h" + name + " edit [id] [option] {args}&n" +
                        "&sEdits already defined blocks so you don't have to re-add them to change something. " +
                        "Options: Name, Solidity, Speed, AllId, TopId, SideID, BottomID, Light, Sound, FullBright, Shape, Draw, FogDensity, (FogHex or FogR, FogG, FogB), FallBack"},
                { "info",     "&h" + name + " info [id]&n" +
                        "&sDisplays information about the given " + scope + " custom block." },
                { "list",    "&h" + name + " list [offset]&n" +
                        "&sPrints a list of the names of the " + scope + " custom blocks, " +
                        "along with their corresponding block ids. " },
                { "remove",  "&h" + name + " remove [id]&n" +
                        "&sRemoves the " + scope + " custom block which as the given block id." },
                { "texture",  "&h" + name + " tex&n" +
                        "&sShows you the terrain link of the current world and a link of the default with ID's overlayed." },
            };          
        } 
        
        static void CustomBlockHandler(Player p, CommandReader cmd, bool global) {
            string opt = cmd.Next();
            if (opt != null)
                opt = opt.ToLower();
            string scope = global ? "global" : "level";
            string name = global ? "/gb" : "/lb";
            BlockDefinition[] defs = global ? BlockDefinition.GlobalDefs : p.World.BlockDefs;
            
            switch (opt) {
                case "create":
                case "add":
                    if (p.currentBD != null)
                        CustomBlockDefineHandler(p, cmd.NextAll(), global, defs);
                    else
                        CustomBlockAddHandler(p, cmd, global, defs);
                    break;
                case "nvm":
                case "abort":
                    if (p.currentBD == null) {
                        p.Message("You weren't creating a {0} custom block.", scope);
                    } else {
                        p.currentBD = null;
                        p.currentBDStep = -1;
                        p.Message("Discarded the {0} custom block that was being created.", scope);
                    } break;
                case "edit":
                case "change":
                    CustomBlockEditHandler(p, cmd, global, defs); break;
                case "copy":
                case "duplicate":
                    CustomBlockDuplicateHandler(p, cmd, global, defs); break;
                case "i":
                case "info":
                    string input = cmd.Next() ?? "n/a";
                    Block infoID;
                    if (!Map.GetBlockByName(p.World, input, false, out infoID) || infoID < Map.MaxCustomBlockType) {
                        p.Message("No blocks by that name or id!");
                        return;
                    }
                    BlockDefinition block = GetCustomBlock(global, defs, (byte)infoID);
                    if (block == null) {
                        p.Message("No {0} custom block by the Name/ID", scope);
                        p.Message("Use \"&h{1} list\" &sto see a list of {0} custom blocks.", scope, name);
                        return;
                    }
                    Block fallback;
                    Map.GetBlockByName(block.FallBack.ToString(), false, out fallback);
                    p.Message("&3---Name&3:&a{0} &3ID:&a{1}&3---", block.Name, block.BlockID);
                    p.Message("   &3FallBack: &a{0}&3, Solidity: &a{2}&3, Speed: &a{1}",
                        fallback.ToString(), block.Speed, block.CollideType);
                    p.Message("   &3Top ID: &a{0}&3, Side ID: &a{1}&3, Bottom ID: &a{2}",
                        block.TopTex, block.SideTex, block.BottomTex);
                    p.Message("   &3Left ID: &a{0}&3, Right ID: &a{1}&3, Front ID: &a{2}&3, Back ID: &a{3}",
                        block.LeftTex, block.RightTex, block.FrontTex, block.BackTex);
                    p.Message("   &3Block Light: &a{0}&3, Sound: &a{1}&3, FullBright: &a{2}",
                        block.BlocksLight, block.WalkSound, block.FullBright);
                    p.Message("   &3Shape: &a{0}&3, Draw: &a{1}&3, Fog Density: &a{2}",
                        block.Shape, block.BlockDraw, block.FogDensity);
                    p.Message("   &3Fog Red: &a{0}&3, Fog Green: &a{1}&3, Fog Blue: &a{2}",
                        block.FogR, block.FogG, block.FogB);
                    p.Message("   &3Min X: &a{0}&3, Max X: &a{1}&3, Min Y: &a{2}&3, Max Y: &a{3}",
                        block.MinX, block.MaxX, block.MinY, block.MaxY);
                    break;
                case "list":
                    CustomBlockListHandler(p, cmd, global, defs); break;
                case "remove":
                case "delete":
                    CustomBlockRemoveHandler(p, cmd, global, defs); break;
                case "tex":
                case "texture":
                case "terrain":
                    p.Message("Terrain IDs: &9http://123dmwm.tk/ID-Overlay.png");
                    p.Message("Current world terrain: &9{0}", p.World.Texture.ToLower().Equals("default") ? Server.DefaultTerrain : p.World.Texture);
                    break;
                default:
                    if (p.currentBD != null) {
                        cmd.Rewind();
                        CustomBlockDefineHandler(p, cmd.NextAll(), global, defs);
                    } else {
                        p.Message("Usage: &H" + name + " [type/value] {args}");
                    }
                    break;
            }
        }

        static void CustomBlockAddHandler(Player p, CommandReader cmd, bool global, BlockDefinition[] defs) {
            int blockId = 0;
            if (!CheckBlockId(p, cmd, out blockId)) return;
            string scope = global ? "global" : "level";
            string name = global ? "/gb" : "/lb";

            BlockDefinition def = GetCustomBlock(global, defs, (byte)blockId);
            if (def != null) {
                p.Message("There is already a {0} custom block with that id.", scope);
                p.Message("Use \"&h{1} remove {0}&s\" this block first.", blockId, name);
                p.Message("Use \"&h{1} list&s\" to see a list of {0} custom blocks.", scope, name);
                return;
            }

            p.currentBD = new BlockDefinition();
            p.currentBD.BlockID = (byte)blockId;
            p.currentBD.Version2 = true;
            p.Message("   &bSet block id to: " + blockId);
            p.Message("&sFrom now on, use &h{0} [value]&s to enter arguments.", name);
            p.Message("&sYou can abort the currently partially " +
                           "created custom block at any time by typing \"&h{0} abort&s\"", name);

            p.currentBDStep = 0;
            PrintStepHelp(p);
        }

        static void CustomBlockListHandler(Player p, CommandReader cmd, bool global, BlockDefinition[] defs) {
            int offset = 0, index = 0, count = 0;
            cmd.NextInt(out offset);
            string scope = global ? "global" : "level";
            string name = global ? "/gb" : "/lb";
            
            for (int i = 0; i < defs.Length; i++) {
                BlockDefinition def = GetCustomBlock(global, defs, (byte)i);
                if (def == null) continue;

                if (index >= offset) {
                    count++;
                    p.Message("&s{2} custom block &h{0} &sname is &h{1}", def.BlockID, def.Name, scope);

                    if (count >= 8) {
                        p.Message("To see the next set of {1} custom blocks, " +
                              "type {2} list {0}", offset + 8, scope, name);
                        return;
                    }
                }
                index++;
            }
        }

        static void CustomBlockRemoveHandler(Player p, CommandReader cmd, bool global, BlockDefinition[] defs) {
            string input = cmd.Next() ?? "n/a";
            Block blockID;
            string scope = global ? "global" : "level";
            string name = global ? "/gb" : "/lb";
            if (!Map.GetBlockByName(p.World, input, false, out blockID) || blockID < Map.MaxCustomBlockType) {
                p.Message("No blocks by that Name/ID!");
                return;
            }
            BlockDefinition def = GetCustomBlock(global, defs, (byte)blockID);
            if (def == null) {
                p.Message("There is no {0} custom block with that name/id.", scope);
                p.Message("Use \"&h{1} list\" &sto see a list of {0} custom blocks.", scope, name);
                return;
            }

            BlockDefinition.Remove(def, defs, p.World);
            if (global) {
                Server.Message("{0} &sremoved the {3} custom block &h{1} &swith ID {2}",
                                   p.ClassyName, def.Name, def.BlockID, scope);
            } else {
                p.World.Players.Message("{0} &sremoved the {3} custom block &h{1} &swith ID {2}",
                                   p.ClassyName, def.Name, def.BlockID, scope);            
            }
        }

        static void CustomBlockDefineHandler(Player p, string args, bool global, BlockDefinition[] defs) {
            // print the current step help if no args given
            if (args.NullOrWhiteSpace()) {
                PrintStepHelp(p); return;
            }

            BlockDefinition def = p.currentBD;
            int step = p.currentBDStep;
            byte value = 0; // as we can't pass properties by reference, make a temp var.
            bool boolVal = true;
            string scope = global ? "global" : "level";

            switch (step) {
                case 0:
                    step++; def.Name = args; def.BlockName = args.ToLower().Replace(" ", "");
                    p.Message("   &bSet name to: " + def.Name);
                    break;
                case 1:
                    if (byte.TryParse(args, out value) && value <= 2) {
                        step++; def.CollideType = value;
                        p.Message("   &bSet solidity to: " + value);
                    }
                    break;
                case 2:
                    float speed;
                    if (float.TryParse(args, out speed)
                        && speed >= 0.25f && value <= 3.96f) {
                        step++; def.Speed = speed;
                        p.Message("   &bSet speed to: " + speed);
                    }
                    break;
                case 3:
                    if (byte.TryParse(args, out value)) {
                        step++; def.TopTex = value;
                        p.Message("   &bSet top texture index to: " + value);
                    }
                    break;
                case 4:
                    if (byte.TryParse(args, out value)) {
                        step++; def.SideTex = value;
                        def.LeftTex = def.SideTex; def.RightTex = def.SideTex;
                        def.FrontTex = def.SideTex; def.BackTex = def.SideTex;
                        p.Message("   &bSet sides texture index to: " + value);
                    }
                    break;
                case 5:
                    if (byte.TryParse(args, out value)) {
                        step++; def.BottomTex = value;
                        p.Message("   &bSet bottom texture index to: " + value);
                    }
                    break;
                case 6:
                    if (bool.TryParse(args, out boolVal)) {
                        step++; def.BlocksLight = boolVal;
                        p.Message("   &bSet blocks light to: " + boolVal);
                    }
                    break;
                case 7:
                    if (byte.TryParse(args, out value) && value <= 11) {
                        step++; def.WalkSound = value;
                        p.Message("   &bSet walk sound to: " + value);
                    }
                    break;
                case 8:
                    if (bool.TryParse(args, out boolVal)) {
                        if (p.Supports(CpeExt.BlockDefinitionsExt) || p.Supports(CpeExt.BlockDefinitionsExt2)) {
                            step = 16;
                        } else {
                            step++;
                        }
                        def.FullBright = boolVal;
                        p.Message("   &bSet full bright to: " + boolVal);
                    }
                    break;
                case 9:
                    if (byte.TryParse(args, out value) && value <= 16) {
                        step++;
                        def.Shape = value;
                        def.MinX = 0; def.MinY = 0; def.MinZ = 0;
                        def.MaxX = 16; def.MaxY = 16; def.MaxZ = value;
                        p.Message("   &bSet block shape to: " + value);
                    }
                    break;
                case 10:
                    if (byte.TryParse(args, out value) && value <= 4) {
                        step++; def.BlockDraw = value;
                        p.Message("   &bSet block draw type to: " + value);
                    }
                    break;
                case 11:
                    if (byte.TryParse(args, out value)) {
                        def.FogDensity = value;
                        step += value == 0 ? 4 : 1;
                        p.Message("   &bSet density of fog to: " + value);
                    }
                    break;
                case 12:
                    if (IsValidHex(args)) {
                        System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml("#" + args.ToUpper().Replace("#", ""));
                        def.FogR = col.R;
                        p.Message("   &bSet red component of fog to: " + col.R);
                        def.FogG = col.G;
                        p.Message("   &bSet green component of fog to: " + col.G);
                        def.FogB = col.B;
                        p.Message("   &bSet blue component of fog to: " + col.B);
                        step += 3;
                    } else {
                        if (byte.TryParse(args, out value)) {
                            step++; def.FogR = value;
                            p.Message("   &bSet red component of fog to: " + value);
                        }
                    }
                    break;
                case 13:
                    if (byte.TryParse(args, out value)) {
                        step++; def.FogG = value;
                        p.Message("   &bSet green component of fog to: " + value);
                    }
                    break;
                case 14:
                    if (byte.TryParse(args, out value)) {
                        step++; def.FogB = value;
                        p.Message("   &bSet blue component of fog to: " + value);
                    }
                    break;
                case 16:
                    if (args.ToLower().Equals("-1")) {
                        p.Message("   &bBlock will display as a Sprite");
                        def.Shape = 0;
                        def.MinX = 0; def.MinY = 0; def.MinZ = 0;
                        def.MaxX = 16; def.MaxY = 16; def.MaxZ = 16;
                        step = 10;
                        break;
                    }
                    if (args.Split().Length != 3) {
                        p.Message("Please specify 3 coordinates");
                        return;
                    }
                    byte minx, miny, minz;
                    if (byte.TryParse(args.Split()[0], out minx)
                        && byte.TryParse(args.Split()[1], out miny)
                        && byte.TryParse(args.Split()[2], out minz)
                        && (minx <= 15 && minx >= 0)
                        && (miny <= 15 && miny >= 0)
                        && (minz <= 15 && minz >= 0)) {
                    } else {
                        p.Message("Invalid coordinates! All 3 must be between 0 and 15");
                        return;
                    }
                    step++;
                    def.MinX = minx;
                    def.MinY = miny;
                    def.MinZ = minz;
                    p.Message("   &bSet minimum coords to X:{0} Y:{1} Z:{2}", minx, miny, minz);
                    break;
                case 17:
                    if (args.Split().Length != 3) {
                        p.Message("Please specify 3 coordinates");
                        return;
                    }
                    byte maxx, maxy, maxz;
                    if (byte.TryParse(args.Split()[0], out maxx)
                        && byte.TryParse(args.Split()[1], out maxy)
                        && byte.TryParse(args.Split()[2], out maxz)
                        && (maxx <= 16 && maxx >= 1)
                        && (maxy <= 16 && maxy >= 1)
                        && (maxz <= 16 && maxz >= 1)) {
                    } else {
                        p.Message("Invalid coordinates! All 3 must be between 1 and 16");
                        return;
                    }
                    step = 10;
                    def.MaxX = maxx;
                    def.MaxY = maxy;
                    def.MaxZ = maxz;
                    def.Shape = maxz;
                    p.Message("   &bSet maximum coords to X:{0} Y:{1} Z:{2}", maxx, maxy, maxz);
                    break;
                default:
                    Block block;
                    if (Map.GetBlockByName(args, false, out block)) {
                        if (block > Map.MaxCustomBlockType) {
                            p.Message("&cThe fallback block must be an original block, " +
                                           "or a block defined in the CustomBlocks extension.");
                            break;
                        }
                        def.FallBack = (byte)block;
                        p.Message("   &bSet fallback block to: " + block);
                        BlockDefinition.Add(def, defs, p.World);
                        p.currentBDStep = -1;
                        p.currentBD = null;

                        if (global) {
                            Server.Message("{0} &screated a new {3} custom block &h{1} &swith ID {2}",
                                       p.ClassyName, def.Name, def.BlockID, scope);
                        } else {
                            p.World.Players.Message("{0} &screated a new {3} custom block &h{1} &swith ID {2}",
                                       p.ClassyName, def.Name, def.BlockID, scope);
                        }
                    }
                    return;
            }
            p.currentBDStep = step;
            PrintStepHelp(p);
        }

        static void CustomBlockDuplicateHandler(Player p, CommandReader cmd, bool global, BlockDefinition[] defs) {
            string input1 = cmd.Next() ?? "n/a", input2 = cmd.Next() ?? "n/a";
            Block srcBlock = Block.None;
            byte dstBlock = (byte)Block.None;
            string scope = global ? "global" : "level";
            string name = global ? "/gb" : "/lb";

            if (!Map.GetBlockByName(p.World, input1, false, out srcBlock) || srcBlock <= Map.MaxCustomBlockType) {
                p.Message("There is no {1} custom block with the id or name: &a{0}", input1, scope);
                p.Message("Use \"&h{1} list&s\" to see a list of {0} custom blocks.", scope, name);
                return;
            }
            if (!Byte.TryParse(input2, out dstBlock) || dstBlock <= (byte)Map.MaxCustomBlockType) {
                p.Message("Destination must be a numerical id and greater than 65."); return;
            }

            BlockDefinition srcDef = GetCustomBlock(global, defs, (byte)srcBlock);
            if (srcDef == null) {
                p.Message("There is no {1} custom block with the id: &a{0}", (byte)srcBlock, scope);
                p.Message("Use \"&h{1} list&s\" to see a list of {0} custom blocks.", scope, name);
                return;
            }
            BlockDefinition dstDef = GetCustomBlock(global, defs, (byte)dstBlock);
            if (dstDef != null) {
                p.Message("There is already a {1} custom block with the id: &a{0}", dstBlock, scope);
                p.Message("Use \"&h{1} remove {0}&s\" on this block first.", dstBlock, name);
                p.Message("Use \"&h{1} list&s\" to see a list of {0} custom blocks.", scope, name);
                return;
            }
            
            BlockDefinition def = srcDef.Copy();
            def.BlockID = (byte)dstBlock;
            BlockDefinition.Add(def, defs, p.World);
            if (global) {
                Server.Message("{0} &screated a new {3} custom block &h{1} &swith ID {2}",
                           p.ClassyName, def.Name, def.BlockID, scope);
            } else {
                p.World.Players.Message("{0} &screated a new {3} custom block &h{1} &swith ID {2}",
                           p.ClassyName, def.Name, def.BlockID, scope);            
            }
        }

        static void CustomBlockEditHandler(Player p, CommandReader cmd, bool global, BlockDefinition[] defs) {
            string input = cmd.Next() ?? "n/a";
            Block blockID;
            if (!Map.GetBlockByName(p.World, input, false, out blockID) || blockID < Map.MaxCustomBlockType) {
                p.Message("No blocks by that Name/ID!");
                return;
            }
            
            BlockDefinition def = GetCustomBlock(global, defs, (byte)blockID);
            string scope = global ? "global" : "level";
            string name = global ? "/gb" : "/lb";
            if (def == null) {
                p.Message("There is no {0} custom block with that Name/ID", scope); return;
            }
            
            string option = cmd.Next() ?? "n/a";
            string args = cmd.NextAll();
            if (string.IsNullOrEmpty(args)) {
                p.Message("Please specify what you want to change the {0} option to.", option);
                return;
            }
            byte value = 0;
            bool boolVal = true;
            bool hasChanged = false;

            switch (option.ToLower()) {
                case "name":
                    p.Message("&bChanged name of &a{0}&b to &A{1}", def.Name, args);
                    def.Name = args; def.BlockName = args.ToLower().Replace(" ", "");
                    break;
                case "solid":
                case "solidity":
                case "collide":
                case "collidetype":
                    if (byte.TryParse(args, out value) && value <= 2) {
                        p.Message("&bChanged solidity of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.CollideType, value);
                        def.CollideType = value;
                        hasChanged = true;
                    }
                    break;
                case "speed":
                    float speed;
                    if (float.TryParse(args, out speed)
                        && speed >= 0.25f && value <= 3.96f) {
                        p.Message("&bChanged speed of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.Speed, speed);
                        def.Speed = speed;
                        hasChanged = true;
                    }
                    break;
                case "allid":
                case "alltex":
                case "alltexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged top, sides, and bottom texture index of &a{0}&b to &a{1}", def.Name, value);
                        def.TopTex = value; def.SideTex = value; def.BottomTex = value;
                        def.LeftTex = value; def.RightTex = value;
                        def.FrontTex = value; def.BackTex = value;
                        hasChanged = true;
                    }
                    break;
                case "topid":
                case "toptex":
                case "toptexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged top texture index of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.TopTex, value);
                        def.TopTex = value;
                        hasChanged = true;
                    }
                    break;
                case "leftid":
                case "lefttex":
                case "lefttexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged left texture index of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.LeftTex, value);
                        def.LeftTex = value;
                        hasChanged = true;
                    }
                    break;
                case "rightid":
                case "righttex":
                case "righttexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged right texture index of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.RightTex, value);
                        def.RightTex = value;
                        hasChanged = true;
                    }
                    break;
                case "frontid":
                case "fronttex":
                case "fronttexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged front texture index of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FrontTex, value);
                        def.FrontTex = value;
                        hasChanged = true;
                    }
                    break;
                case "backid":
                case "backtex":
                case "backtexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged back texture index of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.BackTex, value);
                        def.BackTex = value;
                        hasChanged = true;
                    }
                    break;
                case "sideid":
                case "sidetex":
                case "sidetexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged sides texture index of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.SideTex, value);
                        def.SideTex = value;
                        def.LeftTex = def.SideTex; def.RightTex = def.SideTex;
                        def.FrontTex = def.SideTex; def.BackTex = def.SideTex;
                        hasChanged = true;
                    }
                    break;
                case "bottomid":
                case "bottomtex":
                case "bottomtexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged bottom texture index of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.BottomTex, value);
                        def.BottomTex = value;
                        hasChanged = true;
                    }
                    break;
                case "light":
                case "blockslight":
                    if (bool.TryParse(args, out boolVal)) {
                        p.Message("&bChanged blocks light of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.BlocksLight, boolVal);
                        def.BlocksLight = boolVal;
                        hasChanged = true;
                    }
                    break;
                case "sound":
                case "walksound":
                    if (byte.TryParse(args, out value) && value <= 11) {
                        p.Message("&bChanged walk sound of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.WalkSound, value);
                        def.WalkSound = value;
                        hasChanged = true;
                    }
                    break;
                case "fullbright":
                    if (bool.TryParse(args, out boolVal)) {
                        p.Message("&bChanged full bright of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FullBright, boolVal);
                        def.FullBright = boolVal;
                        hasChanged = true;
                    }
                    break;
                case "size":
                case "shape":
                case "height":
                    if (byte.TryParse(args, out value) && value <= 16) {
                        p.Message("&bChanged block shape of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.Shape, value);
                        def.Shape = value;
                        hasChanged = true;
                    }
                    break;
                case "draw":
                case "blockdraw":
                    if (byte.TryParse(args, out value) && value <= 4) {
                        p.Message("&bChanged block draw type of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.BlockDraw, value);
                        def.BlockDraw = value;
                        hasChanged = true;
                    }
                    break;
                case "fogdensity":
                case "fogd":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged density of fog of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogDensity, value);
                        def.FogDensity = value;
                        hasChanged = true;
                    }
                    break;
                case "foghex":
                    if (IsValidHex(args)) {
                        System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml("#" + args.ToUpper().Replace("#", ""));
                        p.Message("&bChanged red fog component of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogR, col.R);
                        def.FogR = col.R;
                        p.Message("&bChanged green fog component of fog of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogG, col.G);
                        def.FogG = col.G;
                        p.Message("&bChanged blue fog component of fog of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogB, col.B);
                        def.FogB = col.B;
                        hasChanged = true;
                    }
                    break;
                case "fogr":
                case "fogred":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged red fog component of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogR, value);
                        def.FogG = value;
                        hasChanged = true;
                    }
                    break;
                case "fogg":
                case "foggreen":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged green fog component of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogG, value);
                        def.FogG = value;
                        hasChanged = true;
                    }
                    break;
                case "fogb":
                case "fogblue":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged blue fog component of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogB, value);
                        def.FogB = value;
                        hasChanged = true;
                    }
                    break;
                case "fallback":
                case "block":
                    Block newBlock;
                    if (Map.GetBlockByName(args, false, out newBlock)) {
                        if (newBlock > Map.MaxCustomBlockType) {
                            p.Message("&cThe fallback block must be an original block, " +
                                           "or a block defined in the CustomBlocks extension.");
                            break;
                        }
                        p.Message("&bChanged fallback block of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FallBack, newBlock.ToString());
                        def.FallBack = (byte)newBlock;
                        hasChanged = true;
                    }
                    break;
                case "min":
                    if (args.ToLower().Equals("-1")) {
                        p.Message("Block will display as a sprite!");
                        def.Shape = 0;
                        hasChanged = true;
                        break;
                    }
                    if (args.Split().Length != 3) {
                        p.Message("Please specify 3 coordinates!");
                        break;
                    }
                    def.MinX = EditCoord(p, "min X", def.Name, args.Split()[0], def.MinX, ref hasChanged);
                    def.MinY = EditCoord(p, "min Y", def.Name, args.Split()[1], def.MinY, ref hasChanged);
                    def.MinZ = EditCoord(p, "min Z", def.Name, args.Split()[2], def.MinZ, ref hasChanged);
                    hasChanged = true;
                    break;
                case "max":
                    if (args.Split().Length != 3) {
                        p.Message("Please specify 3 coordinates!");
                        break;
                    }
                    def.MaxX = EditCoord(p, "max X", def.Name, args.Split()[0], def.MaxX, ref hasChanged);
                    def.MaxY = EditCoord(p, "max Y", def.Name, args.Split()[1], def.MaxY, ref hasChanged);
                    def.MaxZ = EditCoord(p, "max Z", def.Name, args.Split()[2], def.MaxZ, ref hasChanged);
                    hasChanged = true;
                    break;
                case "minx":
                    def.MinX = EditCoord(p, "min X", def.Name, args, def.MinX, ref hasChanged); break;
                case "miny":
                    def.MinY = EditCoord(p, "min Y", def.Name, args, def.MinY, ref hasChanged); break;
                case "minz":
                    def.MinZ = EditCoord(p, "min Z", def.Name, args, def.MinZ, ref hasChanged); break;
                case "maxx":
                    def.MaxX = EditCoord(p, "max X", def.Name, args, def.MaxX, ref hasChanged); break;
                case "maxy":
                    def.MaxY = EditCoord(p, "max Y", def.Name, args, def.MaxY, ref hasChanged); break;
                case "maxz":
                    def.MaxZ = EditCoord(p, "max Z", def.Name, args, def.MaxZ, ref hasChanged);
                    if (byte.TryParse(args, out value)) {
                        def.Shape = value;
                    }
                    break;
                default:
                    p.Message("Usage: &H" + name + " [type/value] {args}");
                    return;
            }
            if (!hasChanged) return;
            
            if (global) {
                Server.Message("{0} &sedited a {3} custom block &a{1} &swith ID &a{2}",
                               p.ClassyName, def.Name, def.BlockID, scope);
            } else {
                p.World.Players.Message("{0} &sedited a {3} custom block &a{1} &swith ID &a{2}",
                                        p.ClassyName, def.Name, def.BlockID, scope);
            }
            BlockDefinition.Add(def, defs, p.World);

            foreach (Player pl in Server.Players) {
                if (!p.Supports(CpeExt.BlockDefinitions) && 
                    (option.ToLower().Equals("block") || option.ToLower().Equals("fallback"))) {
                    p.JoinWorld(p.World, WorldChangeReason.Rejoin, p.Position);
                }
            }
        }

        static byte EditCoord(Player p, string coord, string name, 
                              string args, byte origValue, ref bool hasChanged) {
            byte value;
            if (byte.TryParse(args, out value) && value <= 16) {
                p.Message("&bChanged {0} coordinate of &a{1}&b from &a{2}&b to &a{3}", coord, name, origValue, value);
                hasChanged = true;
                return value;
            }
            return origValue;
        }

        static bool CheckBlockId(Player p, CommandReader cmd, out int blockId) {
            if (!cmd.HasNext) {
                blockId = 0;
                p.Message("You most provide a block ID argument.");
                return false;
            }
            if (!cmd.NextInt(out blockId)) {
                p.Message("Provided block id is not a number.");
                return false;
            }
            if (blockId <= 65 || blockId >= 255) {
                p.Message("Block id must be between 65-254");
                return false;
            }
            return true;
        }

        static void PrintStepHelp(Player p) {
            string[] help = blockSteps[p.currentBDStep];
            foreach (string m in help)
                p.Message(m);
        }

        static string[][] blockSteps = new[] {
            new [] { "&sEnter the name of the block. You can include spaces." },
            new [] { "&sEnter the solidity of the block (0-2)",
                "&s0 = walk through(air), 1 = swim through (water), 2 = solid" },
            new [] { "&sEnter the movement speed of the new block. (0.25-3.96)" },
            new [] { "&sEnter the terrain.png index for the top texture. (0-255)" },
            new [] { "&sEnter the terrain.png index for the sides texture. (0-255)" },
            new [] { "&sEnter the terrain.png index for the bottom texture. (0-255)" },
            new [] { "&sEnter whether the block prevents sunlight from passing though. (true or false)" },
            new [] { "&sEnter the walk sound index of the block. (0-11)",
                "&s0 = no sound, 1 = wood, 2 = gravel, 3 = grass, 4 = stone,",
                "&s5 = metal, 6 = glass, 7 = wool, 8 = sand, 9 = snow." },
            new [] { "&sEnter whether the block is fully bright (i.e. like lava). (true or false)" },
            new [] { "&sEnter the shape of the block. (0-16)",
                "&s0 = sprite(e.g. roses), 1-16 = cube of the given height",
                "&s(e.g. snow has height '2', slabs have height '8', dirt has height '16')" },
            new [] { "&sEnter the block draw type of this block. (0-4)",
                "&s0 = solid/opaque, 1 = transparent (like glass)",
                "&s2 = transparent (like leaves), 3 = translucent (like water)",
                "&s4 = gas (like air)" },
            new [] { "Enter the density of the fog for the block. (0-255)",
                "0 is treated as no fog, 255 is thickest fog." },
            new [] { "Enter the red component of the fog colour. (0-255) or full hex value" },
            new [] { "Enter the green component of the fog colour. (0-255)" },
            new [] { "Enter the blue component of the fog colour. (0-255)" },
            new [] { "Enter the fallback block for this block.",
                "This block is shown to clients that don't support BlockDefinitions." },
            new [] { "Enter the min X Y Z coords of this block,",
                "Min = 0 Max = 15 Example: &h0 0 0",
                "&h-1&s to make it a sprite" },
            new [] { "Enter the max X Y Z coords of this block,",
                "Min = 1 Max = 16 Example: &h16 16 16" },
        };
        
        static BlockDefinition GetCustomBlock(bool global, BlockDefinition[] defs, byte id) {
            if (global) return defs[id];
            return defs[id] == BlockDefinition.GlobalDefs[id] ? null : defs[id];
        }

        #endregion

        #region HackControl

        static readonly CommandDescriptor CdHackControl = new CommandDescriptor {
            Name = "HackControl",
            Aliases = new[] { "hacks", "hack", "hax" },
            Category = CommandCategory.CPE | CommandCategory.Moderation,
            Permissions = new[] { Permission.Chat },
            Usage = "/Hacks [Player] [Hack] [jumpheight(if needed)]",
            IsConsoleSafe = true,
            Help = "Change the hacking abilities of [Player]&n" +
            "Valid hacks: &aFlying&s, &aNoclip&s, &aSpeedhack&s, &aRespawn&s, &aThirdPerson&s and &aJumpheight",
            Handler = HackControlHandler
        };

        static void HackControlHandler(Player player, CommandReader cmd) {

            PlayerInfo target = InfoCommands.FindPlayerInfo(player, cmd);
            if (target == null || player.Info.Rank != RankManager.HighestRank) {
                player.Message("&sCurrent Hacks for {0}", player.ClassyName);
                player.Message("    &sFlying: &a{0} &sNoclip: &a{1} &sSpeedhack: &a{2}",
                                player.Info.AllowFlying.ToString(),
                                player.Info.AllowNoClip.ToString(),
                                player.Info.AllowSpeedhack.ToString());
                player.Message("    &sRespawn: &a{0} &sThirdPerson: &a{1} &sJumpHeight: &a{2}",
                                player.Info.AllowRespawn.ToString(),
                                player.Info.AllowThirdPerson.ToString(),
                                player.Info.JumpHeight);
                return;
            }


            string hack = (cmd.Next() ?? "null");
            string hackStr = "hack";

            if (hack == "null") {
                player.Message("&sCurrent Hacks for {0}", target.ClassyName);
                player.Message("    &sFlying: &a{0} &sNoclip: &a{1} &sSpeedhack: &a{2}",
                                target.AllowFlying.ToString(),
                                target.AllowNoClip.ToString(),
                                target.AllowSpeedhack.ToString());
                player.Message("    &sRespawn: &a{0} &sThirdPerson: &a{1} &sJumpHeight: &a{2}",
                                target.AllowRespawn.ToString(),
                                target.AllowThirdPerson.ToString(),
                                target.JumpHeight);
                return;
            }

            switch (hack.ToLower()) {
                case "flying":
                case "fly":
                case "f":
                    player.Message("Hacks for {0}", target.ClassyName);
                    player.Message("    Flying: &a{0} &s--> &a{1}", target.AllowFlying.ToString(), (!target.AllowFlying).ToString());
                    target.AllowFlying = !target.AllowFlying;
                    hackStr = "flying";
                    goto sendPacket;
                case "noclip":
                case "clip":
                case "nc":
                    player.Message("Hacks for {0}", target.ClassyName);
                    player.Message("    NoClip: &a{0} &s--> &a{1}", target.AllowNoClip.ToString(), (!target.AllowNoClip).ToString());
                    target.AllowNoClip = !target.AllowNoClip;
                    hackStr = "noclip";
                    goto sendPacket;
                case "speedhack":
                case "speed":
                case "sh":
                    player.Message("Hacks for {0}", target.ClassyName);
                    player.Message("    SpeedHack: &a{0} &s--> &a{1}", target.AllowSpeedhack.ToString(), (!target.AllowSpeedhack).ToString());
                    target.AllowSpeedhack = !target.AllowSpeedhack;
                    hackStr = "speedhack";
                    goto sendPacket;
                case "respawn":
                case "spawn":
                case "rs":
                    player.Message("Hacks for {0}", target.ClassyName);
                    player.Message("    Respawn: &a{0} &s--> &a{1}", target.AllowRespawn.ToString(), (!target.AllowRespawn).ToString());
                    target.AllowRespawn = !target.AllowRespawn;
                    hackStr = "respawn";
                    goto sendPacket;
                case "thirdperson":
                case "third":
                case "tp":
                    player.Message("Hacks for {0}", target.ClassyName);
                    player.Message("    ThirdPerson: &a{0} &s--> &a{1}", target.AllowThirdPerson.ToString(), (!target.AllowThirdPerson).ToString());
                    target.AllowThirdPerson = !target.AllowThirdPerson;
                    hackStr = "thirdperson";
                    goto sendPacket;
                case "jumpheight":
                case "jump":
                case "height":
                case "jh":
                    short height;
                    string third = cmd.Next();
                    if (short.TryParse(third, out height)) {
                        player.Message("Hacks for {0}", target.ClassyName);
                        player.Message("    JumpHeight: &a{0} &s--> &a{1}", target.JumpHeight, height);
                        target.JumpHeight = height;
                        hackStr = "jumpheight";
                        goto sendPacket;
                    } else player.Message("Error: Could not parse \"&a{0}&s\" as a short. Try something between &a0&s and &a32767", third);
                    break;
                default:
                    player.Message(CdHackControl.Help);
                    break;
            }
            return;
        sendPacket:
            if (target.IsOnline) {
                if (target.PlayerObject != player) {
                    target.PlayerObject.Message("{0} has changed your {1} ability, use &h/Hacks &sto check them out.", player.Info.Name, hackStr);
                }
                if (target.PlayerObject.Supports(CpeExt.HackControl)) {
                    target.PlayerObject.Send(Packet.HackControl(
                        target.AllowFlying, target.AllowNoClip, target.AllowSpeedhack,
                        target.AllowRespawn, target.AllowThirdPerson, target.JumpHeight));
                }
            }
        }

        #endregion

        #region ListClients

        static readonly CommandDescriptor CdListClients = new CommandDescriptor {
            Name = "ListClients",
            Aliases = new[] { "pclients", "clients", "whoisanewb" },
            Category = CommandCategory.CPE | CommandCategory.Info,
            IsConsoleSafe = true,
            Help = "Shows a list of currently clients users are using.",
            Handler = ListClientsHandler
        };

        static void ListClientsHandler(Player player, CommandReader cmd) {
            Player[] players = Server.Players;
            var visiblePlayers = players.Where(player.CanSee).OrderBy(p => p, PlayerListSorter.Instance).ToArray();

            Dictionary<string, List<Player>> clients = new Dictionary<string, List<Player>>();
            foreach (var p in visiblePlayers) {
                string appName = p.ClientName;
                if (string.IsNullOrEmpty(appName)) {
                    appName = "(unknown)";
                }

                List<Player> usingClient;
                if (!clients.TryGetValue(appName, out usingClient)) {
                    usingClient = new List<Player>();
                    clients[appName] = usingClient;
                }
                usingClient.Add(p);
            }
            player.Message("Players using:");
            foreach (var kvp in clients) {
                player.Message("  &s{0}:&f {1}",
                               kvp.Key, kvp.Value.JoinToClassyString());
            }
        }

        #endregion

        #region MaxReachDistance

        static readonly CommandDescriptor CdMRD = new CommandDescriptor {
            Name = "MaxReachDistance",
            Aliases = new[] { "MaxReach", "MRD" },
            Category = CommandCategory.CPE | CommandCategory.World,
            Permissions = new[] { Permission.ManageWorlds },
            Help = "Changes the max reachdistance for a world",
            Usage = "/MRD [Distance] (world)",
            Handler = MRDHandler
        };

        private static void MRDHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            string disString = cmd.Next();
            if (disString == null) {
                player.Message(CdMRD.Usage);
                return;
            }
            string worldString = cmd.Next();
            short distance = 160;
            World world = player.World;
            if (!short.TryParse(disString, out distance)) {
                if (disString.ToLower().Equals("normal") || disString.ToLower().Equals("reset") ||
                    disString.ToLower().Equals("default")) {
                    distance = 160;
                } else {
                    player.Message("Invalid distance!");
                    return;
                }
            }
            if (worldString != null) {
                world = WorldManager.FindWorldOrPrintMatches(player, worldString);
                if (world == null) {
                    return;
                }
            }
            player.Message("&sSet max reach distance for world &f{0}&s to &f{1} &s(&f{2}&s blocks)", world.ClassyName, distance, distance / 32);
            world.maxReach = distance;

        }

        #endregion

        #region TextHotKey

        static readonly CommandDescriptor CdtextHotKey = new CommandDescriptor {
            Name = "TextHotKey",
            Aliases = new[] { "HotKey", "thk", "hk" },
            Category = CommandCategory.CPE | CommandCategory.Chat,
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
            if (null != fourth) {
                if (!Byte.TryParse(fourth, out KeyMod)) {
                    player.Message("Error: Invalid Byte ({0})", fourth);
                    return;
                }
            }
            if (player.Supports(CpeExt.TextHotKey)) {
                player.Send(Packet.MakeSetTextHotKey(Label, Action, KeyCode, KeyMod));
            } else {
                player.Message("You do not support TextHotKey");
                return;
            }
        }

        #endregion

        #region Texture

        static readonly CommandDescriptor Cdtex = new CommandDescriptor {
            Name = "texture",
            Aliases = new[] { "texturepack", "tex" },
            Permissions = new[] { Permission.Chat },
            Category = CommandCategory.CPE | CommandCategory.Chat,
            Help = "Tells you information about our custom texture pack.",
            Handler = textureHandler
        };

        static void textureHandler(Player player, CommandReader cmd) {
            if (player.World != null && !string.IsNullOrEmpty(player.World.Texture)) {
                player.Message("This world uses a custom texture pack");
                player.Message("A preview can be found here: ");
                player.Message("  " + (player.World.Texture.ToLower().Equals("default") ? Server.DefaultTerrain : player.World.Texture));
            } else {
                player.Message("You are not in a world with a custom texturepack.");
            }
        }

        #endregion

        #region Weather

        static readonly CommandDescriptor Cdweather = new CommandDescriptor {
            Name = "weather",
            Permissions = new[] { Permission.ReadStaffChat },
            Category = CommandCategory.CPE | CommandCategory.World,
            Help = "Changes player weather ingame 0(sun) 1(rain) 2(snow)",
            Usage = "/weather [Player] [weather]",
            Handler = WeatherHandler
        };

        static void WeatherHandler(Player player, CommandReader cmd) {
            if (cmd.Count == 1) {
                player.Message(Cdweather.Usage);
                return;
            }
            string name = cmd.Next();
            PlayerInfo p = PlayerDB.FindPlayerInfoOrPrintMatches(player, name, SearchOptions.IncludeSelf);
            if (p == null) {
                return;
            }
            string valueText = cmd.Next();
            byte weather;
            if (!byte.TryParse(valueText, out weather)) {
                if (valueText.Equals("sun", StringComparison.OrdinalIgnoreCase)) {
                    weather = 0;
                } else if (valueText.Equals("rain", StringComparison.OrdinalIgnoreCase)) {
                    weather = 1;
                } else if (valueText.Equals("snow", StringComparison.OrdinalIgnoreCase)) {
                    weather = 2;
                }
            }
            if (weather < 0 || weather > 2) {
                player.Message("Please use a valid integer(0,1,2) or string(sun,rain,snow)");
                return;
            }
            if (p != player.Info) {
                if (p.IsOnline) {
                    if (p.PlayerObject.Supports(CpeExt.EnvWeatherType)) {
                        p.PlayerObject.Message("&a{0} set your weather to {1} ({2}&a)", player.Name, weather, weather == 0 ? "&sSun" : (weather == 1 ? "&1Rain" : "&fSnow"));
                        player.Message("&aSet weather for {0} to {1} ({2}&a)", p.Name, weather, weather == 0 ? "&sSun" : (weather == 1 ? "&1Rain" : "&fSnow"));
                        p.PlayerObject.Send(Packet.SetWeather((byte)weather));
                    } else {
                        player.Message("That player does not support WeatherType packet");
                    }
                } else if (p.IsOnline == false || !player.CanSee(p.PlayerObject)) {
                    player.Message("That player is not online!");
                }
            } else {
                if (player.Supports(CpeExt.EnvWeatherType)) {
                    player.Message("&aSet weather to {0} ({1}&a)", weather, weather == 0 ? "&sSun" : (weather == 1 ? "&1Rain" : "&fSnow"));
                    player.Send(Packet.SetWeather((byte)weather));
                } else {
                    player.Message("You don't support WeatherType packet");
                }
            }
        }

        #endregion

        #region ZoneShow

        static readonly CommandDescriptor CdZoneShow = new CommandDescriptor {
            Name = "ZoneSelection",
            Aliases = new[] { "zselection", "zbox", "zshow", "zs" },
            Permissions = new[] { Permission.ManageZones },
            Category = CommandCategory.CPE | CommandCategory.Zone,
            Help = "Lets you configure zone selections.",
            Usage = "/ZShow [Zone Name] [Color or On/Off] [Alpha] [On/Off]",
            Handler = zshowHandler
        };

        private static void zshowHandler(Player player, CommandReader cmd) {
            if (cmd.Count <= 1) {
                CdZoneShow.PrintUsage(player);
                return;
            }
            string zonea = cmd.Next();
            string color = cmd.Next();
            string alp = cmd.Next();
            string bol = cmd.Next();
            short alpha;
            Zone zone = player.World.Map.Zones.Find(zonea);
            if (zone == null) {
                player.Message("Error: Zone not found");
                return;
            }
            if (color == null) {
                player.Message("Error: Missing a Hex Color code");
                player.Message(CdZoneShow.Usage);
                return;
            } else {
                color = color.ToUpper();
            }
            if (color.StartsWith("#")) {
                color = color.ToUpper().Remove(0, 1);
            }
            if (!IsValidHex(color)) {
                if (color.ToLower().Equals("on") || color.ToLower().Equals("true") || color.ToLower().Equals("yes")) {
                    zone.ShowZone = true;
                    if (zone.Color != null) {
                        player.Message("Zone ({0}&s) will now show its bounderies", zone.ClassyName);
                        player.World.Players.Where(p => p.Supports(CpeExt.SelectionCuboid)).Send(Packet.MakeMakeSelection(zone.ZoneID, zone.Name, zone.Bounds,
                            zone.Color, zone.Alpha));
                    }
                    return;
                } else if (color.ToLower().Equals("off") || color.ToLower().Equals("false") || color.ToLower().Equals("no")) {
                    zone.ShowZone = false;
                    player.Message("Zone ({0}&s) will no longer show its bounderies", zone.ClassyName);
                    player.World.Players.Where(p => p.Supports(CpeExt.SelectionCuboid)).Send(Packet.MakeRemoveSelection(zone.ZoneID));
                    return;
                } else {
                    player.Message("Error: \"#{0}\" is not a valid HEX color code.", color);
                    return;
                }
            } else {
                zone.Color = color.ToUpper();
            }

            if (alp == null) {
                player.Message("Error: Missing an Alpha integer");
                player.Message(CdZoneShow.Usage);
                return;
            }
            if (!short.TryParse(alp, out alpha)) {
                player.Message("Error: \"{0}\" is not a valid integer for Alpha.", alp);
                return;
            } else {
                zone.Alpha = alpha;
            }
            if (bol != null) {
                if (!bol.ToLower().Equals("on") && !bol.ToLower().Equals("off") && !bol.ToLower().Equals("true") &&
                    !bol.ToLower().Equals("false") && !bol.ToLower().Equals("0") && !bol.ToLower().Equals("1") &&
                    !bol.ToLower().Equals("yes") && !bol.ToLower().Equals("no")) {
                    zone.ShowZone = false;
                    player.Message("({0}) is not a valid bool statement", bol);
                } else if (bol.ToLower().Equals("on") || bol.ToLower().Equals("true") || bol.ToLower().Equals("1") ||
                           bol.ToLower().Equals("yes")) {
                    zone.ShowZone = true;
                    player.Message("Zone ({0}&s) color set! Bounderies: ON", zone.ClassyName);
                } else if (bol.ToLower().Equals("off") || bol.ToLower().Equals("false") || bol.ToLower().Equals("0") ||
                           bol.ToLower().Equals("no")) {
                    zone.ShowZone = false;
                    player.Message("Zone ({0}&s) color set! Bounderies: OFF", zone.ClassyName);
                }
            } else {
                zone.ShowZone = false;
                player.Message("Zone ({0}&s) color set!", zone.ClassyName);
            }
            if (zone != null) {
                foreach (Player p in player.World.Players) {
                    if (p.Supports(CpeExt.SelectionCuboid)) {
                        if (zone.ShowZone) {
                            p.Send(Packet.MakeMakeSelection(zone.ZoneID, zone.Name, zone.Bounds, zone.Color, alpha));
                        }
                    }
                }
            }
        }

        #endregion

    }
}
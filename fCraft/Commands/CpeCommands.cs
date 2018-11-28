﻿// ProCraft Copyright 2014-2018 Joseph Beauvais <123DMWM@gmail.com>
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
            CommandManager.RegisterCommand(Cdtex);
            CommandManager.RegisterCommand(CdtextHotKey);
            CommandManager.RegisterCommand(CdZoneShow);
            CommandManager.RegisterCommand(CdEntityRot);
        }

        /// <summary> Ensures that the hex color has the correct length (3 or 6 characters)
        /// and character set (alphanumeric chars allowed). </summary>
        public static bool IsValidHex(string hex) {
            if (hex == null) throw new ArgumentNullException("hex");
            if (hex.StartsWith("#")) hex = hex.Remove(0, 1);
            if (hex.Length != 3 && hex.Length != 6) return false;
            
            for (int i = 0; i < hex.Length; i++) {
                char ch = hex[i];
                if (ch < '0' || ch > '9'
                    && ch < 'A' || ch > 'F'
                    && ch < 'a' || ch > 'f') return false;
            }
            return true;
        }
        
        static Player FindPlayer(Player player, CommandReader cmd) {
            string name = cmd.Next();
            if (name == null) name = player.Name;
            return Server.FindPlayerOrPrintMatches(player, name, SearchOptions.IncludeSelf);
        }
        const string validModels = "Valid models: &SAny Block Name/ID, Chibi, Chicken, Creeper, Giant, Humanoid, Pig, Sheep, Skeleton, Spider, Zombie";

        #region AddEntity

        static readonly CommandDescriptor CdEntity = new CommandDescriptor {
            Name = "Entity",
            Aliases = new[] { "AddEntity", "AddEnt", "Ent" },
            Permissions = new[] { Permission.BringAll },
            Category = CommandCategory.CPE | CommandCategory.World | CommandCategory.New,
            Usage = "/ent <create / remove / removeAll / model / list / bring / skin>",
            Help = "Commands for manipulating entities. For help and usage for the individual options, use /help ent <option>.",
            HelpSections = new Dictionary<string, string>{
                { "create", "&H/Ent create <entity name> <model> <skin>&N&S" +
                        "Creates a new entity with the given name. " + validModels},
                { "remove", "&H/Ent remove <entity name> <world>&N&S" +
                        "Removes the given entity." },
                { "removeall", "&H/Ent removeAll&N&S" +
                        "Removes all entities from the world."},
                { "model", "&H/Ent model <entity name> <model>&N&S" +
                        "Changes the model of an entity to the given model. " + validModels},
                { "list", "&H/Ent list <world>&N&S" +
                        "Prints out a list of all the entites on the server."},
                { "bring", "&H/Ent bring <entity name>&N&S" +
                        "Brings the given entity to you."},
                { "skin", "&H/Ent skin <entity name> <skin url or name>&N&S" +
                        "Changes the skin of a bot."}
            },
            Handler = EntityHandler,
        };

        private static void EntityHandler(Player player, CommandReader cmd) {
            string option = cmd.Next();
            if (string.IsNullOrEmpty(option)) {
                CdEntity.PrintUsage(player);
                return;
            }
            if (option.CaselessEquals("reload") && player.Info.Rank == RankManager.HighestRank) {
                Entity.ReloadAll();
                player.Message("Reloaded Entities from file");
                return;
            }

            if (option.CaselessEquals("list")) {
                string search = cmd.Next() ?? player.World.Name;
                World world = WorldManager.FindWorldOrPrintMatches(player, search);
                if (world != null) {
                    player.Message("Entities on &f{0}&S: ", world.Name);
                    player.Message("  &F" + Entity.AllIn(world).JoinToString("&S, &F", n => n.Name));
                }
                return;
            }
            if (option.CaselessEquals("removeall")) {
                string search = cmd.Next() ?? player.World.Name;
                World world = WorldManager.FindWorldOrPrintMatches(player, search);
                if (cmd.IsConfirmed) {
                    Entity.RemoveAllIn(world);
                    player.Message("All entities on &f{0}&S removed.", player.World.Name);
                } else {
                    player.Confirm(cmd, "This will remove all the entites on {0}, are you sure?", player.World.Name);
                }
                return;
            }

            //finally away from the special cases
            string entityName = cmd.Next();
            if (string.IsNullOrEmpty(entityName)) {
                CdEntity.PrintUsage(player);
                return;
            }

            Entity entity = new Entity();
            if (option != "create" && option != "add") {
                entity = Entity.Find(player.World, entityName);
                if (entity == null) {
                    player.Message(
                        "Could not find {0}! Please make sure you spelled the entities name correctly. To view all the entities, type /ent list.",
                        entityName);
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
                    if (Entity.Exists(player.World, entityName)) {
                        player.Message("An entity with that name already exists! To view all entities, type /ent list.");
                        return;
                    }

                    string skinString1 = (cmd.Next() ?? entityName);
                    if (skinString1 != null) skinString1 = ParseSkin(skinString1);
                    
                    sbyte newEntityId = Entity.NextFreeID(player.World.Name);
                    if (newEntityId == Packet.SelfId) {
                        player.Message("No more free ids left! Remove an entity first.");
                    } else {
                        entity = Entity.Create(entityName, skinString1, requestedModel, player.World, player.Position, newEntityId);
                        player.Message("Successfully created entity {0}&S with id:{1} and skin {2}.", entity.Name, entity.ID, entity.Skin, entity.Name);
                    }
                    break;
                case "remove":
                    player.Message("{0} was removed from {1}", entity.Name, player.World.Name);
                    Entity.Remove(entity);
                    break;
                case "model":
                    if (cmd.HasNext) {
                        string model = cmd.Next().ToLower();
                        if (string.IsNullOrEmpty(model)) {
                        	player.Message("Usage is /Ent model <bot> <model>. " + validModels);
                            break;
                        }
                        model = ParseModel(player, model);
                        if (model == null) {
                            player.Message("That wasn't a valid entity model! " + validModels);
                            break;
                        }
                        player.Message("Changed entity model to {0}.", model);
                        entity.ChangeModel(model);
                    } else {
                        player.Message("Usage is /Ent model <bot> <model>. " + validModels);
                    }
                    break;
                case "bring":
                    entity.TeleportTo(player.Position);
                    break;
                case "tp":
                case "teleport":
                    World targetWorld = entity.WorldIn();
                    if (targetWorld == player.World) {
                        if (player.World != null) {
                            player.LastWorld = player.World;
                            player.LastPosition = player.Position;
                        }
                        player.TeleportTo(entity.GetPos());
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
                                    player.Message("Cannot teleport to {0}&S.", entity.Name,
                                                   targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                    break;
                                }
                                if (targetWorld.IsFull) {
                                    player.Message("Cannot teleport to {0}&S because world {1}&S is full.",
                                                   entity.Name, targetWorld.ClassyName);
                                    player.Message("Cannot teleport to {0}&S.", entity.Name,
                                                   targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                    break;
                                }
                                player.StopSpectating();
                                player.JoinWorld(targetWorld, WorldChangeReason.Tp, entity.GetPos());
                                break;
                            case SecurityCheckResult.BlackListed:
                                player.Message("Cannot teleport to {0}&S because you are blacklisted on world {1}",
                                               entity.Name, targetWorld.ClassyName);
                                break;
                            case SecurityCheckResult.RankTooLow:
                                if (player.Info.Rank.Name == "Banned") {
                                    player.Message("&CYou can not change worlds while banned.");
                                    player.Message("Cannot teleport to {0}&S.", entity.Name,
                                                   targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                    break;
                                }

                                if (targetWorld.IsFull) {
                                    if (targetWorld.IsFull) {
                                        player.Message("Cannot teleport to {0}&S because world {1}&S is full.",
                                                       entity.Name, targetWorld.ClassyName);
                                        player.Message("Cannot teleport to {0}&S.", entity.Name,
                                                       targetWorld.ClassyName, targetWorld.AccessSecurity.MinRank.ClassyName);
                                        break;
                                    }
                                    player.StopSpectating();
                                    player.JoinWorld(targetWorld, WorldChangeReason.Tp, entity.GetPos());
                                    break;
                                }
                                player.Message("Cannot teleport to {0}&S because world {1}&S requires {2}+&S to join.",
                                               entity.Name, targetWorld.ClassyName,
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
                        skinString3 = ParseSkin(skinString3);
                    }
                    player.Message("Changed entity skin to {0}.", skinString3 ?? entity.Name);
                    entity.ChangeSkin(skinString3);
                    break;
                default:
                    CdEntity.PrintUsage(player);
                    break;
            }
        }

        #endregion

        #region ChangeModel

        static readonly CommandDescriptor CdChangeModel = new CommandDescriptor {
            Name = "Model",
            Aliases = new[] { "ChangeModel", "cm" },
            Category = CommandCategory.CPE | CommandCategory.Moderation | CommandCategory.New,
            Permissions = new[] { Permission.ReadStaffChat },
            Usage = "/Model [Player] [Model]",
            IsConsoleSafe = true,
            Help = "Change the Model or Skin of [Player]!&N" + validModels,
            Handler = ModelHandler
        };

        private static void ModelHandler(Player player, CommandReader cmd) {
            SetModel(player, cmd, "", p => p.Info.Model, (p, value) => p.Info.Model = value);
        }

        static readonly CommandDescriptor CdAFKModel = new CommandDescriptor {
            Name = "AFKModel",
            Category = CommandCategory.New,
            Permissions = new[] { Permission.Chat },
            Usage = "/AFKModel [Player] [Model]",
            IsConsoleSafe = true,
            Help = "Changes the model of a player when they are AFK.&N" + validModels,
            Handler = AFKModelHandler
        };

        private static void AFKModelHandler(Player player, CommandReader cmd) {
            SetModel(player, cmd, "AFK ",
                     p => p.AFKModel,
                     (p, value) => p.AFKModel = value);
        }
        
        static void SetModel(Player player, CommandReader cmd, string prefix,
                             Func<Player, string> getter, Action<Player, string> setter) {
            Player target = FindPlayer(player, cmd);
            if (target == null) return;

            if (!player.IsStaff && target != player) {
                Rank staffRank = RankManager.GetMinRankWithAnyPermission(Permission.ReadStaffChat);
                if (staffRank != null) {
                    player.Message("You must be {0}&S+ to change another player's {1}Model",
                                   staffRank.ClassyName, prefix);
                } else {
                    player.Message("No ranks have the ReadStaffChat permission," +
                                   "so no one can change other player's {0}Model, yell at the owner.", prefix);
                }
                return;
            }
            if (target.Info.Rank.Index < player.Info.Rank.Index) {
                player.Message("Cannot change the {0}Model of someone higher rank than you.", prefix); return;
            }

            string model = cmd.Next();
            if (string.IsNullOrEmpty(model)) {
                player.Message("Current {2}Model for {0}: &f{1}", target.Name, getter(target), prefix);
                return;
            }
            
            model = ParseModel(player, model);
            if (model == null) {
                player.Message("Model not valid, see &H/Help {0}Model&S.", prefix.TrimEnd());
                return;
            }
            if (getter(target).CaselessEquals(model)) {
                player.Message("&f{0}&S's {2}model is already set to &f{1}", target.Name, model, prefix);
                return;
            }
            
            target.Message("&f{0}&SChanged your {3}model from &f{1} &Sto &f{2}",
                           (target == player ? "" : player.Name + " "), getter(target), model, prefix);
            if (target != player) {
                player.Message("Changed {3}model of &f{0} &Sfrom &f{1} &Sto &f{2}",
                               target.Name, getter(target), model, prefix);
            }
            
            target.oldMob = target.Info.Model;
            target.oldafkMob = target.afkMob;
            setter(target, model);
        }
        
        internal static string ParseModel(Player player, string model) {
            model = model ?? "humanoid";
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
            
            byte blockId;
            Block block;
            if (byte.TryParse(model, out blockId)) {
            } else if (Map.GetBlockByName(player.World, model, false, out block)) {
                model = ((byte)block).ToString();
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
            if (!cmd.HasNext) { CdChangeSkin.PrintUsage(player); return; }            
            Player target = FindPlayer(player, cmd);
            if (target == null) return;

            string skin = cmd.Next();
            if (skin == null) { CdChangeSkin.PrintUsage(player); return; }            
            
            skin = ParseSkin(skin);
            FilterURL(ref skin);
            if (target.Info.skinName == skin) {
                player.Message("&f{0}&S's skin is already set to &f{1}", target.Name, skin);
                return;
            }
            
            target.Message("&f{0}&Shanged your skin from &f{1} &Sto &f{2}", 
                           (target == player ? "&SC" : player.Name + " &Sc"), target.oldskinName, skin);
            
            if (player != target) {
                player.Message("Changed skin of &f{0} &Sto &f{1}", target.Name, skin);
            }
            
            target.oldskinName = target.Info.skinName;
            target.Info.skinName = skin;
        }
        
        static string ParseSkin(string skin) {
            if (skin.StartsWith("--")) {
                return "http://minecraft.net/skin/" + skin.Substring(2) + ".png";
            }
            if (skin.StartsWith("-+")) {
                return "http://skins.minecraft.net/MinecraftSkins/" + skin.Substring(2) + ".png";
            }
            if (skin.StartsWith("++")) {
                return "http://i.imgur.com/" + skin.Substring(2) + ".png";
            }
            return skin;
        }

        #endregion

        #region ClickDistance

        static readonly CommandDescriptor Cdclickdistance = new CommandDescriptor {
            Name = "ReachDistance",
            Aliases = new[] { "Reach", "rd" },
            Permissions = new[] { Permission.DrawAdvanced },
            IsConsoleSafe = true,
            Category = CommandCategory.CPE | CommandCategory.World | CommandCategory.New,
            Help = "Changes player reach distance. Every 32 is one block. Default: 160",
            Usage = "/reach [Player] [distance or reset]",
            Handler = ClickDistanceHandler
        };

        static void ClickDistanceHandler(Player player, CommandReader cmd) {
            Player target = FindPlayer(player, cmd);
            if (target == null) return;

            if (!player.IsStaff && target != player) {
                Rank staffRank = RankManager.GetMinRankWithAnyPermission(Permission.ReadStaffChat);
                if (staffRank != null) {
                    player.Message("You must be {0}&S+ to change another players reach distance", staffRank.ClassyName);
                } else {
                    player.Message("No ranks have the ReadStaffChat permission so no one can change other players reachdistance, yell at the owner.");
                }
                return;
            }
            if (target.Info.Rank.Index < player.Info.Rank.Index) {
                player.Message("Cannot change the Reach Distance of someone higher rank than you.");
                return;
            }
            
            string rawDist = cmd.Next();
            if (string.IsNullOrEmpty(rawDist)) {
                if (target == player) {
                    player.Message("Your current ReachDistance: {0} blocks [Units: {1}]", player.Info.ReachDistance / 32, player.Info.ReachDistance);
                } else {
                    player.Message("Current ReachDistance for {2}: {0} blocks [Units: {1}]", target.Info.ReachDistance / 32, target.Info.ReachDistance, target.Name);
                }
                return;
            }
            
            short distance;
            if (!short.TryParse(rawDist, out distance)) {
                if (rawDist != "reset") {
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
            
            if (distance == target.Info.ReachDistance) {
                if (player != target) {
                    player.Message("{0}'s reach distance is already set to {1}", target.ClassyName, target.Info.ReachDistance);
                } else {
                    player.Message("Your reach distance is already set to {0}", target.Info.ReachDistance);
                }
                return;
            }

            if (player != target) {
                if (target.Supports(CpeExt.ClickDistance)) {
                    target.Message("{0} set your reach distance from {1} to {2} blocks [Units: {3}]", 
                                   player.Name, target.Info.ReachDistance / 32, distance / 32, distance);
                    player.Message("Set reach distance for {0} from {1} to {2} blocks [Units: {3}]", 
                                   target.Name, target.Info.ReachDistance / 32, distance / 32, distance);
                    target.Info.ReachDistance = distance;
                    target.Send(Packet.MakeSetClickDistance(distance));
                } else {
                    player.Message("This player does not support ReachDistance packet");
                }
            } else {
                if (player.Supports(CpeExt.ClickDistance)) {
                    player.Message("Set own reach distance from {0} to {1} blocks [Units: {2}]", 
                                   player.Info.ReachDistance / 32, distance / 32, distance);
                    player.Info.ReachDistance = distance;
                    player.Send(Packet.MakeSetClickDistance(distance));
                } else {
                    player.Message("You don't support ReachDistance packet");
                }
            }
        }

        #endregion

        #region CustomColors

        static readonly CommandDescriptor CdCustomColors = new CommandDescriptor {
            Name = "CustomColors",
            Aliases = new[] { "ccols" },
            Category = CommandCategory.CPE | CommandCategory.Chat | CommandCategory.New,
            Permissions = new[] { Permission.Chat },
            Usage = "/ccols [type] [args]",
            IsConsoleSafe = true,
            Help = "&SModifies the custom colors, or prints information about them.&N" +
                "&STypes are: add, free, list, remove&N" +
                "&SSee &H/help ccols <type>&S for details about each type.",
            HelpSections = new Dictionary<string, string>{
                { "add",     "&H/ccols add [code] [name] [fallback] [hex]&N" +
                        "&Scode is in ASCII. You cannot replace the standard color codes.&N" +
                        "&Sfallback is a standard color code, shown to non-supporting clients.&N" },
                { "free",    "&H/ccols free&N" +
                        "&SPrints a list of free/unused available color codes." },
                { "list",    "&H/ccols list [offset]&N" +
                        "&SPrints a list of the codes, names, and fallback codes of the custom colors. " },
                { "remove",  "&H/ccols remove [code]&N" +
                        "&SRemoves the custom color which has the given color code." }
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
            if (cmd.CountRemaining < 4) { p.Message("Usage: &H/ccols add [code] [name] [fallback] [hex]"); return; }
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
                          ", you must either use a different code or use \"&H/ccols remove " + code + "&S\"");
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
            if (!IsValidHex(hex)) {
                p.Message("\"#{0}\" is not a valid hex color.", hex.Replace("#", "")); return;
            }

            CustomColor col = Color.ParseHex(hex);
            col.Code = code; col.Fallback = fallback; col.Name = name;
            Color.AddExtColor(col);
            p.Message("Successfully added a custom color. &{0} %{0} {1}", col.Code, col.Name.ToLower().UppercaseFirst());
        }

        static void RemoveCustomColorsHandler(Player p, CommandReader cmd) {
            string fullCode = cmd.Next();
            if (fullCode == null) { p.Message("Usage: &H/ccols remove [code]"); return; }
            if (!p.Can(Permission.DefineCustomBlocks)) {
                p.MessageNoAccess(Permission.DefineCustomBlocks);
                return;
            }

            char code = fullCode[0];
            if (Color.IsStandardColorCode(code)) {
                p.Message(code + " is a standard color, and thus cannot be removed."); return;
            }

            if ((int)code >= 256 || Color.ExtColors[code].Undefined) {
                p.Message("There is no custom color with the code " + code + ".");
                p.Message("Use \"&H/ccols list\" &Sto see a list of custom colors.");
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
                    const string format = "{0} - %{1} displays as &{1}{2}&S, and falls back to {3}.";
                    p.Message(format, col.Name, col.Code, Hex(col), col.Fallback);

                    if (count >= 8) {
                        const string helpFormat = "To see the next set of custom colors, type &H/ccols list {0}";
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
            Category = CommandCategory.CPE | CommandCategory.World | CommandCategory.New,
            Permissions = new[] { Permission.ManageWorlds },
            Help = "Prints or changes environmental variables for a world.&N" +
                "Variables are: border, clouds, edge, fog, level, cloudsheight, shadow, sky, sunlight, " +
                "texture, weather, maxfog, cloudspeed, weatherspeed, weatherfade, sidesoffset, skyboxhorspeed, skyboxverspeed&N" +
                "See &H/Help env <Variable>&S for details about a variable.&N" +
                "Type &H/Env <WorldName> normal&S to reset everything.",
            HelpSections = new Dictionary<string, string>{
                { "normal",     "&H/Env <WorldName> normal&N&S" +
                        "Resets all environment settings to their defaults for the given world." },
                { "clouds",     "&H/Env <WorldName> clouds <Color>&N&S" +
                        "Sets color of the clouds." +
                        "&NUse \"normal\" instead of color to reset." },
                { "fog",        "&H/Env <WorldName> fog <Color>&N&S" +
                        "Sets color of the fog. Sky color blends with fog color in the distance. " +
                        "&NUse \"normal\" instead of color to reset." },
                { "shadow",     "&H/Env <WorldName> shadow <Color>&N&S" +
                        "Sets color of the shadowed areas. " +
                        "&NUse \"normal\" instead of color to reset." },
                { "sunlight",   "&H/Env <WorldName> sunlight <Color>&N&S" +
                        "Sets color of the lighted areas. " +
                        "&NUse \"normal\" instead of color to reset." },
                { "sky",        "&H/Env <WorldName> sky <Color>&N&S" +
                        "Sets color of the sky. Sky color blends with fog color in the distance. " +
                        "&NUse \"normal\" instead of color to reset." },
                { "level",      "&H/Env <WorldName> level <#>&N&S" +
                        "Sets height of the map edges/water level, in terms of blocks from the bottom of the map. " +
                        "&NUse \"normal\" instead of a number to reset to default (middle of the map)." },
                { "sideoffset", "&H/Env <WorldName> sideoffset <#>&N&S" +
                        "Sets height of the map sides/bedrock level, in terms of offset vertically from map edges height. " +
                        "&NUse \"normal\" instead of a number to reset to default (-2)." },
                { "cloudsheight","&H/Env <WorldName> cloudsheight <#>&N&S" +
                        "Sets height of the clouds, in terms of blocks from the bottom of the map. " +
                        "&NUse \"normal\" instead of a number to reset to default (map height + 2)." },
                { "edge",       "&H/Env <WorldName> edge <BlockType>&N&S" +
                        "Changes the type of block that's visible beyond the map boundaries. "+
                        "&NUse \"normal\" instead of a number to reset to default (water)." },
                { "border",     "&H/Env <WorldName> border <BlockType>&N&S" +
                        "Changes the type of block that's visible on sides the map boundaries. "+
                        "&NUse \"normal\" instead of a number to reset to default (bedrock)." },
                { "texture",    "&H/Env <WorldName> texture <Texture .PNG Url>&N&S" +
                        "Changes the texture for all visible blocks on a map. "+
                        "&NUse \"normal\" instead of a web link to reset to default (" + Server.DefaultTerrain + ")" },
                { "weather",    "&H/Env <WorldName> weather <0,1,2/sun,rain,snow>&N&S" +
                        "Changes the weather on a specified map. "+
                        "&NUse \"normal\" instead to use default (0/sun)" },
                { "maxfog",     "&H/Env <WorldName> maxfog <#>&N&S" +
                        "Sets the maximum distance clients can see around them. " +
                        "&NUse \"normal\" instead of a number to reset to default (0)." },
                { "cloudspeed", "&H/Env <WorldName> cloudspeed <#>&N&S" +
                        "Sets how fast clouds travel across the sky, relative to normal speed. Can be negative." +
                        "&NUse \"normal\" instead of a number to reset to default (0)." },
                { "weatherspeed","&H/Env <WorldName> weatherspeed <#>&N&S" +
                        "Sets how fast rain/snow falls, relative to normal speed. Can be negative." +
                        "&NUse \"normal\" instead of a number to reset to default (0)." },
                { "weatherfade","&H/Env <WorldName> weatherfade <#>&N&S" +
                        "Sets how quickly rain/snow fades, relative to normal rate." +
                        "&NUse \"normal\" instead of a number to reset to default (0)." },
                { "sidesoffset", "&H/Env <WorldName> sidesoffset <#>&N&S" +
                        "Sets how far below (or above) the border block is compared to the edge block. " +
                        "&NUse \"normal\" instead of a number to reset to default (-2)." },          	
                { "skyboxhorspeed","&H/Env <WorldName> skyoxhorspeed <#>&N&S" +
                        "Sets how quickly skybox rotates horizontally around." +
            			"&Ne.g. 0.5 means it rotates 360 degrees every two seconds." +
                        "&NUse \"normal\" instead of a number to reset to default (0)." },
                { "skyboxverspeed","&H/Env <WorldName> skyoxverspeed <#>&N&S" +
                        "Sets how quickly skybox rotates vertically around." +
            			"&Ne.g. 0.5 means it rotates 360 degrees every two seconds." +
                        "&NUse \"normal\" instead of a number to reset to default (0)." },
            },
            Usage = "/Env <WorldName> <Variable>",
            IsConsoleSafe = true,
            Handler = EnvHandler
        };

        static void EnvHandler(Player player, CommandReader cmd) {
            string arg1 = cmd.Next(), arg2 = cmd.Next(), arg3 = cmd.Next();
            World world;
            
            // Print own world info when just /env
            if (arg1 == null) {
                world = player.World;
                if (world == null) { player.Message("When used from console, /Env requires a world name."); return; }
                ShowEnvSettings(player, world);
                return;
            }
            // Print that world's info when /env worldname
            if (arg2 == null) {
                world = WorldManager.FindWorldOrPrintMatches(player, arg1);
                if (world == null) return;
                ShowEnvSettings(player, world);
                return;
            }

            // Reset env when /env worldname normal
            if (arg2.CaselessEquals("normal")) {
                world = WorldManager.FindWorldOrPrintMatches(player, arg1);
                if (world == null) return;
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
            
            // Handle either /env var value (using current world), or /env world var value
            if (arg3 == null && player.World == null) {
                player.Message("When used from console, /Env requires a world name."); return;
            }
            world = arg3 == null ? player.World : WorldManager.FindWorldOrPrintMatches(player, arg1);
            if (world == null) return;
            string variable = arg3 == null ? arg1 : arg2;
            string value = arg3 == null ? arg2 : arg3;
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
                case "sideoffset":
                case "sidesoffset":
                case "bedrockoffset":
                    SetEnvAppearanceShort(player, world, value, EnvProp.SidesOffset,
                                          "bedrock offset", -2, ref world.SidesOffset);
                    break;
                case "fogdist":
                case "maxfog":
                case "maxdist":
                    SetEnvAppearanceShort(player, world, value, EnvProp.MaxFog,
                                          "max fog distance", 0, ref world.MaxFogDistance);
                    break;
                case "cloudspeed":  
                case "cloudsspeed":
                    SetEnvAppearanceFloat(player, world, value, EnvProp.CloudsSpeed, "clouds speed",
                                          -32767, 32767, 256, 256, ref world.CloudsSpeed);
                    break;
                case "weatherfade":
                    SetEnvAppearanceFloat(player, world, value, EnvProp.WeatherFade, "weather fade rate",
                                          0, 255, 128, 128, ref world.WeatherFade);
                    break;
                case "weatherspeed":
                    SetEnvAppearanceFloat(player, world, value, EnvProp.WeatherSpeed, "weather speed",
                                          -32767, 32767, 256, 256, ref world.WeatherSpeed);
                    break;
                case "skyboxhorspeed":
                case "skyboxhor":
                    SetEnvAppearanceFloat(player, world, value, EnvProp.SkyboxHorSpeed, "skybox horizontal speed",
                                          -32767, 32767, 1024, 0, ref world.SkyboxHorSpeed);
                    break;
                case "skyboxverspeed":
                case "skyboxver":
                    SetEnvAppearanceFloat(player, world, value, EnvProp.SkyboxVerSpeed, "skybox vertical speed",
                                          -32767, 32767, 1024, 0, ref world.SkyboxVerSpeed);
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
                    FilterURL(ref value);
                    if (value.CaselessEquals("default")) {
                        player.Message("Reset texture for {0}&S to {1}", world.ClassyName, Server.DefaultTerrain);
                        value = "Default";
                    } else if (!value.EndsWith(".png") && !value.EndsWith(".zip")) {
                        player.Message("Env Texture: Invalid image type. Please use a \".png\" or \".zip\"", world.ClassyName);
                        return;
                    } else if (!(value.CaselessStarts("http://") || value.CaselessStarts("https://"))) {
                        player.Message("Env Texture: Invalid URL. Please use a \"http://\" or \"https://\" type url.", world.ClassyName);
                        return;
                    } else {
                        player.Message("Set texture for {0}&S to {1}", world.ClassyName, value);
                    }
                    
                    world.Texture = value;
                    foreach (Player p in world.Players) {
                        if (p.Supports(CpeExt.EnvMapAspect))
                            p.Send(Packet.MakeEnvSetMapUrl(world.GetTexture(), p.HasCP437));
                        else if (p.Supports(CpeExt.EnvMapAppearance) || p.Supports(CpeExt.EnvMapAppearance2))
                            p.SendEnvSettings();
                    }
                    break;

                case "weather":
                    byte weather = 0;
                    if (value.CaselessEquals("normal")) {
                        player.Message("Reset weather for {0}&S to normal(0) ", world.ClassyName);
                        world.Weather = 0;
                    } else {
                        if (!byte.TryParse(value, out weather)) {
                            if (value.CaselessEquals("sun")) {
                                weather = 0;
                            } else if (value.CaselessEquals("rain")) {
                                weather = 1;
                            } else if (value.CaselessEquals("snow")) {
                                weather = 2;
                            }
                        }
                        if (weather < 0 || weather > 2) {
                            player.Message("Please use a valid integer(0,1,2) or string(sun,rain,snow)");
                            return;
                        }
                        world.Weather = weather;
                        player.Message("&aSet weather for {0}&a to {1} ({2}&a)", world.ClassyName, weather, weather == 0 ? "&SSun" : (weather == 1 ? "&1Rain" : "&fSnow"));
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
                
        static void FilterURL(ref string url) {
            // a lot of people try linking to the dropbox page instead of directly to file, so we auto correct them
            if (url.StartsWith("http://www.dropbox")) {
                url = "http://dl.dropbox" + url.Substring("http://www.dropbox".Length);
                url = url.Replace("?dl=0", "");
            } else if (url.StartsWith("https://www.dropbox")) {
                url = "https://dl.dropbox" + url.Substring("https://www.dropbox".Length);
                url = url.Replace("?dl=0", "");
            } 
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
                           (world.CloudsSpeed / 256f).ToString("F2") + "%",
                           (world.WeatherSpeed / 256f).ToString("F2") + "%");
            player.Message("  Weather fade rate: {0}",
                           (world.WeatherFade / 128f).ToString("F2") + "%");
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
            world.SidesOffset = -2;
            world.CloudsHeight = short.MinValue;
            world.MaxFogDistance = 0;
            world.EdgeBlock = (byte)Block.Admincrete;
            world.HorizonBlock = (byte)Block.Water;
            world.Texture = "Default";
            world.WeatherSpeed = 256;
            world.CloudsSpeed = 256;
            world.WeatherFade = 128;
            world.SkyboxHorSpeed = 0;
            world.SkyboxVerSpeed = 0;
            
            Logger.Log(LogType.UserActivity,
                       "Env: {0} {1} reset environment settings for world {2}",
                       player.Info.Rank.Name, player.Name, world.Name);
            player.Message("Enviroment settings for world {0} &Swere reset.", world.ClassyName);
            WorldManager.SaveWorldList();
            foreach (Player p in world.Players) {
                if (p.Supports(CpeExt.EnvMapAppearance) || p.Supports(CpeExt.EnvMapAppearance2)
                    || p.Supports(CpeExt.EnvMapAspect))
                    p.SendEnvSettings();
            }
        }

        static void SetEnvColor(Player player, World world, string value,
                                string name, EnvVariable variable, ref string target) {
            if (IsReset(value)) {
                player.Message("Reset {0} for {1}&S to normal", name, world.ClassyName);
                target = null;
            } else if (!IsValidHex(value)) {
                player.Message("Env: \"#{0}\" is not a valid HEX color code.", value);
                return;
            } else {
                target = value;
                player.Message("Set {0} for {1}&S to #{2}", name, world.ClassyName, value);
            }

            foreach (Player p in world.Players) {
                if (p.Supports(CpeExt.EnvColors))
                    p.Send(Packet.MakeEnvSetColor((byte)variable, target));
            }
        }
        
        static void SetEnvAppearanceFloat(Player player, World world, string value, EnvProp prop,
                                          string name, float min, float max, int scale,
                                          short defValue, ref short target) {
            float amount;
            min /= scale; max /= scale;
            if (IsReset(value)) {
                player.Message("Reset {0} for {1}&S to normal", name, world.ClassyName);
                target = defValue;
            } else if (!float.TryParse(value, out amount)) {
                player.Message("Env: \"{0}\" is not a valid decimal.", value);
                return;
            } else if (amount < min || amount > max) {
                player.Message("Env: \"{0}\" must be between {1} and {2}.",
                               value, min.ToString("F2"), max.ToString("F2"));
                return;
            } else {
                target = (short)(amount * scale);
                player.Message("Set {0} for {1}&S to {2}", name, world.ClassyName, amount);
            }
            UpdateAppearance(world, prop, target);
        }

        static void SetEnvAppearanceShort(Player player, World world, string value, EnvProp prop,
                                          string name, short defValue, ref short target) {
            short amount;
            if (IsReset(value)) {
                player.Message("Reset {0} for {1}&S to normal", name, world.ClassyName);
                target = defValue;
            } else if (!short.TryParse(value, out amount)) {
                player.Message("Env: \"{0}\" is not a valid integer.", value);
                return;
            } else {
                target = amount;
                player.Message("Set {0} for {1}&S to {2}", name, world.ClassyName, amount);
            }
            UpdateAppearance(world, prop, target);
        }

        static void SetEnvAppearanceBlock(Player player, World world, string value, EnvProp prop,
                                          string name, Block defValue, ref byte target) {
            if (IsReset(value)) {
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
            UpdateAppearance(world, prop, target);
        }
        
        static bool IsReset(string value) {
            return value.Equals("-1") || value.CaselessEquals("normal")
                || value.CaselessEquals("reset") || value.CaselessEquals("default");
        }
        
        static void UpdateAppearance(World world, EnvProp prop, int target) {
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
            Category = CommandCategory.CPE | CommandCategory.World | CommandCategory.New,
            Permissions = new[] { Permission.ManageWorlds },
            Help = "Environment preset commands&N" +
                "Options are: Delete, Edit, Info, List, Load, Save&N" +
                "See &H/Help EnvPreset <option>&S for details about each variable. ",
            HelpSections = new Dictionary<string, string>{
                { "save",   "&H/EnvPreset Save <PresetName> &N&S" +
                        "Saves Env settings to a defined preset name." },
                { "load",   "&H/EnvPreset Load <PresetName>&N&S" +
                        "Loads an Env preset to a specified world." },
                { "delete", "&H/EnvPreset Delete <PresetName>&N&S" +
                        "Deleted a defined Env preset." },
                { "info",   "&H/EnvPreset Info <PresetName>&N&S" +
                        "Displays Env settings of a defined Preset." },
                { "list",   "&H/EnvPreset List&N&S" +
                        "Lists all Env presets by name."},
                { "update", "&H/EnvPreset Update <PresetName>&N&S" +
                        "Updates an Env preset with the current world settings."}
            },
            Usage = "/EnvPreset <Option> [Args]",
            Handler = EnvPresetHandler
        };

        static void EnvPresetHandler(Player player, CommandReader cmd) {
            string option = cmd.Next();
            string name = cmd.NextAll();
            World world = player.World;
            EnvPresets preset;
            if (string.IsNullOrEmpty(option)) {
                CdEnvPreset.PrintUsage(player);
                return;
            }
            if (!option.CaselessEquals("list") && !option.CaselessEquals("reload") && string.IsNullOrEmpty(name)) {
                CdEnvPreset.PrintUsage(player);
                return;
            }

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
                    if ((preset = EnvPresets.Find(name)) != null) {
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
                    string list = "Presets: &N";
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
            Category = CommandCategory.CPE | CommandCategory.World | CommandCategory.New,
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
            return "&SModifies the " + scope + " custom blocks, or prints information about them.&N" +
                "&STypes are: add, abort, duplicate, edit, info, list, remove, texture&N" +
                "&SSee &H/help " + name + " <type>&S for details about each type.";
        }
        
        static Dictionary<string, string> MakeHelpSections(string scope, string name) {
            return new Dictionary<string, string>{
                { "add",     "&H" + name + " add [id]&N" +
                        "&SBegins the process of defining a " + scope + " custom block with the given block id." },
                { "abort",   "&H" + name + " abort&N" +
                        "&SAborts the custom block that was currently in the process of being " +
                        "defined from the last &H" + name + " add &Scall." },
                { "duplicate",     "&H" + name + " duplicate [source id] [new id]&N" +
                        "&SCreates a new custom block, using all the data of the given existing " + scope + " custom block. " },
                { "edit",     "&H" + name + " edit [id] [option] {args}&N" +
                        "&SEdits already defined blocks so you don't have to re-add them to change something. " +
                        "Options: Name, Solidity, Speed, AllId, TopId, SideID, BottomID, Light, Sound, FullBright, Shape, Draw, FogDensity, (FogHex or FogR, FogG, FogB), FallBack, Order"},
                { "reload",     "&H" + name + " reload&N" +
                        "&SReloads all " + scope + " custom blocks from file. " },
                { "info",     "&H" + name + " info [id]&N" +
                        "&SDisplays information about the given " + scope + " custom block." },
                { "list",    "&H" + name + " list [offset]&N" +
                        "&SPrints a list of the names of the " + scope + " custom blocks, " +
                        "along with their corresponding block ids. " },
                { "remove",  "&H" + name + " remove [id]&N" +
                        "&SRemoves the " + scope + " custom block which as the given block id." },
                { "texture",  "&H" + name + " tex&N" +
                        "&SShows you the terrain link of the current world and a link of the default with ID's overlayed." },
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
                case "reload":
                    if (p.Info.Rank == RankManager.HighestRank && p.Can(Permission.ShutdownServer)) {
                        BlockDefinition.ReLoadGlobalDefinitions();
                    }
                    break;
                case "i":
                case "info":
                    CustomBlockInfoHandler(p, cmd, global, defs); break;
                case "list":
                    CustomBlockListHandler(p, cmd, global, defs); break;
                case "remove":
                case "delete":
                    CustomBlockRemoveHandler(p, cmd, global, defs); break;
                case "tex":
                case "texture":
                case "terrain":
                    p.Message("Terrain IDs: &9http://123dmwm.tk/ID-Overlay.png");
                    p.Message("Current world terrain: &9{0}", p.World.Texture.CaselessEquals("default") ? Server.DefaultTerrain : p.World.Texture);
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
                p.Message("Use \"&H{1} remove {0}&S\" this block first.", blockId, name);
                p.Message("Use \"&H{1} list&S\" to see a list of {0} custom blocks.", scope, name);
                return;
            }

            p.currentBD = new BlockDefinition();
            p.currentBD.BlockID = (byte)blockId;
            p.Message("   &bSet block id to: " + blockId);
            p.Message("From now on, use &H{0} [value]&S to enter arguments.", name);
            p.Message("You can abort the currently partially " +
                      "created custom block at any time by typing \"&H{0} abort&S\"", name);

            p.currentBDStep = 0;
            PrintStepHelp(p);
        }

        static void CustomBlockInfoHandler(Player p, CommandReader cmd, bool global, BlockDefinition[] defs) {
            string scope = global ? "global" : "level";
            string name = global ? "/gb" : "/lb";
            
            Block id;
            if (!cmd.NextBlock(p, false, out id)) return;
            
            BlockDefinition def = GetCustomBlock(global, defs, (byte)id);
            if (def == null) {
                p.Message("No {0} custom block by the Name/ID", scope);
                p.Message("Use \"&H{1} list\" &Sto see a list of {0} custom blocks.", scope, name);
                return;
            }
            Block fallback = (Block)def.FallBack;
            
            p.Message("&3---Name&3:&a{0} &3ID:&a{1}&3---", def.Name, def.BlockID);
            p.Message("   &3FallBack: &a{0}&3, Solidity: &a{2}&3, Speed: &a{1}",
                      Map.GetBlockName(p.World, fallback), def.Speed, def.CollideType);
            p.Message("   &3Top ID: &a{0}&3, Bottom ID: &a{1}",
                      def.TopTex, def.BottomTex);
            p.Message("   &3Left ID: &a{0}&3, Right ID: &a{1}&3, Front ID: &a{2}&3, Back ID: &a{3}",
                      def.LeftTex, def.RightTex, def.FrontTex, def.BackTex);
            p.Message("   &3Block Light: &a{0}&3, Sound: &a{1}&3, FullBright: &a{2}",
                      def.BlocksLight, def.WalkSound, def.FullBright);
            p.Message("   &3Shape: &a{0}&3, Draw: &a{1}&3, Fog Density: &a{2}",
                      def.Shape, def.BlockDraw, def.FogDensity);
            p.Message("   &3Fog Red: &a{0}&3, Fog Green: &a{1}&3, Fog Blue: &a{2}",
                      def.FogR, def.FogG, def.FogB);
            p.Message("   &3Min: (&a{0}&3, &a{1}&3, &a{2}&3), Max: (&a{3}&3, &a{4}&3, &a{5}&3)",
                      def.MinX, def.MinY, def.MinZ, def.MaxX, def.MaxY, def.MaxZ);
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
                    p.Message("{2} custom block &H{0} &Sname is &H{1}", def.BlockID, def.Name, scope);

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
            Block blockID;
            string scope = global ? "global" : "level";
            string name = global ? "/gb" : "/lb";
            if (!cmd.NextBlock(p, false, out blockID))  return;
            
            BlockDefinition def = GetCustomBlock(global, defs, (byte)blockID);
            if (def == null) {
                p.Message("There is no {0} custom block with that name/id.", scope);
                p.Message("Use \"&H{1} list\" &Sto see a list of {0} custom blocks.", scope, name);
                return;
            }

            BlockDefinition.Remove(def, defs, p.World);
            if (global) {
                Server.Message("{0} &Sremoved the {3} custom block &H{1} &Swith ID {2}",
                               p.ClassyName, def.Name, def.BlockID, scope);
            } else {
                p.World.Players.Message("{0} &Sremoved the {3} custom block &H{1} &Swith ID {2}",
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
                    step++; def.Name = args;
                    p.Message("   &bSet name to: " + def.Name);
                    break;
                case 1:
                    if (byte.TryParse(args, out value)) {
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
                        step++; def.SetSidesTex(value);
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
                        step++; def.FullBright = boolVal;
                        p.Message("   &bSet full bright to: " + boolVal);
                    }
                    break;
                case 9:
                    if (args.CaselessEquals("-1")) {
                        p.Message("   &bBlock will display as a Sprite");
                        def.Shape = 0;
                        def.MinX = 0; def.MinY = 0; def.MinZ = 0;
                        def.MaxX = 16; def.MaxY = 16; def.MaxZ = 16;
                        step += 3;
                        break;
                    }
                    
                    string[] minArgs = args.Split();
                    if (minArgs.Length != 3) {
                        p.Message("Please specify 3 coordinates");
                        return;
                    }
                    
                    byte minx, miny, minz;
                    if (byte.TryParse(minArgs[0], out minx)
                        && byte.TryParse(minArgs[1], out miny)
                        && byte.TryParse(minArgs[2], out minz)
                        && (minx >= 0 && minx <= 15)
                        && (miny >= 0 && miny <= 15)
                        && (minz >= 0 && minz <= 15)) {
                    } else {
                        p.Message("Invalid coordinates! All 3 must be between 0 and 15");
                        return;
                    }
                    
                    step++;
                    def.MinX = minx; def.MinY = miny; def.MinZ = minz;
                    p.Message("   &bSet minimum coords to X:{0} Y:{1} Z:{2}", minx, miny, minz);
                    break;
                case 10:
                    string[] maxArgs = args.Split();
                    if (maxArgs.Length != 3) {
                        p.Message("Please specify 3 coordinates");
                        return;
                    }
                    
                    byte maxx, maxy, maxz;
                    if (byte.TryParse(maxArgs[0], out maxx)
                        && byte.TryParse(maxArgs[1], out maxy)
                        && byte.TryParse(maxArgs[2], out maxz)
                        && (maxx >= 1 && maxx <= 16)
                        && (maxy >= 1 && maxy <= 16)
                        && (maxz >= 1 && maxz <= 16)) {
                    } else {
                        p.Message("Invalid coordinates! All 3 must be between 1 and 16");
                        return;
                    }
                    
                    step++;
                    def.MaxX = maxx;
                    def.MaxY = maxy; def.MaxZ = maxz; def.Shape = maxz;
                    p.Message("   &bSet maximum coords to X:{0} Y:{1} Z:{2}", maxx, maxy, maxz);
                    break;
                case 11:
                    if (byte.TryParse(args, out value)) {
                        step++; def.BlockDraw = value;
                        p.Message("   &bSet block draw type to: " + value);
                    }
                    break;
                case 12:
                    if (byte.TryParse(args, out value)) {
                        def.FogDensity = value;
                        step += value == 0 ? 4 : 1;
                        p.Message("   &bSet density of fog to: " + value);
                    }
                    break;
                case 13:
                    if (IsValidHex(args)) {
                        CustomColor col = Color.ParseHex(args);
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
                case 14:
                    if (byte.TryParse(args, out value)) {
                        step++; def.FogG = value;
                        p.Message("   &bSet green component of fog to: " + value);
                    }
                    break;
                case 15:
                    if (byte.TryParse(args, out value)) {
                        step++; def.FogB = value;
                        p.Message("   &bSet blue component of fog to: " + value);
                    }
                    break;
                default:
                    Block block;
                    if (Map.GetBlockByName(p.World, args, false, out block)) {
                        if (block > Map.MaxCPEBlock) {
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
                            Server.Message("{0} &Screated a new {3} custom block &H{1} &Swith ID {2}",
                                           p.ClassyName, def.Name, def.BlockID, scope);
                        } else {
                            p.World.Players.Message("{0} &Screated a new {3} custom block &H{1} &Swith ID {2}",
                                                    p.ClassyName, def.Name, def.BlockID, scope);
                        }
                    }
                    return;
            }
            p.currentBDStep = step;
            PrintStepHelp(p);
        }

        static void CustomBlockDuplicateHandler(Player p, CommandReader cmd, bool global, BlockDefinition[] defs) {
            Block srcBlock, dstBlock;
            string scope = global ? "global" : "level";
            string name = global ? "/gb" : "/lb";

            if (!cmd.NextRawBlock(p, out srcBlock)) return;
            if (!cmd.NextRawBlock(p, out dstBlock)) return;
            if (dstBlock == Block.Air || dstBlock == Block.None) {
                p.Message("Destination block cannot have 0 or 255 ID."); return;
            }

            BlockDefinition srcDef = GetCustomBlock(global, defs, (byte)srcBlock);
            if (srcDef == null && srcBlock <= Map.MaxCPEBlock)
                srcDef = DefaultSet.MakeCustomBlock(srcBlock);
            
            if (srcDef == null) {
                p.Message("There is no {1} custom block with the id: &a{0}", (byte)srcBlock, scope);
                p.Message("Use \"&H{1} list&S\" to see a list of {0} custom blocks.", scope, name);
                return;
            }
            BlockDefinition dstDef = GetCustomBlock(global, defs, (byte)dstBlock);
            if (dstDef != null) {
                p.Message("There is already a {1} custom block with the id: &a{0}", dstBlock, scope);
                p.Message("Use \"&H{1} remove {0}&S\" on this block first.", dstBlock, name);
                p.Message("Use \"&H{1} list&S\" to see a list of {0} custom blocks.", scope, name);
                return;
            }
            
            BlockDefinition def = srcDef.Copy();
            def.BlockID = (byte)dstBlock;
            BlockDefinition.Add(def, defs, p.World);
            if (global) {
                Server.Message("{0} &Screated a new {3} custom block &H{1} &Swith ID {2}",
                               p.ClassyName, def.Name, def.BlockID, scope);
            } else {
                p.World.Players.Message("{0} &Screated a new {3} custom block &H{1} &Swith ID {2}",
                                        p.ClassyName, def.Name, def.BlockID, scope);
            }
        }

        static void CustomBlockEditHandler(Player p, CommandReader cmd, bool global, BlockDefinition[] defs) {
            Block blockID;
            string scope = global ? "global" : "level";
            string name = global ? "/gb" : "/lb";
            
            if (!cmd.NextRawBlock(p, out blockID)) return;
            
            BlockDefinition def = GetCustomBlock(global, defs, (byte)blockID);
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
                    p.Message("&bChanged name of &a{0} &bto &A{1}", def.Name, args);
                    def.Name = args; def.BlockName = args.ToLower().Replace(" ", "");
                    hasChanged = true;
                    break;
                case "solid":
                case "solidity":
                case "collide":
                case "collidetype":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged solidity of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.CollideType, value);
                        def.CollideType = value;
                        hasChanged = true;
                    }
                    break;
                case "speed":
                    float speed;
                    if (float.TryParse(args, out speed)
                        && speed >= 0.25f && value <= 3.96f) {
                        p.Message("&bChanged speed of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.Speed, speed);
                        def.Speed = speed;
                        hasChanged = true;
                    }
                    break;
                case "allid":
                case "alltex":
                case "alltexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged all textures of &a{0} &bto &a{1}", def.Name, value);
                        def.TopTex = value; def.BottomTex = value;
                        def.SetSidesTex(value);
                        hasChanged = true;
                    }
                    break;
                case "topid":
                case "toptex":
                case "toptexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged top texture of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.TopTex, value);
                        def.TopTex = value;
                        hasChanged = true;
                    }
                    break;
                case "leftid":
                case "lefttex":
                case "lefttexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged left texture of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.LeftTex, value);
                        def.LeftTex = value;
                        hasChanged = true;
                    }
                    break;
                case "rightid":
                case "righttex":
                case "righttexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged right texture of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.RightTex, value);
                        def.RightTex = value;
                        hasChanged = true;
                    }
                    break;
                case "frontid":
                case "fronttex":
                case "fronttexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged front texture of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.FrontTex, value);
                        def.FrontTex = value;
                        hasChanged = true;
                    }
                    break;
                case "backid":
                case "backtex":
                case "backtexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged back texture of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.BackTex, value);
                        def.BackTex = value;
                        hasChanged = true;
                    }
                    break;
                case "sideid":
                case "sidetex":
                case "sidetexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged all side textures of &a{0} &bto &a{1}", def.Name, value);
                        def.SetSidesTex(value);
                        hasChanged = true;
                    }
                    break;
                case "bottomid":
                case "bottomtex":
                case "bottomtexture":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged bottom texture index of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.BottomTex, value);
                        def.BottomTex = value;
                        hasChanged = true;
                    }
                    break;
                case "light":
                case "blockslight":
                    if (bool.TryParse(args, out boolVal)) {
                        p.Message("&bChanged blocks light of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.BlocksLight, boolVal);
                        def.BlocksLight = boolVal;
                        hasChanged = true;
                    }
                    break;
                case "sound":
                case "walksound":
                    if (byte.TryParse(args, out value) && value <= 11) {
                        p.Message("&bChanged walk sound of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.WalkSound, value);
                        def.WalkSound = value;
                        hasChanged = true;
                    }
                    break;
                case "fullbright":
                    if (bool.TryParse(args, out boolVal)) {
                        p.Message("&bChanged full bright of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.FullBright, boolVal);
                        def.FullBright = boolVal;
                        hasChanged = true;
                    }
                    break;
                case "sprite":
                case "shape":
                    if (bool.TryParse(args, out boolVal) && value <= 16) {
                        p.Message("&bChanged is a sprite block of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.Shape == 0, value);
                        def.Shape = boolVal ? (byte)0 : def.MaxZ;
                        hasChanged = true;
                    }
                    break;
                case "draw":
                case "blockdraw":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged block draw type of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.BlockDraw, value);
                        def.BlockDraw = value;
                        hasChanged = true;
                    }
                    break;
                case "fogdensity":
                case "fogd":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged density of fog of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.FogDensity, value);
                        def.FogDensity = value;
                        hasChanged = true;
                    }
                    break;
                case "foghex":
                    if (IsValidHex(args)) {
                        CustomColor col = Color.ParseHex(args);
                        p.Message("&bChanged red fog component of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.FogR, col.R);
                        def.FogR = col.R;
                        p.Message("&bChanged green fog component of fog of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.FogG, col.G);
                        def.FogG = col.G;
                        p.Message("&bChanged blue fog component of fog of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.FogB, col.B);
                        def.FogB = col.B;
                        hasChanged = true;
                    }
                    break;
                case "fogr":
                case "fogred":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged red fog component of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.FogR, value);
                        def.FogG = value;
                        hasChanged = true;
                    }
                    break;
                case "fogg":
                case "foggreen":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged green fog component of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.FogG, value);
                        def.FogG = value;
                        hasChanged = true;
                    }
                    break;
                case "fogb":
                case "fogblue":
                    if (byte.TryParse(args, out value)) {
                        p.Message("&bChanged blue fog component of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.FogB, value);
                        def.FogB = value;
                        hasChanged = true;
                    }
                    break;
                case "fallback":
                case "block":
                    Block newBlock;
                    if (Map.GetBlockByName(p.World, args, false, out newBlock)) {
                        if (newBlock > Map.MaxCPEBlock) {
                            p.Message("&cThe fallback block must be an original block, " +
                                      "or a block defined in the CustomBlocks extension.");
                            break;
                        }
                        p.Message("&bChanged fallback block of &a{0} &bfrom &a{1} &bto &a{2}", def.Name, def.FallBack, newBlock.ToString());
                        def.FallBack = (byte)newBlock;
                        
                        DisplayEditMessage(p, def, global);
                        UpdateFallback();
                        return;
                    }
                    break;
                case "min":
                    if (args.CaselessEquals("-1")) {
                        p.Message("Block will display as a sprite!");
                        def.Shape = 0;
                        hasChanged = true;
                        break;
                    }
                    
                    string[] minArgs = args.Split();
                    if (minArgs.Length != 3) {
                        p.Message("Please specify 3 coordinates!");
                        break;
                    }
                    
                    def.MinX = EditCoord(p, "min X", def.Name, minArgs[0], def.MinX, ref hasChanged);
                    def.MinY = EditCoord(p, "min Y", def.Name, minArgs[1], def.MinY, ref hasChanged);
                    def.MinZ = EditCoord(p, "min Z", def.Name, minArgs[2], def.MinZ, ref hasChanged);
                    hasChanged = true;
                    break;
                case "max":
                    string[] maxArgs = args.Split();
                    if (maxArgs.Length != 3) {
                        p.Message("Please specify 3 coordinates!");
                        break;
                    }
                    
                    def.MaxX = EditCoord(p, "max X", def.Name, maxArgs[0], def.MaxX, ref hasChanged);
                    def.MaxY = EditCoord(p, "max Y", def.Name, maxArgs[1], def.MaxY, ref hasChanged);
                    def.MaxZ = EditCoord(p, "max Z", def.Name, maxArgs[2], def.MaxZ, ref hasChanged);
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
                case "order":
                    byte order = 0;
                    if (byte.TryParse(args, out order)) {
                        def.InventoryOrder = order;
                        DisplayEditMessage(p, def, global);
                        UpdateOrder(p.World, def, global);
                        BlockDefinition.Save(global, p.World);
                    }
                    return;
                default:
                    p.Message("Usage: &H" + name + " [type/value] {args}");
                    return;
            }
            
            if (!hasChanged) return;            
            DisplayEditMessage(p, def, global);
            BlockDefinition.Add(def, defs, p.World);
        }
        
        static void DisplayEditMessage(Player p, BlockDefinition def, bool global) {
        	if (global) {
                Server.Message("{0} &Sedited a global custom block &a{1} &Swith ID &a{2}",
                               p.ClassyName, def.Name, def.BlockID);
            } else {
                p.World.Players.Message("{0} &Sedited a level custom block &a{1} &Swith ID &a{2}",
                                        p.ClassyName, def.Name, def.BlockID);
            }
        }
        
        static void UpdateOrder(World world, BlockDefinition def, bool global) {
            if (def.InventoryOrder == -1) return;
            byte id = def.BlockID, order = (byte)def.InventoryOrder;

            foreach (Player player in Server.Players) {
                if (!global && player.World != world) continue;
                if (global && player.World.BlockDefs[id] != BlockDefinition.GlobalDefs[id]) continue;

                if (!player.Supports(CpeExt.InventoryOrder)) continue;
                player.Send(Packet.SetInventoryOrder(id, order));
            }
        }
        
        static void UpdateFallback() {
            foreach (Player player in Server.Players) {
                if (!player.Supports(CpeExt.BlockDefinitions)) {
                    player.JoinWorld(player.World, WorldChangeReason.Rejoin, player.Position);
                }
            }        	
        }

        static byte EditCoord(Player p, string coord, string name,
                              string args, byte origValue, ref bool hasChanged) {
            byte value;
            if (byte.TryParse(args, out value) && value <= 16) {
                p.Message("&bChanged {0} coordinate of &a{1} &bfrom &a{2} &bto &a{3}", coord, name, origValue, value);
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
            if (blockId == 0 || blockId >= 255) {
                p.Message("Block id must be between 1-254");
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
            new [] { "&SEnter the name of the block. You can include spaces." },
            new [] { "&SEnter the solidity of the block (0-2)",
                "&S0 = walk through(air), 1 = swim through (water), 2 = solid" },
            new [] { "&SEnter the movement speed of the new block. (0.25-3.96)" },
            new [] { "&SEnter the terrain.png index for the top texture. (0-255)" },
            new [] { "&SEnter the terrain.png index for the sides texture. (0-255)" },
            new [] { "&SEnter the terrain.png index for the bottom texture. (0-255)" },
            new [] { "&SEnter whether the block prevents sunlight from passing though. (true or false)" },
            new [] { "&SEnter the walk sound index of the block. (0-11)",
                "&S0 = no sound, 1 = wood, 2 = gravel, 3 = grass, 4 = stone,",
                "&S5 = metal, 6 = glass, 7 = wool, 8 = sand, 9 = snow." },
            new [] { "&SEnter whether the block is fully bright (i.e. like lava). (true or false)" },
            new [] { "Enter the min X Y Z coords of this block,",
                "Min = 0 Max = 15 Example: &H0 0 0",
                "&H-1&S to make it a sprite (e.g roses)" },
            new [] { "Enter the max X Y Z coords of this block,",
                "Min = 1 Max = 16 Example: &H16 16 16",
                "&S(e.g. snow has max Z '2', slabs have '8', dirt has '16')" },
            new [] { "&SEnter the block draw type of this block. (0-4)",
                "&S0 = solid/opaque, 1 = transparent (like glass)",
                "&S2 = transparent (like leaves), 3 = translucent (like water)",
                "&S4 = gas (like air)" },
            new [] { "Enter the density of the fog for the block. (0-255)",
                "0 is treated as no fog, 255 is thickest fog." },
            new [] { "Enter the red component of the fog colour. (0-255) or full hex value" },
            new [] { "Enter the green component of the fog colour. (0-255)" },
            new [] { "Enter the blue component of the fog colour. (0-255)" },
            new [] { "Enter the fallback block for this block.",
                "This block is shown to clients that don't support BlockDefinitions." },
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
            Category = CommandCategory.CPE | CommandCategory.Moderation | CommandCategory.New,
            Permissions = new[] { Permission.Chat },
            Usage = "/Hacks [Player] [Hack] [jumpheight(if needed)]",
            IsConsoleSafe = true,
            Help = "Change the hacking abilities of [Player]&N" +
                "Valid hacks: &aFlying&S, &aNoclip&S, &aSpeedhack&S, &aRespawn&S, &aThirdPerson&S and &aJumpheight",
            Handler = HackControlHandler
        };

        static void HackControlHandler(Player player, CommandReader cmd) {
            Player targetPlayer = FindPlayer(player, cmd);            
            if (targetPlayer == null || player.Info.Rank != RankManager.HighestRank) {
                player.Message("Current Hacks for {0}", player.ClassyName);
                PrintPlayerHacks(player, player.Info);
                return;
            }

            string hack = (cmd.Next() ?? "null");
            string hackStr = "hack";
            PlayerInfo target = targetPlayer.Info;
            
            if (hack == "null") {
                player.Message("Current Hacks for {0}", target.ClassyName);
                PrintPlayerHacks(player, target);
                return;
            }

            switch (hack.ToLower()) {
                case "flying":
                case "fly":
                case "f":
                    player.Message("Hacks for {0}", target.ClassyName);
                    player.Message("    Flying: &a{0} &S--> &a{1}", target.AllowFlying, !target.AllowFlying);
                    target.AllowFlying = !target.AllowFlying;
                    hackStr = "flying";
                    break;
                    
                case "noclip":
                case "clip":
                case "nc":
                    player.Message("Hacks for {0}", target.ClassyName);
                    player.Message("    NoClip: &a{0} &S--> &a{1}", target.AllowNoClip, !target.AllowNoClip);
                    target.AllowNoClip = !target.AllowNoClip;
                    hackStr = "noclip";
                    break;
                    
                case "speedhack":
                case "speed":
                case "sh":
                    player.Message("Hacks for {0}", target.ClassyName);
                    player.Message("    SpeedHack: &a{0} &S--> &a{1}", target.AllowSpeedhack, !target.AllowSpeedhack);
                    target.AllowSpeedhack = !target.AllowSpeedhack;
                    hackStr = "speedhack";
                    break;
                    
                case "respawn":
                case "spawn":
                case "rs":
                    player.Message("Hacks for {0}", target.ClassyName);
                    player.Message("    Respawn: &a{0} &S--> &a{1}", target.AllowRespawn, !target.AllowRespawn);
                    target.AllowRespawn = !target.AllowRespawn;
                    hackStr = "respawn";
                    break;
                    
                case "thirdperson":
                case "third":
                case "tp":
                    player.Message("Hacks for {0}", target.ClassyName);
                    player.Message("    ThirdPerson: &a{0} &S--> &a{1}", target.AllowThirdPerson, !target.AllowThirdPerson);
                    target.AllowThirdPerson = !target.AllowThirdPerson;
                    hackStr = "thirdperson";
                    break;
                    
                case "jumpheight":
                case "jump":
                case "height":
                case "jh":
                    float height;
                    string third = cmd.Next();
                    if (!float.TryParse(third, out height)) {
                        player.Message("Error: Could not parse \"&a{0}&S\" as a decimal.", third);
                        return;
                    } else if (height < 0 || height > short.MaxValue / 32.0f) {
                        player.Message("Error: Jump height must be between &a0&S and &a1023.", third);
                        return;
                    } else {
                        player.Message("Hacks for {0}", target.ClassyName);
                        player.Message("    JumpHeight: &a{0} &S--> &a{1}", target.JumpHeight / 32.0f, height);
                        target.JumpHeight = (short)(height * 32);
                        hackStr = "jumpheight";
                    }
                    break;
                default:
                    player.Message(CdHackControl.Help);
                    return;
            }

            if (player != targetPlayer) {
                targetPlayer.Message("{0} has changed your {1} ability, use &H/Hacks &Sto check them out.", player.Info.Name, hackStr);
            }
            if (targetPlayer.Supports(CpeExt.HackControl)) {
                targetPlayer.Send(Packet.HackControl(
                    target.AllowFlying, target.AllowNoClip, target.AllowSpeedhack,
                    target.AllowRespawn, target.AllowThirdPerson, target.JumpHeight));
            }
        }
        
        
        static void PrintPlayerHacks(Player player, PlayerInfo target) {
            player.Message("    &SFlying: &a{0} &SNoclip: &a{1} &SSpeedhack: &a{2}",
                           target.AllowFlying.ToString(),
                           target.AllowNoClip.ToString(),
                           target.AllowSpeedhack.ToString());
            player.Message("    &SRespawn: &a{0} &SThirdPerson: &a{1} &SJumpHeight: &a{2}",
                           target.AllowRespawn.ToString(),
                           target.AllowThirdPerson.ToString(),
                           target.JumpHeight);
        }

        #endregion

        #region ListClients

        static readonly CommandDescriptor CdListClients = new CommandDescriptor {
            Name = "ListClients",
            Aliases = new[] { "pclients", "clients", "whoisanewb" },
            Category = CommandCategory.CPE | CommandCategory.Info | CommandCategory.New,
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
                player.Message("  {0}:&f {1}",
                               kvp.Key, kvp.Value.JoinToClassyString());
            }
        }

        #endregion

        #region TextHotKey

        static readonly CommandDescriptor CdtextHotKey = new CommandDescriptor {
            Name = "TextHotKey",
            Aliases = new[] { "HotKey", "thk", "hk" },
            Category = CommandCategory.CPE | CommandCategory.Chat | CommandCategory.New,
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
                player.Send(Packet.MakeSetTextHotKey(Label, Action, KeyCode, KeyMod, player.HasCP437));
            } else {
                player.Message("You do not support TextHotKey");
            }
        }

        #endregion

        #region Texture

        static readonly CommandDescriptor Cdtex = new CommandDescriptor {
            Name = "texture",
            Aliases = new[] { "texturepack", "tex" },
            Permissions = new[] { Permission.Chat },
            Category = CommandCategory.CPE | CommandCategory.Chat | CommandCategory.New,
            Help = "Tells you information about our custom texture pack.",
            Handler = textureHandler
        };

        static void textureHandler(Player player, CommandReader cmd) {
            if (player.World != null && !string.IsNullOrEmpty(player.World.Texture)) {
                player.Message("This world uses a custom texture pack");
                player.Message("A preview can be found here: ");
                player.Message("  " + (player.World.Texture.CaselessEquals("default") ? Server.DefaultTerrain : player.World.Texture));
            } else {
                player.Message("You are not in a world with a custom texturepack.");
            }
        }

        #endregion

        #region ZoneShow

        static readonly CommandDescriptor CdZoneShow = new CommandDescriptor {
            Name = "ZoneSelection",
            Aliases = new[] { "zselection", "zbox", "zshow", "zs" },
            Permissions = new[] { Permission.ManageZones },
            Category = CommandCategory.CPE | CommandCategory.Zone | CommandCategory.New,
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
            short alpha;
            Zone zone = player.World.Map.Zones.Find(zonea);
            if (zone == null) {
                player.Message("Error: Zone not found");
                return;
            }
            if (color == null) {
                player.Message("Error: Missing a Hex Color code");
                CdZoneShow.PrintUsage(player);
                return;
            } else {
                color = color.ToUpper();
            }
            if (color.StartsWith("#")) {
                color = color.ToUpper().Remove(0, 1);
            }
            
            if (!IsValidHex(color)) {
                if (color.CaselessEquals("on") || color.CaselessEquals("true") || color.CaselessEquals("yes")) {
                    zone.ShowZone = true;
                    if (zone.Color != null) {
                        player.Message("Zone ({0}&S) will now show its boundaries", zone.ClassyName);
                        player.World.Players.Where(p => p.Supports(CpeExt.SelectionCuboid)).Send(Packet.MakeMakeSelection(zone.ZoneID, zone.Name, zone.Bounds,
                                                                                                                          zone.Color, zone.Alpha, player.HasCP437));
                    }
                    return;
                } else if (color.CaselessEquals("off") || color.CaselessEquals("false") || color.CaselessEquals("no")) {
                    zone.ShowZone = false;
                    player.Message("Zone ({0}&S) will no longer show its boundaries", zone.ClassyName);
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
                CdZoneShow.PrintUsage(player);
                return;
            }
            if (!short.TryParse(alp, out alpha)) {
                player.Message("Error: \"{0}\" is not a valid integer for Alpha.", alp);
                return;
            } else {
                zone.Alpha = alpha;
            }
            
            if (cmd.HasNext) {
            	bool show;
            	if (!cmd.NextOnOff(out show)) {
                    player.Message("\"Show\" state must be 'on' or 'off'");
                    return;
            	}
                
            	zone.ShowZone = show;
            	if (show) {
                    player.Message("Zone ({0}&S) color set! Boundaries: ON", zone.ClassyName);
            	} else {
                    player.Message("Zone ({0}&S) color set! Boundaries: OFF", zone.ClassyName);
                }
            } else {
                player.Message("Zone ({0}&S) color set!", zone.ClassyName);
            }
            
            if (zone != null) {
                foreach (Player p in player.World.Players) {
                    if (p.Supports(CpeExt.SelectionCuboid) && zone.ShowZone) {
                        p.Send(Packet.MakeMakeSelection(zone.ZoneID, zone.Name, zone.Bounds, zone.Color, alpha, p.HasCP437));
                    }
                }
            }
        }

        #endregion

        #region EntityRot

        static readonly CommandDescriptor CdEntityRot = new CommandDescriptor {
            Name = "EntityRot",
            Aliases = new[] { "EntityRotation", "ModelRot", "ModelRotation" },
            Category = CommandCategory.CPE | CommandCategory.Moderation | CommandCategory.New,
            Permissions = new[] { Permission.ReadStaffChat },
            Usage = "/EntityRot [Player] x/z [Angle]",
            IsConsoleSafe = true,
            Help = "Sets X or Z axis rotation (in degrees) of that player.",
            Handler = EntityRotHandler
        };

        private static void EntityRotHandler(Player player, CommandReader cmd) {
            string name = cmd.Next(), axis = cmd.Next();
            if (name == null || axis == null) { CdEntityRot.PrintUsage(player); return; }
            
            Player target = Server.FindPlayerOrPrintMatches(player, name, SearchOptions.IncludeSelf);
            if (target == null) return;
            if (target.Info.Rank.Index < player.Info.Rank.Index) {
                player.Message("Cannot change the rotation of someone ranked higher than you."); return;
            }
                        
            int angle = 0;
            if (!cmd.NextInt(out angle)) { player.Message("Angle must be an integer."); return; }
            angle %= 360;
            
            if (axis.CaselessEquals("x")) {
                target.RotX = angle;
                player.Message("{0}&S's X rotation was set to {1} degrees.", target.ClassyName, angle);
            } else if (axis.CaselessEquals("z")) {
                target.RotZ = angle;
                player.Message("{0}&S's Z rotation was set to {1} degrees.", target.ClassyName, angle);
            } else {
                player.Message("Axis name must be X or Z."); return;
            }
        }
        #endregion
    }
}
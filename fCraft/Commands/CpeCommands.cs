// ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Linq;

namespace fCraft {
    
    /// <summary> Most commands for CPE extensions are here. </summary>
    static class CpeCommands {

        internal static void Init() {
            CommandManager.RegisterCommand( CdChangeModel );
            CommandManager.RegisterCommand( CdAFKModel );
            CommandManager.RegisterCommand( CdHackControl );
            CommandManager.RegisterCommand( CdChangeSkin );
            CommandManager.RegisterCommand( CdGlobalBlock );
        }
        
        #region ChangeModel
        
        internal static string[] validEntities =  { "chicken", "creeper", 
            "humanoid", "human", "pig", "sheep", "skeleton", "spider", "zombie"
        };
        
        static readonly CommandDescriptor CdChangeModel = new CommandDescriptor
        {
            Name = "Model",
            Aliases = new[] { "ChangeModel", "cm" },
            Category = CommandCategory.New | CommandCategory.Moderation,
            Permissions = new[] { Permission.ReadStaffChat },
            Usage = "/Model [Player] [Model]",
            IsConsoleSafe = true,
            Help = "Change the Model or Skin of [Player]!&n" +
            "Valid models: &s [Any Block Name or ID#], Chicken, Creeper, Humanoid, Pig, Sheep, Skeleton, Spider, Zombie!",
            Handler = ModelHandler
        };

        private static void ModelHandler(Player player, CommandReader cmd) {
            PlayerInfo otherPlayer = InfoCommands.FindPlayerInfo(player, cmd, cmd.Next() ?? player.Name);
            if (otherPlayer == null) return;
             
            if (!player.IsStaff && otherPlayer != player.Info) {
                Rank staffRank = RankManager.GetMinRankWithAnyPermission(Permission.ReadStaffChat);
                if (staffRank != null) {
                    player.Message("You must be {0}&s+ to change another players Model", staffRank.ClassyName);
                } else {
                    player.Message("No ranks have the ReadStaffChat permission so no one can change other players Model, yell at the owner.");
                }
                return;
            }
            if (otherPlayer.Rank.Index < player.Info.Rank.Index) {
                player.Message("Cannot change the Model of someone higher rank than you.");
                return;
            }
            if (otherPlayer == null) {
                player.Message("Your current Model: &f" + player.Info.Mob);
                return;
            }
            string model = cmd.Next();
            if (string.IsNullOrEmpty(model)) {
                player.Message("Current Model for {0}: &f{1}", otherPlayer.Name, otherPlayer.Mob);
                return;
            }
            if (otherPlayer.IsOnline && otherPlayer.Rank.Index >= player.Info.Rank.Index) {
                if (!validEntities.Contains(model.ToLower())) {
                    Block block;
                    if (Map.GetBlockByName(model, false, out block)) {
                        model = block.GetHashCode().ToString();
                    } else {
                        player.Message("Model not valid, see &h/Help Model&s.");
                        return;
                    }
                }
                if (otherPlayer.Mob.ToLower() == model.ToLower()) {
                    player.Message("&f{0}&s's model is already set to &f{1}", otherPlayer.Name, model);
                    return;
                }
                if (otherPlayer.IsOnline) {
                    otherPlayer.PlayerObject.Message("&f{0}&shanged your model from &f{1} &sto &f{2}", (otherPlayer.PlayerObject == player ? "&sC" : player.Name + " &sc"), otherPlayer.Mob, model);
                }
                if (otherPlayer.PlayerObject != player) {
                    player.Message("&sChanged model of &f{0} &sfrom &f{1} &sto &f{2}", otherPlayer.Name, otherPlayer.Mob, model);
                }
                otherPlayer.oldMob = otherPlayer.Mob;
                otherPlayer.Mob = model;
            } else {
                player.Message("Player not found/online or lower rank than you");
            }
        }

        static readonly CommandDescriptor CdAFKModel = new CommandDescriptor {
            Name = "AFKModel",
            Category = CommandCategory.New,
            Permissions = new[] { Permission.Chat },
            Usage = "/AFKModel [Player] [Model]",
            IsConsoleSafe = true,
            Help = "Change your own model for when you are AFK!&n" +
    "Valid models: &s [Any Block Name or ID#], Chicken, Creeper, Croc, Humanoid, Pig, Printer, Sheep, Skeleton, Spider, Zombie!",
            Handler = AFKModelHandler
        };

        private static void AFKModelHandler(Player player, CommandReader cmd) {
            PlayerInfo otherPlayer = InfoCommands.FindPlayerInfo(player, cmd, cmd.Next() ?? player.Name);
            if (otherPlayer == null) return;
            
            if (!player.IsStaff && otherPlayer != player.Info) {
                Rank staffRank = RankManager.GetMinRankWithAnyPermission(Permission.ReadStaffChat);
                if (staffRank != null) {
                    player.Message("You must be {0}&s+ to change another players AFKModel", staffRank.ClassyName );
                } else {
                    player.Message("No ranks have the ReadStaffChat permission so no one can change other players AFKModel, yell at the owner.");
                }
                return;
            }
            if (otherPlayer.Rank.Index < player.Info.Rank.Index) {
                player.Message("Cannot change the AFKModel of someone higher rank than you.");
                return;
            }
            if (otherPlayer == null) {
                player.Message("Your current AFK Model: &f" + player.AFKModel);
                return;
            }
            string model = cmd.Next();
            if (string.IsNullOrEmpty(model)) {
                CdAFKModel.PrintUsage(player);
                return;
            }
            if (otherPlayer != null && otherPlayer.IsOnline && otherPlayer.Rank.Index >= player.Info.Rank.Index) {
                if (!validEntities.Contains(model.ToLower())) {
                    Block block;
                    if (Map.GetBlockByName(model, false, out block)) {
                        model = block.GetHashCode().ToString();
                    } else {
                        player.Message("Model not valid, see &h/Help AFKModel&s.");
                        return;
                    }
                }
                if (otherPlayer.PlayerObject.AFKModel.ToLower() == model.ToLower()) {
                    player.Message("&f{0}&s's AFKmodel is already set to &f{1}", otherPlayer.Name, model);
                    return;
                }
                if (otherPlayer.IsOnline) {
                    otherPlayer.PlayerObject.Message("&f{0}&shanged your AFKmodel from &f{1} &sto &f{2}", (otherPlayer.PlayerObject == player ? "&sC" : player.Name + " &sc"), otherPlayer.PlayerObject.AFKModel, model);
                }
                if (otherPlayer.PlayerObject != player) {
                    player.Message("&sChanged AFKmodel of &f{0} &sfrom &f{1} &sto &f{2}", otherPlayer.Name, otherPlayer.PlayerObject.AFKModel, model);
                }
                otherPlayer.PlayerObject.AFKModel = model;
            } else {
                player.Message("Player not found/online or lower rank than you");
            }
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
            string namePart = cmd.Next();
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
            PlayerInfo p = InfoCommands.FindPlayerInfo(player, cmd, namePart);
            if (p == null)  return;

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
        
        #region HackControl
        
        static readonly CommandDescriptor CdHackControl = new CommandDescriptor
        {
            Name = "HackControl",
            Aliases = new[] { "hacks", "hack", "hax" },
            Category = CommandCategory.New | CommandCategory.Moderation,
            Permissions = new[] { Permission.Chat},
            Usage = "/Hacks [Player] [Hack] [jumpheight(if needed)]",
            IsConsoleSafe = true,
            Help = "Change the hacking abilities of [Player]&n" +
            "Valid hacks: &aFlying&s, &aNoclip&s, &aSpeedhack&s, &aRespawn&s, &aThirdPerson&s and &aJumpheight",
            Handler = HackControlHandler
        };

        static void HackControlHandler(Player player, CommandReader cmd) {
            string first = cmd.Next();

            if (first == null || player.Info.Rank != RankManager.HighestRank) {
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

            PlayerInfo target = InfoCommands.FindPlayerInfo(player, cmd, first);
            if (target == null) return;

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
                    } else  player.Message("Error: Could not parse \"&a{0}&s\" as a short. Try something between &a0&s and &a32767", third);
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
                if (target.PlayerObject.Supports(CpeExtension.HackControl)) {
                    target.PlayerObject.Send(Packet.HackControl(
                        target.AllowFlying, target.AllowNoClip, target.AllowSpeedhack,
                        target.AllowRespawn, target.AllowThirdPerson, target.JumpHeight));
                }
            }
        }
        
        #endregion
        
        #region Global block

        static readonly CommandDescriptor CdGlobalBlock = new CommandDescriptor {
            Name = "GlobalBlock",
            Aliases = new string[] { "global", "block", "gb" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.DefineCustomBlocks },
            Usage = "/gb [type/value] {args}",
            Help = "&sModifies the global custom blocks, or prints information about them.&n" +
                "&sTypes are: add, abort, duplicate, edit, info, list, remove, texture&n" +
                "&sSee &h/help gb <type>&s for details about each type.",
            HelpSections = new Dictionary<string, string>{
                { "add",     "&h/gb add [id]&n" +
                        "&sBegins the process of defining a global custom block with the given block id." },
                { "abort",   "&h/gb abort&n" +
                        "&sAborts the custom block that was currently in the process of being " +
                        "defined from the last /gb add call." },
                { "duplicate",     "&h/gb duplicate [source id] [new id]&n" +
                        "&sCreates a new custom block, using all the global custom block data of the given existing custom block id. " },
                { "edit",     "&h/gb edit [id] [option] {args}&n" +
                        "&sEdits already defined blocks so you don't have to re-add them to change something. " +
                        "Options: Name, Solidity, Speed, AllId, TopId, SideID, BottomID, Light, Sound, FullBright, Shape, Draw, FogDensity, (FogHex or FogR, FogG, FogB), FallBack"},
                { "info",     "&h/gb info [id]&n" +
                        "&sDisplays custom block information for specified ID." },
                { "list",    "&h/gb list [offset]&n" +
                        "&sPrints a list of the names of the global custom blocks, " +
                        "along with their corresponding block ids. " },
                { "remove",  "&h/gb remove [id]&n" +
                        "&sRemoves the global custom block associated which has the numerical block id." },
                { "texture",  "&h/gb tex&n" +
                        "&sShows you the terrain link of the current world and a link of the default with ID's overlayed." },
            },
            Handler = GlobalBlockHandler
        };

        static void GlobalBlockHandler(Player player, CommandReader cmd) {
            string opt = cmd.Next();
            if (opt != null ) 
                opt = opt.ToLower();
            switch (opt) {
                case "create":
                case "add":
                    if (player.currentGB != null)
                        GlobalBlockDefineHandler(player, cmd.NextAll());
                    else
                        GlobalBlockAddHandler(player, cmd);
                    break;
                case "nvm":
                case "abort":
                    if (player.currentGB == null) {
                        player.Message("You do not have a global custom block definition currently being created.");
                    } else {
                        player.currentGB = null;
                        player.currentGBStep = -1;
                        player.Message("Discarded the global custom block definition that was being created.");
                    }
                    break;
                case "edit":
                case "change":
                    GlobalBlockEditHandler(player, cmd);
                    break;
                case "copy":
                case "duplicate":
                    GlobalBlockDuplicateHandler(player, cmd);
                    break;
                case "i":
                case "info":
                    int id;
                    if (CheckBlockId(player, cmd, out id)) {
                        BlockDefinition block = BlockDefinition.GlobalDefinitions[id];
                        if (block == null) {
                            player.Message("No custom block by the ID: &a{0}", id);
                            player.Message("Use \"&h/gb list\" &sto see a list of global custom blocks.");
                            return;
                        }
                        Block fallback;
                        Map.GetBlockByName(block.FallBack.ToString(), false, out fallback);
                        player.Message("&3---Name&3:&a{0} &3ID:&a{1}&3---", block.Name, block.BlockID);
                        player.Message("   &3FallBack: &a{0}&3, Solidity: &a{2}&3, Speed: &a{1}",
                            fallback.ToString(), block.Speed, block.CollideType);
                        player.Message("   &3Top ID: &a{0}&3, Side ID: &a{1}&3, Bottom ID: &a{2}", 
                            block.TopTex, block.SideTex, block.BottomTex);
                        player.Message("   &3Block Light: &a{0}&3, Sound: &a{1}&3, FullBright: &a{2}", 
                            block.BlocksLight.ToString(), block.WalkSound, block.FullBright.ToString());
                        player.Message("   &3Shape: &a{0}&3, Draw: &a{1}&3, Fog Density: &a{2}", 
                            block.Shape, block.BlockDraw, block.FogDensity);
                        player.Message("   &3Fog Red: &a{0}&3, Fog Green: &a{1}&3, Fog Blue: &a{2}",
                            block.FogR, block.FogG, block.FogB);
                        player.Message("   &3Min X: &a{0}&3, Max X: &a{1}&3, Min Y: &a{2}&3, Max Y: &a{3}",
                            block.MinX, block.MaxX, block.MinY, block.MaxY);
                    }
                    break;
                case "list":
                    GlobalBlockListHandler(player, cmd);
                    break;
                case "remove":
                case "delete":
                    GlobalBlockRemoveHandler(player, cmd);
                    break;
                case "tex":
                case "texture":
                case "terrain":
                    player.Message("Terrain IDs: &9http://123dmwm.tk/ID-Overlay.png");
                    player.Message("Current world terrain: &9{0}", player.World.Texture == "Default" ? Server.DefaultTerrain : player.World.Texture);
                    break;
                default:
                    if (player.currentGB != null) {
                        cmd.Rewind();
                        GlobalBlockDefineHandler(player, cmd.NextAll());
                    } else {
                        CdGlobalBlock.PrintUsage(player);
                    }
                    break;
            }            
        }
        
        static void GlobalBlockAddHandler(Player player, CommandReader cmd) {
            int blockId = 0;
            if (!CheckBlockId(player, cmd, out blockId))
                return;
            
            BlockDefinition def = BlockDefinition.GlobalDefinitions[blockId];
            if (def != null) {
                player.Message("There is already a globally defined custom block with that block id.");
                player.Message("Use \"&h/gb remove {0}&s\" this block first.", blockId);
                player.Message("Use \"&h/gb list&s\" to see a list of global custom blocks.");
                return;
            }
            
            player.currentGB = new BlockDefinition();
            player.currentGB.BlockID = (byte)blockId;
            player.Message("   &bSet block id to: " + blockId);
            player.Message("&sFrom now on, use &h/gb [value]&s to enter arguments.");
            player.Message("&sYou can abort the currently partially " +
                           "created custom block at any time by typing \"&h/gb abort&s\"");
            
            player.currentGBStep = 0;
            PrintStepHelp(player);
        }
        
        static void GlobalBlockListHandler(Player player, CommandReader cmd) {
            int offset = 0, index = 0, count = 0;
            cmd.NextInt( out offset );
            BlockDefinition[] defs = BlockDefinition.GlobalDefinitions;
            for( int i = 0; i < defs.Length; i++ ) {
                BlockDefinition def = defs[i];
                if (def == null) continue;
                
                if (index >= offset) {
                    count++;
                    player.Message("&sCustom block &h{0} &shas name &h{1}", def.BlockID, def.Name);
                    
                    if(count >= 8) {
                        player.Message("To see the next set of global definitions, " +
                                       "type /gb list {0}", offset + 8);
                        return;
                    }
                }
                index++;
            }
        }
        
        static void GlobalBlockRemoveHandler(Player player, CommandReader cmd) {
            int blockId = 0;
            if (!CheckBlockId(player, cmd, out blockId))
                return;
            
            BlockDefinition def = BlockDefinition.GlobalDefinitions[blockId];
            if (def == null) {
                player.Message("There is no globally defined custom block with that block id.");
                player.Message("Use \"&h/gb list\" &sto see a list of global custom blocks.");
                return;
            }
            
            BlockDefinition.RemoveGlobalBlock(def);
            foreach (Player p in Server.Players ) {
                if (p.Supports(CpeExtension.BlockDefinitions))
                    BlockDefinition.SendGlobalRemove(p, def);
            }
            BlockDefinition.SaveGlobalDefinitions();
            
            Server.Message( "{0} &sremoved the global custom block &h{1} &swith ID {2}",
                                   player.ClassyName, def.Name, def.BlockID );
            ReloadAllPlayers();
        }

        static void GlobalBlockDefineHandler(Player player, string args) {
            // print the current step help if no args given
            if (string.IsNullOrWhiteSpace(args)) {
                PrintStepHelp(player); return;
            }

            BlockDefinition def = player.currentGB;
            int step = player.currentGBStep;
            byte value = 0; // as we can't pass properties by reference, make a temp var.
            bool boolVal = true;

            switch (step) {
                case 0:
                    step++; def.Name = args;
                    player.Message("   &bSet name to: " + def.Name);
                    break;
                case 1:
                    if (byte.TryParse(args, out value) && value <= 2) {
                        step++; def.CollideType = value;
                        player.Message("   &bSet solidity to: " + value);
                    }
                    break;
                case 2:
                    float speed;
                    if (float.TryParse(args, out speed)
                        && speed >= 0.25f && value <= 3.96f) {
                        step++; def.Speed = speed;
                        player.Message("   &bSet speed to: " + speed);
                    }
                    break;
                case 3:
                    if (byte.TryParse(args, out value)) {
                        step++; def.TopTex = value;
                        player.Message("   &bSet top texture index to: " + value);
                    }
                    break;
                case 4:
                    if (byte.TryParse(args, out value)) {
                        step++; def.SideTex = value;
                        player.Message("   &bSet sides texture index to: " + value);
                    }
                    break;
                case 5:
                    if (byte.TryParse(args, out value)) {
                        step++; def.BottomTex = value;
                        player.Message("   &bSet bottom texture index to: " + value);
                    }
                    break;
                case 6:
                    if (bool.TryParse(args, out boolVal)) {
                        step++; def.BlocksLight = boolVal;
                        player.Message("   &bSet blocks light to: " + boolVal);
                    }
                    break;
                case 7:
                    if (byte.TryParse(args, out value) && value <= 11) {
                        step++; def.WalkSound = value;
                        player.Message("   &bSet walk sound to: " + value);
                    }
                    break;
                case 8:
                    if (bool.TryParse(args, out boolVal)) {
                        if (player.Supports(CpeExtension.BlockDefinitionsExt)) {
                            step = 16;
                        } else {
                            step++;
                        }
                        def.FullBright = boolVal;
                        player.Message("   &bSet full bright to: " + boolVal);
                    }
                    break;
                case 9:
                    if (byte.TryParse(args, out value) && value <= 16) {
                        step++;
                        def.Shape = value;
                        def.MinX = 0;
                        def.MinY = 0;
                        def.MinZ = 0;
                        def.MaxX = 16;
                        def.MaxY = 16;
                        def.MaxZ = value;
                        player.Message("   &bSet block shape to: " + value);
                    }
                    break;
                case 10:
                    if (byte.TryParse(args, out value) && value <= 4) {
                        step++; def.BlockDraw = value;
                        player.Message("   &bSet block draw type to: " + value);
                    }
                    break;
                case 11:
                    if (byte.TryParse(args, out value)) {
                        def.FogDensity = value;
                        step += value == 0 ? 4 : 1;
                        player.Message("   &bSet density of fog to: " + value);
                    }
                    break;
                case 12:
                    if (WorldCommands.IsValidHex(args)) {
                        System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml("#" + args.ToUpper().Replace("#", ""));
                        def.FogR = col.R;
                        player.Message("   &bSet red component of fog to: " + col.R);
                        def.FogG = col.G;
                        player.Message("   &bSet green component of fog to: " + col.G);
                        def.FogB = col.B;
                        player.Message("   &bSet blue component of fog to: " + col.B);
                        step += 3;
                    } else {
                        if (byte.TryParse(args, out value)) {
                            step++; def.FogR = value;
                            player.Message("   &bSet red component of fog to: " + value);
                        }
                    }
                    break;
                case 13:
                    if (byte.TryParse(args, out value)) {
                        step++; def.FogG = value;
                        player.Message("   &bSet green component of fog to: " + value);
                    }
                    break;
                case 14:
                    if (byte.TryParse(args, out value)) {
                        step++; def.FogB = value;
                        player.Message("   &bSet blue component of fog to: " + value);
                    }
                    break;
                case 16:
                    if (args.ToLower().Equals("-1")) {
                        player.Message("   &bBlock will display as a Sprite");
                        def.Shape = 0;
                        def.MinX = 0;
                        def.MinY = 0;
                        def.MinZ = 0;
                        def.MaxX = 16;
                        def.MaxY = 16;
                        def.MaxZ = 16;
                        step = 10;
                        break;
                    }
                    if (args.Split().Count() != 3) {
                        player.Message("Please specify 3 coordinates");
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
                        player.Message("Invalid coordinates! All 3 must be between 0 and 15");
                        return;
                    }
                    step++;
                    def.MinX = minx;
                    def.MinY = miny;
                    def.MinZ = minz;
                    player.Message("   &bSet minimum coords to X:{0} Y:{1} Z:{2}", minx, miny, minz);
                    break;
                case 17:
                    if (args.Split().Count() != 3) {
                        player.Message("Please specify 3 coordinates");
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
                        player.Message("Invalid coordinates! All 3 must be between 1 and 16");
                        return;
                    }
                    step = 10;
                    def.MaxX = maxx;
                    def.MaxY = maxy;
                    def.MaxZ = maxz;
                    def.Shape = maxz;
                    player.Message("   &bSet maximum coords to X:{0} Y:{1} Z:{2}", maxx, maxy, maxz);
                    break;
                default:
                    Block block;
                    if (Map.GetBlockByName(args, false, out block)) {
                        if (block > Map.MaxCustomBlockType) {
                            player.Message("&cThe fallback block must be an original block, " +
                                           "or a block defined in the CustomBlocks extension.");
                            break;
                        }
                        def.FallBack = (byte)block;
                        player.Message("   &bSet fallback block to: " + block.ToString());
                        BlockDefinition.DefineGlobalBlock(def);

                        foreach (Player p in Server.Players) {
                            if (p.Supports(CpeExtension.BlockDefinitions) || p.Supports(CpeExtension.BlockDefinitionsExt))
                                BlockDefinition.SendGlobalAdd(p, def);
                        }

                        BlockDefinition.SaveGlobalDefinitions();
                        player.currentGBStep = -1;
                        player.currentGB = null;

                        Server.Message("{0} &screated a new global custom block &h{1} &swith ID {2}",
                                       player.ClassyName, def.Name, def.BlockID);
                        ReloadAllPlayers();
                    }
                    return;
            }
            player.currentGBStep = step;
            PrintStepHelp(player);
        }
        
        static void GlobalBlockDuplicateHandler(Player p, CommandReader cmd) {
            int sourceId, newId;
            if (!CheckBlockId(p, cmd, out sourceId) || !CheckBlockId(p, cmd, out newId))
                return;
            
            BlockDefinition srcDef = BlockDefinition.GlobalDefinitions[sourceId];
            if (srcDef == null) {
                p.Message("There is no custom block with the id: &a{0}", sourceId);
                p.Message("Use \"&h/gb list&s\" to see a list of global custom blocks.");
                return;
            }
            BlockDefinition dstDef = BlockDefinition.GlobalDefinitions[newId];
            if (dstDef != null) {
                p.Message("There is already a custom block with the id: &a{0}", newId);
                p.Message("Use \"&h/gb remove {0}&s\" on this block first.", newId);
                p.Message("Use \"&h/gb list&s\" to see a list of global custom blocks.");
                return;
            }            
            BlockDefinition def = srcDef.Copy();
            def.BlockID = (byte)newId;
            BlockDefinition.DefineGlobalBlock(def);
            Server.Message("{0} &screated a new global custom block &h{1} &swith ID {2}",
                           p.ClassyName, def.Name, def.BlockID);
            
            foreach (Player pl in Server.Players) {
                if (pl.Supports(CpeExtension.BlockDefinitions))
                    BlockDefinition.SendGlobalAdd(pl, def);
            }            
        }
        
        static void GlobalBlockEditHandler(Player player, CommandReader cmd) {
            int blockId;
            if (!CheckBlockId(player, cmd, out blockId))
                return;
            BlockDefinition def = BlockDefinition.GlobalDefinitions[blockId];
            if (def == null) {
                player.Message("There are no custom defined blocks by that ID");
                return;
            }
            BlockDefinition newDef = def;
            string option = cmd.Next() ?? "n/a";
            string args = cmd.NextAll();
            if (string.IsNullOrEmpty(args)) {
                player.Message("Please specify what you want to change the {0} option to.", option);
                return;
            }
            byte value = 0;
            bool boolVal = true;
            bool hasChanged = false;

            switch (option.ToLower()) {
                case "name":
                    player.Message("&bChanged name of &a{0}&b to &A{1}", def.Name, args);
                    newDef.Name = args;
                    break;
                case "solid":
                case "solidity":
                case "collide":
                case "collidetype":
                    if (byte.TryParse(args, out value) && value <= 2) {
                        player.Message("&bChanged solidity of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.CollideType, value);
                        newDef.CollideType = value;
                        hasChanged = true;
                    }
                    break;
                case "speed":
                    float speed;
                    if (float.TryParse(args, out speed)
                        && speed >= 0.25f && value <= 3.96f) {
                        player.Message("&bChanged speed of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.Speed, speed);
                        newDef.Speed = speed;
                        hasChanged = true;
                    }
                    break;
                case "allid":
                case "alltex":
                case "alltexture":
                    if (byte.TryParse(args, out value)) {
                        player.Message("&bChanged top, sides, and bottom texture index of &a{0}&b to &a{1}", def.Name, value);
                        newDef.TopTex = value;
                        newDef.SideTex = value;
                        newDef.BottomTex = value;
                        hasChanged = true;
                    }
                    break;
                case "topid":
                case "toptex":
                case "toptexture":
                    if (byte.TryParse(args, out value)) {
                        player.Message("&bChanged top texture index of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.TopTex, value);
                        newDef.TopTex = value;
                        hasChanged = true;
                    }
                    break;
                case "sideid":
                case "sidetex":
                case "sidetexture":
                    if (byte.TryParse(args, out value)) {
                        player.Message("&bChanged sides texture index of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.SideTex, value);
                        newDef.SideTex = value;
                        hasChanged = true;
                    }
                    break;
                case "bottomid":
                case "bottomtex":
                case "bottomtexture":
                    if (byte.TryParse(args, out value)) {
                        player.Message("&bChanged bottom texture index of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.BottomTex, value);
                        newDef.BottomTex = value;
                        hasChanged = true;
                    }
                    break;
                case "light":
                case "blockslight":
                    if (bool.TryParse(args, out boolVal)) {
                        player.Message("&bChanged blocks light of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.BlocksLight, boolVal);
                        newDef.BlocksLight = boolVal;
                        hasChanged = true;
                    }
                    break;
                case "sound":
                case "walksound":
                    if (byte.TryParse(args, out value) && value <= 11) {
                        player.Message("&bChanged walk sound of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.WalkSound, value);
                        newDef.WalkSound = value;
                        hasChanged = true;
                    }
                    break;
                case "fullbright":
                    if (bool.TryParse(args, out boolVal)) {
                        player.Message("&bChanged full bright of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FullBright, boolVal);
                        newDef.FullBright = boolVal;
                        hasChanged = true;
                    }
                    break;
                case "size":
                case "shape":
                case "height":
                    if (byte.TryParse(args, out value) && value <= 16) {
                        player.Message("&bChanged block shape of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.Shape, value);
                        newDef.Shape = value;
                        hasChanged = true;
                    }
                    break;
                case "draw":
                case "blockdraw":
                    if (byte.TryParse(args, out value) && value <= 4) {
                        player.Message("&bChanged block draw type of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.BlockDraw, value);
                        newDef.BlockDraw = value;
                        hasChanged = true;
                    }
                    break;
                case "fogdensity":
                case "fogd":
                    if (byte.TryParse(args, out value)) {
                        player.Message("&bChanged density of fog of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogDensity, value);
                        newDef.FogDensity = value;
                        hasChanged = true;
                    }
                    break;
                case "foghex":
                    if (WorldCommands.IsValidHex(args)) {
                        System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml("#" + args.ToUpper().Replace("#", ""));
                        player.Message("&bChanged red fog component of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogR, col.R);
                        newDef.FogR = col.R;
                        player.Message("&bChanged green fog component of fog of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogG, col.G);
                        newDef.FogG = col.G;
                        player.Message("&bChanged blue fog component of fog of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogB, col.B);
                        newDef.FogB = col.B;
                        hasChanged = true;
                    }
                    break;
                case "fogr":
                case "fogred":
                    if (byte.TryParse(args, out value)) {
                        player.Message("&bChanged red fog component of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogR, value);
                        def.FogG = value;
                        hasChanged = true;
                    }
                    break;
                case "fogg":
                case "foggreen":
                    if (byte.TryParse(args, out value)) {
                        player.Message("&bChanged green fog component of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogG, value);
                        newDef.FogG = value;
                        hasChanged = true;
                    }
                    break;
                case "fogb":
                case "fogblue":
                    if (byte.TryParse(args, out value)) {
                        player.Message("&bChanged blue fog component of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FogB, value);
                        newDef.FogB = value;
                        hasChanged = true;
                    }
                    break;
                case "fallback":
                case "block":
                    Block block;
                    Map.GetBlockByName(def.FallBack.ToString(), false, out block);
                    Block newBlock;
                    if (Map.GetBlockByName(args, false, out newBlock)) {
                        if (block > Map.MaxCustomBlockType) {
                            player.Message("&cThe fallback block must be an original block, " +
                                           "or a block defined in the CustomBlocks extension.");
                            break;
                        }
                        player.Message("&bChanged fallback block of &a{0}&b from &a{1}&b to &a{2}", def.Name, def.FallBack, block.ToString());
                        newDef.FallBack = (byte)block;
                        hasChanged = true;
                    }
                    break;
                case "min":
                    if (args.ToLower().Equals("-1")){
                        player.Message("Block will display as a sprite!");
                        newDef.Shape = 0;
                        hasChanged = true;
                        break;
                    }
                    if (args.Split().Count() != 3) {
                        player.Message("Please specify 3 coordinates!");
                        break;
                    }
                    newDef.MinX = EditCoord(player, "min X", def.Name, args.Split()[0], def.MinX, ref hasChanged);
                    newDef.MinY = EditCoord(player, "min Y", def.Name, args.Split()[1], def.MinY, ref hasChanged);
                    newDef.MinZ = EditCoord(player, "min Z", def.Name, args.Split()[2], def.MinZ, ref hasChanged);
                    hasChanged = true;
                    break;
                case "max":
                    if (args.Split().Count() != 3) {
                        player.Message("Please specify 3 coordinates!");
                        break;
                    }
                    newDef.MaxX = EditCoord(player, "max X", def.Name, args.Split()[0], def.MaxX, ref hasChanged);
                    newDef.MaxY = EditCoord(player, "max Y", def.Name, args.Split()[1], def.MaxY, ref hasChanged);
                    newDef.MaxZ = EditCoord(player, "max Z", def.Name, args.Split()[2], def.MaxZ, ref hasChanged);
                    hasChanged = true;
                    break;
                case "minx":
                    newDef.MinX = EditCoord(player, "min X", def.Name, args, def.MinX, ref hasChanged); break;
                case "miny":
                    newDef.MinY = EditCoord(player, "min Y", def.Name, args, def.MinY, ref hasChanged); break;
                case "minz":
                    newDef.MinZ = EditCoord(player, "min Z", def.Name, args, def.MinZ, ref hasChanged); break;
                case "maxx":
                    newDef.MaxX = EditCoord(player, "max X", def.Name, args, def.MaxX, ref hasChanged); break;
                case "maxy":
                    newDef.MaxY = EditCoord(player, "max Y", def.Name, args, def.MaxY, ref hasChanged); break;
                case "maxz":
                    newDef.MaxZ = EditCoord(player, "max Z", def.Name, args, def.MaxZ, ref hasChanged);
                    if (byte.TryParse(args, out value)) {
                        newDef.Shape = value;
                    }
                    break;
                default:
                    CdGlobalBlock.PrintUsage(player);
                    return;
            }
            if (hasChanged) {
                Server.Message("{0} &sedited a global custom block &a{1} &swith ID &a{2}",
                               player.ClassyName, newDef.Name, newDef.BlockID);
                BlockDefinition.RemoveGlobalBlock(def);
                BlockDefinition.DefineGlobalBlock(newDef);

                foreach (Player p in Server.Players) {
                    if (p.Supports(CpeExtension.BlockDefinitions)) {
                        BlockDefinition.SendGlobalRemove(p, def);
                        BlockDefinition.SendGlobalAdd(p, newDef);
                    }
                }

                BlockDefinition.SaveGlobalDefinitions();
                ReloadAllPlayers();
            }
        }
        
        static byte EditCoord(Player player, string coord, string name, string args, byte origValue, ref bool hasChanged) {
            byte value;
            if (byte.TryParse(args, out value) && value <= 16) {
                player.Message("&bChanged {0} coordinate of &a{1}&b from &a{2}&b to &a{3}", coord, name, origValue, value);
                hasChanged = true;
                return value;
            }
            return origValue;
        }

        static bool CheckBlockId(Player player, CommandReader cmd, out int blockId) {
            if (!cmd.HasNext) {
                blockId = 0;
                player.Message("You most provide a block ID argument.");
                return false;
            }
            if (!cmd.NextInt(out blockId)) {
                player.Message("Provided block id is not a number.");
                return false;
            }           
            if (blockId <= 0 || blockId >= 255) {
                player.Message("Block id must be between 1-254");
                return false;
            }
            return true;
        }
        
        static void PrintStepHelp(Player p) {
            string[] help = globalBlockSteps[p.currentGBStep];
            foreach (string m in help)
                p.Message(m);
        }

        static void ReloadAllPlayers() {
            Player[] cache = Server.Players;
            for (int i = 0; i < cache.Length; i++) {
                Player p = cache[i];
                World world = p.World;
                if (world == null || !p.Supports(CpeExtension.BlockDefinitions))
                    continue;
                p.JoinWorld(world, WorldChangeReason.Rejoin, p.Position);
            }
        }

        static string[][] globalBlockSteps = new [] {
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
                "Min = 0 Max = 15 Example: &h/gb 0 0 0",
                "&h/gb -1&s to make it a sprite" },
            new [] { "Enter the max X Y Z coords of this block,",
                "Min = 1 Max = 16 Example: &h/gb 16 16 16" },
        };
            
        #endregion
    }
}
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
            Usage = "/gb <type/value> <args>",
            Help = "&sModifies the global custom blocks, or prints information about them.&n" +
                "&sTypes are: add, abort, list, remove, texture&n" +
                "&sSee &h/help gb <type>&s for details about each type.",
            HelpSections = new Dictionary<string, string>{
                { "add",     "&h/gb add [id]&n" +
                        "&sBegins the process of defining a global custom block with the given block id." },
                { "abort",   "&h/gb abort&n" +
                        "&sAborts the custom block that was currently in the process of being " +
                        "defined from the last /gb add call." },
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
                        step++; def.FullBright = boolVal;
                        player.Message("   &bSet full bright to: " + boolVal);
                    }
                    break;
                case 9:
                    if (byte.TryParse(args, out value) && value <= 16) {
                        step++; def.Shape = value;
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
                    if (byte.TryParse(args, out value)) {
                        step++; def.FogR = value;
                        player.Message("   &bSet red component of fog to: " + value);
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
                default:
                    Block block;
                    if (Map.GetBlockByName(args, false, out block)) {
                        if (block > Map.MaxCustomBlockType) {
                            player.Message("&cThe fallback block must be an original block, " +
                                           "or a block defined in the CustomBlocks extension.");
                        }
                        def.FallBack = (byte)block;
                        player.Message("   &bSet fallback block to: " + block.ToString());
                        BlockDefinition.DefineGlobalBlock(def);

                        foreach (Player p in Server.Players) {
                            if (p.Supports(CpeExtension.BlockDefinitions))
                                BlockDefinition.SendGlobalAdd(p, def);
                        }

                        BlockDefinition.SaveGlobalDefinitions();
                        player.currentGBStep = -1;
                        player.currentGB = null;

                        Server.Message("{0} &screated a new global custom block &h{1} &swith ID {2}",
                                       player.ClassyName, def.Name, def.BlockID);
                    }
                    return;
            }
            player.currentGBStep = step;
            PrintStepHelp(player);
        }
        
        static bool CheckBlockId(Player player, CommandReader cmd, out int blockId) {
            if (!cmd.NextInt(out blockId)) {
                player.Message("Provided block id is not a number.");
                return false;
            }
            
            if (blockId <= 0 || blockId > 255) {
                player.Message("Block id must be between 1-255");
                return false;
            }
            return true;
        }
        
        static void PrintStepHelp(Player p) {
            string[] help = globalBlockSteps[p.currentGBStep];
            foreach (string m in help)
                p.Message(m);
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
            new [] { "&sEnter the walk sound index of the block. (0-11)" },
            new [] { "&sEnter whether the block is fully bright (i.e. like lava). (true or false)" },
            new [] { "&sEnter the shape of the block. (0-16)",
                "&s0 = sprite(e.g. roses), 1-16 = cube of the given height",
                "&s(e.g. slabs have height '8', snow has height '2', dirt has height '16')" },
            new [] { "&sEnter the block draw type of this block. (0-4)",
                "&s0 = solid/opaque, 1 = transparent (like glass)",
                "&s2 = transparent (like leaves), 3 = translucent (like water)",
                "&s4 = gas (like air)" },
            new [] { "Enter the density of the fog for the block. (0-255)",
                "0 is treated as no fog, 255 is thickest fog." },
            new [] { "Enter the red component of the fog colour. (0-255)" },
            new [] { "Enter the green component of the fog colour. (0-255)" },
            new [] { "Enter the blue component of the fog colour. (0-255)" },
            new [] { "Enter the numerical fallback block id for this block.",
                "This block is shown to clients that don't support BlockDefinitions." },
        };
            
        #endregion
    }
}
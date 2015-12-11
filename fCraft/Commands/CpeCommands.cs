// ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>
using System;
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
            Aliases = new string[] { "Global", "GB" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/GlobalBlock [name] [id] [args] [fallback]",
            Help = "Good luck buddy!",
            Handler = GlobalBlockHandler
        };

        static void GlobalBlockHandler( Player player, CommandReader cmd ) {
            try {
                string name = cmd.Next();
                byte id = byte.Parse(cmd.Next());
                byte[] args = new byte[15];
                for (int i = 0; i < 15; i++)
                    args[i] = byte.Parse(cmd.Next());
                
                BlockDefinition def = new BlockDefinition(
                    id, name, args[0], (float)Math.Pow(2, (args[1] - 128) / 64f),
                    args[2], args[3], args[4], args[5] == 0, args[6], args[7] != 0,
                    args[8], args[9], args[10], args[11], args[12], args[13], args[14]);
                BlockDefinition.DefineGlobalBlock(def);
                foreach( Player p in Server.Players ) {
                    if( p.Supports(CpeExtension.BlockDefinitions))
                       BlockDefinition.SendGlobalDefinitions(p);
                }
                BlockDefinition.SaveGlobalDefinitions();
            } catch(Exception ex) {
                player.Message(ex.ToString());
                System.Console.WriteLine(ex.ToString());
                player.Message("Ya dun goofed");
            }
        }
        
        #endregion
    }
}
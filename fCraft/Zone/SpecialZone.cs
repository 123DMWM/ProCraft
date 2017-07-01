//  ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;

namespace fCraft {
    public static partial class SpecialZone {
        public const string Door = "Door_";
        public const string Sign = "Sign_";
        public const string Command = "command_";
        public const string ConsoleCommand = "c_command_";

        public const string Deny = "deny_";
        public const string Text = "text_";
        public const string Respawn = "respawn_";
        public const string Checkpoint = "checkpoint_";
        public const string Death = "death_";
        
        public static bool IsSpecialAffect(string name) {
            return name.CaselessStarts(Door) || name.CaselessStarts(Sign) ||
                name.CaselessStarts(Command) || name.CaselessStarts(ConsoleCommand);
        }
        
        public static bool IsSpecialMove(string name) {
            return name.CaselessStarts(Deny) || name.CaselessStarts(Text) ||
                name.CaselessStarts(Respawn) || name.CaselessStarts(Checkpoint) ||
                name.CaselessStarts(Deny);
        }

        /// <summary> Checks if a zone name makes it a special zone, and if so, whether the player can manage the special zone. </summary>
        public static bool CanManage(string name, Player player, string action) {
            if (name == null) return false;
            Rank rank = RankManager.GetMinRankWithAnyPermission(Permission.ManageSpecialZones);
            
            if (name.CaselessStarts(Command) || name.CaselessStarts(ConsoleCommand)) {
                if (player.Info.Rank == RankManager.HighestRank && player.Can(Permission.ManageSpecialZones))
                    return true;
                
                if (rank != null)
                    player.Message("You must be {0}&S to {1} command zone.", RankManager.HighestRank.ClassyName, action);
                else
                    player.Message("No rank has permission to {0} command zone.", action);
                return false;
            } else if (IsSpecialAffect(name) || IsSpecialMove(name)) {
                if (player.Can(Permission.ManageSpecialZones))
                    return true;
                
                if (rank != null)
                    player.Message("You must be {0}&S to {1} special zone.", rank.ClassyName, action);
                else
                    player.Message("No rank has permission to {0} special zone.", action);
                return false;
            }
            return true;
        }
        
        static string GetSignMessage(Player p, Zone zone) {
            string path = SignPath(p, zone);
            if (!File.Exists(path)) return null;            
            string[] lines = File.ReadAllLines(path);
            return String.Join("&N", lines);
        }
        
        static string SignPath(Player p, Zone zone) {
            string path = Path.Combine(Paths.SignsPath, p.World.Name);
            return Path.Combine(path, zone.Name + ".txt");
        }
        
        static void SendZoneMessage(Player p, string message) {
            if ((DateTime.UtcNow - p.LastZoneNotification).Seconds <= 2) return;
            p.Message(message);
            p.LastZoneNotification = DateTime.UtcNow;
        }
    }
}
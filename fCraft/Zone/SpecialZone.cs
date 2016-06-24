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
            return name.StartsWith(Door) || name.StartsWith(Sign) ||
                name.StartsWith(Command) || name.StartsWith(ConsoleCommand);
        }
        
        public static bool IsSpecialMove(string name) {
            return name.StartsWith(Deny) || name.StartsWith(Text) ||
                name.StartsWith(Respawn) || name.StartsWith(Checkpoint) ||
                name.StartsWith(Deny);
        }

        /// <summary> Checks if a zone name makes it a special zone, and if so, whether the player can manage the special zone. </summary>
        public static bool CanManage(string name, Player player, string action) {
            if (name == null) return false;
            Rank rank = RankManager.GetMinRankWithAnyPermission(Permission.ManageSpecialZones);
            
            if (name.StartsWith(Command) || name.StartsWith(ConsoleCommand)) {
                if (player.Info.Rank == RankManager.HighestRank && player.Can(Permission.ManageSpecialZones))
                    return true;
                
                if (rank != null)
                    player.Message("You must be {0}&s to {1} command zone.", RankManager.HighestRank.ClassyName, action);
                else
                    player.Message("No rank has permission to {1} command zone.", action);
                return false;
            } else if (IsSpecialAffect(name) || IsSpecialMove(name)) {
                if (player.Can(Permission.ManageSpecialZones))
                    return true;
                
                if (rank != null)
                    player.Message("You must be {0}&s to {1} special zone.", rank.ClassyName);
                else
                    player.Message("No rank has permission to {1} special zone.");
                return false;
            }
            return true;
        }
        
        static void SendZoneMessage(Player p, Zone zone, string backup) {
            string path = "./signs/" + p.World.Name + "/" + zone.Name + ".txt";
            string message = backup;
            if (File.Exists(path)) {
                string[] lines = File.ReadAllLines(path);
                message = String.Join("&n", lines);
            }
            
            if ((DateTime.UtcNow - p.LastZoneNotification).Seconds > 2) {
                p.Message(message);
                p.LastZoneNotification = DateTime.UtcNow;
            }
        }
    }
}
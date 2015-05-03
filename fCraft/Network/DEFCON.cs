using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Network
{
    public class DEFCON
    {
        public static int Level = 6;
        public static double FreezeBuild = 250;
        public static double FreezeDelete = 100;
        public static double FreezeRatioUpperLimit = 20;
        public static double FreezeRatioLowerLimit = 0.1;
        public static double PlacedDeleteMixedThreshold = 150;

        #region DEFCON_6
        public class DEFCON_6
        {
            public double BlockPower(Block blocktype)
            {
                return 0d;
            }
            public string[] AffectedRanks = null;
            public string[] DeniedRanks = null;
            public string ClassyName = "&fOFF";
        }
        #endregion

        #region DEFCON_5
        public class DEFCON_5
        {
            public double BlockPower(Block blocktype)
            {
                switch (blocktype)
                {
                    case Block.Air:
                        return 1d;
                    case Block.Log:
                    case Block.Wood:
                        return 0.4d;
                    case Block.Stone:
                    case Block.Grass:
                    case Block.Glass:
                    case Block.Dirt:
                    case Block.Leaves:
                        return 0.8d;
                    case Block.RedFlower:
                    case Block.YellowFlower:
                    case Block.TNT:
                    case Block.Sponge:
                    case Block.RedMushroom:
                    case Block.BrownMushroom:
                    case Block.Sapling:
                        return 0.7d;
                    case Block.Red:
                    case Block.Orange:
                    case Block.Yellow:
                    case Block.Lime:
                    case Block.Green:
                    case Block.Teal:
                    case Block.Cyan:
                    case Block.Aqua:
                    case Block.Blue:
                    case Block.Indigo:
                    case Block.Violet:
                    case Block.Magenta:
                    case Block.Pink:
                    case Block.White:
                    case Block.Gray:
                    case Block.Black:
                        return 0.5;
                    default:
                        return 1d;

                }
            }
            public string[] AffectedRanks = new[] { "NewPlayer" };
            public string[] DeniedRanks = null;
            public string ClassyName = "&3DEFCON 5";
        }
        #endregion

        #region DEFCON_4
        public class DEFCON_4
        {

            public double BlockPower(Block blocktype)
            {
                switch (blocktype)
                {
                    case Block.Air:
                        return 1.5d;
                    case Block.Log:
                    case Block.Wood:
                        return 0.6d;
                    case Block.Stone:
                    case Block.Grass:
                    case Block.Glass:
                    case Block.Dirt:
                        return 1d;
                    case Block.RedFlower:
                    case Block.YellowFlower:
                    case Block.TNT:
                    case Block.Sponge:
                    case Block.RedMushroom:
                    case Block.BrownMushroom:
                    case Block.Sapling:
                        return 0.9d;
                    case Block.Red:
                    case Block.Orange:
                    case Block.Yellow:
                    case Block.Lime:
                    case Block.Green:
                    case Block.Teal:
                    case Block.Cyan:
                    case Block.Aqua:
                    case Block.Blue:
                    case Block.Indigo:
                    case Block.Violet:
                    case Block.Magenta:
                    case Block.Pink:
                    case Block.White:
                    case Block.Gray:
                    case Block.Black:
                        return 1.2;
                    default:
                        return 1.5d;

                }
            }
            public string[] AffectedRanks = new[] { "NewPlayer", "AverageJoe" };
            public string[] DeniedRanks = null;
            public string ClassyName = "&2DEFCON 4";
        }
        #endregion

        #region DEFCON_3
        public class DEFCON_3
        {

            public double BlockPower(Block blocktype)
            {
                switch (blocktype)
                {
                    case Block.Air:
                        return 3d;
                    case Block.Log:
                    case Block.Wood:
                        return 1d;
                    case Block.Stone:
                    case Block.Grass:
                    case Block.Glass:
                    case Block.Dirt:
                        return 1.5d;
                    case Block.RedFlower:
                    case Block.YellowFlower:
                    case Block.TNT:
                    case Block.Sponge:
                    case Block.RedMushroom:
                    case Block.BrownMushroom:
                    case Block.Sapling:
                        return 1.4d;
                    case Block.Red:
                    case Block.Orange:
                    case Block.Yellow:
                    case Block.Lime:
                    case Block.Green:
                    case Block.Teal:
                    case Block.Cyan:
                    case Block.Aqua:
                    case Block.Blue:
                    case Block.Indigo:
                    case Block.Violet:
                    case Block.Magenta:
                    case Block.Pink:
                    case Block.White:
                    case Block.Gray:
                    case Block.Black:
                        return 1.3;
                    default:
                        return 3d;

                }
            }
            public string[] AffectedRanks = new[] { "NewPlayer", "AverageJoe", "Constructor" };
            public string[] DeniedRanks = new[] { "NewPlayer" };
            public string ClassyName = "&aDEFCON 3";
        }
        #endregion

        #region DEFCON_2
        public class DEFCON_2
        {

            public double BlockPower(Block blocktype)
            {
                switch (blocktype)
                {
                    case Block.Air:
                        return 5d;
                    case Block.Log:
                    case Block.Wood:
                        return 1.6d;
                    case Block.Stone:
                    case Block.Grass:
                    case Block.Glass:
                    case Block.Dirt:
                        return 2d;
                    case Block.RedFlower:
                    case Block.YellowFlower:
                    case Block.TNT:
                    case Block.Sponge:
                    case Block.RedMushroom:
                    case Block.BrownMushroom:
                    case Block.Sapling:
                        return 2d;
                    case Block.Red:
                    case Block.Orange:
                    case Block.Yellow:
                    case Block.Lime:
                    case Block.Green:
                    case Block.Teal:
                    case Block.Cyan:
                    case Block.Aqua:
                    case Block.Blue:
                    case Block.Indigo:
                    case Block.Violet:
                    case Block.Magenta:
                    case Block.Pink:
                    case Block.White:
                    case Block.Gray:
                    case Block.Black:
                        return 1.8;
                    default:
                        return 5d;

                }
            }
            public string[] AffectedRanks = new[] { "NewPlayer", "AverageJoe", "Constructor", "Citizen" };
            public string[] DeniedRanks = new[] { "NewPlayer", "AverageJoe" };
            public string ClassyName = "&sDEFCON 2";
        }
        #endregion

        #region DEFCON_1
        public class DEFCON_1
        {

            public double BlockPower(Block blocktype)
            {
                switch (blocktype)
                {
                    case Block.Air:
                        return 10d;
                    case Block.Log:
                    case Block.Wood:
                        return 3d;
                    case Block.Stone:
                    case Block.Grass:
                    case Block.Glass:
                    case Block.Dirt:
                        return 4d;
                    case Block.RedFlower:
                    case Block.YellowFlower:
                    case Block.TNT:
                    case Block.Sponge:
                    case Block.RedMushroom:
                    case Block.BrownMushroom:
                    case Block.Sapling:
                        return 3.8d;
                    case Block.Red:
                    case Block.Orange:
                    case Block.Yellow:
                    case Block.Lime:
                    case Block.Green:
                    case Block.Teal:
                    case Block.Cyan:
                    case Block.Aqua:
                    case Block.Blue:
                    case Block.Indigo:
                    case Block.Violet:
                    case Block.Magenta:
                    case Block.Pink:
                    case Block.White:
                    case Block.Gray:
                    case Block.Black:
                        return 3.5;
                    default:
                        return 10d;

                }
            }
            public string[] AffectedRanks = new[] { "NewPlayer", "AverageJoe", "Constructor", "Citizen", "Trainee" };
            public string[] DeniedRanks = new[] { "NewPlayer", "AverageJoe", "Constructor", "Citizen" };
            public string ClassyName = "&cDEFCON 1";
        }
        #endregion

        #region Warnings & Punishments
        void PunishGrief(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() == 0 && !player.Info.SecurityTrip)
                {
                    player.Kick("Kicked for suspected griefing!", LeaveReason.BlockSpamKick);
                    //Player.Console.Message("Player was kicked as an anti-grief precaution!");
                    //Server.Message(Color.Warning + "Player {0}" + Color.Warning + " was kicked for suspected griefing!", player.ClassyName);
                    Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cwas kicked for suspected griefing!");
                    player.Info.SecurityTrip = true;
                }
                else if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() >= 1 && !player.Info.SecurityTrip) {
                    player.Message("&cWarning: You have been frozen for suspected griefing. A Moderator/Admin will assist you shortly!");
                    Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &chas been frozen for suspected griefing!");
                    //Player.Console.Message("Player was Frozen as an anti-grief precaution!");
                    player.Info.Freeze(Player.Console, false, true);
                    player.Info.SecurityTrip = true;
                    player.BlocksDeletedThisSession = 0d;
                    player.BlocksPlacedThisSession = 0d;
                    player.BlocksPlacedDeletedMixed = 0d;
                }
        }

        void WarnGrief(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (MinRankCanFreezeOthers.Players.Count() > 0)
            {
                //MinRankCanFreezeOthers.Players.Message("&CWarning: Player {0}&C may be griefing! {1}B/{2}D", player.ClassyName, ((int)player.BlocksPlacedThisSession).ToString(), ((int)player.BlocksDeletedThisSession).ToString());
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be griefing! " + ((int)player.BlocksPlacedThisSession).ToString() + "B/" + ((int)player.BlocksDeletedThisSession).ToString() + "D");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be griefing! " + ((int)player.BlocksPlacedThisSession).ToString() + "B/" + ((int)player.BlocksDeletedThisSession).ToString() + "D");
                player.Message("Warning: You are Deleting a lot of blocks this session.&nBuild something, or you may get frozen!");
                player.Warned = true;
            }
            else
            {
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be griefing! " + player.BlocksPlacedThisSession.ToString() + "B/" + player.BlocksDeletedThisSession.ToString() + "D");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be griefing! " + ((int)player.BlocksPlacedThisSession).ToString() + "B/" + ((int)player.BlocksDeletedThisSession).ToString() + "D");
                player.Message("Warning: You are Deleting a lot of blocks this session.&nBuild something, or you may get kicked!");
                player.Warned = true;
            }
        }

        void PunishSpam(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() == 0 && !player.Info.SecurityTrip)
            {
                player.Kick("Kicked for suspected block spam!", LeaveReason.BlockSpamKick);
                //Player.Console.Message("Player " + player.ClassyName + " was kicked as an anti-block spam precaution!");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cwas kicked as an anti-block spam precaution!");
                Server.Message(Color.Warning + "Player {0}" + Color.Warning + " was kicked for suspected block spamming!", player.ClassyName);
                player.Info.SecurityTrip = true;
            }
            else if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() >= 1 && !player.Info.SecurityTrip)
            {
                player.Message("Warning: You have been frozen for suspected block spam. A Moderator/Admin will assist you shortly! :D");
                //Player.Console.Message("Player was Frozen as an anti-block spam precaution!");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &chas been frozen an anti-block spam precaution!");
                player.Info.Freeze(Player.Console, false, true);
                player.Info.SecurityTrip = true;
                player.BlocksDeletedThisSession = 0d;
                player.BlocksPlacedThisSession = 0d;
                player.BlocksPlacedDeletedMixed = 0d;
            }
        }

        void WarnSpam(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (MinRankCanFreezeOthers.Players.Count() > 0)
            {
                //MinRankCanFreezeOthers.Players.Message("&CWarning: Player {0}&C may be block spamming! {1}B/{2}D", player.ClassyName, ((int)player.BlocksPlacedThisSession).ToString(), ((int)player.BlocksDeletedThisSession).ToString());
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be block spamming! " + ((int)player.BlocksPlacedThisSession).ToString() + "B/" + ((int)player.BlocksDeletedThisSession).ToString() + "D");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be block spamming! " + ((int)player.BlocksPlacedThisSession).ToString() + "B/" + ((int)player.BlocksDeletedThisSession).ToString() + "D");
                player.Message("Warning: You are Placing a lot of blocks this session.&nIt might look like you are block spamming. You may get frozen!");
                player.Warned = true;
            }
            else
            {
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be griefing! " + player.BlocksPlacedThisSession.ToString() + "B/" + player.BlocksDeletedThisSession.ToString() + "D");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be block spamming! " + ((int)player.BlocksPlacedThisSession).ToString() + "B/" + ((int)player.BlocksDeletedThisSession).ToString() + "D");
                player.Message("Warning: You are Placing a lot of blocks this session.&nIt might look like you are block spamming. You may get kicked!");
                player.Warned = true;
            }
        }

        void WarnHighThreshold(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (MinRankCanFreezeOthers.Players.Count() > 0)
            {
                //MinRankCanFreezeOthers.Players.Message("&CWarning: Player {0}&C may be spamming! Placed a lot of blocks quickly.");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be block spamming! Placed a lot of blocks quickly.");
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be spamming! Placed a lot of blocks quickly.");
                player.Message("Warning: You are Placing blocks a bit too fast! Slow down a bit, Or you may get frozen!");
                player.Warned = true;
            }
            else
            {
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be spamming! Placed a lot of blocks quickly.");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be block spamming! Placed a lot of blocks quickly.");
                player.Message("Warning: You are Placing blocks a bit too fast! Slow down a bit, Or you may get kicked!");
                player.Warned = true;
            }
        }

        void PunishHighThreshold(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() == 0 && !player.Info.SecurityTrip)
            {
                player.Kick("Kicked for suspected block spam!", LeaveReason.BlockSpamKick);
                //Player.Console.Message("Player was Kicked as an anti-block spam precaution!");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cwas kicked for suspected block spamming! Placed a lot of blocks quickly.");
                Server.Message(Color.Warning + "Player {0}" + Color.Warning + " was kicked for suspected block spamming!", player.ClassyName);
                player.Info.SecurityTrip = true;
            }
            else if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() >= 1 && !player.Info.SecurityTrip)
            {
                player.Message("Warning: You have been frozen for suspected block spam. A Moderator/Admin will assist you shortly! :D");
                //Player.Console.Message("Player was Frozen as an anti-block spam precaution!");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &chas been frozen for suspected block spamming! Placed a lot of blocks quickly.");
                player.Info.Freeze(Player.Console, false, true);
                player.Info.SecurityTrip = true;
                player.BlocksDeletedThisSession = 0d;
                player.BlocksPlacedThisSession = 0d;
                player.BlocksPlacedDeletedMixed = 0d;
            }
        }

        void WarnLowThreshold(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (MinRankCanFreezeOthers.Players.Count() > 0)
            {
                //MinRankCanFreezeOthers.Players.Message("&CWarning: Player {0}&C may be griefing! Deleted a lot of blocks quickly.");
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be grief! Deleted a lot of blocks quickly.");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be griefing! Deleted a lot of blocks quickly.");
                player.Message("Warning: You are Deleting blocks a bit too fast! Slow down a bit, Or you may get frozen!");
                player.Warned = true;
            }
            else
            {
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be griefing! Deleted a lot of blocks quickly.");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be griefing! Deleted a lot of blocks quickly.");
                player.Message("Warning: You are Deleting blocks a bit too fast! Slow down a bit, Or you may get kicked!");
                player.Warned = true;
            }
        }

        void PunishLowThreshold(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() == 0 && !player.Info.SecurityTrip)
            {
                player.Kick("Kicked for suspected griefing!", LeaveReason.BlockSpamKick);
                //Player.Console.Message("Player was Kicked as an anti-grief precaution!");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cwas kicked for griefing! Deleted a lot of blocks quickly.");
                Server.Message(Color.Warning + "Player {0}" + Color.Warning + " was kicked for suspected griefing!", player.ClassyName);
                player.Info.SecurityTrip = true;
            }
            else if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() >= 1 && !player.Info.SecurityTrip)
            {
                player.Message("Warning: You have been frozen for suspected griefing. A Moderator/Admin will assist you shortly! :D");
                //Player.Console.Message("Player was Frozen as an anti-griefing precaution!");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &chas been frozen for suspected griefing! Deleted a lot of blocks quickly.");
                player.Info.Freeze(Player.Console, false, true);
                player.Info.SecurityTrip = true;
                player.BlocksDeletedThisSession = 0d;
                player.BlocksPlacedThisSession = 0d;
                player.BlocksPlacedDeletedMixed = 0d;
            }
        }

        void WarnRatioHigh(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (MinRankCanFreezeOthers.Players.Count() > 0)
            {
                //MinRankCanFreezeOthers.Players.Message("&CWarning: Player {0}&C may be spamming! B:D Too High.");
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be spamming! B:D Too High.");
                player.Message("Warning: You are Placing TOO Many blocks this session. Ask for a &h/review&s to get promoted, or you may get frozen!");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be block spamming! Placed too many blocks this session");
                player.Warned = true;
            }
            else
            {
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be spamming! B:D Too High.");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be block spamming! Placed too many blocks this session");
                player.Message("Warning: You are Placing TOO Many blocks this session. Ask for a &h/review&s to get promoted, or you may get kicked!");
                player.Warned = true;
            }
        }
        
        void PunishRatioHigh(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() == 0 && !player.Info.SecurityTrip)
            {
                player.Kick("Kicked for suspected block spam!", LeaveReason.BlockSpamKick);
                //Player.Console.Message("Player was Kicked as an anti-block spam precaution!");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " has been kicked for suspected block spamming! Placed too many blocks this session");
                Server.Message(Color.Warning + "Player {0}" + Color.Warning + " &cwas kicked for suspected block spamming!", player.ClassyName);
                player.Info.SecurityTrip = true;
            }
            else if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() >= 1 && !player.Info.SecurityTrip)
            {
                player.Message("Warning: You have been frozen for suspected block spam. A Moderator/Admin will assist you shortly! :D");
                //Player.Console.Message("Player was Frozen as an anti-block spam precaution!");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &chas been frozen for suspected block spamming! Placed too many blocks this session");
                player.Info.Freeze(Player.Console, false, true);
                player.Info.SecurityTrip = true;
                player.BlocksDeletedThisSession = 0d;
                player.BlocksPlacedThisSession = 0d;
                player.BlocksPlacedDeletedMixed = 0d;
            }
        }

        void WarnRatioLow(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (MinRankCanFreezeOthers.Players.Count() > 0)
            {
                //MinRankCanFreezeOthers.Players.Message("&CWarning: Player {0}&C may be griefing! B:D Too Low.");
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be griefing! B:D Too Low.");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be griefing! Deleted too many blocks this session");
                player.Message("Warning: You are Deleting TOO Many blocks this session. Build something, or you may get frozen!");
                player.Warned = true;
            }
            else
            {
                //Player.Console.Message("Warning: Player " + player.ClassyName + " may be spamming! B:D Too High.");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &cmay be griefing! Deleted too many blocks this session");
                player.Message("Warning: You are Deleting TOO Many blocks this session. Build something, or you may get kicked!");
                player.Warned = true;
            }
        }

        void PunishRatioLow(Player player)
        {
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() == 0 && !player.Info.SecurityTrip)
            {
                player.Kick("Kicked for suspected griefing!", LeaveReason.BlockSpamKick);
                //Player.Console.Message("Player was Kicked as an anti-grief precaution!");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &chas been kicked for suspected griefing! Deleted too many blocks this session");
                Server.Message(Color.Warning + "Player {0}" + Color.Warning + " was kicked for suspected griefing!", player.ClassyName);
                player.Info.SecurityTrip = true;
            }
            else if (Server.Players.RankedAbove(MinRankCanFreezeOthers).Count() >= 1 && !player.Info.SecurityTrip)
            {
                player.Message("Warning: You have been frozen for suspected griefing. A Moderator/Admin will assist you shortly! :D");
                //Player.Console.Message("Player was Frozen as an anti-griefing precaution!");
                Chat.SendStaffSay(Player.Console, "&cDEFCON: Player " + player.ClassyName + " &chas been frozen for suspected griefing! Deleted too many blocks this session");
                player.Info.Freeze(Player.Console, false, true);
                player.Info.SecurityTrip = true;
                player.BlocksDeletedThisSession = 0d;
                player.BlocksPlacedThisSession = 0d;
                player.BlocksPlacedDeletedMixed = 0d;
            }
        }
        #endregion

        public double FeedCorrectDefcon(int Level, Block BlockType)
        {
            switch (Level)
            {
                case 6: return (new DEFCON_6()).BlockPower(BlockType);
                case 5: return (new DEFCON_5()).BlockPower(BlockType);
                case 4: return (new DEFCON_4()).BlockPower(BlockType);
                case 3: return (new DEFCON_3()).BlockPower(BlockType);
                case 2: return (new DEFCON_2()).BlockPower(BlockType);
                case 1: return (new DEFCON_1()).BlockPower(BlockType);
                default: return 0d;
            }
        }

        public bool AllowedBuild(Player player, Block thisblock)
        {
            double BlockAdjustementResult;
            //Test if player is denied the right to build at ALL due to defcon.
            if (Level == 6)
            {
                //DEFCON not set. ABORT!
                return true;
            }

            switch (Level)
            #region switches
            {
                case 6:
                    DEFCON_6 Defcon6 = new DEFCON_6();                    
                    return true;
                case 5:
                    DEFCON_5 Defcon5 = new DEFCON_5();
                    if (Defcon5.DeniedRanks != null)
                    {
                        foreach (string DeniedRankName in Defcon5.DeniedRanks)
                        {
                            if (player.Info.Rank.Name == DeniedRankName)
                            {
                                player.Message("You are unable to build or delete, as the Servers DEFCON is set to {0}", Defcon5.ClassyName);
                                return false;
                            }
                        }
                        BlockAdjustementResult = Defcon5.BlockPower(thisblock);
                    }
                    else
                    {
                        BlockAdjustementResult = Defcon5.BlockPower(thisblock);
                    }
                    if (thisblock == Block.Air && (player.World.Name != "Grief" || player.World.Name != "Spleef")) {
                        player.BlocksDeletedThisSession += BlockAdjustementResult;
                        player.BlocksPlacedDeletedMixed -= BlockAdjustementResult;
                    }
                    else if (player.World.Name != "Grief" || player.World.Name != "Spleef"){
                        player.BlocksPlacedThisSession += BlockAdjustementResult;
                        player.BlocksPlacedDeletedMixed += BlockAdjustementResult;
                    }
                    return true;
                case 4:
                    DEFCON_4 Defcon4 = new DEFCON_4();
                    if (Defcon4.DeniedRanks != null)
                    {
                        foreach (string DeniedRankName in Defcon4.DeniedRanks)
                        {
                            if (player.Info.Rank.Name == DeniedRankName)
                            {
                                player.Message("You are unable to build or delete, as the Servers DEFCON is set to {0}", Defcon4.ClassyName);
                                return false;
                            }
                        }
                        BlockAdjustementResult = Defcon4.BlockPower(thisblock);
                    }
                    else
                    {
                        BlockAdjustementResult = Defcon4.BlockPower(thisblock);
                    }
                    if (thisblock == Block.Air && (player.World.Name != "Grief" || player.World.Name != "Spleef"))
                    {
                        player.BlocksDeletedThisSession += BlockAdjustementResult;
                        player.BlocksPlacedDeletedMixed -= BlockAdjustementResult;
                    }
                    else if (player.World.Name != "Grief" || player.World.Name != "Spleef")
                    {
                        player.BlocksPlacedThisSession += BlockAdjustementResult;
                        player.BlocksPlacedDeletedMixed += BlockAdjustementResult;
                    }
                    return true;
                case 3:
                    DEFCON_3 Defcon3 = new DEFCON_3();
                    if (Defcon3.DeniedRanks != null)
                    {
                        foreach (string DeniedRankName in Defcon3.DeniedRanks)
                        {
                            if (player.Info.Rank.Name == DeniedRankName)
                            {
                                player.Message("You are unable to build or delete, as the Servers DEFCON is set to {0}", Defcon3.ClassyName);
                                return false;
                            }
                        }
                        BlockAdjustementResult = Defcon3.BlockPower(thisblock);
                    }
                    else
                    {
                        BlockAdjustementResult = Defcon3.BlockPower(thisblock);
                    }
                    if (thisblock == Block.Air && (player.World.Name != "Grief" || player.World.Name != "Spleef"))
                    {
                        player.BlocksDeletedThisSession += BlockAdjustementResult;
                        player.BlocksPlacedDeletedMixed -= BlockAdjustementResult;
                    }
                    else if (player.World.Name != "Grief" || player.World.Name != "Spleef")
                    {
                        player.BlocksPlacedThisSession += BlockAdjustementResult;
                        player.BlocksPlacedDeletedMixed += BlockAdjustementResult;
                    }
                    return true;
                case 2:
                    DEFCON_2 Defcon2 = new DEFCON_2();
                    if (Defcon2.DeniedRanks != null)
                    {
                        foreach (string DeniedRankName in Defcon2.DeniedRanks)
                        {
                            if (player.Info.Rank.Name == DeniedRankName)
                            {
                                player.Message("You are unable to build or delete, as the Servers DEFCON is set to {0}", Defcon2.ClassyName);
                                return false;
                            }
                        }
                        BlockAdjustementResult = Defcon2.BlockPower(thisblock);
                    }
                    else
                    {
                        BlockAdjustementResult = Defcon2.BlockPower(thisblock);
                    }
                    if (thisblock == Block.Air && (player.World.Name != "Grief" || player.World.Name != "Spleef"))
                    {
                        player.BlocksDeletedThisSession += BlockAdjustementResult;
                        player.BlocksPlacedDeletedMixed -= BlockAdjustementResult;
                    }
                    else if (player.World.Name != "Grief" || player.World.Name != "Spleef")
                    {
                        player.BlocksPlacedThisSession += BlockAdjustementResult;
                        player.BlocksPlacedDeletedMixed += BlockAdjustementResult;
                    }
                    return true;
                case 1:
                    DEFCON_1 Defcon1 = new DEFCON_1();
                    if (Defcon1.DeniedRanks != null)
                    {
                        foreach (string DeniedRankName in Defcon1.DeniedRanks)
                        {
                            if (player.Info.Rank.Name == DeniedRankName)
                            {
                                player.Message("You are unable to build or delete, as the Servers DEFCON is set to {0}", Defcon1.ClassyName);
                                return false;
                            }
                        }
                        BlockAdjustementResult = Defcon1.BlockPower(thisblock);
                    }
                    else
                    {
                        BlockAdjustementResult = Defcon1.BlockPower(thisblock);
                    }
                    if (thisblock == Block.Air && (player.World.Name != "Grief" || player.World.Name != "Spleef"))
                    {
                        player.BlocksDeletedThisSession += BlockAdjustementResult;
                        player.BlocksPlacedDeletedMixed -= BlockAdjustementResult;
                    }
                    else if (player.World.Name != "Grief" || player.World.Name != "Spleef")
                    {
                        player.BlocksPlacedThisSession += BlockAdjustementResult;
                        player.BlocksPlacedDeletedMixed += BlockAdjustementResult;
                    }
                    return true;
                default:
                    //NO DEFCON. NO LOOKUP OR ADJUSTMENT REQUIRED.
                    return true;
            }
            #endregion
        }

        public void OLDProcessor(Player player) {
            Rank freezerank = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (player.BlocksDeletedThisSession < 0) player.BlocksDeletedThisSession = 0;
            if (player.BlocksPlacedThisSession < 0) player.BlocksPlacedThisSession = 0;
            if (player.BlocksPlacedThisSession == 0 && player.BlocksDeletedThisSession == 10)
            {
                freezerank.Players.Message("&CWarning: Player {0} may be griefing! {1}B/{2}D", player.ClassyName, player.BlocksPlacedThisSession.ToString(), player.BlocksDeletedThisSession.ToString());
                Player.Console.Message("Warning: Player " + player.ClassyName + " may be griefing! " + player.BlocksPlacedThisSession.ToString() + "B/" + player.BlocksDeletedThisSession.ToString() + "D");
                if (Server.Players.RankedAbove(freezerank).Count() == 0 && !player.Info.SecurityTrip)
                {
                    player.Kick("Kicked for suspected griefing!", LeaveReason.BlockSpamKick);
                    if (!player.Info.SecurityTrip) Player.Console.Message("Player was kicked as a precaution!");
                    Server.Message(Color.Warning + "Player {0}" + Color.Warning + " was kicked for suspected griefing!", player.ClassyName);
                }
                else player.Message("Warning: You are Deleting a lot of blocks this session.&nBuild something, or you may get frozen!");
            }
            if (player.BlocksPlacedThisSession == 25 && player.BlocksDeletedThisSession == 0)
            {
                Server.Players.Can(Permission.ReadStaffChat).Message("&CWarning: Player {0} may be spamming! {1}B/{2}D", player.ClassyName, player.BlocksPlacedThisSession.ToString(), player.BlocksDeletedThisSession.ToString());
                Player.Console.Message("Warning: Player " + player.ClassyName + " may be spamming! " + player.BlocksPlacedThisSession.ToString() + "B/" + player.BlocksDeletedThisSession.ToString() + "D");
                if (Server.Players.RankedAbove(freezerank).Count() == 0 && !player.Info.SecurityTrip)
                {
                    player.Kick("Kicked for suspected block spamming!", LeaveReason.BlockSpamKick);
                    if (!player.Info.SecurityTrip) Player.Console.Message("Player was kicked as a precaution!");
                    Server.Message(Color.Warning + "Player {0}" + Color.Warning + " was kicked for suspected block spamming!", player.ClassyName);
                }
                else player.Message("Warning: You are Placing a lot of blocks this session.&nDelete something, or you may get frozen!");
            }
            if (player.BlocksDeletedThisSession != 0)
            {
                if (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession == 10d / 50d && player.BlocksDeletedThisSession >= 25)
                {
                    Server.Players.Can(Permission.ReadStaffChat).Message("&CWarning: Player {0} may be griefing! B:D Ratio: {1}", player.ClassyName, (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession).ToString());
                    Player.Console.Message("Warning: Player " + player.ClassyName + " may be griefing! B:D Ratio: " + (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession).ToString());
                    if (Server.Players.RankedAbove(freezerank).Count() == 0 && !player.Info.SecurityTrip)
                    {
                        player.Kick("Kicked for suspected griefing!", LeaveReason.BlockSpamKick);
                        if (!player.Info.SecurityTrip) Player.Console.Message("Player was kicked as a precaution!");
                        Server.Message(Color.Warning + "Player {0}" + Color.Warning + " was kicked for suspected griefing!", player.ClassyName);
                    }
                    else player.Message("Warning: You are Deleting a lot of blocks this session.&nBuild something, or you may get frozen!");
                }
                if (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession == 7d && player.BlocksPlacedThisSession >= 200)
                {
                    Server.Players.Can(Permission.ReadStaffChat).Message("&CWarning: Player {0} may be spamming! B:D Ratio: {1}", player.ClassyName, (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession).ToString());
                    Player.Console.Message("Warning: Player " + player.ClassyName + " may be spamming! B:D Ratio: " + (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession).ToString());
                    if (Server.Players.RankedAbove(freezerank).Count() == 0 && !player.Info.SecurityTrip)
                    {
                        player.Kick("Kicked for suspected block spamming!", LeaveReason.BlockSpamKick);
                        if (!player.Info.SecurityTrip) Player.Console.Message("Player was kicked as a precaution!");
                        Server.Message(Color.Warning + "Player {0}" + Color.Warning + " was kicked for suspected block spamming!", player.ClassyName);
                    }
                    else player.Message("Warning: You are Placing a lot of blocks this session.&nDelete something, or you may get frozen!");
                }
                if (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession <= 0.1d && player.BlocksDeletedThisSession >= 100)
                {
                    Server.Players.Can(Permission.ReadStaffChat).Message("&CWarning: Player {0} may be griefing! B:D Ratio: {1}", player.ClassyName, (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession).ToString());
                    if (!player.Info.SecurityTrip) Server.Players.Can(Permission.ReadStaffChat).Message("&sPlayer was Frozen as a precaution!");
                    Player.Console.Message("Warning: Player " + player.ClassyName + " may be griefing! B:D Ratio: " + (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession).ToString());
                    if (!player.Info.SecurityTrip) Player.Console.Message("Player was Frozen as a precaution!");
                    if (!player.Info.SecurityTrip) player.Info.Freeze(Player.Console, false, true);
                    player.BlocksDeletedThisSession = 0d;
                    player.BlocksPlacedThisSession = 0d;
                    player.BlocksPlacedDeletedMixed = 0d;
                    if (!player.Info.SecurityTrip) player.Message("&SYou have been Frozen for suspected Griefing!&nSee a Moderator for further assistance!");
                    if (!player.Info.SecurityTrip) player.Info.SecurityTrip = true;
                }
                if (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession >= 25d && player.BlocksPlacedThisSession >= 200)
                {
                    Server.Players.Can(Permission.ReadStaffChat).Message("&CWarning: Player {0} may be spamming! B:D Ratio: {1}", player.ClassyName, (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession).ToString());
                    if (!player.Info.SecurityTrip) Server.Players.Can(Permission.ReadStaffChat).Message("&sPlayer was Frozen as a precaution!");
                    Player.Console.Message("Warning: Player " + player.ClassyName + " may be spamming! B:D Ratio: " + (player.BlocksPlacedThisSession / player.BlocksDeletedThisSession).ToString());
                    if (!player.Info.SecurityTrip) Player.Console.Message("Player was Frozen as a precaution!");
                    if (!player.Info.SecurityTrip) player.Info.Freeze(Player.Console, false, true);
                    player.BlocksDeletedThisSession = 0d;
                    player.BlocksPlacedThisSession = 0d;
                    player.BlocksPlacedDeletedMixed = 0d;
                    if (!player.Info.SecurityTrip) player.Message("&SYou have been Frozen for suspected Block Spamming!&nSee a Moderator for further assistance!");
                    if (!player.Info.SecurityTrip) player.Info.SecurityTrip = true;
                }
            }
            else
            {
                if (player.BlocksPlacedThisSession >= 500)
                {
                    Server.Players.Can(Permission.ReadStaffChat).Message("&CWarning: Player {0} may be spamming! {1}B/{2}D", player.ClassyName, player.BlocksPlacedThisSession.ToString(), player.BlocksDeletedThisSession.ToString());
                    if (!player.Info.SecurityTrip) Server.Players.Can(Permission.ReadStaffChat).Message("&sPlayer was Frozen as a precaution!");
                    Player.Console.Message("Warning: Player " + player.ClassyName + " may be spamming! " + player.BlocksPlacedThisSession.ToString() + "B/0D");
                    if (!player.Info.SecurityTrip) Player.Console.Message("Player was Frozen as a precaution!");
                    if (!player.Info.SecurityTrip) player.Info.Freeze(Player.Console, false, true);
                    player.BlocksDeletedThisSession = 0d;
                    player.BlocksPlacedThisSession = 0d;
                    player.BlocksPlacedDeletedMixed = 0d;
                    if (!player.Info.SecurityTrip) player.Message("&SYou have been Frozen for suspected Block Spamming!&nSee a Moderator for further assistance!");
                    if (!player.Info.SecurityTrip) player.Info.SecurityTrip = true;
                }
            }
            if (player.BlocksPlacedDeletedMixed == 100d)
            {
                Server.Players.Can(Permission.ReadStaffChat).Message("&CWarning: Player {0} may be Spamming!&n100 blocks placed is rapid succession.", player.ClassyName);
                Player.Console.Message("Warning: Player " + player.ClassyName + " may be Spamming!&n100 blocks placed is rapid succession.", player.ClassyName);
                if (Server.Players.RankedAbove(freezerank).Count() == 0 && !player.Info.SecurityTrip)
                {
                    player.Kick("Kicked for suspected block spamming!", LeaveReason.BlockSpamKick);
                    if (!player.Info.SecurityTrip) Player.Console.Message("Player was kicked as a precaution!");
                    Server.Message(Color.Warning + "Player {0}" + Color.Warning + " was kicked for suspected block spamming!", player.ClassyName);
                }
                else player.Message("Warning: You are Deleting a lot of blocks this session.&nBuild something, or you may get frozen!");
            }
            if (player.BlocksPlacedDeletedMixed == -75d)
            {
                Server.Players.Can(Permission.ReadStaffChat).Message("&CWarning: Player {0} may be Griefing!&n75 blocks deleted is rapid succession.", player.ClassyName);
                Player.Console.Message("Warning: Player " + player.ClassyName + " may be Griefing!&n75 blocks deleted is rapid succession.", player.ClassyName);
                if (Server.Players.RankedAbove(freezerank).Count() == 0 && !player.Info.SecurityTrip)
                {
                    player.Kick("Kicked for suspected griefing!", LeaveReason.BlockSpamKick);
                    if (!player.Info.SecurityTrip) Player.Console.Message("Player was kicked as a precaution!");
                    Server.Message(Color.Warning + "Player {0}" + Color.Warning + " was kicked for suspected griefing!", player.ClassyName);
                }
                else player.Message("Warning: You are Deleting a lot of blocks this session.&nBuild something, or you may get frozen!");
            }
            if (player.BlocksPlacedDeletedMixed >= 250d)
            {
                Server.Players.Can(Permission.ReadStaffChat).Message("&CWarning: Player {0} may be Spamming!&n250 blocks placed is rapid succession.", player.ClassyName);
                if (!player.Info.SecurityTrip) Server.Players.Can(Permission.ReadStaffChat).Message("&sPlayer was Frozen as a precaution!");
                Player.Console.Message("Warning: Player " + player.ClassyName + " may be Spamming!&n250 blocks placed is rapid succession.", player.ClassyName);
                if (!player.Info.SecurityTrip) Player.Console.Message("Player was Frozen as a precaution!");
                if (!player.Info.SecurityTrip) player.Info.Freeze(Player.Console, false, true);
                player.BlocksDeletedThisSession = 0d;
                player.BlocksPlacedThisSession = 0d;
                player.BlocksPlacedDeletedMixed = 0d;
                if (!player.Info.SecurityTrip) player.Message("&SYou have been Frozen for suspected Block Spamming!&nSee a Moderator for further assistance!");
                if (!player.Info.SecurityTrip) player.Info.SecurityTrip = true;
            }
            if (player.BlocksPlacedDeletedMixed <= -200d)
            {
                Server.Players.Can(Permission.ReadStaffChat).Message("&CWarning: Player {0} may be Griefing!&n200 blocks deleted is rapid succession.", player.ClassyName);
                if (!player.Info.SecurityTrip) Server.Players.Can(Permission.ReadStaffChat).Message("&sPlayer was Frozen as a precaution!");
                Player.Console.Message("Warning: Player " + player.ClassyName + " may be Griefing!&n200 blocks deleted is rapid succession.", player.ClassyName);
                if (!player.Info.SecurityTrip) Player.Console.Message("Player was Frozen as a precaution!");
                if (!player.Info.SecurityTrip) player.Info.Freeze(Player.Console, false, true);
                player.BlocksDeletedThisSession = 0d;
                player.BlocksPlacedThisSession = 0d;
                player.BlocksPlacedDeletedMixed = 0d;
                if (!player.Info.SecurityTrip) player.Message("&SYou have been Frozen for suspected Griefing!&nSee a Moderator for further assistance!");
                if (!player.Info.SecurityTrip) player.SecurityTrip = true;
            }
        }

        public bool Check(Player player, Block thisblock)
        {
            //CAN player build? If so, Adjust Build Powers...
            if (player.World.Name == "Grief" || player.World.Name == "Spleef") return true;
            if (DEFCON.Level == 6) return true;
            if (!(AllowedBuild(player, thisblock))) return false;
            //Check players ratios, take action.
            #region EvenOutOverTime
            if (player.TimeLastBlockChange == null) player.TimeLastBlockChange = DateTime.Now;
            int Delta = (int)(DateTime.Now - player.TimeLastBlockChange).ToSeconds();
            if (Delta > 1)
            {
                if (Delta > (int)Math.Abs(player.BlocksPlacedDeletedMixed)) Delta = (int)Math.Abs(player.BlocksPlacedDeletedMixed);
                if (player.BlocksPlacedDeletedMixed >= Delta)
                {
                    player.BlocksPlacedDeletedMixed -= Delta;
                }
                else if (player.BlocksPlacedDeletedMixed <= -Delta)
                {
                    player.BlocksPlacedDeletedMixed += Delta;
                }
                else if (player.BlocksPlacedDeletedMixed >= 1) player.BlocksPlacedDeletedMixed -= 1;
                else if (player.BlocksPlacedDeletedMixed <= -1) player.BlocksPlacedDeletedMixed += 1;
            }
            player.TimeLastBlockChange = DateTime.Now;
            #endregion

            #region Initialise
            Rank MinRankCanFreezeOthers = RankManager.GetMinRankWithAllPermissions(Permission.Freeze);
            if (player.BlocksDeletedThisSession < 0) player.BlocksDeletedThisSession = 0;
            if (player.BlocksPlacedThisSession < 0) player.BlocksPlacedThisSession = 0;
            #endregion

            #region Warnings
            //Built 0, Deleted Half Of Limit:
            #region 0|*/2
            if (player.BlocksPlacedThisSession <= 0 && player.BlocksDeletedThisSession >= FreezeDelete / 2 && !player.Warned)
            {
                WarnGrief(player);
                return true;
                }
            #endregion
            //Built 0, Deleted The Limit:
            #region 0|*
            if (player.BlocksPlacedThisSession == 0 && player.BlocksDeletedThisSession >= FreezeDelete && player.Warned) {
                PunishGrief(player);
                return false;
            }
            #endregion
            //Built Half Limit, Deleted 0:
            #region */2|0
            if (player.BlocksPlacedThisSession >= FreezeBuild / 2 && player.BlocksDeletedThisSession ==0 && !player.Warned)
            {
                WarnSpam(player);
                return true;
            }
            #endregion
            //Built Limit, Deleted 0:
            #region *|0
            if (player.BlocksPlacedThisSession >= FreezeBuild && player.BlocksDeletedThisSession == 0 && player.Warned)
            {
                PunishSpam(player);
                return false;
            }
            #endregion

            //Mixed Above Threshold/2
            #region ->*/2
            if (player.BlocksPlacedDeletedMixed >= PlacedDeleteMixedThreshold / 2 && !player.Warned) {
                WarnHighThreshold(player);
                return true;
            }
            #endregion
            //Mixed Above Threshold
            #region ->*
            if (player.BlocksPlacedDeletedMixed >= PlacedDeleteMixedThreshold && player.Warned)
            {
                PunishHighThreshold(player);
                return false;
            }
            #endregion
            //Mixed Below Threshold/2
            #region */2<-
            if (-player.BlocksPlacedDeletedMixed >= PlacedDeleteMixedThreshold / 2 && !player.Warned)
            {
                WarnLowThreshold(player);
                return true;
            }
            #endregion
            //Mixed Below Threshold
            #region *<-
            if (-player.BlocksPlacedDeletedMixed >= PlacedDeleteMixedThreshold && player.Warned)
            {
                PunishLowThreshold(player);
                return false;
            }
            #endregion

            //BDR Based Calculations.
            #region BDR
            if (player.BlocksDeletedThisSession > FreezeDelete)
            {
                double BDR = player.BlocksPlacedThisSession / player.BlocksDeletedThisSession;
                if (BDR >= FreezeRatioUpperLimit / 2 && !player.Warned)
                {
                    WarnRatioHigh(player);
                    return true;
                }
                if (BDR >= FreezeRatioUpperLimit && player.Warned)
                {
                    PunishRatioHigh(player);
                    return false;
                }
                if (BDR <= FreezeRatioLowerLimit * 2 && !player.Warned)
                {
                    WarnRatioLow(player);
                    return true;
                }
                if (BDR <= FreezeRatioLowerLimit && player.Warned)
                {
                    PunishRatioLow(player);
                    return false;
                }
            }
            #endregion

            #endregion



            return true;
        }
    }
}
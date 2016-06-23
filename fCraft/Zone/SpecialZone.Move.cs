//  ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using JetBrains.Annotations;

namespace fCraft {
    public static partial class SpecialZone {

        public const string Deny = "deny_";
        public const string Text = "text_";
        public const string Respawn = "respawn_";
        public const string Checkpoint = "checkpoint_";
        public const string Death = "death_";
        
        internal static bool CheckMoveZone(Player p, Zone zone, ref bool deniedZone, Position newPos) {
            if (zone.Name.StartsWith(Deny)) {
                return HandleDeny(p, zone, ref deniedZone, newPos);
            } else if (zone.Name.StartsWith(Text)) {
                return HandleText(p, zone, ref deniedZone, newPos);
            } else if (zone.Name.StartsWith(Respawn)) {
                return HandleRespawn(p, zone, ref deniedZone, newPos);
            } else if (zone.Name.StartsWith(Checkpoint)) {
                return HandleCheckpoint(p, zone, ref deniedZone, newPos);
            } else if (zone.Name.StartsWith(Death)) {
                return HandleDeath(p, zone, ref deniedZone, newPos);
            }
            return false;
        }
        
        static bool HandleDeny(Player p, Zone zone, ref bool deniedZone, Position newPos) {
            if (!zone.Controller.Check(p.Info) || zone.Controller.MinRank >= p.Info.Rank) {
                if (!zone.Bounds.Contains(p.lastValidPosition.X / 32, p.lastValidPosition.Y / 32, (p.lastValidPosition.Z - 32) / 32)) {
                    p.lastValidPosition = p.Position;
                }
                if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, (newPos.Z - 32) / 32)) {
                    deniedZone = true;
                    if (zone.Controller.MinRank.NextRankUp != null) {
                        p.sendZoneMessage(zone, "&WYou must be at least rank " + zone.Controller.MinRank.NextRankUp.Name + "&w to enter this area.");
                    } else {
                        p.sendZoneMessage(zone, "&WNo rank may enter this area.");
                    }
                    p.SendNow(Packet.MakeSelfTeleport(p.lastValidPosition));
                    return true;
                }
            }
            return false;
        }
        
        static bool HandleText(Player p, Zone zone, ref bool deniedZone, Position newPos) {
            if (!zone.Controller.Check(p.Info) || zone.Controller.MinRank >= p.Info.Rank) {
                if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, (newPos.Z - 32) / 32)) {
                    p.sendZoneMessage(zone, "&WThis zone is marked as a text area, but no text is added to the message!");
                    return true;
                }
            }
            return false;
        }
        
        static bool HandleRespawn(Player p, Zone zone, ref bool deniedZone, Position newPos) {
            if (!zone.Controller.Check(p.Info) || zone.Controller.MinRank >= p.Info.Rank) {
                if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, (newPos.Z - 32) / 32)) {
                    p.sendZoneMessage(zone, "&WRespawned!");
                    p.TeleportTo(p.WorldMap.getSpawnIfRandom());
                    return true;
                }
            }
            return false;
        }

        static bool HandleCheckpoint(Player p, Zone zone, ref bool deniedZone, Position newPos) {
            Position centre = new Position(zone.Bounds.XCentre * 32 + 16, zone.Bounds.YCentre * 32 + 16, zone.Bounds.ZCentre * 32 + 64);
            if (p.Info.CheckPoint == centre) return false;
            if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, (newPos.Z - 32) / 32)) {
                p.sendZoneMessage(zone, "&aCheckPoint &sreached! This is now your respawn point.");
                p.Info.CheckPoint = centre;
                return true;
            }
            return false;
        }
        
        static bool HandleDeath(Player p, Zone zone, ref bool deniedZone, Position newPos) {
            if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, (newPos.Z - 32) / 32)) {
                p.sendZoneMessage(zone, "&WYou Died!");
                p.TeleportTo(p.Info.CheckPoint != new Position(-1, -1, -1) ? p.Info.CheckPoint : p.WorldMap.Spawn);
                return true;
            }
            return false;
        }
    }
}
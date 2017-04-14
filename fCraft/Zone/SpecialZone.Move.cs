//  ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using JetBrains.Annotations;

namespace fCraft {
    public static partial class SpecialZone {
        
        internal static bool CheckMoveZone(Player p, Zone zone, ref bool deniedZone, Position newPos) {
            if (zone.Name.CaselessStarts(Deny)) {
                return HandleDeny(p, zone, ref deniedZone, newPos);
            } else if (zone.Name.CaselessStarts(Text)) {
                return HandleText(p, zone, ref deniedZone, newPos);
            } else if (zone.Name.CaselessStarts(Respawn)) {
                return HandleRespawn(p, zone, ref deniedZone, newPos);
            } else if (zone.Name.CaselessStarts(Checkpoint)) {
                return HandleCheckpoint(p, zone, ref deniedZone, newPos);
            } else if (zone.Name.CaselessStarts(Death)) {
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
                        SendZoneMessage(p, "&WYou must be at least rank " + zone.Controller.MinRank.NextRankUp.Name + "&W to enter this area.");
                    } else {
                        SendZoneMessage(p, "&WNo rank may enter this area.");
                    }
                    p.SendNow(p.TeleportPacket(Packet.SelfId, p.lastValidPosition));
                    return true;
                }
            }
            return false;
        }
        
        static bool HandleText(Player p, Zone zone, ref bool deniedZone, Position newPos) {
            if (!zone.Controller.Check(p.Info) || zone.Controller.MinRank >= p.Info.Rank) {
                if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, (newPos.Z - 32) / 32)) {
                    string message = GetSignMessage(p, zone);
                    if (message == null)
                        message = "&WThis zone is marked as a text area, but no text is added to the message!";
                    
                    SendZoneMessage(p, message);
                    return true;
                }
            }
            return false;
        }
        
        static bool HandleRespawn(Player p, Zone zone, ref bool deniedZone, Position newPos) {
            if (!zone.Controller.Check(p.Info) || zone.Controller.MinRank >= p.Info.Rank) {
                if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, (newPos.Z - 32) / 32)) {
                    SendZoneMessage(p, "&WRespawned!");
                    p.TeleportTo(p.WorldMap.getSpawnIfRandom());
                    return true;
                }
            }
            return false;
        }

        static bool HandleCheckpoint(Player p, Zone zone, ref bool deniedZone, Position newPos) {
            Position centre = new Position(zone.Bounds.XCentre * 32 + 16, zone.Bounds.YCentre * 32 + 16, zone.Bounds.ZCentre * 32 + 64);
            if (p.CheckPoint == centre) return false;
            if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, (newPos.Z - 32) / 32)) {
                SendZoneMessage(p, "&aCheckPoint &Sreached! This is now your respawn point.");
                p.CheckPoint = centre;
                return true;
            }
            return false;
        }
        
        static bool HandleDeath(Player p, Zone zone, ref bool deniedZone, Position newPos) {
            if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, (newPos.Z - 32) / 32)) {
                SendZoneMessage(p, "&WYou Died!");
                p.TeleportTo(p.CheckPoint != new Position(-1, -1, -1) ? p.CheckPoint : p.WorldMap.Spawn);
                return true;
            }
            return false;
        }
    }
}
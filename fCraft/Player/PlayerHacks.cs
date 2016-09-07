// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft {
    public static class PlayerHacks {
        
        public static Packet MakePacket(Player p, string motd) {
            bool canFly = true, canNoClip = true, canSpeed = true, canRespawn = true;
            bool useMotd = GetHacksFromMotd(p, motd, ref canFly, 
                                            ref canNoClip, ref canSpeed, ref canRespawn);
            
            if (useMotd)
                return Packet.HackControl(canFly, canNoClip, canSpeed, canRespawn, canNoClip, -1);
            
            return Packet.HackControl(p.Info.AllowFlying, p.Info.AllowNoClip, p.Info.AllowSpeedhack,
                                      p.Info.AllowRespawn, p.Info.AllowThirdPerson, p.Info.JumpHeight);
        }
        
        static bool GetHacksFromMotd(Player p, string motd, ref bool canFly, 
		                             ref bool canNoClip, ref bool canSpeed, ref bool canRespawn) {
            if (String.IsNullOrEmpty(motd)) return false;
            bool useMotd = false;
            
            foreach (string part in motd.ToLower().Split()) {
                switch (part) {
                    case "-fly":
                    case "+fly":
                        canFly = part == "+fly";
                        useMotd = true;
                        break;
                    case "-noclip":
                    case "+noclip":
                        canNoClip = part == "+noclip";
                        useMotd = true;
                        break;
                    case "-speed":
                    case "+speed":
                        canSpeed = part == "+speed";
                        useMotd = true;
                        break;
                    case "-respawn":
                    case "+respawn":
                        canRespawn = part == "+respawn";
                        useMotd = true;
                        break;
                    case "-hax":
                    case "+hax":
                        canFly = part == "+hax";
                        canNoClip = part == "+hax";
                        canSpeed = part == "+hax";
                        canRespawn = part == "+hax";
                        useMotd = true;
                        break;
                    case "+ophax":
                        canFly = p.IsStaff;
                        canNoClip = p.IsStaff;
                        canSpeed = p.IsStaff;
                        canRespawn = p.IsStaff;
                        useMotd = true;
                        break;
                    default:
                        break;
                }
            }
            return useMotd;
        }
    }
}
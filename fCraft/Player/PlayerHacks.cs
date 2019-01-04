﻿// ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft {
    public static class PlayerHacks {
        
        public static Packet MakePacket(Player p, string motd) {
            bool canFly = true, canNoClip = true, canSpeed = true, canRespawn = true, canThird = true;
            short jumpHeight = -1;
            bool useMotd = GetHacksFromMotd(p, motd, ref canFly, ref canNoClip,
                                            ref canSpeed, ref canRespawn, ref canThird, ref jumpHeight);
            
            if (useMotd)
                return Packet.HackControl(canFly, canNoClip, canSpeed, canRespawn, canThird, jumpHeight);
            
            return Packet.HackControl(p.Info.AllowFlying, p.Info.AllowNoClip, p.Info.AllowSpeedhack,
                                      p.Info.AllowRespawn, p.Info.AllowThirdPerson, p.Info.JumpHeight);
        }
        
        static bool GetHacksFromMotd(Player p, string motd, ref bool fly, ref bool noclip,
                                     ref bool speed, ref bool respawn, ref bool third, ref short jumpHeight) {
            if (String.IsNullOrEmpty(motd)) return false;
            bool useMotd = false;
            
            foreach (string part in motd.ToLower().Split()) {
                if (part == "-fly" || part == "+fly") {
                    fly     = part == "+fly";
                } else if (part == "-noclip" || part == "+noclip") {
                    noclip  = part == "+noclip";
                } else if (part == "-speed" || part == "+speed") {
                    speed   = part == "+speed";
                } else if (part == "-respawn" || part == "+respawn") {
                    respawn = part == "+respawn";
                } else if (part == "-thirdperson" || part == "+thirdperson") {
                    third   = part == "+thirdperson";
                }  else if (part == "-hax" || part == "+hax") {
                    fly     = part == "+hax";
                    noclip  = part == "+hax";
                    speed   = part == "+hax";
                    respawn = part == "+hax";
                    third   = part == "+hax";
                } else if (part == "-ophax" || part == "+ophax") {
                    if (!p.IsStaff) { useMotd = true; continue; }
                    fly     = part == "+ophax";
                    noclip  = part == "+ophax";
                    speed   = part == "+ophax";
                    respawn = part == "+ophax";
                    third   = part == "+ophax";
                } else if (part.StartsWith("jumpheight=")) {
                    string heightPart = part.Substring(part.IndexOf('=') + 1);
                    float value;
                    if (float.TryParse(heightPart, out value))
                        jumpHeight = (short)(value * 32);
                } else {
                    continue;
                }
                useMotd = true;
            }
            return useMotd;
        }
    }
}
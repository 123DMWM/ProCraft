// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using System.Collections.Generic;

namespace fCraft {

    /// <summary> Helper methods for spawning and despawning entities. </summary>
    public static class Entities {
        
        public static string ModelFor(Player dst, Player entity) {
            string model = entity.IsAFK ? entity.AFKModel : entity.Info.Model;           
            if (dst.Info.Rank.CanSee(entity.Info.Rank) && (model.CaselessEquals("air") || model.CaselessEquals("0"))) {
                model = "humanoid";
            }
            return model;
        }

        public static void Spawn(Player dst, bool sendNow, Player entity, sbyte id) {
            string name = entity.Info.Rank.Color + entity.Name;
            string skin = entity.Info.Skin, model = ModelFor(dst, entity);        
            Spawn(dst, sendNow, id, name, skin, model, 
                  entity.Position, entity.RotX, entity.RotZ);
        }
        
        public static void Spawn(Player dst, bool sendNow, Entity entity) {
            Spawn(dst, sendNow, entity.ID, entity.Name, entity.Skin,
                  entity.Model, entity.GetPos(), 0, 0);
        }
        
        public static void Spawn(Player dst, bool sendNow, sbyte id, string name,
                                 string skin, string model, Position pos, int rotX, int rotZ) {
            Send(dst, sendNow, SpawnPacket(dst, id, name, skin, pos));
            if (dst.Supports(CpeExt.ChangeModel)) {
                Send(dst, sendNow, Packet.MakeChangeModel(id, model, dst.HasCP437));
            }
            
            if (dst.Supports(CpeExt.EntityProperty)) {
                Send(dst, sendNow, Packet.MakeEntityProperty(id, EntityProp.RotationX, rotX));
                Send(dst, sendNow, Packet.MakeEntityProperty(id, EntityProp.RotationZ, rotZ));
            }
        }
        

        static Packet SpawnPacket(Player dst, sbyte id, string name, string skin, Position pos) {
            name = Color.SubstituteSpecialColors(name, dst.FallbackColors);            
            if (dst.Supports(CpeExt.ExtPlayerList2)) {
                return Packet.MakeExtAddEntity2(id, name, skin, pos, dst.HasCP437, dst.supportsExtPositions);
            } else {
                return Packet.MakeAddEntity(id, name, pos, dst.HasCP437, dst.supportsExtPositions);
            }
        }
        
        static void Send(Player dst, bool sendNow, Packet p) {
            if (sendNow) {
                dst.SendNow(p);
            } else {
                dst.Send(p);
            }
        }
    }
}
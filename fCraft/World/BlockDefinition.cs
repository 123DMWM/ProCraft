﻿// ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using ServiceStack.Text;

namespace fCraft {

    public sealed class BlockDefinition {
        
        public byte BlockID { get; set; }
        public string Name { get; set; }
        public byte CollideType { get; set; }
        public float Speed { get; set; }
        public byte TopTex  { get; set; }
        public byte SideTex { get; set; } 
        public byte BottomTex { get; set; }
        public bool BlocksLight { get; set; }
        public byte WalkSound { get; set; }
        public bool FullBright { get; set; }
        public byte Shape { get; set; }
        public byte BlockDraw { get; set; }
        public byte FogDensity { get; set; }
        public byte FogR { get; set; }
        public byte FogG { get; set; } 
        public byte FogB { get; set; }    
        public byte FallBack { get; set; }
        
        public BlockDefinition() { }
        
        public BlockDefinition(byte id, string name, byte collideType, float speed,
                               byte topTex, byte sideTex, byte bottomTex,
                               bool blocksLight, byte walkSound, bool fullBright,
                               byte shape, byte blockDraw, byte fogDensity,
                               byte fogR, byte fogG, byte fogB, byte fallback) {
            
            BlockID = id; Name = name; CollideType = collideType;
            Speed = speed; TopTex = topTex; SideTex = sideTex;
            BottomTex = bottomTex; BlocksLight = blocksLight;
            WalkSound = walkSound; FullBright = fullBright;
            Shape = shape; BlockDraw = blockDraw; FogDensity = fogDensity;
            FogR = fogR; FogG = fogG; FogB = fogB; FallBack = fallback;
        }
        
        public static BlockDefinition[] GlobalDefinitions = new BlockDefinition[256];
        
        public static void DefineGlobalBlock(BlockDefinition def) {
            string name = def.Name.ToLower().Replace(" ", "");         
            Map.BlockNames[name] = (Block)def.BlockID;
            Map.BlockNames[def.BlockID.ToString()] = (Block)def.BlockID;
            
            GlobalDefinitions[def.BlockID] = def;
            Map.FallbackBlocks[def.BlockID] = (Block)def.FallBack;
        }
        
        public static void RemoveGlobalBlock(BlockDefinition def) {
            string name = def.Name.ToLower().Replace(" ", "");         
            Map.BlockNames.Remove(name);
            Map.BlockNames.Remove(def.BlockID.ToString());
            
            GlobalDefinitions[def.BlockID] = null;
            Map.FallbackBlocks[def.BlockID] = Block.Air;
        }
        
        public static void SendGlobalDefinitions(Player p) {
            for (int i = 0; i < GlobalDefinitions.Length; i++) {
                BlockDefinition def = GlobalDefinitions[i];
                if (def == null) continue;
                
                p.Send(def.MakeDefinePacket());
                p.Send(Packet.MakeSetBlockPermission(
                    (Block)def.BlockID, true, true));
            }
        }
        
         public static void SendGlobalAdd(Player p, BlockDefinition def) {
            p.Send(def.MakeDefinePacket());
            p.Send(Packet.MakeSetBlockPermission((Block)def.BlockID, true, true));
        }
        
        public static void SendGlobalRemove(Player p, BlockDefinition def) {
            p.Send(Packet.MakeRemoveBlockDefinition(def.BlockID));
            p.Send(Packet.MakeSetBlockPermission((Block)def.BlockID, false, false));
        }
        
        Packet MakeDefinePacket() {
            // speed = 2^((raw - 128) / 64);
            // therefore raw = 64log2(speed) + 128
            byte rawSpeed = (byte)(64 * Math.Log(Speed, 2) + 128);
            return Packet.MakeDefineBlock(
                BlockID, Name, CollideType, rawSpeed, TopTex, SideTex, BottomTex,
                BlocksLight, WalkSound, FullBright, Shape, BlockDraw,
                FogDensity, FogR, FogG, FogB);
        }
        
        
        const string path = "GlobalBlocks.txt";
        
        public static void SaveGlobalDefinitions() {
            try {
                SaveGlobal();
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "BlockDefinitions.SaveGlobal: " + ex);
            }
        }
        
        static void SaveGlobal() {
            using (Stream s = File.Create(path)) {
                JsonSerializer.SerializeToStream(GlobalDefinitions, s);
            }
        }
        
        public static void LoadGlobalDefinitions() {
            if (!File.Exists(path)) return;
            
            try {
                LoadGlobal();
                for (int i = 0; i < GlobalDefinitions.Length; i++) {
                    if (GlobalDefinitions[i] == null) 
                        continue;
                    // fixup for servicestack not writing out null entries
                    if (GlobalDefinitions[i].Name == null) {
                        GlobalDefinitions[i] = null; continue;
                    }
                    DefineGlobalBlock(GlobalDefinitions[i]);
                }
            } catch (Exception ex) {
                GlobalDefinitions = new BlockDefinition[256];
                Logger.Log(LogType.Error, "BlockDefinitions.LoadGlobal: " + ex);
            }
        }
        
        static void LoadGlobal() {
            using (Stream s = File.OpenRead("globalblocks.txt")) {
                GlobalDefinitions = (BlockDefinition[])
                    JsonSerializer.DeserializeFromStream(typeof(BlockDefinition[]), s);
            }
        }
    }
}
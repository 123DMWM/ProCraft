// ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using ServiceStack.Text;

namespace fCraft {

    public sealed class BlockDefinition {
        
        public byte BlockID;
        public string Name;
        public byte CollideType;
        public float Speed;
        public byte TopTex, SideTex, BottomTex;
        public bool BlocksLight;
        public byte WalkSound;
        public bool FullBright;
        public byte Shape;
        public byte BlockDraw;
        public byte FogDensity, FogR, FogG, FogB;
        
        public byte FallBack; // for non-supporting clients
        
        public Packet MakeDefinePacket() {
            // speed = 2^((raw - 128) / 64);
            // therefore raw = 64log2(speed) + 128
            byte rawSpeed = (byte)(64 * Math.Log(Speed, 2) + 128);
            return Packet.MakeDefineBlock(
                BlockID, Name, CollideType, rawSpeed, TopTex, SideTex, BottomTex,
                BlocksLight, WalkSound, FullBright, Shape, BlockDraw,
                FogDensity, FogR, FogG, FogB);
        }
        
        public static BlockDefinition[] GlobalDefinitions = new BlockDefinition[256];
        
        public static void DefineGlobalBlock(BlockDefinition def) {
            GlobalDefinitions[def.BlockID] = def;
            Map.BlockNames[def.Name] = (Block)def.BlockID;
            Map.FallbackBlocks[def.BlockID] = (Block)def.FallBack;
        }
        
        public static void RemoveGlobalBlock(BlockDefinition def) {
            GlobalDefinitions[def.BlockID] = null;
            Map.BlockNames.Remove(def.Name);
            Map.FallbackBlocks[def.BlockID] = Block.Air;
        }
        
        public void Serialize(Stream stream) {
            JsonSerializer.SerializeToStream(this, stream);
        }
        
        public static BlockDefinition Deserialize(Stream stream) {
            return (BlockDefinition)JsonSerializer.DeserializeFromStream(typeof(BlockDefinition), stream);
        }
        
        public static BlockDefinition Deserialize(string json) {
            return (BlockDefinition)JsonSerializer.DeserializeFromString(json, typeof(BlockDefinition));
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
    }
}

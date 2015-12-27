// ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using ServiceStack.Text;
using System.Diagnostics;

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
        
        // BlockDefinitionsExt fields
        public byte MinX { get; set; }
        public byte MinY { get; set; }
        public byte MinZ { get; set; }
        public byte MaxX { get; set; }
        public byte MaxY { get; set; }
        public byte MaxZ { get; set; }
        
        public static BlockDefinition[] GlobalDefinitions = new BlockDefinition[256];
        
        public static void DefineGlobalBlock(BlockDefinition def) {
            // fixup for legacy files
            if (def.MinX == 0 && def.MaxX == 0 ) 
            	def.MaxX = 16;
            if (def.MinY == 0 && def.MaxY == 0 ) 
            	def.MaxY = 16;
            if (def.MinZ == 0 && def.MaxZ == 0 )
            	def.MaxZ = def.Shape == 0 ? (byte)16 : def.Shape;
            
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
                
                if (p.Supports(CpeExtension.BlockDefinitionsExt) && def.Shape != 0)
                    p.SendNow(Packet.MakeDefineBlockExt(def));
                else
                    p.SendNow(Packet.MakeDefineBlock(def));
                p.SendNow(Packet.MakeSetBlockPermission(
                    (Block)def.BlockID, true, true));
            }
        }
        
         public static void SendGlobalAdd(Player p, BlockDefinition def) {
            if (p.Supports(CpeExtension.BlockDefinitionsExt) && def.Shape != 0)
                p.Send(Packet.MakeDefineBlockExt(def));
            else
                p.Send(Packet.MakeDefineBlock(def));
            p.Send(Packet.MakeSetBlockPermission((Block)def.BlockID, true, true));
        }
        
        public static void SendGlobalRemove(Player p, BlockDefinition def) {
            p.Send(Packet.MakeRemoveBlockDefinition(def.BlockID));
            p.Send(Packet.MakeSetBlockPermission((Block)def.BlockID, false, false));
        }
        
        public static void SaveGlobalDefinitions() {
            try {
                SaveGlobal();
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "BlockDefinitions.SaveGlobal: " + ex);
            }
        }
        
        static void SaveGlobal() {
            Stopwatch sw = Stopwatch.StartNew();
            using (Stream s = File.Create(Paths.GlobalDefinitionsFileName)) {
                JsonSerializer.SerializeToStream(GlobalDefinitions, s);
            }
            Logger.Log(LogType.Debug, "BlockDefinitions.SaveGlobal: Saved Block definitions in {0}ms", sw.ElapsedMilliseconds);
        }
        
        public static void LoadGlobalDefinitions() {
            if (!File.Exists(Paths.GlobalDefinitionsFileName)) return;
            
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
            using (Stream s = File.OpenRead(Paths.GlobalDefinitionsFileName)) {
                GlobalDefinitions = (BlockDefinition[])
                    JsonSerializer.DeserializeFromStream(typeof(BlockDefinition[]), s);
            }
        }
    }
}
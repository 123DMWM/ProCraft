// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
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
        // BlockDefinitionsExt v2 fields
        public bool Version2 { get; set; }
        public byte LeftTex { get; set; } 
        public byte RightTex { get; set; } 
        public byte FrontTex { get; set; } 
        public byte BackTex { get; set; }         
        
        public BlockDefinition Copy() {
            BlockDefinition def = new BlockDefinition();
            def.BlockID = BlockID; def.Name = Name;
            def.CollideType = CollideType; def.Speed = Speed;
            def.TopTex = TopTex; def.SideTex = SideTex;
            def.BottomTex = BottomTex; def.BlocksLight = BlocksLight;
            def.WalkSound = WalkSound; def.FullBright = FullBright;
            def.Shape = Shape; def.BlockDraw = BlockDraw;
            def.FogDensity = FogDensity; def.FogR = FogR;
            def.FogG = FogG; def.FogB = FogB;
            def.FallBack = FallBack;
            def.MinX = MinX; def.MinY = MinY; def.MinZ = MinZ;
            def.MaxX = MaxX; def.MaxY = MaxY; def.MaxZ = MaxZ;
            def.Version2 = Version2;
            def.LeftTex = LeftTex; def.RightTex = RightTex;
            def.FrontTex = FrontTex; def.BackTex = BackTex;
            return def;
        }
        
        public static BlockDefinition[] GlobalDefinitions = new BlockDefinition[256];
        
        public static void DefineGlobalBlock(BlockDefinition def) {
            string name = def.Name.ToLower().Replace(" ", "");         
            Map.BlockNames[name] = (Block)def.BlockID;
            Map.BlockNames[def.BlockID.ToString()] = (Block)def.BlockID;            
            GlobalDefinitions[def.BlockID] = def;
        }
        
        public static void RemoveGlobalBlock(BlockDefinition def) {
            string name = def.Name.ToLower().Replace(" ", "");         
            Map.BlockNames.Remove(name);
            Map.BlockNames.Remove(def.BlockID.ToString());           
            GlobalDefinitions[def.BlockID] = null;
        }
        
        #region Packets
        
        public static void SendGlobalDefinitions(Player p) {
            for (int i = (int)Map.MaxCustomBlockType + 1; i < GlobalDefinitions.Length; i++) {
                BlockDefinition def = GlobalDefinitions[i];
                if (def == null) continue;
                
                if (p.Supports(CpeExt.BlockDefinitionsExt2) && def.Shape != 0)
                    p.SendNow(Packet.MakeDefineBlockExt(def, true));
                else if (p.Supports(CpeExt.BlockDefinitionsExt) && def.Shape != 0)
                    p.SendNow(Packet.MakeDefineBlockExt(def, false));
                else
                    p.SendNow(Packet.MakeDefineBlock(def));
                p.SendNow(Packet.MakeSetBlockPermission(
                    (Block)def.BlockID, true, true));
            }
        }
        
         public static void SendGlobalAdd(Player p, BlockDefinition def) {
            if (p.Supports(CpeExt.BlockDefinitionsExt2) && def.Shape != 0)
                p.Send(Packet.MakeDefineBlockExt(def, true));
            else if (p.Supports(CpeExt.BlockDefinitionsExt) && def.Shape != 0)
                p.Send(Packet.MakeDefineBlockExt(def, false));
            else
                p.Send(Packet.MakeDefineBlock(def));
            p.Send(Packet.MakeSetBlockPermission((Block)def.BlockID, true, true));
        }
        
        public static void SendGlobalRemove(Player p, BlockDefinition def) {
            p.Send(Packet.MakeRemoveBlockDefinition(def.BlockID));
            p.Send(Packet.MakeSetBlockPermission((Block)def.BlockID, false, false));
        }
        #endregion
        
        
        #region I/O
        
        public static void SaveGlobalDefinitions() {
            try {
                Save(GlobalDefinitions, Paths.GlobalDefinitionsFileName);
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "BlockDefinitions.SaveGlobal: " + ex);
            }
        }
        
        static void Save(BlockDefinition[] defs, string path) {
            Stopwatch sw = Stopwatch.StartNew();
            using (Stream s = File.Create(path))
                JsonSerializer.SerializeToStream(GlobalDefinitions, s);
            Logger.Log(LogType.Debug, "BlockDefinitions.SaveGlobal: Saved Block definitions in {0} ms", sw.ElapsedMilliseconds);
        }
        
        public static void LoadGlobalDefinitions() {
            if (!File.Exists(Paths.GlobalDefinitionsFileName)) return;
            
            try {
                int count;
                GlobalDefinitions = Load(Paths.GlobalDefinitionsFileName, out count);              
                Logger.Log(LogType.SystemActivity, "BlockDefinitions.LoadGlobal: Loaded " + count + " blocks");
            } catch (Exception ex) {
                GlobalDefinitions = new BlockDefinition[256];
                Logger.Log(LogType.Error, "BlockDefinitions.LoadGlobal: " + ex);
            }
        }
        
        static BlockDefinition[] Load(string path, out int count) {
            BlockDefinition[] defs;
            using (Stream s = File.OpenRead(path))
                defs = (BlockDefinition[])JsonSerializer.DeserializeFromStream(typeof(BlockDefinition[]), s);
            
            count = 0;
            for (int i = (int)Map.MaxCustomBlockType + 1; i < defs.Length; i++) {
                if (defs[i] == null) continue;
                // fixup for servicestack not writing out null entries
                if (defs[i].Name == null) { defs[i] = null; continue; }
                
                FixupLegacy(defs[i]);
                DefineGlobalBlock(defs[i]);
                count++;
            }
            return defs;
        }
        
        static void FixupLegacy(BlockDefinition def) {           
            if (def.MinX == 0 && def.MaxX == 0) 
                def.MaxX = 16;
            if (def.MinY == 0 && def.MaxY == 0) 
                def.MaxY = 16;
            if (def.MinZ == 0 && def.MaxZ == 0)
                def.MaxZ = def.Shape == 0 ? (byte)16 : def.Shape;
            if (!def.Version2) {
                def.Version2 = true;
                def.LeftTex = def.SideTex; def.RightTex = def.SideTex;
                def.FrontTex = def.SideTex; def.BackTex = def.SideTex;
            }
        }
        #endregion
    }
}
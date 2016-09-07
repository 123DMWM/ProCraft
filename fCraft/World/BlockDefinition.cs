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
        
        /// <summary> Name used in commands. (e.g. "Mossy Slabs" becomes "mossyslabs")"</summary>
        public string BlockName;
        
        public BlockDefinition Copy() {
            BlockDefinition def = new BlockDefinition();
            def.BlockID = BlockID; def.Name = Name; def.BlockName = BlockName;
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
        
        public static BlockDefinition[] GlobalDefs = new BlockDefinition[256];
        
        public static void Add(BlockDefinition def, BlockDefinition[] defs, World world) {
            byte id = def.BlockID;
            bool global = defs == GlobalDefs;
            if (global) {
                World[] worlds = WorldManager.Worlds;
                foreach (World w in worlds) {
                    if (w.BlockDefs[id] == null) w.BlockDefs[id] = def;
                }
                
                string name = def.Name.ToLower().Replace(" ", "");
                Map.BlockNames[name] = (Block)def.BlockID;
                Map.BlockNames[def.BlockID.ToString()] = (Block)def.BlockID;
            }
            defs[id] = def;
            
            Player[] players = Server.Players;
            foreach (Player pl in players) {
                if (!global && pl.World != world) continue;
                if (!pl.Supports(CpeExt.BlockDefinitions)) continue;
                if (global && pl.World.BlockDefs[id] != GlobalDefs[id]) continue;
                SendAdd(pl, def);
            }
            Save(global, world);
        }
        
        public static void Remove(BlockDefinition def, BlockDefinition[] defs, World world) {
            byte id = def.BlockID;
            bool global = defs == GlobalDefs;
            if (global) {
                World[] worlds = WorldManager.Worlds;
                foreach (World w in worlds) {
                    if (w.BlockDefs[id] == GlobalDefs[id]) w.BlockDefs[id] = null;
                }
                
                string name = def.Name.ToLower().Replace(" ", "");
                Map.BlockNames.Remove(name);
                Map.BlockNames.Remove(def.BlockID.ToString());
            }
            defs[id] = null;
            
            Player[] players = Server.Players;
            foreach (Player pl in players) {
                if (!global && pl.World != world) continue;
                if (global && pl.World.BlockDefs[id] != null) continue;
                SendRemove(pl, def);
            }
            Save(global, world);
        }
        
        #region Packets
        
        internal static void SendNowRemoveOldBlocks(Player p, World oldWorld) {
            BlockDefinition[] defs = oldWorld.BlockDefs;
            for (int i = (int)Map.MaxCustomBlockType + 1; i < defs.Length; i++) {
                BlockDefinition def = defs[i];
                if (def == null || def == GlobalDefs[i]) continue;
                p.SendNow(Packet.MakeRemoveBlockDefinition((byte)i));
            }
        }
        
        internal static void SendNowBlocks(Player p) {
            BlockDefinition[] defs = p.World.BlockDefs;
            for (int i = (int)Map.MaxCustomBlockType + 1; i < defs.Length; i++) {
                BlockDefinition def = defs[i];
                if (def == null) continue;              
                p.SendNow(GetPacket(p, def));
            }
        }
        
        public static void SendAdd(Player p, BlockDefinition def) {
            p.Send(GetPacket(p, def));
            if (!p.Supports(CpeExt.BlockPermissions)) return;
            
            p.Send(Packet.MakeSetBlockPermission(
                (Block)def.BlockID, p.World.Buildable, p.World.Deletable));
        }
        
        public static void SendRemove(Player p, BlockDefinition def) {
            p.Send(Packet.MakeRemoveBlockDefinition(def.BlockID));
            if (!p.Supports(CpeExt.BlockPermissions)) return;
            
            p.Send(Packet.MakeSetBlockPermission(
                (Block)def.BlockID, false, false));
        }
        
        static Packet GetPacket(Player p, BlockDefinition def) {
            if (p.Supports(CpeExt.BlockDefinitionsExt2) && def.Shape != 0) {
                return Packet.MakeDefineBlockExt(def, true);
            } else if (p.Supports(CpeExt.BlockDefinitionsExt) && def.Shape != 0) {
                return Packet.MakeDefineBlockExt(def, false);
            } else {
                return Packet.MakeDefineBlock(def);
            }
        }
        #endregion
        
        
        #region I/O
        
        public static void SaveGlobalDefinitions() {
            Stopwatch sw = Stopwatch.StartNew();
            Save(true, null);
            Logger.Log(LogType.Debug, "BlockDefinitions.SaveGlobal: Saved Block definitions in {0} ms", sw.ElapsedMilliseconds);
        }
        
        public static void Save(bool global, World world) {
            BlockDefinition[] defs = global ? GlobalDefs : world.BlockDefs;
            // We don't want to save global blocks in the world's custom blocks list
            if (!global) {
                BlockDefinition[] realDefs = new BlockDefinition[256];
                for (int i = 0; i < 256; i++)
                    realDefs[i] = defs[i] == GlobalDefs[i] ? null : defs[i];
                defs = realDefs;
            }

            string path = Paths.BlockDefsDirectory;
            path = Path.Combine(path, global ? Paths.GlobalDefsFile : world.Name + ".txt");
            try {
                using (Stream s = File.Create(path))
                    JsonSerializer.SerializeToStream(defs, s);
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "BlockDefinitions.Save: " + ex);
            }
        }
        
        public static void LoadGlobalDefinitions() {
            string path = Path.Combine(Paths.BlockDefsDirectory, Paths.GlobalDefsFile);
            if (!File.Exists(path)) {
                path = Paths.GlobalDefsFile;
                if (!File.Exists(path)) return;
            }
            
            int count;
            GlobalDefs = Load(path, out count);
            Logger.Log(LogType.SystemActivity, "BlockDefinitions.LoadGlobal: Loaded " + count + " blocks");
        }

        public static void ReLoadGlobalDefinitions() {
            //Dont know what I am doing here....

            /*GlobalDefs = new BlockDefinition[256];
            Player[] players = Server.Players;
            foreach (Player pl in players) {
                SendRemoveOldCustomBlocks(pl, pl.World);
            }
            LoadGlobalDefinitions();
            int nothing;
            foreach (World world in WorldManager.Worlds) {
                world.BlockDefs = new BlockDefinition[256];
                string blockDefPath = Path.Combine(Paths.BlockDefsDirectory, world.Name + ".txt");
                if (File.Exists(blockDefPath)) {
                    BlockDefinition[] defs = Load(blockDefPath, out nothing);
                    for (int i = 0; i < defs.Length; i++) {
                        if (defs[i] == null) defs[i] = GlobalDefs[i];
                    }
                    world.BlockDefs = defs;
                } else world.BlockDefs = GlobalDefs;
            }
            foreach (Player p in players) {
                SendCustomBlocks(p);
            }*/
        }

        public static BlockDefinition[] Load(string path, out int count) {
            BlockDefinition[] defs;
            count = 0;
            try {
                using (Stream s = File.OpenRead(path))
                    defs = (BlockDefinition[])JsonSerializer.DeserializeFromStream(typeof(BlockDefinition[]), s);
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "BlockDefinitions.Load: " + ex);
                return new BlockDefinition[256];
            }
            
            for (int i = 0; i < defs.Length; i++) {
                if (defs[i] == null) continue;
                // fixup for servicestack not writing out null entries
                if (defs[i].Name == null) { defs[i] = null; continue; }
                
                FixupLegacy(defs[i]);
                count++;
                defs[i].BlockName = defs[i].Name.ToLower().Replace(" ", "");
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
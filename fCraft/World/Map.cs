// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using fCraft.Drawing;
using fCraft.MapConversion;
using JetBrains.Annotations;
using System.Diagnostics;

namespace fCraft {
    /// <summary> Represents a map file (associated with a world or not).
    /// Maps can be created blank (using Map constructor), generated terrain (using MapGenerator),
    /// or loaded from file (using fCraft.MapConversion.MapUtility). </summary>
    public unsafe sealed class Map {
        /// <summary> Current default map format for saving. </summary>
        public const MapFormat SaveFormat = MapFormat.FCMv3;

        /// <summary> The world associated with this map, if any. May be null. </summary>
        [CanBeNull]
        public World World { get; set; }

        /// <summary> Map width, in blocks. Equivalent to Notch's X (horizontal). </summary>
        public readonly int Width;

        /// <summary> Map length, in blocks. Equivalent to Notch's Z (horizontal). </summary>
        public readonly int Length;

        /// <summary> Map height, in blocks. Equivalent to Notch's Y (vertical). </summary>
        public readonly int Height;
        
        /// <summary> Map boundaries. Can be useful for calculating volume or interesections. </summary>
        public readonly BoundingBox Bounds;

        /// <summary> Map volume, in terms of blocks. </summary>
        public readonly int Volume;

        public const Block MaxLegalBlockType = Block.Obsidian; //Highest block before CPE

        public const Block MaxCustomBlockType = Block.StoneBrick;
        internal readonly static Block[] FallbackBlocks = new Block[256];

        static void DefineFallbackBlocks()
        {
            for (int i = 0; i <= (int)Block.Obsidian; i++)
            {
                FallbackBlocks[i] = (Block)i;
            }
            FallbackBlocks[(int)Block.CobbleSlab] = Block.Slab;
            FallbackBlocks[(int)Block.Rope] = Block.BrownMushroom;
            FallbackBlocks[(int)Block.Sandstone] = Block.Sand;
            FallbackBlocks[(int)Block.Snow] = Block.Air;
            FallbackBlocks[(int)Block.Fire] = Block.StillLava;
            FallbackBlocks[(int)Block.LightPink] = Block.Pink;
            FallbackBlocks[(int)Block.DarkGreen] = Block.Green;
            FallbackBlocks[(int)Block.Brown] = Block.Dirt;
            FallbackBlocks[(int)Block.DarkBlue] = Block.Blue;
            FallbackBlocks[(int)Block.Turquoise] = Block.Cyan;
            FallbackBlocks[(int)Block.Ice] = Block.Glass;
            FallbackBlocks[(int)Block.Tile] = Block.Iron;
            FallbackBlocks[(int)Block.Magma] = Block.Obsidian;
            FallbackBlocks[(int)Block.Pillar] = Block.White;
            FallbackBlocks[(int)Block.Crate] = Block.Wood;
            FallbackBlocks[(int)Block.StoneBrick] = Block.Stone;
        }
        
        public unsafe byte[] GetFallbackMapRanderer() {
            BlockDefinition.LoadGlobalDefinitions();
            byte[] translatedBlocks = (byte[])Blocks.Clone();
            int volume = translatedBlocks.Length;
            
            BlockDefinition[] defs = BlockDefinition.GlobalDefs;
            byte* fallback = stackalloc byte[256];
            for (int i = 0; i < 256; i++) {
                fallback[i] = (byte)FallbackBlocks[i];
                if (defs[i] == null) continue;
                fallback[i] = defs[i].FallBack;
            }
            
            fixed (byte* ptr = translatedBlocks) {
                for (int i = 0; i < volume; i++) {
                    byte block = ptr[i];
                    if (block > (byte)MaxCustomBlockType)
                        ptr[i] = fallback[block];
                }
            }
            return translatedBlocks;
        }

        /// <summary> Default spawning point on the map. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> If spawn coordinates are outside the map. </exception>
        public Position Spawn {
            get {
                return spawn;
            }
            set {
                if (value != Position.RandomSpawn) {
                    if (value.X > Width * 32 || value.Y > Length * 32 || value.X < 0 || value.Y < 0 || value.Z < 0) {
                        Logger.Log(LogType.Warning, "Map.Spawn: Coordinates are outside the map!");
                        return;
                    }
                }
                spawn = value;
                HasChangedSinceSave = true;
            }
        }
        Position spawn;

        public Position getSpawnIfRandom() {
            if (spawn == Position.RandomSpawn) {
                Random rnd = new Random();
                Vector3I P = HighestFreeSpace(rnd.Next(Width), rnd.Next(Length), Height);
                return new Position(P.X * 32, P.Y * 32, P.Z * 32, 0, 0);
            }
            return spawn;
        }
        
        public Vector3I HighestFreeSpace(int x, int y, int z) {
            while (z > 0 && GetBlock(x, y, z) != Block.Air) {
                z--;
            }
            
            while (z < Height) {
                Block bFeet = GetBlock(x, y, z), bHead = GetBlock(x, y, z + 1);
                bool freeFeet = bFeet == Block.Air || bFeet == Block.None;
                bool freeHead = bHead == Block.Air || bHead == Block.None;
                
                if (freeFeet && freeHead) break;
                z++;
            }
            return new Vector3I(x, y, z);
        }

        /// <summary> Resets spawn to the default location (top center of the map). </summary>
        public void ResetSpawn() {
            Spawn = new Position(Width * 16, Length * 16,
                                  Math.Min(short.MaxValue, Height * 32));
        }


        /// <summary> Whether the map was modified since last time it was saved. </summary>
        public bool HasChangedSinceSave { get; internal set; }


        // FCMv3 additions
        public DateTime DateModified { get; set; }
        public DateTime DateCreated { get; set; }
        public Guid Guid { get; set; }

        /// <summary> Array of map blocks.
        /// Use Index(x,y,h) to convert coordinates to array indices.
        /// Use QueueUpdate() for working on live maps to
        /// ensure that all players get updated. </summary>
        public byte[] Blocks;



        /// <summary> Map metadata, excluding zones. </summary>
        public MetadataCollection<string> Metadata { get; private set; }

        /// <summary> All zones within a map. </summary>
        public ZoneCollection Zones { get; private set; }

        /// <summary> Creates an empty new map of given dimensions.
        /// Dimensions cannot be changed after creation. </summary>
        /// <param name="world"> World that owns this map. May be null, and may be changed later. </param>
        /// <param name="width"> Width (horizontal, Notch's X). </param>
        /// <param name="length"> Length (horizontal, Notch's Z). </param>
        /// <param name="height"> Height (vertical, Notch's Y). </param>
        /// <param name="initBlockArray"> If true, the Blocks array will be created. </param>
        /// <exception cref="ArgumentOutOfRangeException"> If width/length/height are not between 16 and 1024. </exception>
        public Map( World world, int width, int length, int height, bool initBlockArray ) {
            if( !IsValidDimension( width ) ) throw new ArgumentOutOfRangeException( "width", "Invalid map dimension." );
            if( !IsValidDimension( length ) ) throw new ArgumentOutOfRangeException( "length", "Invalid map dimension." );
            if( !IsValidDimension( height ) ) throw new ArgumentOutOfRangeException( "height", "Invalid map dimension." );
            DateCreated = DateTime.UtcNow;
            DateModified = DateCreated;
            Guid = Guid.NewGuid();

            Metadata = new MetadataCollection<string>();
            Metadata.Changed += OnMetaOrZoneChange;

            Zones = new ZoneCollection();
            Zones.Changed += OnMetaOrZoneChange;

            World = world;

            Width = width;
            Length = length;
            Height = height;
            Bounds = new BoundingBox( Vector3I.Zero, Width, Length, Height );
            Volume = Bounds.Volume;

            if( initBlockArray ) {
                Blocks = new byte[Volume];
            }

            ResetSpawn();
        }


        void OnMetaOrZoneChange( object sender, EventArgs args ) {
            HasChangedSinceSave = true;
        }


        #region Saving

        /// <summary> Saves this map to a file in the default format (FCMv3). </summary>
        /// <returns> Whether the saving succeeded. </returns>
        public bool Save( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            string tempFileName = fileName + ".temp";

            // save to a temporary file
            try {
                HasChangedSinceSave = false;
                if( !MapUtility.TrySave( this, tempFileName, SaveFormat ) ) {
                    HasChangedSinceSave = true;
                }

            } catch( IOException ex ) {
                HasChangedSinceSave = true;
                Logger.Log( LogType.Error,
                            "Map.Save: Unable to open file \"{0}\" for writing: {1}",
                            tempFileName, ex );
                if( File.Exists( tempFileName ) )
                    File.Delete( tempFileName );
                return false;
            }

            // move newly-written file into its permanent destination
            try {
                Paths.MoveOrReplaceFile( tempFileName, fileName );
                Logger.Log( LogType.SystemActivity,
                            "Saved map to {0}", fileName );

            } catch( Exception ex ) {
                HasChangedSinceSave = true;
                Logger.Log( LogType.Error,
                            "Map.Save: Error trying to replace file \"{0}\": {1}",
                            fileName, ex );
                if( File.Exists( tempFileName ) )
                    File.Delete( tempFileName );
                return false;
            }
            return true;
        }

        #endregion


        #region Block Getters / Setters

        /// <summary> Converts given coordinates to a block array index. </summary>
        /// <param name="x"> X coordinate (width). </param>
        /// <param name="y"> Y coordinate (length, Notch's Z). </param>
        /// <param name="z"> Z coordinate (height, Notch's Y). </param>
        /// <returns> Index of the block in Map.Blocks array. </returns>
        public int Index( int x, int y, int z ) {
            return (z * Length + y) * Width + x;
        }


        /// <summary> Converts given coordinates to a block array index. </summary>
        /// <param name="coords"> Coordinate vector (X,Y,Z). </param>
        /// <returns> Index of the block in Map.Blocks array. </returns>
        public int Index( Vector3I coords ) {
            return (coords.Z * Length + coords.Y) * Width + coords.X;
        }

        

        /// <summary> Sets a block in a safe way.
        /// Note that using SetBlock does not relay changes to players.
        /// Use QueueUpdate() for changing blocks on live maps/worlds. </summary>
        /// <param name="x"> X coordinate (width). </param>
        /// <param name="y"> Y coordinate (length, Notch's Z). </param>
        /// <param name="z"> Z coordinate (height, Notch's Y). </param>
        /// <param name="type"> Block type to set. </param>
        public void SetBlock(int x, int y, int z, Block type)
        {
            if (x < Width && y < Length && z < Height && x >= 0 && y >= 0 && z >= 0)
            {
                Blocks[Index(x, y, z)] = (byte)type;
                HasChangedSinceSave = true;
            }
        }

        
        

        

        public bool SetBlockNoNeighborChange(int x, int y, int z, Block newBlock)
        {
            Block oldBlock = GetBlock(x, y, z);
            if (oldBlock == newBlock || oldBlock == Block.None)
                return false;
                 

            Blocks[Index(x, y, z)] = (byte)newBlock;
            HasChangedSinceSave = true;

            if (HasChangedSinceSave)
            {
                Player[] players = World.Players;
                Block block = GetBlock(x, y, z);
                for( int i = 0; i < players.Length; i++ ) {
                    // cannot reuse packet as each player may require different modifications to block field
                    players[i].SendBlock( new Vector3I( x, y, z ), block );
                }
            }
            return true;
        }
                
        /// <summary> Sets a block at given coordinates. Checks bounds. </summary>
        /// <param name="coords"> Coordinate vector (X,Y,Z). </param>
        /// <param name="type"> Block type to set. </param>
        public void SetBlock( Vector3I coords, Block type ) {
            if( coords.X < Width && coords.Y < Length && coords.Z < Height && coords.X >= 0 && coords.Y >= 0 && coords.Z >= 0 ) {
                Blocks[Index( coords )] = (byte)type;
                HasChangedSinceSave = true;
            }
        }


        /// <summary> Sets a block at given coordinates. </summary>
        /// <param name="index"> Index of the block (use map.Index(x,y,z)). </param>
        /// <param name="type"> Block type to set. </param>
        public void SetBlock( int index, Block type ) {
            Blocks[index] = (byte)type;
            HasChangedSinceSave = true;
        }


        /// <summary> Gets a block at given coordinates. Checks bounds. </summary>
        /// <param name="x"> X coordinate (width). </param>
        /// <param name="y"> Y coordinate (length, Notch's Z). </param>
        /// <param name="z"> Z coordinate (height, Notch's Y). </param>
        /// <returns> Block type, as a Block enumeration. Block.None if coordinates were out of bounds. </returns>
        public Block GetBlock( int x, int y, int z ) {
            if( x < Width && y < Length && z < Height && x >= 0 && y >= 0 && z >= 0 )
                return (Block)Blocks[Index( x, y, z )];
            return Block.None;
        }


        /// <summary> Gets a block at given coordinates. Checks bounds. </summary>
        /// <param name="coords"> Coordinate vector (X,Y,Z). </param>
        /// <returns> Block type, as a Block enumeration. Undefined if coordinates were out of bounds. </returns>
        public Block GetBlock( Vector3I coords ) {
            if( coords.X < Width && coords.Y < Length && coords.Z < Height && coords.X >= 0 && coords.Y >= 0 && coords.Z >= 0 )
                return (Block)Blocks[Index( coords )];
            return Block.None;
        }

        /// <summary> Get the name of the block, used when blockdefinition blocks should show their Name instead of ID .</summary>
        public static string GetBlockName(World world, Block block) {
            Block outBlock;
            if (GetBlockByName(world, block.ToString(), false, out outBlock)) {
                if (world.BlockDefs[(byte)outBlock] != null) {
                    return world.BlockDefs[(int)outBlock].Name;
                }
                return outBlock.ToString();
            } else {
                return Block.None.ToString();
            }
        }


        /// <summary> Checks whether the given coordinate (in block units) is within the bounds of the map. </summary>
        /// <param name="x"> X coordinate (width). </param>
        /// <param name="y"> Y coordinate (length, Notch's Z). </param>
        /// <param name="z"> Z coordinate (height, Notch's Y). </param>
        public bool InBounds( int x, int y, int z ) {
            return x < Width && y < Length && z < Height && x >= 0 && y >= 0 && z >= 0;
        }


        /// <summary> Checks whether the given coordinate (in block units) is within the bounds of the map. </summary>
        /// <param name="vec"> Coordinate vector (X,Y,Z). </param>
        public bool InBounds( Vector3I vec ) {
            return vec.X < Width && vec.Y < Length && vec.Z < Height && vec.X >= 0 && vec.Y >= 0 && vec.Z >= 0;
        }

        #endregion


        #region Block Updates & Simulation

        // Queue of block updates. Updates are applied by ProcessUpdates()
        readonly ConcurrentQueue<BlockUpdate> updates = new ConcurrentQueue<BlockUpdate>();


        /// <summary> Number of blocks that are waiting to be processed. </summary>
        public int UpdateQueueLength {
            get { return updates.Length; }
        }

        /// <summary> Queues a new block update to be processed.
        /// Due to concurrent nature of the server, there is no guarantee
        /// that updates will be applied in any specific order. </summary>
        public void QueueUpdate( BlockUpdate update ) {
            updates.Enqueue( update );
        }


        /// <summary> Clears all pending updates. </summary>
        public void ClearUpdateQueue()
        {
            BlockUpdate ignored;
            while (updates.TryDequeue(out ignored)) { }
        }


        // Applies pending updates and sends them to players (if applicable).
        internal void ProcessUpdates() {
            if( World == null ) {
                throw new InvalidOperationException( "Map must be assigned to a world to process updates." );
            }

            if( World.IsLocked ) {
                if( World.IsPendingMapUnload ) {
                    World.UnloadMap( true );
                }
                return;
            }

            int packetsSent = 0;
            bool canFlush = false;
            int maxPacketsPerUpdate = Server.CalculateMaxPacketsPerUpdate( World );
            BlockUpdate update = new BlockUpdate();
            while( packetsSent < maxPacketsPerUpdate ) {
                if( !updates.TryDequeue( out update ) ) {
                    if( World.IsFlushing ) canFlush = true;
                    break;
                }
                
                HasChangedSinceSave = true;
                if( !InBounds( update.X, update.Y, update.Z ) ) continue;
                int blockIndex = Index( update.X, update.Y, update.Z );
                Blocks[blockIndex] = (byte)update.BlockType;

                if( !World.IsFlushing ) {
                    Player[] players = World.Players;
                    for( int i = 0; i < players.Length; i++ ) {
                        Player p = players[i];
                        if (p == update.Origin) continue;
                        p.SendBlock( new Vector3I( update.X, update.Y, update.Z ), update.BlockType );
                    }
                }
                packetsSent++;
            }

            if( drawOps.Count > 0 ) {
                lock( drawOpLock ) {
                    if( drawOps.Count > 0 ) {
                        packetsSent += ProcessDrawOps( maxPacketsPerUpdate - packetsSent );
                    }
                }
            } else if( canFlush ) {
                World.EndFlushMapBuffer();
            }

            if( packetsSent == 0 && World.IsPendingMapUnload ) {
                World.UnloadMap( true );
            }
        }

        #endregion


        #region Draw Operations

        /// <summary> Number of active draw operations. </summary>
        public int DrawQueueLength {
            get { return drawOps.Count; }
        }

        /// <summary> Total estimated number of blocks left to process, from all draw operations combined. </summary>
        public long DrawQueueBlockCount {
            get {
                lock( drawOpLock ) {
                    return drawOps.Sum( op => (long)op.BlocksLeftToProcess );
                }
            }
        }

        readonly List<DrawOperation> drawOps = new List<DrawOperation>();

        readonly object drawOpLock = new object();


        internal void QueueDrawOp( [NotNull] DrawOperation op ) {
            if( op == null ) throw new ArgumentNullException( "op" );
            lock( drawOpLock ) {
                drawOps.Add( op );
            }
        }


        int ProcessDrawOps( int maxTotalUpdates ) {
            if( World == null ) throw new InvalidOperationException( "No world assigned" );
            int blocksDrawnTotal = 0;
            for( int i = 0; i < drawOps.Count; i++ ) {
                DrawOperation op = drawOps[i];

                // remove a cancelled drawOp from the list
                if( op.IsCancelled ) {
                    op.End();
                    drawOps.RemoveAt( i );
                    i--;
                    continue;
                }

                // draw a batch of blocks
                int blocksToDraw = maxTotalUpdates / (drawOps.Count - i);
                op.StartBatch();
#if DEBUG
                int blocksDrawn = op.DrawBatch( blocksToDraw );
#else
                int blocksDrawn;
                try{
                    blocksDrawn = op.DrawBatch( blocksToDraw );
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "DrawOp error", "ProCraft", ex, false );
                    op.Player.Message( "&WError occurred in your draw command: {0}: {1}",
                                       ex.GetType().Name, ex.Message );
                    drawOps.RemoveAt( i );
                    op.End();
                    return blocksDrawnTotal;
                }
#endif
                blocksDrawnTotal += blocksDrawn;
                if( blocksDrawn > 0 ) {
                    HasChangedSinceSave = true;
                }
                maxTotalUpdates -= blocksDrawn;

                // remove a completed drawOp from the list
                if( op.IsDone ) {
                    op.End();
                    drawOps.RemoveAt( i );
                    i--;
                }
            }
            return blocksDrawnTotal;
        }


        /// <summary> Cancels and stops all active draw operations. </summary>
        public void CancelAllDrawOps() {
            lock( drawOpLock ) {
                for( int i = 0; i < drawOps.Count; i++ ) {
                    drawOps[i].Cancel();
                    drawOps[i].End();
                }
                drawOps.Clear();
            }
        }

        #endregion


        #region Utilities
        /// <summary> Checks if a given map dimension (width, height, or length) is acceptable.
        /// Values between 1 and 16384 are technically allowed. </summary>
        public static bool IsValidDimension( int dimension ) {
            return dimension >= 16 && dimension <= 16384;
        }


        /// <summary> Checks if a given map dimension (width, height, or length) is among the set of recommended values
        /// Recommended values are: 16, 32, 64, 128, 256, 512, 1024 </summary>
        public static bool IsRecommendedDimension( int dimension ) {
            return dimension >= 16 && (dimension & (dimension - 1)) == 0 && dimension <= 1024;
        }


        /// <summary> Converts nonstandard (50-255) blocks using the given mapping. </summary>
        /// <param name="mapping"> Byte array of length 256. </param>
        /// <returns> True if any blocks needed conversion/mapping. </returns>
        public bool ConvertBlockTypes( [NotNull] byte[] mapping ) {
            if( mapping == null ) throw new ArgumentNullException( "mapping" );
            if( mapping.Length != 256 ) throw new ArgumentException( "Mapping must list all 256 blocks", "mapping" );

            bool mapped = false;
            fixed( byte* ptr = Blocks ) {
                for( int j = 0; j < Blocks.Length; j++ ) {
                    if( ptr[j] > 65 ) {
                        ptr[j] = mapping[ptr[j]];
                        mapped = true;
                    }
                }
            }
            if( mapped ) HasChangedSinceSave = true;
            return mapped;
        }

        static readonly byte[] ZeroMapping = new byte[256];

        /// <summary> Replaces all nonstandard (50-255) blocks with air. </summary>
        /// <returns> True if any blocks needed replacement. </returns>
        public bool RemoveUnknownBlocktypes() {
            return ConvertBlockTypes( ZeroMapping );
        }


        static readonly Dictionary<string, Block> BlockNames = new Dictionary<string, Block>();

        static Map() {
            // add default names for blocks, and their numeric codes
            foreach (Block block in Enum.GetValues(typeof(Block)))
            {
                BlockNames.Add(block.ToString().ToLower(), block);
                BlockNames.Add(((int)block).ToStringInvariant(), block);
            }
            DefineFallbackBlocks();
            // alternative names for blocks
            BlockNames["skip"] = Block.None;

            BlockNames["a"] = Block.Air;
            BlockNames["nothing"] = Block.Air;
            BlockNames["empty"] = Block.Air;
            BlockNames["delete"] = Block.Air;
            BlockNames["erase"] = Block.Air;
            BlockNames["blank"] = Block.Air;

            BlockNames["cement"] = Block.Stone;
            BlockNames["concrete"] = Block.Stone;

            BlockNames["g"] = Block.Grass;
            BlockNames["gras"] = Block.Grass; // common typo

            BlockNames["soil"] = Block.Dirt;
            BlockNames["cobble"] = Block.Cobblestone;
            BlockNames["stones"] = Block.Cobblestone;
            BlockNames["rocks"] = Block.Cobblestone;
            BlockNames["plank"] = Block.Wood;
            BlockNames["planks"] = Block.Wood;
            BlockNames["board"] = Block.Wood;
            BlockNames["boards"] = Block.Wood;
            BlockNames["tree"] = Block.Sapling;
            BlockNames["plant"] = Block.Sapling;
            BlockNames["adminium"] = Block.Admincrete;
            BlockNames["adminite"] = Block.Admincrete;
            BlockNames["opcrete"] = Block.Admincrete;
            BlockNames["hardrock"] = Block.Admincrete;
            BlockNames["solid"] = Block.Admincrete;
            BlockNames["bedrock"] = Block.Admincrete;
            BlockNames["w"] = Block.Water;
            BlockNames["l"] = Block.Lava;
            BlockNames["gold_ore"] = Block.GoldOre;
            BlockNames["iron_ore"] = Block.IronOre;
            BlockNames["copperore"] = Block.IronOre;
            BlockNames["copper_ore"] = Block.IronOre;
            BlockNames["ore"] = Block.IronOre;
            BlockNames["coals"] = Block.Coal;
            BlockNames["coalore"] = Block.Coal;
            BlockNames["blackore"] = Block.Coal;

            BlockNames["trunk"] = Block.Log;
            BlockNames["stump"] = Block.Log;
            BlockNames["treestump"] = Block.Log;
            BlockNames["treetrunk"] = Block.Log;

            BlockNames["leaf"] = Block.Leaves;
            BlockNames["foliage"] = Block.Leaves;

            BlockNames["cheese"] = Block.Sponge;

            BlockNames["redcloth"] = Block.Red;
            BlockNames["redwool"] = Block.Red;
            BlockNames["orangecloth"] = Block.Orange;
            BlockNames["orangewool"] = Block.Orange;
            BlockNames["yellowcloth"] = Block.Yellow;
            BlockNames["yellowwool"] = Block.Yellow;
            BlockNames["limecloth"] = Block.Lime;
            BlockNames["limewool"] = Block.Lime;
            BlockNames["greenyellow"] = Block.Lime;
            BlockNames["yellowgreen"] = Block.Lime;
            BlockNames["lightgreen"] = Block.Lime;
            BlockNames["lightgreencloth"] = Block.Lime;
            BlockNames["lightgreenwool"] = Block.Lime;
            BlockNames["greencloth"] = Block.Green;
            BlockNames["greenwool"] = Block.Green;
            BlockNames["springgreen"] = Block.Teal;
            BlockNames["emerald"] = Block.Teal;
            BlockNames["tealwool"] = Block.Teal;
            BlockNames["tealcloth"] = Block.Teal;
            BlockNames["aquawool"] = Block.Aqua;
            BlockNames["aquacloth"] = Block.Aqua;
            BlockNames["cyanwool"] = Block.Cyan;
            BlockNames["cyancloth"] = Block.Cyan;
            BlockNames["lightblue"] = Block.Blue;
            BlockNames["bluewool"] = Block.Blue;
            BlockNames["bluecloth"] = Block.Blue;
            BlockNames["indigowool"] = Block.Indigo;
            BlockNames["indigocloth"] = Block.Indigo;
            BlockNames["violetwool"] = Block.Violet;
            BlockNames["violetcloth"] = Block.Violet;
            BlockNames["lightpurple"] = Block.Violet;
            BlockNames["purple"] = Block.Violet;
            BlockNames["purplewool"] = Block.Violet;
            BlockNames["purplecloth"] = Block.Violet;
            BlockNames["fuchsia"] = Block.Magenta;
            BlockNames["magentawool"] = Block.Magenta;
            BlockNames["magentacloth"] = Block.Magenta;
            BlockNames["darkpink"] = Block.Pink;
            BlockNames["pinkwool"] = Block.Pink;
            BlockNames["pinkcloth"] = Block.Pink;
            BlockNames["darkgray"] = Block.Black;
            BlockNames["darkgrey"] = Block.Black;
            BlockNames["grey"] = Block.Gray;
            BlockNames["lightgray"] = Block.Gray;
            BlockNames["lightgrey"] = Block.Gray;
            BlockNames["cloth"] = Block.White;
            BlockNames["cotton"] = Block.White;

            BlockNames["yellow_flower"] = Block.YellowFlower;
            BlockNames["flower"] = Block.YellowFlower;
            BlockNames["dandelion"] = Block.YellowFlower;
            BlockNames["rose"] = Block.RedFlower;
            BlockNames["redrose"] = Block.RedFlower;
            BlockNames["red_flower"] = Block.RedFlower;

            BlockNames["mushroom"] = Block.BrownMushroom;
            BlockNames["shroom"] = Block.BrownMushroom;
            BlockNames["brown_shroom"] = Block.BrownMushroom;
            BlockNames["red_shroom"] = Block.RedMushroom;

            BlockNames["goldblock"] = Block.Gold;
            BlockNames["goldsolid"] = Block.Gold;
            BlockNames["golden"] = Block.Gold;
            BlockNames["copper"] = Block.Gold;
            BlockNames["brass"] = Block.Gold;

            BlockNames["ironblock"] = Block.Iron;
            BlockNames["steel"] = Block.Iron;
            BlockNames["metal"] = Block.Iron;
            BlockNames["silver"] = Block.Iron;

            BlockNames["stairs"] = Block.DoubleSlab;
            BlockNames["steps"] = Block.DoubleSlab;
            BlockNames["slabs"] = Block.DoubleSlab;
            BlockNames["doublestep"] = Block.DoubleSlab;
            BlockNames["doublestair"] = Block.DoubleSlab;
            BlockNames["double_step"] = Block.DoubleSlab;
            BlockNames["double_stair"] = Block.DoubleSlab;
            BlockNames["double_slab"] = Block.DoubleSlab;
            BlockNames["staircasefull"] = Block.DoubleSlab;
            BlockNames["step"] = Block.Slab;
            BlockNames["stair"] = Block.Slab;
            BlockNames["halfstep"] = Block.Slab;
            BlockNames["halfblock"] = Block.Slab;
            BlockNames["staircasestep"] = Block.Slab;

            BlockNames["brick"] = Block.Brick;
            BlockNames["explosive"] = Block.TNT;
            BlockNames["dynamite"] = Block.TNT;

            BlockNames["book"] = Block.Books;
            BlockNames["shelf"] = Block.Books;
            BlockNames["shelves"] = Block.Books;
            BlockNames["bookcase"] = Block.Books;
            BlockNames["bookshelf"] = Block.Books;
            BlockNames["bookshelves"] = Block.Books;

            BlockNames["moss"] = Block.MossyCobble;
            BlockNames["mossy"] = Block.MossyCobble;
            BlockNames["stonevine"] = Block.MossyCobble;
            BlockNames["mossyrock"] = Block.MossyCobble;
            BlockNames["mossyrocks"] = Block.MossyCobble;
            BlockNames["mossystone"] = Block.MossyCobble;
            BlockNames["mossystones"] = Block.MossyCobble;
            BlockNames["greencobblestone"] = Block.MossyCobble;
            BlockNames["mossycobblestone"] = Block.MossyCobble;
            BlockNames["mossy_cobblestone"] = Block.MossyCobble;
            BlockNames["blockthathasgreypixelsonitmostlybutsomeareactuallygreen"] = Block.MossyCobble;

            BlockNames["onyx"] = Block.Obsidian;

            BlockNames["cobbleslab"] = Block.CobbleSlab;
            BlockNames["cobblestoneslab"] = Block.CobbleSlab;
            BlockNames["cslab"] = Block.CobbleSlab;

            BlockNames["rope"] = Block.Rope;
            BlockNames["ladder"] = Block.Rope;

            BlockNames["sandstone"] = Block.Sandstone;
            BlockNames["snow"] = Block.Snow;
            BlockNames["fire"] = Block.Fire;
            BlockNames["lightpink"] = Block.LightPink;
            BlockNames["forestgreen"] = Block.DarkGreen;
            BlockNames["darkgreen"] = Block.DarkGreen;
            BlockNames["brown"] = Block.Brown;
            BlockNames["darkblue"] = Block.DarkBlue;
            BlockNames["deepblue"] = Block.DarkBlue;
            BlockNames["turquoise"] = Block.Turquoise;
            BlockNames["turq"] = Block.Turquoise;
            BlockNames["ice"] = Block.Ice;
            BlockNames["tile"] = Block.Tile;
            BlockNames["magma"] = Block.Magma;
            BlockNames["pillar"] = Block.Pillar;
            BlockNames["crate"] = Block.Crate;
            BlockNames["stonebrick"] = Block.StoneBrick;
            
            BlockNames["hot_lava"] = Block.StillLava;
            BlockNames["cold_water"] = Block.StillWater;
        }


        /// <summary> Calculates a 2D heightmap, based on the highest solid block for each column of blocks. 
        /// Air, Brown/Red mushrooms, Glass, Leaves, Red/Yellow flowers, and Saplings are considered non-solid. </summary>
        /// <returns> A 2D array of same Width/Length as the map.
        /// Value at each coordinate corresponds to the highest solid point on the map. </returns>
        public short[,] ComputeHeightmap() {
            short[,] shadows = new short[Width, Length];
            for( int x = 0; x < Width; x++ ) {
                for( int y = 0; y < Length; y++ ) {
                    int index = (Height * Length + y) * Width + x;
                    for( short z = (short)( Height - 1 ); z >= 0; z-- ) {
                        index -= Length * Width;
                        switch( Blocks[index] ) {
                            case (byte)Block.Air:
                            case (byte)Block.BrownMushroom:
                            case (byte)Block.Glass:
                            case (byte)Block.Leaves:
                            case (byte)Block.RedFlower:
                            case (byte)Block.RedMushroom:
                            case (byte)Block.Sapling:
                            case (byte)Block.YellowFlower:
                                continue;
                            default:
                                shadows[x, y] = z;
                                break;
                        }
                        break;
                    }
                }
            }
            return shadows;
        }


        /// <summary> Finds Block corresponding to given blockName. </summary>
        /// <param name="blockName"> Given block name to parse. </param>
        /// <param name="allowNoneBlock"> Whether "none" block type is acceptable. </param>
        /// <param name="block"> Block corresponding to given blockName; Block.None if value could not be parsed. </param>
        /// <returns> True if given blockName was parsed as an acceptable block type. </returns>
        /// <exception cref="ArgumentNullException"> blockName is null. </exception>
        public static bool GetBlockByName([CanBeNull] World world, [NotNull] string blockName, 
                                          bool allowNoneBlock, out Block block) {
            if (blockName == null) throw new ArgumentNullException("blockName");
            if (BlockNames.TryGetValue(blockName.ToLower(), out block)) {
                return block == Block.None ? allowNoneBlock : true;
            }
            
            BlockDefinition[] defs = world == null ? BlockDefinition.GlobalDefs : world.BlockDefs;
            byte id;
            if (Byte.TryParse(blockName, out id)) {
                BlockDefinition def = defs[id];
                if (def != null) { block = (Block)id; return true; }
            } else {
                foreach (BlockDefinition def in defs) {
                    if (def == null || !def.BlockName.CaselessEquals(blockName)) continue;
                    block = (Block)def.BlockID; return true;
                }
            }
            block = Block.None; return false;
        }

        const int bufferSize = 64 * 1024;
        internal void CompressMap(Player dst) {
            byte[] array = Blocks;
            using (LevelChunkStream ms = new LevelChunkStream(dst))
                using (GZipStream compressor = new GZipStream(ms, CompressionMode.Compress, true))
            {
                int count = IPAddress.HostToNetworkOrder(array.Length); // convert to big endian
                compressor.Write(BitConverter.GetBytes(count), 0, 4);
                ms.length = array.Length;
                
                for (int i = 0; i < array.Length; i += bufferSize) {
                    int len = Math.Min(bufferSize, array.Length - i);
                    ms.position = i;
                    compressor.Write(array, i, len);
                }
            }
        }
        
        internal void CompressAndConvertMap(byte maxLegal, Player dst) {
            byte[] array = Blocks;
            byte* fallback = stackalloc byte[256];
            MakeFallbacks(fallback, maxLegal, World);
            using (LevelChunkStream ms = new LevelChunkStream(dst))
                using (GZipStream compressor = new GZipStream(ms, CompressionMode.Compress, true))
            {
                int count = IPAddress.HostToNetworkOrder(array.Length); // convert to big endian
                compressor.Write(BitConverter.GetBytes(count), 0, 4);
                ms.length = array.Length;
                
                byte[] buffer = new byte[bufferSize];
                for (int i = 0; i < array.Length; i += bufferSize) {
                    int len = Math.Min(bufferSize, array.Length - i);
                    for (int j = 0; j < len; j++)
                        buffer[j] = fallback[array[i + j]];
                    
                    ms.position = i;
                    compressor.Write(buffer, 0, len);
                }
            }
        }
        
        unsafe static void MakeFallbacks(byte* fallback, byte maxLegal, World world) {
            BlockDefinition[] defs = world.BlockDefs;
            bool hasCPEBlocks = maxLegal == (byte)MaxCustomBlockType;

            for (int i = 0; i < 256; i++) {
                fallback[i] = (byte)FallbackBlocks[i];
                if (defs[i] == null) continue;
                fallback[i] = defs[i].FallBack;
                
                // Handle CPE defined fallback blocks for custom blocks
                if (fallback[i] > (byte)maxLegal)
                    fallback[i] = (byte)FallbackBlocks[fallback[i]];
            }
            for (int i = 0; i <= (byte)maxLegal; i++)
                fallback[i] = (byte)i;
        }


        /// <summary> Searches the map, from top to bottom, for the first appearance of a given block. </summary>
        /// <param name="x"> X coordinate (width). </param>
        /// <param name="y"> Y coordinate (length, Notch's Z). </param>
        /// <param name="id"> Block type to search for. </param>
        /// <returns> Height (Z coordinate; Notch's Y) of the blocktype's first appearance.
        /// -1 if given blocktype was not found. </returns>
        public int SearchColumn( int x, int y, Block id ) {
            return SearchColumn( x, y, id, Height - 1 );
        }


        /// <summary> Searches the map, from top to bottom, for the first appearance of a given block. </summary>
        /// <param name="x"> X coordinate (width). </param>
        /// <param name="y"> Y coordinate (length, Notch's Z). </param>
        /// <param name="id"> Block type to search for. </param>
        /// <param name="zStart"> Starting height. No blocks above this point will be checked. </param>
        /// <returns> Height (Z coordinate; Notch's Y) of the blocktype's first appearance.
        /// -1 if given blocktype was not found. </returns>
        public int SearchColumn( int x, int y, Block id, int zStart ) {
            for( int z = zStart; z > 0; z-- ) {
                if( GetBlock( x, y, z ) == id ) {
                    return z;
                }
            }
            return -1; // -1 means 'not found'
        }

        #endregion
    }
}
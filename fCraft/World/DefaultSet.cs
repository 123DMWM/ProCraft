// ProCraft Copyright 2014-2018 Joseph Beauvais <123DMWM@gmail.com>

// Based off code from https://github.com/Hetal728/MCGalaxy/blob/master/MCGalaxy/Blocks/DefaultSet.cs
// which was based off code from https://github.com/UnknownShadow200/ClassicalSharp/blob/master/ClassicalSharp/Blocks/DefaultSet.cs
// As the author of both of these files, I hereby license the code to be freely used without needing to follow the licenses of the aforementioned projects.
using System;
using System.Text;

namespace fCraft {
    
    /// <summary> Stores default properties for blocks in Minecraft Classic. (and CPE blocks). </summary>
    public static class DefaultSet {
        
        /// <summary> Constructs a custom block, with the default properties of the given classic/CPE block. </summary>
        public static BlockDefinition MakeCustomBlock(Block b) {
            BlockDefinition def = new BlockDefinition();
            byte raw = (byte)b;
            def.BlockID = raw;
            def.Name = Name(raw);
            def.CollideType = (byte)Collide(b);
            def.Speed = 1;
            def.BlocksLight = BlocksLight(b);
            
            def.TopTex = topTex[raw];
            def.BottomTex = bottomTex[raw];
            def.SetSidesTex(sideTex[raw]);
            def.WalkSound = (byte)StepSound(b);
            
            def.FullBright = FullBright(b);
            def.Shape = Draw(b) == DrawType.Sprite ? (byte)0 : (byte)1;
            DrawType blockDraw = Draw(b);
            if (blockDraw == DrawType.Sprite) blockDraw = DrawType.Transparent;
            def.BlockDraw = (byte)blockDraw;
            
            def.FogDensity = FogDensity(b);
            CustomColor fog = FogColor(b);
            def.FogR = fog.R; def.FogG = fog.G; def.FogB = fog.B;
            def.FallBack = raw;
            
            def.MaxX = 16; def.MaxZ = Height(b); def.MaxY = 16;
            return def;
        }
        
        /// <summary> Gets the default height of a block. A value of 16 is full height. </summary>
        public static byte Height(Block b) {
            if (b == Block.Slab) return 8;
            if (b == Block.CobbleSlab) return 8;
            if (b == Block.Snow) return 2;
            return 16;
        }
        
        /// <summary> Gets whether a block is full bright / light emitting by default. </summary>
        public static bool FullBright(Block b) {
            return b == Block.Lava || b == Block.StillLava
                || b == Block.Magma || b == Block.Fire;
        }
        
        /// <summary> Gets the default fog density of a block, in packed form. </summary>
        public static byte FogDensity(Block b) {
            if (b == Block.Water || b == Block.StillWater)
                return 11; // (128 * 0.1f - 1);
            if (b == Block.Lava || b == Block.StillLava)
                return 229; // (128 * 1.8f - 1);
            return 0;
        }
        
        /// <summary> Gets the default fog color of a block. </summary>
        public static CustomColor FogColor(Block b) {
            if (b == Block.Water || b == Block.StillWater)
                return new CustomColor(5, 5, 51);
            if (b == Block.Lava || b == Block.StillLava)
                return new CustomColor(153, 25, 0);
            return default(CustomColor);
        }
        
        /// <summary> Gets the default collide type of a block, see CollideType class. </summary>
        public static CollideType Collide(Block b) {
            if (b >= Block.Water && b <= Block.StillLava)
                return CollideType.SwimThrough;
            if (b == Block.Snow || b == Block.Air || Draw(b) == DrawType.Sprite)
                return CollideType.WalkThrough;
            return CollideType.Solid;
        }
        
        /// <summary> Gets whether a block blocks light (prevents light passing through) by default. </summary>
        public static bool BlocksLight(Block b) {
            return !(b == Block.Glass || b == Block.Leaves
                     || b == Block.Air || Draw(b) == DrawType.Sprite);
        }
        

        /// <summary> Gets the default step sound of a block. </summary>
        public static SoundType StepSound(Block b) {
            if (b == Block.Glass) return SoundType.Stone;
            if (b == Block.Rope) return SoundType.Cloth;
            if (Draw(b) == DrawType.Sprite) return SoundType.None;
            
            if (b >= Block.Red && b <= Block.White)
                return SoundType.Cloth;
            if (b >= Block.LightPink && b <= Block.Turquoise)
                return SoundType.Cloth;
            if (b == Block.Iron || b == Block.Gold)
                return SoundType.Metal;
            
            if (b == Block.Books || b == Block.Wood
                || b == Block.Log || b == Block.Crate || b == Block.Fire)
                return SoundType.Wood;
            
            if (b == Block.Rope) return SoundType.Cloth;
            if (b == Block.Sand) return SoundType.Sand;
            if (b == Block.Snow) return SoundType.Snow;
            if (b == Block.Glass) return SoundType.Glass;
            if (b == Block.Dirt || b == Block.Gravel)
                return SoundType.Gravel;
            
            if (b == Block.Grass || b == Block.Sapling || b == Block.TNT
                || b == Block.Leaves || b == Block.Sponge)
                return SoundType.Grass;
            
            if (b >= Block.YellowFlower && b <= Block.RedMushroom)
                return SoundType.Grass;
            if (b >= Block.Water && b <= Block.StillLava)
                return SoundType.None;
            if (b >= Block.Stone && b <= Block.StoneBrick)
                return SoundType.Stone;
            return SoundType.None;
        }
        

        /// <summary> Gets the default draw type of a block, see Draw class. </summary>        
        public static DrawType Draw(Block b) {
            if (b == Block.Air || b == Block.None) return DrawType.Gas;
            if (b == Block.Leaves) return DrawType.TransparentThick;

            if (b == Block.Ice || b == Block.Water || b == Block.StillWater)
                return DrawType.Translucent;
            if (b == Block.Glass || b == Block.Leaves)
                return DrawType.Transparent;
            
            if (b >= Block.YellowFlower && b <= Block.RedMushroom)
                return DrawType.Sprite;
            if (b == Block.Sapling || b == Block.Rope || b == Block.Fire)
                return DrawType.Sprite;
            return DrawType.Opaque;
        }
        
        
        const string RawNames = "Air_Stone_Grass_Dirt_Cobblestone_Wood_Sapling_Bedrock_Water_Still water_Lava" +
            "_Still lava_Sand_Gravel_Gold ore_Iron ore_Coal ore_Log_Leaves_Sponge_Glass_Red_Orange_Yellow_Lime_Green" +
            "_Teal_Aqua_Cyan_Blue_Indigo_Violet_Magenta_Pink_Black_Gray_White_Dandelion_Rose_Brown mushroom_Red mushroom" +
            "_Gold_Iron_Double slab_Slab_Brick_TNT_Bookshelf_Mossy rocks_Obsidian_Cobblestone slab_Rope_Sandstone" +
            "_Snow_Fire_Light pink_Forest green_Brown_Deep blue_Turquoise_Ice_Ceramic tile_Magma_Pillar_Crate_Stone brick";    
        
        static string Name(int block) {
            // Find start and end of this particular block name
            int start = 0;
            for (int i = 0; i < block; i++)
                start = RawNames.IndexOf('_', start) + 1;
            
            int end = RawNames.IndexOf('_', start);
            if (end == -1) end = RawNames.Length;
            
            return RawNames.Substring(start, end - start);
        }
        
        
        static byte[] topTex = new byte[] { 0,  1,  0,  2, 16,  4, 15, 17, 14, 14,
            30, 30, 18, 19, 32, 33, 34, 21, 22, 48, 49, 64, 65, 66, 67, 68, 69, 70, 71,
            72, 73, 74, 75, 76, 77, 78, 79, 13, 12, 29, 28, 24, 23,  6,  6,  7,  9,  4,
            36, 37, 16, 11, 25, 50, 38, 80, 81, 82, 83, 84, 51, 54, 86, 26, 53, 52, };
        static byte[] sideTex = new byte[] { 0,  1,  3,  2, 16,  4, 15, 17, 14, 14,
            30, 30, 18, 19, 32, 33, 34, 20, 22, 48, 49, 64, 65, 66, 67, 68, 69, 70, 71,
            72, 73, 74, 75, 76, 77, 78, 79, 13, 12, 29, 28, 40, 39,  5,  5,  7,  8, 35,
            36, 37, 16, 11, 41, 50, 38, 80, 81, 82, 83, 84, 51, 54, 86, 42, 53, 52, };
        static byte[] bottomTex = new byte[] { 0,  1,  2,  2, 16,  4, 15, 17, 14, 14,
            30, 30, 18, 19, 32, 33, 34, 21, 22, 48, 49, 64, 65, 66, 67, 68, 69, 70, 71,
            72, 73, 74, 75, 76, 77, 78, 79, 13, 12, 29, 28, 56, 55,  6,  6,  7, 10,  4,
            36, 37, 16, 11, 57, 50, 38, 80, 81, 82, 83, 84, 51, 54, 86, 58, 53, 52 };
    }
    
    public enum DrawType : byte {
        Opaque = 0,
        Transparent = 1,
        TransparentThick = 2, // e.g. leaves render all neighbours
        Translucent = 3,
        Gas = 4,
        Sprite = 5,
    }
    
    public enum CollideType : byte {
        WalkThrough = 0, // Gas (usually also used by sprite)
        SwimThrough = 1, // Liquid
        Solid       = 2, // Solid
        Ice         = 3, // Solid and partially slidable on.
        SlipperyIce = 4, // Solid and fully slidable on.        
        LiquidWater = 5, // Water style 'swimming'/'bobbing'    
        LiquidLava  = 6, // Lava style 'swimming'/'bobbing'
    }
    
    public enum SoundType : byte {
        None, Wood, Gravel, Grass, Stone,
        Metal, Glass, Cloth, Sand, Snow,
    }
}
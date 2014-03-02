using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    class Fallback
    {
        public static Dictionary<Block, Block> FallbackID = new Dictionary<Block, Block>()
        {
            {Block.CobbleSlab, Block.Slab},
            {Block.Rope, Block.BrownMushroom},
            {Block.Sandstone, Block.Sand},
            {Block.Snow, Block.Air},
            {Block.Fire, Block.Lava},
            {Block.LightPink, Block.Pink},
            {Block.DarkGreen, Block.Green},
            {Block.Brown, Block.Dirt},
            {Block.DarkBlue, Block.Blue},
            {Block.Turquoise, Block.Cyan},
            {Block.Ice, Block.Glass},
            {Block.Tile, Block.Iron},
            {Block.Magma, Block.Obsidian},
            {Block.Pillar, Block.White},
            {Block.Crate, Block.Wood},
            {Block.StoneBrick, Block.Stone},
        };

        public static Block GetFallBack(Block ID)
        {
            return FallbackID[ID];
        }
    }
}

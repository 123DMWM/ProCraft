

// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>

namespace fCraft.Drawing
{
    /// <summary> Draw operation that creates a simple cuboid. </summary>
    public sealed class OverlayDrawOperation : DrawOperation
    {
        public override string Name
        {
            get { return "Overlay"; }
        }

        public override int ExpectedMarks
        {
            get { return 2; }
        }

        public OverlayDrawOperation(Player player)
            : base(player)
        {
        }


        public override bool Prepare(Vector3I[] marks)
        {
            if (!base.Prepare(marks)) return false;
            BlocksTotalEstimate = Bounds.Volume;
            Coords.X = Bounds.XMin;
            Coords.Y = Bounds.YMin;
            Coords.Z = Bounds.ZMax;
            return true;
        }


        bool top = false;

        public override int DrawBatch(int maxBlocksToDraw)
        {
            int blocksDone = 0;
            for (; Coords.X <= Bounds.XMax; Coords.X++)
            {
                for (; Coords.Y <= Bounds.YMax; Coords.Y++)
                {
                    for (; !top && (Coords.Z >= Bounds.ZMin); Coords.Z--)
                    {
                        Block down = Map.GetBlock(new Vector3I(Coords.X, Coords.Y, Coords.Z - 1));
                        Block here = Map.GetBlock(new Vector3I(Coords.X, Coords.Y, Coords.Z));
                        if (down != Block.Air && down != Block.Water && down != Block.StillWater && 
                            down != Block.Lava && down != Block.StillLava && down != Block.Sapling && 
                            down != Block.YellowFlower && down != Block.RedFlower && down != Block.BrownMushroom && 
                            down != Block.RedMushroom && down != Block.Slab && down != Block.CobbleSlab && 
                            down != Block.Rope && down != Block.Snow && down != Block.Fire && 
                            here == Block.Air)
                        {
                            if (!DrawOneBlock()) continue;
                            blocksDone++;
                            top = true;
                            if (blocksDone >= maxBlocksToDraw)
                            {
                                Coords.Z--;
                                return blocksDone;
                            }
                        }
                        else if (Coords.Z == Bounds.ZMax && here != Block.Air)
                        {
                            top = true;
                        }
						else if (down != Block.Air && here == Block.Air)
						{
							top = true;
						}
                    }
                    Coords.Z = Bounds.ZMax;
                    top = false;
                }
                Coords.Y = Bounds.YMin;
                if (TimeToEndBatch)
                {
                    Coords.X++;
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }
    }
}


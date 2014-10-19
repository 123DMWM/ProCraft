// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | Copyright 2014 123DMWM <shmo1joe2@gmail.com>
using System;

namespace fCraft.Drawing {
    /// <summary> Brush that creates a diagonal rainbow pattern, using
    /// Red, Orange, Yellow, Green, Aqua, Blue, and Violet blocks. </summary>
    public sealed class RainbowBrush : IBrushFactory, IBrush {
        /// <summary> Global singleton instance of RainbowBrush. </summary>
        public static readonly RainbowBrush Instance = new RainbowBrush();

        static readonly Block[] Rainbow = {
            Block.Red,
            Block.Orange,
            Block.Yellow, 
            Block.Lime,
            Block.Green,
            Block.Teal, 
            Block.Aqua, 
            Block.Cyan, 
            Block.Blue, 
            Block.Indigo, 
            Block.Violet, 
            Block.Magenta,
            Block.Pink
        };

        public string Name {
            get { return "Rainbow"; }
        }

        public int AlternateBlocks {
            get { return 1; }
        }

        public string[] Aliases {
            get { return null; }
        }

        public string Help {
            get { return "Rainbow brush: Creates a diagonal 7-color rainbow pattern."; }
        }

        public string Description {
            get { return Name; }
        }

        public IBrushFactory Factory {
            get { return this; }
        }

        RainbowBrush() { }


        public IBrush MakeBrush(Player player, CommandReader cmd) {
            return this;
        }


        public IBrush MakeDefault() {
            return this;
        }


        public bool Begin(Player player, DrawOperation state) {
            return true;
        }


        public Block NextBlock(DrawOperation state) {
            if (state == null)
                throw new ArgumentNullException("state");
            return Rainbow[(state.Coords.X + state.Coords.Y + state.Coords.Z) % 13];
        }


        public void End() { }


        public IBrush Clone() {
            return this;
        }
    }
}
// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft.Drawing {
    /// <summary> Brush that creates a diagonal BWRainbow pattern, using
    /// Red, Orange, Yellow, Green, Aqua, Blue, and Violet blocks. </summary>
    public sealed class BWRainbowBrush : IBrushFactory, IBrush {
        /// <summary> Global singleton instance of BWRainbowBrush. </summary>
        public static readonly BWRainbowBrush Instance = new BWRainbowBrush();

        private static readonly Block[] BWRainbow = {
            Block.Obsidian, Block.Black, Block.Gray, Block.White, Block.Iron,
            Block.White, Block.Gray, Block.Black
        };

        public string Name {
            get { return "BWRainbow"; }
        }

        public int AlternateBlocks {
            get { return 1; }
        }

        public string[] Aliases {
            get { return null; }
        }

        public string Help {
            get { return "Creates a diagonal 8-color black and white rainbow pattern."; }
        }

        public string Description {
            get { return Name; }
        }

        public IBrushFactory Factory {
            get { return this; }
        }

        BWRainbowBrush() { }


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
            return BWRainbow[(state.Coords.X + state.Coords.Y + state.Coords.Z) % 8];
        }


        public void End() { }


        public IBrush Clone() {
            return this;
        }
    }
}
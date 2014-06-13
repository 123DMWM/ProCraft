// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | Copyright 2014 123DMWM <shmo1joe2@gmail.com>
using System;

namespace fCraft.Drawing {
    /// <summary> Brush that creates a diagonal rainbow pattern, using
    /// Red, Orange, Yellow, Green, Aqua, Blue, and Violet blocks. </summary>
    public sealed class RainbowBrush : IBrushFactory, IBrush, IBrushInstance {
        public static readonly RainbowBrush Instance = new RainbowBrush();

        RainbowBrush() { }

        public string Name {
            get { return "Rainbow"; }
        }

        public int AlternateBlocks {
            get { return 1; }
        }

        public string[] Aliases {
            get { return null; }
        }

        const string HelpString = "Rainbow brush: Creates a diagonal 13-color rainbow pattern.";
        public string Help {
            get { return HelpString; }
        }


        public string Description {
            get { return Name; }
        }

        public IBrushFactory Factory {
            get { return this; }
        }


        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            return this;
        }


        public IBrushInstance MakeInstance( Player player, CommandReader cmd, DrawOperation state ) {
            return this;
        }

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

        public string InstanceDescription {
            get { return "Rainbow"; }
        }

        public IBrush Brush {
            get { return Instance; }
        }

        public bool Begin( Player player, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            return true;
        }


        public Block NextBlock( DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            return Rainbow[(state.Coords.X + state.Coords.Y + state.Coords.Z) % 13];
        }


        public void End() { }
    }
}
// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | Copyright 2014 123DMWM <shmo1joe2@gmail.com>
using System;

namespace fCraft.Drawing
{
    /// <summary> Brush that creates a diagonal rainbow pattern, using
    /// Red, Orange, Yellow, Green, Aqua, Blue, and Violet blocks. </summary>
    public sealed class BWRainbowBrush : IBrushFactory, IBrush, IBrushInstance
    {
        public static readonly BWRainbowBrush Instance = new BWRainbowBrush();

        BWRainbowBrush() { }

        public string Name
        {
            get { return "BWRainbow"; }
        }

        public int AlternateBlocks
        {
            get { return 1; }
        }

        public string[] Aliases
        {
            get { return null; }
        }

        const string HelpString = "Black and White Rainbow brush: Creates a diagonal 6-color rainbow pattern.";
        public string Help
        {
            get { return HelpString; }
        }


        public string Description
        {
            get { return Name; }
        }

        public IBrushFactory Factory
        {
            get { return this; }
        }


        public IBrush MakeBrush(Player player, CommandReader cmd)
        {
            return this;
        }


        public IBrushInstance MakeInstance(Player player, CommandReader cmd, DrawOperation state)
        {
            return this;
        }

        static readonly Block[] BWRainbow = {
            Block.Obsidian,
            Block.Black, 
            Block.Gray,
            Block.White,
            Block.Iron,
            Block.White,
            Block.Gray, 
            Block.Black
        };

        public string InstanceDescription
        {
            get { return "BWRainbow"; }
        }

        public IBrush Brush
        {
            get { return Instance; }
        }

        public bool Begin(Player player, DrawOperation state)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (state == null) throw new ArgumentNullException("state");
            return true;
        }


        public Block NextBlock(DrawOperation state)
        {
            if (state == null) throw new ArgumentNullException("state");
            return BWRainbow[(state.Coords.X + state.Coords.Y + state.Coords.Z) % 8];
        }


        public void End() { }
    }
}
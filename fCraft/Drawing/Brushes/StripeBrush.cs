// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft.Drawing {
    /// <summary> Constructs StripedBrush. </summary>
    public sealed class StripedBrushFactory : IBrushFactory {
        /// <summary> Singleton instance of the StripedBrushFactory. </summary>
        public static readonly StripedBrushFactory Instance = new StripedBrushFactory();

        public string Name {
            get { return "Striped"; }
        }

        public string[] Aliases { get; private set; }

        public string Help {
            get {
                return "Fills the area with alternating Striped pattern. " +
                       "If only one block name is given, leaves every other block untouched.";
            }
        }


        StripedBrushFactory() {
            Aliases = new[] { "stripe" };
        }


        public IBrush MakeBrush(Player player, CommandReader cmd) {
            if (player == null)
                throw new ArgumentNullException("player");
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            Block block, altBlock;

            // first block type is required
            if (!cmd.NextBlock(player, true, out block)) {
                player.Message("{0}: Please specify at least one block type.", Name);
                return null;
            }

            // second block type is optional
            if (cmd.HasNext) {
                if (!cmd.NextBlock(player, true, out altBlock))
                    return null;
            } else {
                altBlock = Block.None;
            }

            return new StripedBrush(block, altBlock);
        }


        public IBrush MakeDefault() {
            // There is no default for this brush: parameters always required.
            return null;
        }
    }


    /// <summary> Brush that alternates between two block types, in a Striped pattern. </summary>
    public sealed class StripedBrush : IBrush {
        public int AlternateBlocks {
            get { return 1; }
        }

        /// <summary> First block in the alternating pattern. </summary>
        public Block Block1 { get; private set; }

        /// <summary> Second block in the alternating pattern. </summary>
        public Block Block2 { get; private set; }

        public string Description {
            get {
                if (Block2 != Block.None) {
                    return String.Format("{0}({1},{2})", Factory.Name, Block1, Block2);
                } else if (Block1 != Block.None) {
                    return String.Format("{0}({1})", Factory.Name, Block1);
                } else {
                    return Factory.Name;
                }
            }
        }

        public IBrushFactory Factory {
            get { return StripedBrushFactory.Instance; }
        }


        /// <summary> Initializes a new instance of StripedBrush. </summary>
        public StripedBrush(Block block1, Block block2) {
            Block1 = block1;
            Block2 = block2;
        }


        public bool Begin(Player player, DrawOperation state) {
            if (player == null)
                throw new ArgumentNullException("player");
            if (state == null)
                throw new ArgumentNullException("state");
            return true;
        }


        public Block NextBlock(DrawOperation state) {
            if (((state.Coords.X + state.Coords.Y + state.Coords.Z) & 2) == 2) {
                return Block1;
            } else {
                return Block2;
            }
        }


        public void End() { }


        public IBrush Clone() {
            return new StripedBrush(Block1, Block2);
        }
    }
}

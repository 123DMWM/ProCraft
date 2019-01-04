// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Constructs ReplaceNotBrushBrush. </summary>
    public sealed class ReplaceNotBrushBrushFactory : IBrushFactory {
        /// <summary> Singleton instance of the ReplaceNotBrushBrushFactory. </summary>
        public static readonly ReplaceNotBrushBrushFactory Instance = new ReplaceNotBrushBrushFactory();


        ReplaceNotBrushBrushFactory() {
            Aliases = new[] { "rnb" };
        }


        public string Name {
            get { return "ReplaceNotBrush"; }
        }

        public string[] Aliases { get; private set; }

        public string Help {
            get {
                return "Replaces all blocks except the given type with output of another brush. " +
                       "Usage: &H/Brush rnb <Block> <BrushName>";
            }
        }


        public IBrush MakeBrush(Player player, CommandReader cmd) {
            if (player == null)
                throw new ArgumentNullException("player");
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (!cmd.HasNext) {
                player.Message("ReplaceNotBrush usage: &H/Brush rnb <Block> <BrushName>");
                return null;
            }

            Block block;
            if (!cmd.NextBlock(player, false, out block))
                return null;

            string brushName = cmd.Next();
            if (brushName == null || !CommandManager.IsValidCommandName(brushName)) {
                player.Message("ReplaceNotBrush usage: &H/Brush rnb <Block> <BrushName>");
                return null;
            }
            IBrushFactory brushFactory = BrushManager.GetBrushFactory(brushName);

            if (brushFactory == null) {
                player.Message("Unrecognized brush \"{0}\"", brushName);
                return null;
            }

            IBrush newBrush = brushFactory.MakeBrush(player, cmd);
            if (newBrush == null) {
                return null;
            }

            return new ReplaceNotBrushBrush(block, newBrush);
        }


        public IBrush MakeDefault() {
            // There is no default for this brush: parameters always required.
            return null;
        }
    }


    /// <summary> Brush that replaces all blocks of the given type with output of a brush. </summary>
    public sealed class ReplaceNotBrushBrush : IBrush {
        public int AlternateBlocks {
            get { return 1; }
        }

        public Block Block { get; private set; }

        public IBrushFactory Factory {
            get { return ReplaceNotBrushBrushFactory.Instance; }
        }

        public IBrush Replacement { get; private set; }

        public string Description {
            get {
                return String.Format("{0}({1} -> {2})",
                                     Factory.Name,
                                     Block,
                                     Replacement.Description);
            }
        }


        public ReplaceNotBrushBrush(Block block, [NotNull] IBrush replacement) {
            Block = block;
            Replacement = replacement;
        }


        public bool Begin(Player player, DrawOperation op) {
            if (player == null)
                throw new ArgumentNullException("player");
            if (op == null)
                throw new ArgumentNullException("op");
            op.Context |= BlockChangeContext.Replaced;
            return Replacement.Begin(player, op);
        }


        public Block NextBlock(DrawOperation op) {
            if (op == null)
                throw new ArgumentNullException("op");
            Block block = op.Map.GetBlock(op.Coords);
            if (block == Block) {
                return Block.None;
            }
            return Replacement.NextBlock(op);
        }


        public void End() {
            Replacement.End();
        }


        public IBrush Clone() {
            return new ReplaceNotBrushBrush(Block, Replacement.Clone());
        }
    }
}

// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft.Drawing {
    /// <summary> A self-contained DrawOperation that provides its own brush.
    /// Purpose of this class is mostly to take care of the boilerplate code. </summary>
    public abstract class DrawOpWithBrush : DrawOperation, IBrushFactory, IBrush {
        public abstract override string Description { get; }


        protected DrawOpWithBrush(Player player)
            : base(player) { }


        public abstract bool ReadParams(CommandReader cmd);

        protected abstract Block NextBlock();

        #region IBrushFactory Members

        string IBrushFactory.Name {
            get { return Name; }
        }

        string IBrushFactory.Help {
            get { throw new NotImplementedException(); }
        }

        string[] IBrushFactory.Aliases {
            get { return null; }
        }


        IBrush IBrushFactory.MakeBrush(Player player, CommandReader cmd) {
            return this;
        }


        IBrush IBrushFactory.MakeDefault() {
            throw new NotImplementedException();
        }

        #endregion

        #region IBrush Members

        IBrushFactory IBrush.Factory {
            get { return this; }
        }

        string IBrush.Description {
            get { return Description; }
        }

        int IBrush.AlternateBlocks {
            get { return 1; }
        }


        bool IBrush.Begin(Player player, DrawOperation op) {
            return true;
        }


        Block IBrush.NextBlock(DrawOperation op) {
            return NextBlock();
        }


        void IBrush.End() { }


        IBrush IBrush.Clone() {
            throw new NotImplementedException();
        }

        #endregion
    }
}
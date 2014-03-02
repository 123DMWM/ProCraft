﻿// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing
{
    /// <summary> A self-contained DrawOperation that provides its own brush.
    /// Purpose of this class is mostly to take care of the boilerplate code. </summary>
    public abstract class DrawOpWithBrush : DrawOperation, IBrushFactory, IBrush, IBrushInstance
    {

        public override abstract string Description
        {
            get;
        }

        protected DrawOpWithBrush(Player player)
            : base(player)
        {
        }

        public abstract bool ReadParams(CommandReader cmd);


        protected abstract Block NextBlock();


        #region IBrushFactory Members

        string IBrushFactory.Name
        {
            get { return Name; }
        }

        string IBrushFactory.Help
        {
            get { throw new NotImplementedException(); }
        }

        string[] IBrushFactory.Aliases
        {
            get { return null; }
        }

        IBrush IBrushFactory.MakeBrush(Player player, CommandReader cmd)
        {
            return this;
        }

        #endregion


        #region IBrush Members

        IBrushFactory IBrush.Factory
        {
            get { return this; }
        }

        string IBrush.Description
        {
            get { throw new NotImplementedException(); }
        }

        IBrushInstance IBrush.MakeInstance(Player player, CommandReader cmd, DrawOperation op)
        {
            if (ReadParams(cmd))
            {
                return this;
            }
            else
            {
                return null;
            }
        }

        #endregion


        #region IBrushInstance Members

        IBrush IBrushInstance.Brush
        {
            get { return this; }
        }

        string IBrushInstance.InstanceDescription
        {
            get { return Description; }
        }

        int IBrushInstance.AlternateBlocks
        {
            get { return 1; }
        }

        bool IBrushInstance.Begin(Player player, DrawOperation op)
        {
            return true;
        }

        Block IBrushInstance.NextBlock(DrawOperation op)
        {
            return NextBlock();
        }

        void IBrushInstance.End() { }

        #endregion
    }
}
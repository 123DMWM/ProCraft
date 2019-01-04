﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft.Drawing {
    /// <summary> Draw operation that handles /Undo and /Redo commands.
    /// Applies changes stored in a given UndoState object. </summary>
    public sealed class UndoDrawOperation : DrawOpWithBrush {
        readonly BlockChangeContext undoContext = BlockChangeContext.Drawn | BlockChangeContext.UndoneSelf;

        public UndoState State { get; private set; }

        public bool Redo { get; private set; }

        public override int ExpectedMarks {
            get { return 0; }
        }

        public override string Description {
            get { return Name; }
        }

        public override string Name {
            get {
                if( Redo ) {
                    return "Redo";
                } else {
                    return "Undo";
                }
            }
        }


        public UndoDrawOperation( Player player, UndoState state, bool redo )
            : base( player ) {
            State = state;
            Redo = redo;
            if( Redo ) {
                undoContext |= BlockChangeContext.Redone;
            }
        }


        public override bool Prepare( Vector3I[] marks ) {
            Brush = this;
            if( !base.Prepare( marks ) ) return false;
            BlocksTotalEstimate = State.Buffer.Count;
            Context = undoContext;
            Bounds = State.GetBounds();
            return true;
        }


        public override bool Begin() {
            if( !RaiseBeginningEvent( this ) ) return false;
            if( Redo ) {
                UndoState = Player.RedoBegin( this );
            } else {
                UndoState = Player.UndoBegin( this );
            }
            StartTime = DateTime.UtcNow;
            HasBegun = true;
            Map.QueueDrawOp( this );
            RaiseBeganEvent( this );
            return true;
        }


        int undoBufferIndex;
        Block block;

        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; undoBufferIndex < State.Buffer.Count; undoBufferIndex++ ) {
                UndoBlock update = State.Get( undoBufferIndex );
                Coords.X = update.Index % State.Width;
                Coords.Y = (update.Index / State.Width) % State.Length;
                Coords.Z = (update.Index / State.Width) / State.Length;
                block = update.Block;
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw || TimeToEndBatch ) {
                        undoBufferIndex++;
                        return blocksDone;
                    }
                }
            }
            IsDone = true;
            return blocksDone;
        }


        protected override Block NextBlock() {
            return block;
        }

        public override bool ReadParams( CommandReader cmd ) {
            return true;
        }
    }
}
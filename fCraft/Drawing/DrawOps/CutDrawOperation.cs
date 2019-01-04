﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft.Drawing {
    /// <summary> Draw operation that handles /Cut command.
    /// Copies everything into currently-selected copy slot as soon as Begin() is called,
    /// then gradually fills the area. </summary>
    public sealed class CutDrawOperation : DrawOperation {
        public override string Name {
            get { return "Cut"; }
        }

        public override int ExpectedMarks {
            get { return 2; }
        }

        public override string Description {
            get {
                var normalBrush = Brush as NormalBrush;
                if( normalBrush != null ) {
                    if( normalBrush.AlternateBlocks > 0 && normalBrush.Blocks[0] == Block.Air ) {
                        return Name;
                    } else {
                        return String.Format( "{0}/{1}", Name, normalBrush.Blocks[0] );
                    }
                } else {
                    return base.Description;
                }
            }
        }


        public CutDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( Player.World == null && !Player.IsSuper ) PlayerOpException.ThrowNoWorld( Player );
            if( !base.Prepare( marks ) ) return false;

            BlocksTotalEstimate = Bounds.Volume;
            Coords = Bounds.MinVertex;

            Context |= BlockChangeContext.Cut;
            return true;
        }


        public override bool Begin() {
            // remember dimensions and orientation
            CopyState copyInfo = new CopyState( Marks[0], Marks[1] );

            for( int x = Bounds.XMin; x <= Bounds.XMax; x++ ) {
                for( int y = Bounds.YMin; y <= Bounds.YMax; y++ ) {
                    for( int z = Bounds.ZMin; z <= Bounds.ZMax; z++ ) {
                        copyInfo.Blocks[x - Bounds.XMin, y - Bounds.YMin, z - Bounds.ZMin] = Map.GetBlock( x, y, z );
                    }
                }
            }
            copyInfo.OriginWorld = Player.World.Name;
            copyInfo.CopyTime = DateTime.UtcNow;
            Player.SetCopyState( copyInfo );

            Player.Message( "{0} blocks cut into slot #{1}. You can now &H/Paste",
                            Bounds.Volume, Player.CopySlot + 1 );
            Player.Message( "Origin at {0} {1}{2} corner.",
                            (copyInfo.Orientation.Z == 1 ? "bottom" : "top"),
                            (copyInfo.Orientation.Y == 1 ? "south" : "north"),
                            (copyInfo.Orientation.X == 1 ? "east" : "west") );

            return base.Begin();
        }


        // lifted straight from CuboidDrawOp
        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        if( DrawOneBlock() ) {
                            blocksDone++;
                            if( blocksDone >= maxBlocksToDraw ) {
                                Coords.Z++;
                                return blocksDone;
                            }
                        }
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
                if( TimeToEndBatch ) {
                    Coords.X++;
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }
    }
}
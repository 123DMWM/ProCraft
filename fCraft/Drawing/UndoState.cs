// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2018 Joseph Beauvais <123DMWM@gmail.com>
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace fCraft.Drawing {
    public sealed class UndoState {
        public UndoState( DrawOperation op ) {
            Op = op;
            Buffer = new List<UndoBlock>();
        }

        public readonly DrawOperation Op;
        public readonly List<UndoBlock> Buffer;
        public bool IsTooLargeToUndo;
        public readonly object SyncRoot = new object();
        public int Width, Height, Length;

        public bool Add( Vector3I coord, Map map, Block block ) {
            if (Width == 0) { Width = map.Width; Height = map.Height; Length = map.Length; }
            lock( SyncRoot ) {
                if( BuildingCommands.MaxUndoCount < 1 || Buffer.Count <= BuildingCommands.MaxUndoCount ) {
                    int index = coord.X + Width * (coord.Y + Length * coord.Z);
                    Buffer.Add( new UndoBlock( index, block ) );
                    return true;
                } else if( !IsTooLargeToUndo ) {
                    IsTooLargeToUndo = true;
                    Buffer.Clear();
                }
                return false;
            }
        }

        public UndoBlock Get( int index ) {
            lock( SyncRoot ) {
                return Buffer[index];
            }
        }

        public BoundingBox GetBounds() {
            lock( SyncRoot ) {
                if( Buffer.Count == 0 ) return BoundingBox.Empty;
                Vector3I min = new Vector3I( int.MaxValue, int.MaxValue, int.MaxValue );
                Vector3I max = new Vector3I( int.MinValue, int.MinValue, int.MinValue );
                int x, y, z;
                for( int i = 0; i < Buffer.Count; i++ ) {
                    int index = Buffer[i].Index;
                    x = index % Width;
                    y = (index / Width) % Length;
                    z = (index / Width) / Length;
                    
                    if( x < min.X ) min.X = x;
                    if( y < min.Y ) min.Y = y;
                    if( z < min.Z ) min.Z = z;
                    if( x > max.X ) max.X = x;
                    if( y > max.Y ) max.Y = y;
                    if( z > max.Z ) max.Z = z;
                }
                return new BoundingBox( min, max );
            }
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct UndoBlock {
        public UndoBlock( int index, Block block ) {
            Index = index;
            Block = block;
        }

        public readonly int Index;
        public readonly Block Block;
    }
}
// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System.IO;
using System.Runtime.InteropServices;

namespace fCraft{
    /// <summary> Struct representing a single block change.
    /// You may safely cast byte* pointers directly to BlockDBEntry* and vice versa. </summary>
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BlockDBEntry {
        /// <summary> UTC Unix timestamp of the change. </summary>
        public readonly int Timestamp;

        /// <summary> Numeric PlayerDB id of the player who made the change. </summary>
        public readonly int PlayerID;

        /// <summary> X coordinate (horizontal), in terms of blocks. </summary>
        public readonly ushort X;

        /// <summary> Y coordinate (horizontal), in terms of blocks. </summary>
        public readonly ushort Y;

        /// <summary> Z coordinate (vertical), in terms of blocks. </summary>
        public readonly ushort Z;

        /// <summary> Block that previously occupied this coordinate </summary>
        public readonly Block OldBlock;

        /// <summary> Block that now occupies this coordinate </summary>
        public readonly Block NewBlock;

        /// <summary> Change's (X,Y,Z) coordinates as a vector. </summary>
        public Vector3I Coord {
            get { return new Vector3I( X, Y, Z ); }
        }

        /// <summary> Context for this block change. </summary>
        public readonly BlockChangeContext Context;


        public BlockDBEntry( int timestamp, int playerID, ushort x, ushort y, ushort z,
                            Block oldBlock, Block newBlock, BlockChangeContext flags ) {
            Timestamp = timestamp;
            PlayerID = playerID;
            X = x;
            Y = y;
            Z = z;
            OldBlock = oldBlock;
            NewBlock = newBlock;
            Context = flags;
        }

        public BlockDBEntry( int timestamp, int playerID, Vector3I coords,
                            Block oldBlock, Block newBlock, BlockChangeContext flags ) {
            Timestamp = timestamp;
            PlayerID = playerID;
            X = (ushort)coords.X;
            Y = (ushort)coords.Y;
            Z = (ushort)coords.Z;
            OldBlock = oldBlock;
            NewBlock = newBlock;
            Context = flags;
        }

        public void Serialize( Stream stream, byte[] buffer ) {
            WriteInt( Timestamp, buffer, 0 );
            WriteInt( PlayerID, buffer, 4 );
            buffer[8] = (byte)X; buffer[9] = (byte)(X >> 8);
            buffer[10] = (byte)Y; buffer[11] = (byte)(Y >> 8);
            buffer[12] = (byte)Z; buffer[13] = (byte)(Z >> 8);
            
            buffer[14] = (byte)OldBlock;
            buffer[15] = (byte)NewBlock;
            WriteInt( (int)Context, buffer, 16 );
            stream.Write( buffer, 0, buffer.Length );
        }
        
        static void WriteInt( int value, byte[] buffer, int offset ) {
            buffer[offset + 0] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }
    }
}

// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    sealed class PacketReader : BinaryReader {
        public PacketReader( [NotNull] Stream stream ) :
            base( stream ) { }

        public OpCode ReadOpCode() {
            return (OpCode)ReadByte();
        }
        
        byte[] one = new byte[1];
        public override byte ReadByte() {
            int count = BaseStream.Read(one, 0, 1);
            if (count == 0) throw new EndOfStreamException("End of file");
            return one[0];
        }
        
        public override short ReadInt16() {
            return IPAddress.NetworkToHostOrder( base.ReadInt16() );
        }
        
        
        public override ushort ReadUInt16() {
            return (ushort)IPAddress.NetworkToHostOrder( base.ReadInt16() );
        }
        

        public override int ReadInt32() {
            return IPAddress.NetworkToHostOrder( base.ReadInt32() );
        }


        char[] characters = new char[Packet.StringSize];
        public override string ReadString() {
            int length = 0;
            byte[] data = ReadBytes( Packet.StringSize );
            for( int i = Packet.StringSize - 1; i >= 0; i-- ) {
                byte code = data[i];
                if (code == 0) code = 0x20; // NULL to space
                if (length == 0 && code != 0x20) { length = i + 1; }
                
                // Treat code as an index in code page 437
                if( code < 0x20 ) {
                    characters[i] = Chat.ControlCharReplacements[code];
                } else if( code < 0x7F ) {
                    characters[i] = (char)code;
                } else {
                    characters[i] = Chat.ExtendedCharReplacements[code - 0x7F];
                }
            }
            return new String( characters, 0, length );
        }
    }
}
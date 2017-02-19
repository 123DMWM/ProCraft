// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    sealed class PacketWriter : BinaryWriter {
        public PacketWriter( [NotNull] Stream stream )
            : base( stream ) { }


        public void Write( OpCode opcode ) {
            Write( (byte)opcode );
        }


        public override void Write( short data ) {
            base.Write( IPAddress.HostToNetworkOrder( data ) );
        }


        public override void Write( string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.Length > 64 ) throw new ArgumentException( "String is too long (>64).", "str" );
            Write( Encoding.ASCII.GetBytes( str.PadRight( 64 ) ) );
        }
        
        
        public static void WriteString(string str, byte[] array, int offset, bool hasCP437) {
            if (hasCP437) WriteCP437(str, array, offset);
            else WriteAscii(str, array, offset);
        }
        
        const int StringSize = 64;
        static void WriteAscii(string str, byte[] array, int offset) {
            int count = Math.Min(str.Length, StringSize);
            for (int i = 0; i < count; i++) {
                char raw = str[i].UnicodeToCp437();
                array[offset + i] = raw >= '\u0080' ? (byte)'?' : (byte)raw;
            }
            for (int i = count; i < StringSize; i++)
                array[offset + i] = (byte)' ';
        }

        static void WriteCP437(string str, byte[] array, int offset) {
            int count = Math.Min(str.Length, StringSize);
            for (int i = 0; i < count; i++)
                array[offset + i] = (byte)str[i].UnicodeToCp437();
            for (int i = count; i < StringSize; i++)
                array[offset + i] = (byte)' ';
        }
    }
}
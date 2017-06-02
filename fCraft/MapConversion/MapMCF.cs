using System;
using System.IO;
using System.IO.Compression;

namespace fCraft.MapConversion {
    /// <summary> .mcf map conversion implementation, for converting .mcf map format into fCraft's default map format. </summary>
    public class MapMCF : MapMCSharp {
        public override string ServerName {
            get { return "MCForge-Redux"; }
        }

        public override string FileExtension {
            get { return "mcf"; }             
        }

        public override MapFormat Format {
            get { return MapFormat.MCF; }
        }

        public override bool ClaimsName( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.CaselessEnds( ".mcf" );
        }

        public override Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                    Map map = LoadHeaderInternal( gs );
                    map.Blocks = new byte[map.Volume];

                    int i = 0, lo, hi;
                    while ((lo = gs.ReadByte()) != -1) {
                        hi = gs.ReadByte();
                        int id = lo | (hi << 8);

                        if (id <= (byte)Block.StoneBrick) map.Blocks[i] = (byte)id;
                        else map.Blocks[i] = Mapping[id & 0xFF]; 
                        // NOTE: This breaks the 5 block ids past 255, but I doubt they really had much use.
                        i++;
                    }
                    return map;
                }
            }
        }
    	

        public override bool Save( Map mapToSave, string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.Create( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    BinaryWriter bs = new BinaryWriter( gs );
                    SaveHeader( mapToSave, bs );

                    // Convert byte array to short array, as the MCF file stores an array of shorts for blocks
                    byte[] src = mapToSave.Blocks;
                    byte[] blocks = new byte[src.Length * 2];                   
                    for( int i = 0; i < src.Length; i++ )
                    	blocks[i * 2] = src[i];
                    bs.Write( blocks );

                    bs.Close();
                }
                return true;
            }
        }
    }
}

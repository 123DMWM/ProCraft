// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> ClassicWorld map conversion implementation, for converting ClassicWorld map format into fCraft's default map format. </summary>
    public sealed class MapCW : IMapImporter {

        public string ServerName {
            get { return "ClassicalSharp/ClassiCube client"; }
        }

        public bool SupportsImport {
            get { return true; }
        }

        public bool SupportsExport {
            get { return false; }
        }

        public string FileExtension {
            get { return "cw"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.ClassicWorld; }
        }


        public bool ClaimsName( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.CaselessEnds( ".cw" );
        }


        public bool Claims( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
                    BinaryReader bs = new BinaryReader( gs );
                    return ( bs.ReadByte() == 10 && NBTag.ReadString( bs ) == "ClassicWorld" );
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            Map map = Load( fileName );
            map.Blocks = null;
            return map;
        }


        public Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
                NBTag tag = NBTag.ReadStream( gs );

                // ReSharper disable UseObjectOrCollectionInitializer
                Map map = new Map( null,
                                   tag["X"].GetShort(),
                                   tag["Z"].GetShort(),
                                   tag["Y"].GetShort(),
                                   false );
                // ReSharper restore UseObjectOrCollectionInitializer
                
                NBTag spawnTag = tag["Spawn"];
                map.Spawn = new Position {
                	X = (short)(spawnTag["X"].GetShort() * 32),
                	Y = (short)(spawnTag["Z"].GetShort() * 32),
                	Z = (short)(spawnTag["Y"].GetShort() * 32),
                	R = spawnTag["H"].GetByte(),
                	L = spawnTag["P"].GetByte(),
                };

                map.Blocks = tag["BlockArray"].GetBytes();
                return map;
            }
        }
    }
}
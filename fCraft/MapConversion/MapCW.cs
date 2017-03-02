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
                NBTag root = NBTag.ReadStream( gs );

                // ReSharper disable UseObjectOrCollectionInitializer
                Map map = new Map( null,
                                  root["X"].GetShort(),
                                  root["Z"].GetShort(),
                                  root["Y"].GetShort(),
                                  false );
                // ReSharper restore UseObjectOrCollectionInitializer
                
                NBTag spawnTag = root["Spawn"];
                map.Spawn = new Position {
                    X = (short)(spawnTag["X"].GetShort() * 32),
                    Y = (short)(spawnTag["Z"].GetShort() * 32),
                    Z = (short)(spawnTag["Y"].GetShort() * 32),
                    R = spawnTag["H"].GetByte(),
                    L = spawnTag["P"].GetByte(),
                };
                
                // read UUID
                map.Guid = new Guid( root["UUID"].GetBytes() );
                
                // read creation/modification dates of the file (for fallback)
                DateTime fileCreationDate = File.GetCreationTime( fileName );
                DateTime fileModTime = File.GetCreationTime( fileName );

                // try to read embedded creation date
                if( root.Contains( "TimeCreated" ) ) {
                    map.DateCreated = DateTimeUtil.ToDateTime( root["TimeCreated"].GetLong() );
                } else {
                    // for fallback, pick the older of two filesystem dates
                    map.DateCreated = (fileModTime > fileCreationDate) ? fileCreationDate : fileModTime;
                }

                // try to read embedded modification date
                if( root.Contains( "LastModified" ) ) {
                    map.DateModified = DateTimeUtil.ToDateTime( root["LastModified"].GetLong() );
                } else {
                    // for fallback, use file modification date
                    map.DateModified = fileModTime;
                }

                map.Blocks = root["BlockArray"].GetBytes();
                return map;
            }
        }
    }
}
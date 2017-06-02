﻿// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.IO;

namespace fCraft.MapConversion {
    /// <summary> Saves the raw blocks of a map to disc, uncompressed. Does not save dimensions. </summary>
    public sealed class MapRaw : IMapExporter {

        public string ServerName {
            get { return "Raw"; }
        }

        public bool SupportsImport {
            get { return false; }
        }

        public bool SupportsExport {
            get { return true; }
        }

        public string FileExtension {
            get { return "raw"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }

        public MapFormat Format {
            get { return MapFormat.Raw; }
        }

        public bool Save( Map mapToSave, string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.Create( fileName ) ) {
                mapStream.Write( mapToSave.Blocks, 0, mapToSave.Blocks.Length );
            }
            return true;
        }
    }
}
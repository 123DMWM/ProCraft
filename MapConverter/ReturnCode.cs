// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

namespace fCraft.MapConverter {
    enum ReturnCode {
        Success = 0,
        ArgumentError = 1,
        UnrecognizedImporter = 2,
        UnrecognizedExporter = 3,
        InputDirNotFound = 4,
        PathError = 5,
        UnsupportedSaveFormat = 6
    }
}
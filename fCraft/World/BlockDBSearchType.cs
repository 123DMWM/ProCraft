// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | Copyright 2014 123DMWM <shmo1joe2@gmail.com>

namespace fCraft {
    /// <summary> Describes what kind of results should BlockDB.Lookup return. </summary>
    public enum BlockDBSearchType {
        /// <summary> All BlockDB Entries (even those that have been overriden) are returned,
        /// possibly multiple entries per coordinate. </summary>
        ReturnAll,

        /// <summary> Only one newest entry is returned for each coordinate. </summary>
        ReturnNewest,

        /// <summary> Only one oldest entry is returned for each coordinate. </summary>
        ReturnOldest
    }
}
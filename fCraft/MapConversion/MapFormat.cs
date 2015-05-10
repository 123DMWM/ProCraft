// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

namespace fCraft.MapConversion {
    /// <summary> An enumeration of map formats supported by fCraft. </summary>
    public enum MapFormat {
        /// <summary> Unidentified map. </summary>
        Unknown,

        /// <summary> Obsolete map format previously used by fCraft before release 0.500 </summary>
        FCMv2,

        /// <summary> Obsolete map format previously used by fCraft 0.500-0.6xx </summary>
        FCMv3,

        /// <summary> Map format used by MCSharp and its forks (MCZall/MCLawl). Initial support added by Tyler (TkTech). </summary>
        MCSharp,

        /// <summary> Map format used by MinerCPP and LuaCraft. Initial support added by Tyler (TkTech). </summary>
        MinerCPP,

        /// <summary> Map format used by Myne and its derivatives (MyneCraft/iCraft). </summary>
        Myne,

        /// <summary> Map format used by Mojang's classic and survivaltest. </summary>
        Classic,

        /// <summary> Map format used by Mojang's indev. </summary>
        Indev,

        /// <summary> Map format used by JTE's server. </summary>
        JTE,

        /// <summary> Map foramt used by D3 server. </summary>
        D3,

        /// <summary> Map format used by MCForge-Redux </summary>
        MCF,

        /// <summary> Format used by Opticraft v0.2+. Support contributed by Jared Klopper (LgZ-optical). </summary>
        Opticraft,

        Raw
    }


    /// <summary> Type of map storage (file or folder-based). </summary>
    public enum MapStorageType {
        /// <summary> Map consists of a single file. </summary>
        SingleFile,

        /// <summary> Map consists of a directory with multiple files (e.g. Myne maps). </summary>
        Directory
    }
}

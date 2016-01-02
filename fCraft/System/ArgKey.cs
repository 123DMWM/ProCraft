// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>

namespace fCraft {
    /// <summary> Enumerates the recognized command-line switches/arguments.
    /// Args are parsed in Server.InitLibrary </summary>
    public enum ArgKey {
        /// <summary> Working path (directory) that fCraft should use.
        /// If the path is relative, it's computed against the location of fCraft.dll </summary>
        Path,

        /// <summary> Path (directory) where the log files should be placed.
        /// If the path is relative, it's computed against the working path. </summary>
        LogPath,

        /// <summary> Path (directory) where the map files should be loaded from/saved to.
        /// If the path is relative, it's computed against the working path. </summary>
        MapPath,

        /// <summary> Path (file) of the configuration file.
        /// If the path is relative, it's computed against the working path. </summary>
        Config,

        /// <summary> If NoRestart flag is present, fCraft will shutdown instead of restarting.
        /// Useful if you are using an auto-restart script/process monitor of some sort. </summary>
        NoRestart,

        /// <summary> If ExitOnCrash flag is present, fCraft frontends will exit
        /// at once in the event of an unrecoverable crash, instead of showing a message. </summary>
        ExitOnCrash,

        /// <summary> Disables all logging. </summary>
        NoLog,

        /// <summary> Disables colors in CLI frontends. </summary>
        NoConsoleColor
    };
}

// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft {
    /// <summary> Command categories. A command may belong to more than one category.
    /// Use binary flag logic to test whether a command belongs to a particular category. </summary>
    [Flags]
    public enum CommandCategory {
        /// <summary> Default command category. Do not use it. </summary>
        None = 0,

        /// <summary> Building-related commands: drawing, binding, copy/paste. </summary>
        Building = 1,

        /// <summary> Chat-related commands: messaging, ignoring, muting, etc. </summary>
        Chat = 2,

        /// <summary> Information commands: server, world, zone, rank, and player infos. </summary>
        Info = 4,

        /// <summary> Moderation commands: kick, ban, rank, tp/bring, etc. </summary>
        Moderation = 8,

        /// <summary> Server maintenance commands: reloading configs, editing PlayerDB, importing data, etc. </summary>
        Maintenance = 16,

        /// <summary> World-related commands: joining, loading, renaming, etc. </summary>
        World = 32,

        /// <summary> Zone-related commands: creating, editing, testing, etc. </summary>
        Zone = 64,

        /// <summary> Commands that are only used for diagnostics and debugging. </summary>
        Debug = 128,

        /// <summary> Commands that are custom </summary>
        Custom = 256,

        /// <summary> Commands that are only used with cpe blocks. </summary>
        CPE = 512,

        /// <summary> Commands that are only used for diagnostics and debugging. </summary>
        New = 1024
    }
}

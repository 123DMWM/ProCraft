// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides a way for printing an object's name beautified with Minecraft color codes.
    /// It was "classy" in a sense that it was colored based on "class" (rank) of a player/world/zone. </summary>
    public interface IClassy {
        /// <summary> Name optionally formatted with minecraft color codes or other decorations. </summary>
        [NotNull]
        string ClassyName { get; }
    }
}

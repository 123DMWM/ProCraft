// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | Copyright 2014 123DMWM <shmo1joe2@gmail.com>

namespace fCraft.Drawing {
    /// <summary> Draw operation that handles non-aligned (single-mark) pasting for /Paste and /PasteNot.
    /// Preserves original orientation of the CopyState. </summary>
    sealed class QuickPasteDrawOperation : PasteDrawOperation {
        public override string Name {
            get {
                return Not ? "PasteNot" : "Paste";
            }
        }

        public QuickPasteDrawOperation( Player player, bool not )
            : base( player, not ) {
        }

        public override bool Prepare( Vector3I[] marks ) {
            return base.Prepare( new[] { marks[0], marks[0] } );
        }
    }
}

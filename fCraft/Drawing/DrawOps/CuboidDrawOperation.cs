// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>

namespace fCraft.Drawing {
    /// <summary> Draw operation that creates a simple cuboid. </summary>
    public sealed class CuboidDrawOperation : DrawOperation {
        public override string Name {
            get { return "Cuboid"; }
        }

        public override int ExpectedMarks {
            get { return 2; }
        }


        public CuboidDrawOperation(Player player)
            : base(player) { }


        public override bool Prepare(Vector3I[] marks) {
            if (!base.Prepare(marks))
                return false;
            BlocksTotalEstimate = Bounds.Volume;
            Coords = Bounds.MinVertex;
            return true;
        }


        public override int DrawBatch(int maxBlocksToDraw) {
            return DrawBatchWithinBounds(maxBlocksToDraw);
        }
    }
}

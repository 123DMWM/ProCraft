// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>

using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Structure representing a pending update to the map's block array.
    /// Contains information about the block coordinates, type, and change's origin. </summary>
    public struct BlockUpdate {
        /// <summary> Player who initiated the block change. Can be null. </summary>
        [CanBeNull] public readonly Player Origin;

        public readonly ushort X, Y, Z;

        /// <summary> Type of block to set at the given coordinates. </summary>
        public readonly Block BlockType;

        public BlockUpdate( Player origin, ushort x, ushort y, ushort z, Block blockType ) {
            Origin = origin;
            X = x;
            Y = y;
            Z = z;
            BlockType = blockType;
        }

        public BlockUpdate( Player origin, Vector3I coord, Block blockType ) {
            Origin = origin;
            X = (ushort)coord.X;
            Y = (ushort)coord.Y;
            Z = (ushort)coord.Z;
            BlockType = blockType;
        }
    }
}
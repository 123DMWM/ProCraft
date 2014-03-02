// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System.Runtime.InteropServices;

namespace fCraft
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PhysicsUpdate
    {
        public PhysicsUpdate(int x, int y, int z, Block oldBlock, byte delay)
        {
            X = (short)x;
            Y = (short)y;
            Z = (short)z;
            OldBlock = oldBlock;
            Delay = delay;
        }

        public readonly short X, Y, Z;
        public readonly Block OldBlock;
        public byte Delay;
    }
}
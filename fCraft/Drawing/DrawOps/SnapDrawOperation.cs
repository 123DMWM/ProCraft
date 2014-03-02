using System;
using System.Collections.Generic;

namespace fCraft.Drawing
{
    public sealed class SnapDrawOperation : DrawOperation
    {
        public override string Name
        {
            get { return "Snap"; }
        }

        public override int ExpectedMarks
        {
            get { return 2; }
        }

        public SnapDrawOperation(Player player)
            : base(player)
        {
        }


        public override bool Prepare(Vector3I[] marks)
        {
            if (!base.Prepare(marks)) return false;

            Vector3I diff = new Vector3I(marks[1] - marks[0]);
            foreach (Axis a in Enum.GetValues(typeof(Axis)))
                marks[1][a] = (Math.Abs(diff[a]) > Math.Abs(diff[diff.LongestAxis]) >> 1) ?
                   marks[0][a] + Math.Abs(diff[diff.LongestAxis]) * Math.Sign(diff[a]) :
                   marks[0][a];

            BlocksTotalEstimate = Math.Max(Bounds.Width, Math.Max(Bounds.Height, Bounds.Length));

            coordEnumerator = LineEnumerator(marks[0], marks[1]).GetEnumerator();
            return true;
        }


        IEnumerator<Vector3I> coordEnumerator;
        public override int DrawBatch(int maxBlocksToDraw)
        {
            int blocksDone = 0;
            while (coordEnumerator.MoveNext())
            {
                Coords = coordEnumerator.Current;
                if (DrawOneBlock())
                {
                    blocksDone++;
                    if (blocksDone >= maxBlocksToDraw) return blocksDone;
                }
                if (TimeToEndBatch) return blocksDone;
            }
            IsDone = true;
            return blocksDone;
        }
    }
}
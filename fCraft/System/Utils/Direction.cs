using System;

namespace fCraft {
    public enum Direction {
        None, PlusX, MinusX, PlusZ, MinusZ
    }

    public static class DirectionFinder  {
        public static Direction GetDirection(Vector3I[] marks) {
            int lenX = Math.Abs(marks[1].X - marks[0].X);
            int lenY = Math.Abs(marks[1].Y - marks[0].Y);
            
            if (lenX > lenY) {
                return marks[0].X < marks[1].X ? Direction.PlusX : Direction.MinusX;
            } else if (lenX < lenY) {
                return marks[0].Y < marks[1].Y ? Direction.PlusZ : Direction.MinusZ;
            } else {
                return Direction.None;
            }
        }
    }
}
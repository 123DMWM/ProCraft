﻿// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System.Drawing;

namespace fCraft.GUI {
    public sealed class IsoCatResult {
        internal IsoCatResult( bool cancelled, Bitmap bitmap, Rectangle cropRectangle ) {
            Cancelled = cancelled;
            Bitmap = bitmap;
            CropRectangle = cropRectangle;
        }

        public bool Cancelled { get; private set; }
        public Bitmap Bitmap { get; private set; }
        public Rectangle CropRectangle { get; set; }
    }
}
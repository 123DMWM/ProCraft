// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus>
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    sealed class CustomPictureBox : PictureBox {
        protected override void OnPaint( PaintEventArgs pe ) {
            if( Image != null ) {
                pe.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                if( Image.Height * 3 > Height || Image.Width * 3 > Width ) {
                    pe.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                } else {
                    pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                }
            }
            base.OnPaint( pe );
        }
    }
}
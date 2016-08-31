using System;
using System.Drawing;

/*        ----
        Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com>
        All rights reserved.

        Redistribution and use in source and binary forms, with or without
        modification, are permitted provided that the following conditions are met:
         * Redistributions of source code must retain the above copyright
              notice, this list of conditions and the following disclaimer.
            * Redistributions in binary form must reproduce the above copyright
             notice, this list of conditions and the following disclaimer in the
             documentation and/or other materials provided with the distribution.
            * Neither the name of 800Craft or the names of its
             contributors may be used to endorse or promote products derived from this
             software without specific prior written permission.

        THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
        ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
        WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
        DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
        DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
        (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
        LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
        ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
        (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
        SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
        ----*/
namespace fCraft {
    public class FontHandler : BitmapDrawOp {

        //instance
        public FontHandler(Block block, Vector3I[] marks, Player p, Direction dir) {
            direction = dir;
            blockCount = 0;
            player = p;
            origin = marks[0];
            blockColor = block;
            undoState = player.DrawBegin( null );
        }

        public void CreateGraphicsAndDraw(string text) {
            SizeF size = MeasureTextSize(text, player.font );
            Bitmap img = new Bitmap((int)size.Width, (int)size.Height);
            
            using (Graphics g = Graphics.FromImage(img)) {
                g.FillRectangle(Brushes.White, 0, 0, img.Width, img.Height); //make background, else crop will not work
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit; //fix to bleeding
                g.DrawString(text, player.font, Brushes.Black, 0, 0);
                
                img = Crop(img); //crop the image to fix all problems with location
                img.RotateFlip(RotateFlipType.Rotate180FlipX); //flip this badboy
                Draw(img);
                img.Dispose();
            }
        }

        //Measure the size of the string length using IDisposable. Backport from 800Craft Client
        public static SizeF MeasureTextSize(string text, Font font) {
            using (Bitmap bmp = new Bitmap(1, 1))
                using (Graphics g = Graphics.FromImage(bmp))
            {
                return g.MeasureString(text, font);
            }
        }
    }
}

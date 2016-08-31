using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

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
    public abstract class BitmapDrawOp {
        protected Player player;
        protected Direction direction;
        public int blockCount; //blockcount for player message. ++ when drawing
        protected int blocks = 0, //drawn blocks
        blocksDenied = 0; //denied blocks (zones, ect)
        protected fCraft.Drawing.UndoState undoState;
        protected Vector3I origin;
        protected Block blockColor;
        
        public void Draw(Bitmap img) {
            //guess how big the draw will be
            int count = 0, white = System.Drawing.Color.White.ToArgb();
            for (int y = 0; y < img.Height; y++)
                for (int x = 0; x < img.Width; x++)
            {
                if (img.GetPixel(x, y).ToArgb() == white) continue;
                count++;
            }
            
            //check if player can make the drawing
            if (!player.CanDraw(count )) {
                player.Message(String.Format("You are only allowed to run commands that affect up to {0} blocks. " +
                                             "This one would affect {1} blocks.",
                                             player.Info.Rank.DrawLimit, count));
                return;
            }
            
            int dirX = 0, dirY = 0;
            if (direction == Direction.PlusX) dirX = 1;
            if (direction == Direction.MinusX) dirX = -1;
            if (direction == Direction.PlusZ) dirY = 1;
            if (direction == Direction.MinusZ) dirY = -1;
            if (dirX == 0 && dirY == 0) return; //if blockcount = 0, message is shown and returned
            
            for (int z = 0; z < img.Height; z++)
                for (int i = 0; i < img.Width; i++)
            {
                if (img.GetPixel(i, z).ToArgb() == white) continue;
                
                Vector3I coords = new Vector3I(origin.X + dirX * i, origin.Y + dirY * i, origin.Z + z);
                BuildingCommands.DrawOneBlock(
                    player, player.World.Map, blockColor,
                    coords, BlockChangeContext.Drawn,
                    ref blocks, ref blocksDenied, undoState);
                blockCount++;
            }
        }

        public static Bitmap Crop(Bitmap bmp) {
            int w = bmp.Width;
            int h = bmp.Height;
            Func<int, bool> allWhiteRow = row => {
                for (int i = 0; i < w; i++) {
                    if (bmp.GetPixel(i, row).R != 255)
                        return false;
                }
                return true;
            };
            Func<int, bool> allWhiteColumn = col => {
                for (int i = 0; i < h; i++) {
                    if (bmp.GetPixel(col, i).R != 255)
                        return false;
                }
                return true;
            };
            
            int topmost = 0, bottommost = 0;
            for (int row = 0; row < h; row++) {
                if (allWhiteRow(row)) {
                    topmost = row;
                } else { break; }
            }
            for (int row = h - 1; row >= 0; row--) {
                if (allWhiteRow(row)) {
                    bottommost = row;
                } else { break; }
            }
            
            int leftmost = 0, rightmost = 0;
            for (int col = 0; col < w; col++) {
                if (allWhiteColumn(col)) {
                    leftmost = col;
                } else { break; }
            }
            for (int col = w - 1; col >= 0; col--) {
                if (allWhiteColumn(col)) {
                    rightmost = col;
                } else { break; }
            }
            
            if ( rightmost == 0 ) rightmost = w; // As reached left
            if ( bottommost == 0 ) bottommost = h; // As reached top.
            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;
            if ( croppedWidth == 0 ) {// No border on left or right
                leftmost = 0;
                croppedWidth = w;
            }
            if ( croppedHeight == 0 ) {// No border on top or bottom
                topmost = 0;
                croppedHeight = h;
            }
            
            try {
                Bitmap cropped = new Bitmap( croppedWidth, croppedHeight );
                using (Graphics g = Graphics.FromImage( cropped )) {
                    g.DrawImage(bmp,
                                new RectangleF( 0, 0, croppedWidth, croppedHeight ),
                                new RectangleF( leftmost, topmost, croppedWidth, croppedHeight ),
                                GraphicsUnit.Pixel);
                }
                return cropped;
            } catch {
                return bmp; //return original image, I guess
            }
        }
    }
}

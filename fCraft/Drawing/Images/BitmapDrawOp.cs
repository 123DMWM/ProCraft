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
            int white = System.Drawing.Color.White.ToArgb();
            int left, right, top, bottom;
            int count = Crop(img, out left, out right, out top, out bottom);
            
            //check if player can make the drawing
            if (!player.CanDraw(count)) {
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
            
            for (int yy = top; yy <= bottom; yy++)
                for (int xx = left; xx <= right; xx++)
            {
                if (img.GetPixel(xx, yy).ToArgb() == white) continue;
                int dx = xx - left, dy = bottom - yy;
                
                Vector3I coords = new Vector3I(origin.X + dirX * dx, origin.Y + dirY * dx, origin.Z + dy);
                BuildingCommands.DrawOneBlock(
                    player, player.World.Map, blockColor,
                    coords, BlockChangeContext.Drawn,
                    ref blocks, ref blocksDenied, undoState);
                blockCount++;
            }
        }

        static int Crop(Bitmap bmp, 
                        out int left, out int right, out int top, out int bottom) {
            int count = 0, white = System.Drawing.Color.White.ToArgb();
            left = bmp.Width; right = 0;
            top = bmp.Height; bottom = 0;
            
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
            {
                if (bmp.GetPixel(x, y).ToArgb() == white) continue;
                count++;
                left = Math.Min(x, left);
                right = Math.Max(x, right);
                top = Math.Min(y, top);
                bottom = Math.Max(y, bottom);
            }
            return count;
        }
    }
}

﻿// Copyright 2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Drawing;
using System.IO;
using System.Net;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    internal sealed class ImageDrawOperation : DrawOpWithBrush, IDisposable {
        static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(6);

        public Uri ImageUrl { get; private set; }
        public Bitmap ImageBitmap { get; private set; }
        public BlockPalette Palette { get; private set; }

        Block[] drawBlocks;

        int imageX, imageY;
        int layer;
        Vector3I coordOffsets = Vector3I.Zero;
        Vector3I layerVector = Vector3I.Zero;
        int coordMultiplierX, coordMultiplierY;

        int actualImageHeight, actualImageWidth;
        int minY, maxY;
        int minX, maxX;

        public override int ExpectedMarks {
            get { return 2; }
        }

        public override string Name {
            get { return "Image"; }
        }

        public override string Description {
            get {
                if (ImageUrl == null) return Name;
                string fileName = ImageUrl.IsFile
                                      ? Path.GetFileName(ImageUrl.AbsolutePath)
                                      : ImageUrl.AbsolutePath;
                return String.Format("{0}({1}, {2})", Name, fileName, Palette.Name);
            }
        }


        public ImageDrawOperation([NotNull] Player player)
            : base(player) { }


        public ImageDrawOperation([NotNull] Player player, [NotNull] BlockPalette palette, [NotNull] Uri imageUrl)
            : base(player) {
            if (palette == null)
                throw new ArgumentNullException("palette");
            if (imageUrl == null)
                throw new ArgumentNullException("imageUrl");
            Palette = palette;
            ImageUrl = imageUrl;
        }


        public ImageDrawOperation([NotNull] Player player, [NotNull] BlockPalette palette, [NotNull] Bitmap bitmap)
            : base(player) {
            if (palette == null)
                throw new ArgumentNullException("palette");
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");
            Palette = palette;
            ImageBitmap = bitmap;
        }


        public override bool ReadParams(CommandReader cmd) {
            // get image URL
            string urlString = cmd.Next();
            if (urlString.NullOrWhiteSpace()) return false;

            if (urlString.StartsWith("http://imgur.com/")) {
                urlString = "http://i.imgur.com/" + urlString.Substring("http://imgur.com/".Length) + ".png";
            }
            // if string starts with "++", load image from imgur
            if (urlString.StartsWith("++")) {
                urlString = "http://i.imgur.com/" + urlString.Substring(2) + ".png";
            }
            // prepend the protocol, if needed (assume http)
            if (!urlString.CaselessStarts("http://") && !urlString.CaselessStarts("https://")) {
                urlString = "http://" + urlString;
            }
            if (!urlString.CaselessStarts("http://i.imgur.com")) {
                Player.Message("For safety reasons we only accept images uploaded to &9http://imgur.com/ &SSorry for this inconvenience.");
                return false;
            }
            if (!urlString.CaselessEnds(".png") && !urlString.CaselessEnds(".jpg") && !urlString.CaselessEnds(".gif")) {
                Player.Message("URL must be a link to an image");
                return false;
            }

            // validate the image URL
            Uri url;
            if (!Uri.TryCreate(urlString, UriKind.Absolute, out url)) {
                Player.Message("DrawImage: Invalid URL given.");
                return false;
            } else if (!url.Scheme.Equals(Uri.UriSchemeHttp) && !url.Scheme.Equals(Uri.UriSchemeHttps)) {
                Player.Message("DrawImage: Invalid URL given. Only HTTP and HTTPS links are allowed.");
                return false;
            }
            ImageUrl = url;

            // Check if player gave optional second argument (palette name)
            string paletteName = cmd.Next();
            if (paletteName != null) {
                Palette = BlockPalette.FindPalette(paletteName);
                if (Palette == null) {
                    Player.Message("DrawImage: Unrecognized palette \"{0}\". Available palettes are: \"{1}\"",
                                   paletteName, BlockPalette.Palettes.JoinToString(pal => pal.Name));
                    return false;
                }
            } else {
                // default to "Light" (lit single-layer) palette
                Palette = BlockPalette.FindPalette("Light");
            }

            // All set
            return true;
        }


        public override bool Prepare(Vector3I[] marks) {
            // Check the given marks
            if (marks == null)
                throw new ArgumentNullException("marks");
            if (marks.Length != 2) {
                throw new ArgumentException("DrawImage: Exactly 2 marks needed.", "marks");
            }

            // Make sure that a direction was given
            Vector3I delta = marks[1] - marks[0];
            if (Math.Abs(delta.X) == Math.Abs(delta.Y)) {
                throw new ArgumentException(
                    "DrawImage: Second mark must specify a definite direction " +
                    "(north, east, south, or west) from first mark.",
                    "marks");
            }
            Marks = marks;

            // Download the image
            if (ImageBitmap == null) {
                if (ImageUrl == null) {
                    throw new InvalidOperationException(
                        "Either ImageBitmap or ImageUrl must be set before calling Prepare()");
                }
                
                HttpWebRequest request = HttpUtil.CreateRequest(ImageUrl, DownloadTimeout);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    // Check that the remote file was found. The ContentType
                    // check is performed since a request for a non-existent
                    // image file might be redirected to a 404-page, which would
                    // yield the StatusCode "OK", even though the image was not found.
                    if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Moved ||
                         response.StatusCode == HttpStatusCode.Redirect) &&
                        response.ContentType.CaselessStarts("image")) {
                        // if the remote file was found, download it
                        using (Stream inputStream = response.GetResponseStream()) {
                            // TODO: check file size limit?
                            ImageBitmap = new Bitmap(inputStream);
                        }
                    } else {
                        throw new Exception("Error downloading image: " + response.StatusCode);
                    }
                }
            }

            Vector3I endCoordOffset = CalculateCoordConversion(delta);

            // Calculate maximum bounds, and warn if we're pushing out of the map
            BoundingBox fullBounds = new BoundingBox(Marks[0], Marks[0] + endCoordOffset);
            if (fullBounds.XMin < 0 || fullBounds.XMax > Map.Width - 1) {
                Player.Message("&WDrawImage: Not enough room horizontally (X), image cut off.");
            }
            if (fullBounds.YMin < 0 || fullBounds.YMax > Map.Length - 1) {
                Player.Message("&WDrawImage: Not enough room horizontally (Y), image cut off.");
            }
            if (fullBounds.ZMin < 0 || fullBounds.ZMax > Map.Height - 1) {
                Player.Message("&WDrawImage: Not enough room vertically, image cut off.");
            }

            // clip bounds to world boundaries
            Bounds = Map.Bounds.GetIntersection(fullBounds);
            BlocksTotalEstimate = Bounds.Volume;

            // set starting coordinate
            imageX = minX;
            imageY = minY;
            layer = 0;

            Brush = this;
            return true;
        }


        Vector3I CalculateCoordConversion(Vector3I delta) {
            Vector3I endCoordOffset = Vector3I.Zero;

            // Figure out vertical drawing direction
            if (delta.Z < 0) {
                // drawing downwards
                actualImageHeight = Math.Min(Marks[0].Z + 1, ImageBitmap.Height);
                minY = 0;
                maxY = actualImageHeight - 1;
                coordOffsets.Z = Marks[0].Z;
            } else {
                // drawing upwards
                actualImageHeight = Math.Min(Marks[0].Z + ImageBitmap.Height, Map.Height) - Marks[0].Z;
                minY = ImageBitmap.Height - actualImageHeight;
                maxY = ImageBitmap.Height - 1;
                coordOffsets.Z = (actualImageHeight - 1) + Marks[0].Z;
            }

            // Figure out horizontal drawing direction and orientation
            if (Math.Abs(delta.X) > Math.Abs(delta.Y)) {
                // drawing along the X-axis
                bool faceTowardsOrigin = delta.Y < 0 || delta.Y == 0 && Marks[0].Y < Map.Length / 2;
                coordOffsets.Y = Marks[0].Y;
                if (delta.X > 0) {
                    // X+
                    actualImageWidth = Math.Min(Marks[0].X + ImageBitmap.Width, Map.Width) - Marks[0].X;
                    if (faceTowardsOrigin) {
                        // X+y+
                        minX = ImageBitmap.Width - actualImageWidth;
                        maxX = ImageBitmap.Width - 1;
                        coordOffsets.X = Marks[0].X + (actualImageWidth - 1);
                        coordMultiplierX = -1;
                        layerVector.Y = -1;
                    } else {
                        // X+y-
                        minX = 0;
                        maxX = actualImageWidth - 1;
                        coordOffsets.X = Marks[0].X;
                        coordMultiplierX = 1;
                        layerVector.Y = 1;
                    }
                } else {
                    // X-
                    actualImageWidth = Math.Min(Marks[0].X + 1, ImageBitmap.Width);
                    if (faceTowardsOrigin) {
                        // X-y+
                        minX = 0;
                        maxX = actualImageWidth - 1;
                        coordOffsets.X = Marks[0].X;
                        coordMultiplierX = -1;
                        layerVector.Y = -1;
                    } else {
                        // X-y-
                        minX = ImageBitmap.Width - actualImageWidth;
                        maxX = ImageBitmap.Width - 1;
                        coordOffsets.X = Marks[0].X - (actualImageWidth - 1);
                        coordMultiplierX = 1;
                        layerVector.Y = 1;
                    }
                }
            } else {
                // drawing along the Y-axis
                bool faceTowardsOrigin = delta.X < 0 || delta.X == 0 && Marks[0].X < Map.Width / 2;
                coordOffsets.X = Marks[0].X;
                if (delta.Y > 0) {
                    // Y+
                    actualImageWidth = Math.Min(Marks[0].Y + ImageBitmap.Width, Map.Length) - Marks[0].Y;
                    if (faceTowardsOrigin) {
                        // Y+x+
                        minX = 0;
                        maxX = actualImageWidth - 1;
                        coordOffsets.Y = Marks[0].Y;
                        coordMultiplierY = 1;
                        layerVector.X = -1;
                    } else {
                        // Y+x-
                        minX = ImageBitmap.Width - actualImageWidth;
                        maxX = ImageBitmap.Width - 1;
                        coordOffsets.Y = Marks[0].Y + (actualImageWidth - 1);
                        coordMultiplierY = -1;
                        layerVector.X = 1;
                    }
                } else {
                    // Y-
                    actualImageWidth = Math.Min(Marks[0].Y + 1, ImageBitmap.Width);
                    if (faceTowardsOrigin) {
                        // Y-x+
                        minX = ImageBitmap.Width - actualImageWidth;
                        maxX = ImageBitmap.Width - 1;
                        coordOffsets.Y = Marks[0].Y - (actualImageWidth - 1);
                        coordMultiplierY = 1;
                        layerVector.X = -1;
                    } else {
                        // Y-x-
                        minX = 0;
                        maxX = actualImageWidth - 1;
                        coordOffsets.Y = Marks[0].Y;
                        coordMultiplierY = -1;
                        layerVector.X = 1;
                    }
                }
            }
            return endCoordOffset;
        }


        public override int DrawBatch(int maxBlocksToDraw) {
            int blocksDone = 0;
            //byte sid = 255;
            for (; imageX <= maxX; imageX++) {
                for (; imageY <= maxY; imageY++) {
                    // find matching palette entry
                    System.Drawing.Color color = ImageBitmap.GetPixel(imageX, imageY);
                    drawBlocks = Palette.FindBestMatch(color);
                    Coords.Z = coordOffsets.Z - (imageY - minY);
                    // draw layers
                    for (; layer < Palette.Layers; layer++) {
                        Coords.X = (imageX - minX) * coordMultiplierX + coordOffsets.X + layerVector.X * layer;
                        Coords.Y = (imageX - minX) * coordMultiplierY + coordOffsets.Y + layerVector.Y * layer;
                        if (DrawOneBlock()) {
                            blocksDone++;
                            /*BoundingBox placetobuild = new BoundingBox(Coords.X, Coords.Y, Coords.Z, Coords.X, Coords.Y, Coords.Z);                    
                            string rtn = "" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
                            Block newBlock = Player.WorldMap.GetBlock(placetobuild.XMin, placetobuild.YMin, placetobuild.ZMin);
                            if (newBlock != Block.Air)
                            {
                                Player.Send(Packet.MakeMakeSelection(sid, "DrawImage" + sid, placetobuild, rtn, 255));
                                if (sid == 0) {
                                    sid = 255;
                                } else {
                                    sid--;
                                }
                            }*/ 
                            if (blocksDone >= maxBlocksToDraw) {
                                layer++;
                                return blocksDone;
                            }
                        }
                    }
                    layer = 0;
                }
                imageY = minY;
                if (TimeToEndBatch) {
                    imageX++;
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }


        protected override Block NextBlock() {
            return drawBlocks[layer];
        }


        public void Dispose() {
            if (ImageBitmap != null) {
                ImageBitmap.Dispose();
                ImageBitmap = null;
            }
        }
    }
}

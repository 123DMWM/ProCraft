﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using System.Linq;
using fCraft.Drawing;
using fCraft.MapConversion;
using System.Collections.Generic;
using JetBrains.Annotations;
using RandomMaze;
using System.Drawing;

namespace fCraft {
    /// <summary> Commands for placing specific blocks (solid, water, grass),
    /// and switching block placement modes (paint, bind). </summary>
    static class BuildingCommands {

        public static int MaxUndoCount = 2000000;
        public const int MaxCalculationExceptions = 100;

        const string GeneralDrawingHelp = " Use &H/Cancel&S to cancel selection mode. " +
                                          "Use &H/Undo&S to stop and undo the last command.";
        internal static void Init() {
            CommandManager.RegisterCommand( CdBind );
            CommandManager.RegisterCommand( CdGrass );
            CommandManager.RegisterCommand( CdSolid );
            CommandManager.RegisterCommand( CdLava );
            CommandManager.RegisterCommand( CdWater );
            CommandManager.RegisterCommand( CdPaint );
            CommandManager.RegisterCommand( CdTree );
            CommandManager.RegisterCommand( CdCancel );
            CommandManager.RegisterCommand( CdMark );
            CommandManager.RegisterCommand( CdMarkAll );
            CommandManager.RegisterCommand( CdDoNotMark );
            CommandManager.RegisterCommand( CdUndo );
            CommandManager.RegisterCommand( CdRedo );
            CommandManager.RegisterCommand( CdReplace );
            CommandManager.RegisterCommand( CdReplaceNot );
            CommandManager.RegisterCommand( CdReplaceBrush );
            CommandManager.RegisterCommand( CdReplaceNotBrush );
            CdReplace.Help += GeneralDrawingHelp;
            CdReplaceNot.Help += GeneralDrawingHelp;
            CdReplaceBrush.Help += GeneralDrawingHelp;
            CdReplaceNotBrush.Help += GeneralDrawingHelp;
            CommandManager.RegisterCommand( CdCopySlot );
            CommandManager.RegisterCommand( CdCopy );
            CommandManager.RegisterCommand( CdCut );
            CommandManager.RegisterCommand( CdPaste );
            CommandManager.RegisterCommand( CdPasteNot );
            CommandManager.RegisterCommand( CdPasteX );
            CommandManager.RegisterCommand( CdPasteNotX );
            CommandManager.RegisterCommand( CdMirror );
            CommandManager.RegisterCommand( CdRotate );
            CdCut.Help += GeneralDrawingHelp;
            CdPaste.Help += GeneralDrawingHelp;
            CdPasteNot.Help += GeneralDrawingHelp;
            CdPasteX.Help += GeneralDrawingHelp;
            CdPasteNotX.Help += GeneralDrawingHelp;
            CommandManager.RegisterCommand( CdRestore );
            CdRestore.Help += GeneralDrawingHelp;
            CommandManager.RegisterCommand( CdCuboid );
            CommandManager.RegisterCommand( CdCuboidWireframe );
            CommandManager.RegisterCommand( CdCuboidHollow );
            CdCuboid.Help += GeneralDrawingHelp;
            CdCuboidHollow.Help += GeneralDrawingHelp;
            CdCuboidWireframe.Help += GeneralDrawingHelp;
            CommandManager.RegisterCommand( CdEllipsoid );
            CommandManager.RegisterCommand( CdEllipsoidHollow );
            CdEllipsoid.Help += GeneralDrawingHelp;
            CdEllipsoidHollow.Help += GeneralDrawingHelp;
            CommandManager.RegisterCommand( CdLine );
            CommandManager.RegisterCommand(CdSnap);
            CdSnap.Help += GeneralDrawingHelp;
            CommandManager.RegisterCommand( CdTriangle );
            CommandManager.RegisterCommand( CdTriangleWireframe );
            CdLine.Help += GeneralDrawingHelp;
            CdTriangle.Help += GeneralDrawingHelp;
            CdTriangleWireframe.Help += GeneralDrawingHelp;
            CommandManager.RegisterCommand( CdSphere );
            CommandManager.RegisterCommand( CdSphereHollow );
            CommandManager.RegisterCommand( CdTorus );
            CdSphere.Help += GeneralDrawingHelp;
            CdSphereHollow.Help += GeneralDrawingHelp;
            CdTorus.Help += GeneralDrawingHelp;
            CommandManager.RegisterCommand( CdFill2D );
            CommandManager.RegisterCommand( CdFill3D );
            CdFill2D.Help += GeneralDrawingHelp;
            CdFill3D.Help += GeneralDrawingHelp;
            CommandManager.RegisterCommand( CdUndoArea );
            CommandManager.RegisterCommand( CdUndoPlayer );
            CommandManager.RegisterCommand( CdHighlight );
            CommandManager.RegisterCommand( CdUndoAreaNot );
            CommandManager.RegisterCommand( CdUndoPlayerNot );
            CdUndoArea.Help += GeneralDrawingHelp;
            CdUndoAreaNot.Help += GeneralDrawingHelp;
            CommandManager.RegisterCommand( CdStatic );
            CommandManager.RegisterCommand( CdWalls );
            CommandManager.RegisterCommand( CdPlace );
            CommandManager.RegisterCommand( CdDisPlace );
            CommandManager.RegisterCommand( CdCenter );
            CommandManager.RegisterCommand( CdMazeCuboid );
            CommandManager.RegisterCommand( CdWrite );
            CommandManager.RegisterCommand( CdDraw2D );
            CommandManager.RegisterCommand( CdSetFont );
            CommandManager.RegisterCommand( CdDrawImage );
            CommandManager.RegisterCommand( CdPlane );
            CommandManager.RegisterCommand( CdPlaneW );
            CommandManager.RegisterCommand( CdOverlay );
            CommandManager.RegisterCommand(CdReplaceAll);
            CommandManager.RegisterCommand(CdSnake);
        }
        #region Cuboid
        static readonly CommandDescriptor CdCuboid = new CommandDescriptor {
            Name = "Cuboid",
            Aliases = new[] { "blb", "c", "z" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Fills a rectangular area (cuboid) with blocks.",
            Handler = CuboidHandler
        };

        static void CuboidHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new CuboidDrawOperation( player ) );
        }

        static readonly CommandDescriptor CdCuboidWireframe = new CommandDescriptor {
            Name = "CuboidW",
            Aliases = new[] { "cubw", "cw", "bfb" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Draws a wireframe box (a frame) around the selected rectangular area.",
            Handler = CuboidWireframeHandler
        };

        static void CuboidWireframeHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new CuboidWireframeDrawOperation( player ) );
        }
        
        static readonly CommandDescriptor CdCuboidHollow = new CommandDescriptor {
            Name = "CuboidH",
            Aliases = new[] { "cubh", "ch", "h", "bhb" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Surrounds the selected rectangular area with a box of blocks. " +
                   "Unless two blocks are specified, leaves the inside untouched.",
            Handler = CuboidHollowHandler
        };

        static readonly CommandDescriptor CdOverlay = new CommandDescriptor
        {
            Name = "Overlay",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Covers blocks that are underneath air.",
            Handler = overlayHandler
        };

        static void overlayHandler(Player player, CommandReader cmd)
        {
            DrawOperationBegin(player, cmd, new OverlayDrawOperation(player));
        }

        static void CuboidHollowHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new CuboidHollowDrawOperation( player ) );
        }
        #endregion
        #region plane
        private static readonly CommandDescriptor CdPlane = new CommandDescriptor
        {
            Name = "Plane",
            Aliases = new[] { "Quad" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Draws a plane between three points.",
            Handler = PlaneHandler
        };

        private static void PlaneHandler(Player player, CommandReader cmd)
        {
            DrawOperationBegin(player, cmd, new PlaneDrawOperation(player));
        }

        private static readonly CommandDescriptor CdPlaneW = new CommandDescriptor
        {
            Name = "PlaneW",
            Aliases = new[] { "QuadW" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Draws a wireframe plane between four points.",
            Handler = PlaneWHandler
        };

        private static void PlaneWHandler(Player player, CommandReader cmd)
        {
            DrawOperationBegin(player, cmd, new PlaneWireframeDrawOperation(player));
        }
        #endregion
        #region Ellipsoid
        static readonly CommandDescriptor CdEllipsoid = new CommandDescriptor {
            Name = "Ellipsoid",
            Aliases = new[] { "e" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Fills an ellipsoid-shaped area (elongated sphere) with blocks.",
            Handler = EllipsoidHandler
        };

        static void EllipsoidHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new EllipsoidDrawOperation( player ) );
        }
        static readonly CommandDescriptor CdEllipsoidHollow = new CommandDescriptor {
            Name = "EllipsoidH",
            Aliases = new[] { "eh" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Surrounds the selected an ellipsoid-shaped area (elongated sphere) with a shell of blocks.",
            Handler = EllipsoidHollowHandler
        };

        static void EllipsoidHollowHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new EllipsoidHollowDrawOperation( player ) );
        }
        #endregion
        #region sphere
        static readonly CommandDescriptor CdSphere = new CommandDescriptor {
            Name = "Sphere",
            Aliases = new[] { "sp", "spheroid" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help = "Fills a spherical area with blocks. " +
                   "The first mark denotes the CENTER of the sphere, and " +
                   "distance to the second mark denotes the radius.",
            Handler = SphereHandler
        };

        static void SphereHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new SphereDrawOperation( player ) );
        }

        static readonly CommandDescriptor CdSphereHollow = new CommandDescriptor {
            Name = "SphereH",
            Aliases = new[] { "sph", "hsphere" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help = "Surrounds a spherical area with a shell of blocks. " +
                   "The first mark denotes the CENTER of the sphere, and " +
                   "distance to the second mark denotes the radius.",
            Handler = SphereHollowHandler
        };

        static void SphereHollowHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new SphereHollowDrawOperation( player ) );
        }
        #endregion
        #region line
        static readonly CommandDescriptor CdLine = new CommandDescriptor {
            Name = "Line",
            Aliases = new[] { "ln" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Draws a continuous line between two points with blocks. " +
                   "Marks do not have to be aligned.",
            Handler = LineHandler
        };

        static void LineHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new LineDrawOperation( player ) );
        }
        #endregion
        #region Snap
        static readonly CommandDescriptor CdSnap = new CommandDescriptor
        {
            Name = "Snap",
            Aliases = new[] { "LineSnap" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Draws a line along the axis or at 45degrees thereof." +
               "Second mark automatically snaps into position.",
            Handler = SnapHandler
        };

        static void SnapHandler(Player player, CommandReader cmd)
        {
            DrawOperationBegin(player, cmd, new SnapDrawOperation(player));
        }
        #endregion
        #region triangle
        static readonly CommandDescriptor CdTriangleWireframe = new CommandDescriptor {
            Name = "TriangleW",
            Aliases = new[] { "tw" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Draws lines between three points, to form a triangle.",
            Handler = TriangleWireframeHandler
        };

        static void TriangleWireframeHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new TriangleWireframeDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdTriangle = new CommandDescriptor {
            Name = "Triangle",
            Aliases = new[] { "t" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Draws a triangle between three points.",
            Handler = TriangleHandler
        };

        static void TriangleHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new TriangleDrawOperation( player ) );
        }
        #endregion
        #region torus
        static readonly CommandDescriptor CdTorus = new CommandDescriptor {
            Name = "Torus",
            Aliases = new[] { "donut", "bagel" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help = "Draws a horizontally-oriented torus. The first mark denotes the CENTER of the torus, horizontal " +
                   "distance to the second mark denotes the ring radius, and the vertical distance to the second mark denotes the " +
                   "tube radius",
            Handler = TorusHandler
        };

        static void TorusHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new TorusDrawOperation( player ) );
        }
        #endregion
        #region drawoperation
        static void DrawOperationBegin( Player player, CommandReader cmd, DrawOperation op ) {
            // try to create instance of player's currently selected brush
            // all command parameters are passed to the brush
            IBrush brush = player.ConfigureBrush(cmd);

            // MakeInstance returns null if there were problems with syntax, abort
            if( brush == null ) return;
            op.Brush = brush;
            player.SelectionStart( op.ExpectedMarks, DrawOperationCallback, op, Permission.Draw );
            player.Message( "{0}: Click or &H/Mark&S {1} blocks.",
                            op.Description, op.ExpectedMarks );
        }


        static void DrawOperationCallback( Player player, Vector3I[] marks, object tag ) {
            DrawOperation op = (DrawOperation)tag;
            if( !op.Prepare( marks ) ) return;
            if( !player.Info.ClassicubeVerified ) {
                player.Message("As you had an older minecraft.net account, you must have an admin verify your " +
                               "new classicube.net account actually is you with /verify before you can use drawing commands.");
                op.Cancel();
                return;
            }
            if( !player.CanDraw( op.BlocksTotalEstimate ) ) {
                player.Message( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   op.Bounds.Volume );
                op.Cancel();
                return;
            }
            player.Message( "{0}: Processing ~{1} blocks.",
                            op.Description, op.BlocksTotalEstimate );
            op.Begin();
        }
        #endregion
        #region SetFont
        static CommandDescriptor CdSetFont = new CommandDescriptor()
        {
            Name = "SetFont",
            Aliases = new[] { "FontSet", "Font", "Sf" },
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new Permission[] { Permission.Draw },
            IsConsoleSafe = false,
            Help = "Sets the properties for /Write, such as: font and size",
            Handler = SetFontHandler,
            Usage = "/SetFont < Font | Size | Reset | Style> <Variable>"
        };

        static void SetFontHandler(Player player, CommandReader cmd) {
            string arg = cmd.Next();
            if (arg == null) {
                CdSetFont.PrintUsage(player); return;
            }
            
            if (arg.CaselessEquals("reset")) {
                player.font = new Font("Times New Roman", 20, FontStyle.Regular);
                player.Message("SetFont: Font reverted back to default ({0} size {1})",
                    player.font.FontFamily.Name, player.font.Size);
            } else if (arg.CaselessEquals("font")) {
                HandleFont(player, cmd);
            } else if (arg.CaselessEquals("size")) {
                int size = -1;
                if (cmd.NextInt(out size)) {
                    if (size < 5) {
                        player.Message("&WIncorrect font size ({0}): Size needs to be at least 5" +
                                       "(which is ideal for minecraft font, not the others)", size);
                        return;
                    }
                    player.Message("SetFont: Size changed from {0} to {1} ({2})", player.font.Size, size, player.font.FontFamily.Name);
                    player.font = new System.Drawing.Font(player.font.FontFamily, size);
                } else {
                    player.Message("&WInvalid size, use /SetFont Size FontSize. Example: /SetFont Size 14");
                }
            } else if (arg.CaselessEquals("style")) {
                string style = cmd.Next();
                if (style == null) {
                    CdSetFont.PrintUsage(player);
                } else if (style.CaselessEquals("italic")) {
                    player.font = new System.Drawing.Font(player.font, FontStyle.Italic);
                    player.Message("SetFont: Style set to Italic ({0})", player.font.FontFamily.Name);
                } else if (style.CaselessEquals("bold")) {
                    player.font = new System.Drawing.Font(player.font, FontStyle.Bold);
                    player.Message("SetFont: Style set to Bold ({0})", player.font.FontFamily.Name);
                } else if (style.CaselessEquals("regular")) {
                    player.font = new System.Drawing.Font(player.font, FontStyle.Regular);
                    player.Message("SetFont: Style set to Regular ({0})", player.font.FontFamily.Name);
                } else {
                    player.Message("Style must be: Italic, Bold, or Regular");
                }
            } else {
                CdSetFont.PrintUsage(player);
            }
        }
        
        static void HandleFont(Player player, CommandReader cmd) {
            string name = cmd.NextAll();
            if (!Directory.Exists(Paths.FontsPath)) {
                Directory.CreateDirectory(Paths.FontsPath);
                player.Message("There are no fonts available for this server. " +
                               "Font is set to default: {0}", player.font.FontFamily.Name);
                return;
            }
            
            string[] available = GetAvailableFonts();
            if (available == null) {
                player.Message("No fonts have been found."); return;
            }
            if (String.IsNullOrEmpty(name)) {
                player.Message("{0} fonts Available: {1}", 
                               available.Length, available.JoinToString());
                return;
            }
            
            string match = null;
            for (int i = 0; i < available.Length; i++) {
                string font = available[i];
                if (font == null) continue;
                if (!font.CaselessStarts(name)) continue;
                
                if (font.CaselessEquals(name)) {
                    match = font; break;
                } else if (match == null) {
                    match = font;
                } else {
                    var matches = available.Where(f => f.CaselessStarts(name));
                    player.Message("Multiple font files matched \"{0}\": {1}",
                                   name, matches.JoinToString());
                    return;
                }
            }
            
            if (match != null) {
                player.Message("Your font has changed to \"{0}\":", match);
                string path = System.IO.Path.Combine(Paths.FontsPath, match + ".ttf");
                player.font = new System.Drawing.Font(player.LoadFontFamily(path), player.font.Size);
            } else {
                player.Message("No fonts found for \"{0}\". Available fonts: {1}",
                               name, available.JoinToString());
            }
        }
        
        #endregion
        #region Draw2d
        static readonly CommandDescriptor CdDraw2D = new CommandDescriptor
        {
            Name = "Draw2D",
            Aliases = new[] { "D2d" },
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help = "/Draw2D, then select a shape (Polygon, spiral, star). You can then choose a size in blocks " +
            " for the shape before selecting two points." +
            "Example: /Draw2d Polygon 50. Polygon and triangle can be used with any number of points " +
            "exceeding 3, which should follow the 'Size' argument",
            Usage = "/Draw2D <Shape> <Size> <Points> <Fill: true/false>",
            Handler = Draw2DHandler,
        };

        static void Draw2DHandler(Player player, CommandReader cmd)
        {
            string Shape = cmd.Next();
            if (Shape == null)
            {
                CdDraw2D.PrintUsage(player);
                return;
            }
            switch (Shape.ToLower())
            {
                case "polygon":
                case "star":
                case "spiral":
                    break;
                default:
                    CdDraw2D.PrintUsage(player);
                    return;
            }
            int radius = 0;
            int Points = 0;
            if (!cmd.NextInt(out radius))
            {
                radius = 20;
            }
            if (!cmd.NextInt(out Points))
            {
                Points = 5;
            }
            bool fill = true;
            if (cmd.HasNext)
            {
                if (!bool.TryParse(cmd.Next(), out fill))
                {
                    fill = true;
                }
            }
            Draw2DData tag = new Draw2DData() { Shape = Shape, Points = Points, Radius = radius, Fill = fill };
            player.Message("Draw2D({0}): Click 2 blocks or use &H/Mark&S to set direction.", Shape);
            player.SelectionStart(2, Draw2DCallback, tag, Permission.DrawAdvanced);
        }

        struct Draw2DData
        {
            public int Radius;
            public int Points;
            public string Shape;
            public bool Fill;
        }

        static void Draw2DCallback(Player player, Vector3I[] marks, object tag)
        {
            Block block = new Block();
            Draw2DData data = (Draw2DData)tag;
            int radius = data.Radius;
            int Points = data.Points;
            bool fill = data.Fill;
            string Shape = data.Shape;
            if (player.LastUsedBlockType == Block.None)
            {
                block = Block.Stone;
            }
            else
            {
                block = player.LastUsedBlockType;
            }
            Direction direction = DirectionFinder.GetDirection(marks);
            try
            {
                ShapesLib lib = new ShapesLib(block, marks, player, radius, direction);
                switch (Shape.ToLower())
                {
                    case "polygon":
                        lib.DrawRegularPolygon(Points, 18, fill);
                        break;
                    case "star":
                        lib.DrawStar(Points, radius, fill);
                        break;
                    case "spiral":
                        lib.DrawSpiral();
                        break;
                    default:
                        player.Message("&WUnknown shape");
                        CdDraw2D.PrintUsage(player);
                        lib = null;
                        return;
                }

                if (lib.blockCount > 0)
                {
                    player.Message("/Draw2D: Drawing {0} with a size of '{1}' using {2} blocks of {3}",
                                   Shape, radius, lib.blockCount, Map.GetBlockName(player.World, block));
                }
                else
                {
                    player.Message("&WNo direction was set");
                }
                lib = null; //get lost
            }
            catch (Exception e)
            {
                player.Message(e.Message);
            }
        }
        #endregion
        #region write
        static readonly CommandDescriptor CdWrite = new CommandDescriptor
        {
            Name = "Write",
            Aliases = new[] { "Text", "Wt" },
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            IsConsoleSafe = false,
            Help = "/Write Sentence, then click 2 blocks. The first is the starting point, the second is the direction",
            Usage = "/Write Sentence",
            Handler = WriteHandler,
        };

        static void WriteHandler(Player player, CommandReader cmd) {
            string sentence = cmd.NextAll();
            if (String.IsNullOrEmpty(sentence)) {
                CdWrite.PrintUsage(player);
                return;
            }
            player.Message("Write: Click 2 blocks or use &H/Mark&S to set direction.");
            player.SelectionStart(2, WriteCallback, sentence, Permission.DrawAdvanced);
        }

        static void WriteCallback(Player player, Vector3I[] marks, object tag) {
            Block block = player.LastUsedBlockType;
            if (block == Block.None) block = Block.Stone;
            Direction direction = DirectionFinder.GetDirection(marks);
            string sentence = (string)tag;
            
            try {
                FontHandler render = new FontHandler(block, marks, player, direction);
                render.CreateGraphicsAndDraw(sentence);
                if (render.blockCount > 0) {
                    player.Message("/Write (Size {0}, {1}: Writing '{2}' using {3} blocks of {4}",
                        player.font.Size, player.font.FontFamily.Name, sentence, render.blockCount,
                        Map.GetBlockName(player.World, block));
                } else {
                    player.Message("&WNo direction was set");
                }
            } catch (Exception e) {
                player.Message(e.Message);
                Logger.Log(LogType.Error, "WriteCommand: " + e);
            }
        }

        static string[] GetAvailableFonts() {
            if (!Directory.Exists(Paths.FontsPath)) return null;
            
            string[] files = Directory.GetFiles(Paths.FontsPath, "*.ttf", SearchOption.TopDirectoryOnly)
                .Select(name => System.IO.Path.GetFileNameWithoutExtension(name))
                .Where(name => !String.IsNullOrEmpty(name))
                .ToArray();
            return files.Length == 0 ? null : files;
        }
        #endregion
        #region Tree
        static readonly CommandDescriptor CdTree = new CommandDescriptor
        {
            Name = "Tree",
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Usage = "/Tree Shape Height",
            Help = "Plants a tree of given shape and height. Available shapes: Normal, Bamboo and Palm. Cone, Round, Rainforest, Mangrove",
            Handler = TreeHandler
        };

        static void TreeHandler(Player player, CommandReader cmd)
        {
            string shapeName = cmd.Next();
            int height;
            Forester.TreeShape shape;

            // that's one ugly if statement... does the job though.
            if (shapeName == null ||
                !cmd.NextInt(out height) ||
                !EnumUtil.TryParse(shapeName, out shape, true) ||
                shape == Forester.TreeShape.Stickly ||
                shape == Forester.TreeShape.Procedural)
            {
                CdTree.PrintUsage(player);
                player.Message("Available shapes: Normal, Bamboo, Palm, Cone, Round, Rainforest, Mangrove.");
                return;
            }

            if (height < 6 || height > 1024)
            {
                player.Message("Tree height must be 6 blocks or above");
                return;
            }
            int volume = (int)Math.Pow(height, 3);
            if (!player.CanDraw(volume))
            {
                player.Message(String.Format("You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.Info.Rank.DrawLimit, volume));
                return;
            }

            Map map = player.World.Map;

            ForesterArgs args = new ForesterArgs
            {
                Height = height - 1,
                Operation = Forester.ForesterOperation.Add,
                Map = map,
                Shape = shape,
                TreeCount = 1,
                RootButtresses = false,
                Roots = Forester.RootMode.None,
                Rand = new Random()
            };
            player.SelectionStart(1, TreeCallback, args, CdTree.Permissions);
            player.Message("Tree: Place a block or type /Mark to use your location.");
        }

        static void TreeCallback(Player player, Vector3I[] marks, object tag)
        {
            ForesterArgs args = (ForesterArgs)tag;
            int blocksPlaced = 0, blocksDenied = 0;
            UndoState undoState = player.DrawBegin(null);
            args.BlockPlacing +=
                (sender, e) =>
               DrawOneBlock(player, player.World.Map, e.Block, new Vector3I(e.Coords.X, e.Coords.Y, e.Coords.Z),
                              BlockChangeContext.Drawn,
                              ref blocksPlaced, ref blocksDenied, undoState);
            Forester.SexyPlant(args, marks[0]);
            DrawingFinished(player, "/Tree: Planted", blocksPlaced, blocksDenied);
        }
        #endregion
        #region walls
        private static readonly CommandDescriptor CdWalls = new CommandDescriptor
        {
            Name = "Walls",
            IsConsoleSafe = false,
            RepeatableSelection = true,
            Category = CommandCategory.New | CommandCategory.Building,
            IsHidden = false,
            Permissions = new[] { Permission.Draw },
            Help = "Fills a rectangular area of walls",
            Handler = WallsHandler
        };

        private static void WallsHandler(Player player, CommandReader cmd)
        {
            DrawOperationBegin(player, cmd, new WallsDrawOperation(player));
        }
        #endregion
        #region displace
        private static readonly CommandDescriptor CdDisPlace = new CommandDescriptor
        {
            Name = "DPlace",
            Aliases = new[] { "distanceplace", "displace", "dp" },
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/DPlace [block] [distance away]",
            Help = "Places a block a certain distance away from where you are, in the direction you are looking",
            Handler = DisPlace
        };

        private static void DisPlace(Player player, CommandReader cmd) {
            if (cmd.CountRemaining < 2) {
                CdDisPlace.PrintUsage(player);
                return;
            }
            
            Block block;            
            if (!cmd.NextBlock(player, false, out block)) return;
            int length;
            if (!cmd.NextInt(out length)) return;
            
            byte yaw = player.Position.R, pitch = player.Position.L;
            Vector3I pos = player.Position.ToBlockCoordsRaw(); pos.Y--;
            Vector3I dir = Vector3I.FlatDirection(yaw, pitch);
            pos += dir * length;
            
            pos = player.WorldMap.Bounds.Clamp(pos);
            if (player.CanPlace(player.World.Map, pos, block, BlockChangeContext.Drawn) != CanPlaceResult.Allowed) {
                player.Message("&WYou are not allowed to build here");
                return;
            }

            Player.RaisePlayerPlacedBlockEvent(player, player.WorldMap, pos, player.WorldMap.GetBlock(pos),
                                               block, BlockChangeContext.Drawn);
            BlockUpdate blockUpdate = new BlockUpdate(null, pos, block);
            player.World.Map.QueueUpdate(blockUpdate);
            player.Message("Block placed at {0} ({1} blocks away from you)", pos, length);
        }

        #endregion
        #region Place
        private static readonly CommandDescriptor CdPlace = new CommandDescriptor {
            Name = "Place",
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/Place [x] [y] [z] and/or [block]",
            Help = "Places a block at specified XYZ or directly below your feet.",
            IsConsoleSafe = true,
            Handler = PlaceHandler
        };

        private static void PlaceHandler(Player player, CommandReader cmd) {
            bool isConsole = (player == Player.Console);
            if (isConsole && cmd.Count < 6) {
                player.Message("When used by console /Place requires a world name.");
                player.Message("/Place [x] [y] [z] [block] [world]");
                return;
            }
            Block block = Block.Stone;
            if (!isConsole && player.LastUsedBlockType != Block.None)
                block = player.LastUsedBlockType;
            Vector3I coords;
            int x, y, z;
            if (cmd.NextInt(out x) && cmd.NextInt(out y) && cmd.NextInt(out z)) {
                if (cmd.HasNext && !cmd.NextBlock(player, false, out block)) return;
                
                coords = new Vector3I(x, y, z);
            } else if (isConsole) {
                player.Message("Invalid coordinates!");
                return;
            } else {
                cmd.Rewind();
                if (cmd.HasNext && !cmd.NextBlock(player, false, out block)) return;
                
                coords = new Vector3I(player.Position.BlockX, player.Position.BlockY, (player.Position.Z - 64) / 32);
            }
            World world;
            if (player == Player.Console) {
                string worldName = cmd.Next();
                if (string.IsNullOrEmpty(worldName)) {
                    player.Message("Console must specify a world!");
                }
                world = WorldManager.FindWorldOrPrintMatches(player, worldName);
                if (world == null)
                    return;
            } else {
                world = player.World;
            }
            bool unLoad = false;
            if (!world.IsLoaded) {
                world.LoadMap();
                unLoad = true;
            }
            coords = world.map.Bounds.Clamp(coords);
            
            if (player == Player.Console) {
                BlockUpdate blockUpdate = new BlockUpdate(player, coords, block);
                player.Info.ProcessBlockPlaced((byte)block);
                world.map.QueueUpdate(blockUpdate);
                Player.RaisePlayerPlacedBlockEvent(player, world.map, coords, block, world.map.GetBlock(coords), BlockChangeContext.Manual);
            } else {
                player.SendNow(Packet.MakeSetBlock(coords, block));
                player.PlaceBlockWithEvents(coords, ClickAction.Build, block);
            }
            
            if (!isConsole) 
                player.Message("{0} placed at {1}", Map.GetBlockName(player.World, block), coords);
            if (unLoad)
                world.UnloadMap(true);
        }

        #endregion
        #region center
        private static readonly CommandDescriptor CdCenter = new CommandDescriptor
        {
            Name = "Center",
            Aliases = new[] { "Centre" },
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new[] { Permission.Build },
            RepeatableSelection = true,
            Usage = "/Center",
            Help = "Places a block at the center for a chosen cuboided area",
            Handler = CenterHandler
        };

        private static void CenterHandler(Player player, CommandReader cmd) {
            player.SelectionStart(2, CenterCallback, null, CdCenter.Permissions);
            player.Message("Center: Place a block or type /Mark to use your location.");
        }

        private static void CenterCallback(Player player, Vector3I[] marks, object tag) {
            if (player.LastUsedBlockType == Block.None) {
                 player.Message("&WCannot deduce desired block. Click a block or type out the block name.");
                 return;
            }
            
            BoundingBox bounds = new BoundingBox(marks[0], marks[1]);
            int blocksDrawn = 0, blocksSkipped = 0;
            UndoState undoState = player.DrawBegin(null);

            World playerWorld = player.World;
            if (playerWorld == null)
                PlayerOpException.ThrowNoWorld(player);
            
            Vector3I[] coords = new Vector3I[8];
            int count = 0, cenX = bounds.XCentre, cenY = bounds.YCentre, cenZ = bounds.ZCentre;
            
            if ((bounds.Width % 2) == 0) {
                coords[count] = new Vector3I(cenX + 1, cenY, cenZ); count++;
            }
            if ((bounds.Width % 2) == 0 && (bounds.Length % 2) == 0) {
                coords[count] = new Vector3I(cenX + 1, cenY + 1, cenZ); count++;
            }
            if ((bounds.Width % 2) == 0 && (bounds.Height % 2) == 0) {
                coords[count] = new Vector3I(cenX + 1, cenY, cenZ + 1); count++;
            }
            if ((bounds.Width % 2) == 0 && (bounds.Length % 2) == 0 && (bounds.Height % 2) == 0) {
                coords[count] = new Vector3I(cenX + 1, cenY + 1, cenZ + 1); count++;
            }
            if ((bounds.Length % 2) == 0) {
                coords[count] = new Vector3I(cenX, cenY + 1, cenZ); count++;
            }
            if ((bounds.Length % 2) == 0 && (bounds.Height % 2) == 0) {
                coords[count] =  new Vector3I(cenX, cenY + 1, cenZ + 1); count++;
            }
            if ((bounds.Height % 2) == 0) {
                coords[count] = new Vector3I(cenX, cenY, cenZ + 1); count++;
            }            
            coords[count] = new Vector3I(cenX, cenY, cenZ); count++;
            
            for (int i = 0; i < count; i++) {
                DrawOneBlock(player, player.World.Map, player.LastUsedBlockType, coords[i],
                             BlockChangeContext.Drawn, ref blocksDrawn, ref blocksSkipped, undoState);
            }
            DrawingFinished(player, "Placed", blocksDrawn, blocksSkipped);
        }
        #endregion
        #region Fill

        static readonly CommandDescriptor CdFill2D = new CommandDescriptor
        {
            Name = "Fill2D",
            Aliases = new[] { "f2d" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help = "Fills a continuous area with blocks, in 2D. " +
                   "Takes just 1 mark, and replaces blocks of the same type as the block you clicked. " +
                   "Works similar to \"Paint Bucket\" tool in Photoshop. " +
                   "Direction of effect is determined by where the player is looking.",
            Handler = Fill2DHandler
        };

        static void Fill2DHandler(Player player, CommandReader cmd)
        {
            Fill2DDrawOperation op = new Fill2DDrawOperation(player);

            IBrush brush = player.ConfigureBrush(cmd);
            if (brush == null) return;
            op.Brush = brush;

            player.SelectionStart(1, Fill2DCallback, op, Permission.Draw);
            player.Message("{0}: Click a block to start filling.", op.Description);
        }


        static void Fill2DCallback(Player player, Vector3I[] marks, object tag)
        {
            DrawOperation op = (DrawOperation)tag;
            if (!op.Prepare(marks)) return;
            if (player.WorldMap.GetBlock(marks[0]) == Block.Air)
            {
                Logger.Log(LogType.UserActivity,
                            "Fill2D: Asked {0} to confirm replacing air on world {1}",
                            player.Name,
                    // ReSharper disable PossibleNullReferenceException
                            player.World.Name);
                // ReSharper restore PossibleNullReferenceException
                player.Confirm(Fill2DConfirmCallback, op, "{0}: Replace air?", op.Description);
            }
            else
            {
                Fill2DConfirmCallback(player, op, false);
            }
        }


        static void Fill2DConfirmCallback(Player player, object tag, bool fromConsole)
        {
            Fill2DDrawOperation op = (Fill2DDrawOperation)tag;
            int maxDim = Math.Max(op.Bounds.Width, Math.Max(op.Bounds.Length, op.Bounds.Height));
            int otherDim = op.Bounds.Volume / maxDim;
            player.Message("{0}: Filling in a {1}x{2} area...",
                            op.Description, maxDim, otherDim);
            op.Begin();
        }


        static readonly CommandDescriptor CdFill3D = new CommandDescriptor {
            Name = "Fill3D",
            Aliases = new[] { "f3d" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help = "Fills a continuous volume with blocks, in 3D. " +
                   "Takes just 1 mark, and replaces blocks of the same type as the block you clicked.",
            Handler = Fill3DHandler
        };

        static void Fill3DHandler( Player player, CommandReader cmd ) {
            Fill3DDrawOperation op = new Fill3DDrawOperation( player );

            IBrush brush = player.ConfigureBrush(cmd);
            if( brush == null ) return;
            op.Brush = brush;

            player.SelectionStart( 1, Fill3DCallback, op, Permission.Draw );
            player.Message( "{0}: Click a block to start filling.", op.Description );
        }


        static void Fill3DCallback( Player player, Vector3I[] marks, object tag ) {
            DrawOperation op = (DrawOperation)tag;
            if( !op.Prepare( marks ) ) return;
            if( player.WorldMap.GetBlock( marks[0] ) == Block.Air ) {
                Logger.Log( LogType.UserActivity,
                            "Fill3D: Asked {0} to confirm replacing air on world {1}",
                            player.Name,
                            player.World.Name );
                player.Confirm( Fill3DConfirmCallback, op, "{0}: Replace air?", op.Description );
            } else {
                Fill3DConfirmCallback( player, op, false );
            }
        }


        static void Fill3DConfirmCallback( Player player, object tag, bool fromConsole ) {
            Fill3DDrawOperation op = (Fill3DDrawOperation)tag;
            player.Message( "{0}: Filling in a {1}x{2}x{3} area...",
                            op.Description, op.Bounds.Width, op.Bounds.Length, op.Bounds.Height );
            op.Begin();
        }



        #endregion
        #region Block Commands

        static readonly CommandDescriptor CdSolid = new CommandDescriptor
        {
            Name = "Solid",
            Aliases = new[] { "S" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build, Permission.PlaceAdmincrete },
            Help =
                "Toggles the admincrete placement mode. When enabled, any stone block you place is replaced with admincrete.",
            Usage = "/Solid [on/off]",
            Handler = SolidHandler
        };

        static void SolidHandler([NotNull] Player player, [NotNull] CommandReader cmd)
        {
            bool turnSolidOn = (player.GetBind(Block.Stone) != Block.Admincrete);

            if (cmd.HasNext && !cmd.NextOnOff(out turnSolidOn))
            {
                CdSolid.PrintUsage(player);
                return;
            }

            if (turnSolidOn)
            {
                player.Bind(Block.Stone, Block.Admincrete);
                player.Message("Solid: ON. Stone blocks are replaced with admincrete.");
            }
            else
            {
                player.ResetBind(Block.Stone);
                player.Message("Solid: OFF");
            }
        }

        static readonly CommandDescriptor CdPaint = new CommandDescriptor {
            Name = "Paint",
            Aliases = new[] { "p" },
            Permissions = new[] { Permission.Build, Permission.Delete },
            Category = CommandCategory.Building,
            Help = "When paint mode is on, any block you delete will be replaced with the block you are holding. " +
                   "Paint command toggles this behavior on and off.",
            Handler = PaintHandler
        };

        static void PaintHandler( Player player, CommandReader cmd ) {
            player.IsPainting = !player.IsPainting;
            player.Message("Paint: " + (player.IsPainting ? "&2On" : "&4Off"));
        }




        static readonly CommandDescriptor CdGrass = new CommandDescriptor
        {
            Name = "Grass",
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new[] { Permission.Build },
            Usage = "/Grass",
            Help = "Toggles the grass placement mode. When enabled, any dirt block you place is replaced with a grass block.",
            Handler = GrassHandler
        };

        static void GrassHandler(Player player, CommandReader cmd)
        {
            player.GrassGrowth = !player.GrassGrowth;
            player.Message("Dirt -> Grass: " + (player.GrassGrowth ? "&2On" : "&4Off"));
        }

        static readonly CommandDescriptor CdWater = new CommandDescriptor
        {
            Name = "Water",
            Aliases = new[] { "W" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build, Permission.PlaceWater },
            Help =
                "Toggles the water placement mode. When enabled, any blue or cyan block you place is replaced with water.",
            Usage = "/Water [on/off]",
            Handler = WaterHandler
        };

        static void WaterHandler([NotNull] Player player, [NotNull] CommandReader cmd)
        {
            bool turnWaterOn = (player.GetBind(Block.Aqua) != Block.Water ||
                                player.GetBind(Block.Cyan) != Block.Water ||
                                player.GetBind(Block.Blue) != Block.Water);

            if (cmd.HasNext && !cmd.NextOnOff(out turnWaterOn))
            {
                CdWater.PrintUsage(player);
                return;
            }

            if (turnWaterOn)
            {
                player.Bind(Block.Aqua, Block.Water);
                player.Bind(Block.Cyan, Block.Water);
                player.Bind(Block.Blue, Block.Water);
                player.Message("Water: ON. Blue blocks are replaced with water.");
            }
            else
            {
                player.ResetBind(Block.Aqua, Block.Cyan, Block.Blue);
                player.Message("Water: OFF");
            }
        }


        static readonly CommandDescriptor CdLava = new CommandDescriptor
        {
            Name = "Lava",
            Aliases = new[] { "L" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build, Permission.PlaceLava },
            Help = "Toggles the lava placement mode. When enabled, any red block you place is replaced with lava.",
            Usage = "/Lava [on/off]",
            Handler = LavaHandler
        };

        static void LavaHandler([NotNull] Player player, [NotNull] CommandReader cmd)
        {
            bool turnLavaOn = (player.GetBind(Block.Red) != Block.Lava);

            if (cmd.HasNext && !cmd.NextOnOff(out turnLavaOn))
            {
                CdLava.PrintUsage(player);
                return;
            }

            if (turnLavaOn)
            {
                player.Bind(Block.Red, Block.Lava);
                player.Message("Lava: ON. Red blocks are replaced with lava.");
            }
            else
            {
                player.ResetBind(Block.Red);
                player.Message("Lava: OFF");
            }
        }
        
        static readonly CommandDescriptor CdBind = new CommandDescriptor {
            Name = "Bind",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build },
            Help = "Assigns one blocktype to another. " +
                   "Allows to build blocktypes that are not normally buildable directly: admincrete, lava, water, grass, double step. " +
                   "Calling &H/Bind BlockType&S without second parameter resets the binding. If used with no params, ALL bindings are reset.",
            Usage = "/Bind OriginalBlockType ReplacementBlockType",
            Handler = BindHandler
        };

        static void BindHandler( Player player, CommandReader cmd ) {
            if( !cmd.HasNext ) {
                player.Message( "All bindings have been reset." );
                player.ResetAllBinds();
                return;
            }

            Block originalBlock;
            if( !cmd.NextBlock( player, false, out originalBlock ) ) return;

            if( !cmd.HasNext ) {
                if( player.GetBind( originalBlock ) != originalBlock ) {
                    player.Message( "{0} is no longer bound to {1}",
                                    originalBlock,
                                    player.GetBind( originalBlock ) );
                    player.ResetBind( originalBlock );
                } else {
                    player.Message( "{0} is not bound to anything.",
                                    originalBlock );
                }
                return;
            }

            Block replacementBlock;
            if( !cmd.NextBlock( player, false, out replacementBlock ) ) return;

            if( cmd.HasNext ) {
                CdBind.PrintUsage( player );
                return;
            }

            Permission permission = Permission.Build;
            switch( replacementBlock ) {
                case Block.Grass:
                    permission = Permission.PlaceGrass;
                    break;
                case Block.Admincrete:
                    permission = Permission.PlaceAdmincrete;
                    break;
                case Block.Water:
                    permission = Permission.PlaceWater;
                    break;
                case Block.Lava:
                    permission = Permission.PlaceLava;
                    break;
            }
            if( player.Can( permission ) ) {
                player.Bind( originalBlock, replacementBlock );
                player.Message( "{0} is now replaced with {1}", originalBlock, replacementBlock );
            } else {
                player.Message( "&WYou do not have {0} permission.", permission );
            }
        }

        #endregion
        #region Replace

        static void ReplaceHandlerInternal([NotNull] IBrushFactory factory, [NotNull] Player player,
                                           [NotNull] CommandReader cmd) {
            if (factory == null)
                throw new ArgumentNullException("factory");
            if (player == null)
                throw new ArgumentNullException("player");
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            CuboidDrawOperation op = new CuboidDrawOperation(player);
            IBrush brush = factory.MakeBrush(player, cmd);
            if (brush == null) return;
            op.Brush = brush;

            player.SelectionStart(2, DrawOperationCallback, op, Permission.Draw);
            player.Message("{0}: Click or &H/Mark&S 2 blocks.", op.Brush.Description);
        }


        static readonly CommandDescriptor CdReplace = new CommandDescriptor {
            Name = "Replace",
            Aliases = new[] { "r" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection=true,
            Usage = "/Replace BlockToReplace [AnotherOne, ...] ReplacementBlock",
            Help = "Replaces all blocks of specified type(s) in an area.",
            Handler = ReplaceHandler
        };

        static void ReplaceHandler( Player player, CommandReader cmd ) {
            ReplaceHandlerInternal(ReplaceBrushFactory.Instance, player, cmd);
        }


        static readonly CommandDescriptor CdReplaceAll = new CommandDescriptor {
            Name = "ReplaceAll",
            Aliases = new[] { "ra" },
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Usage = "/Replace BlockToReplace [AnotherOne, ...] ReplacementBlock",
            Help = "Replaces all blocks of specified type(s) on the map.",
            Handler = ReplaceAllHandler
        };

        static void ReplaceAllHandler(Player player, CommandReader cmd) {
            CuboidDrawOperation op = new CuboidDrawOperation(player);
            IBrush brush = ReplaceBrushFactory.Instance.MakeBrush(player, cmd);
            if (brush == null) return;
            op.Brush = brush;

            player.SelectionStart(2, DrawOperationCallback, op, Permission.Draw);
            Map map = player.WorldMap;
            player.SelectionResetMarks();
            player.SelectionAddMark(map.Bounds.MinVertex, false, false);
            player.SelectionAddMark(map.Bounds.MaxVertex, true, true);
        }


        static readonly CommandDescriptor CdReplaceNot = new CommandDescriptor {
            Name = "ReplaceNot",
            Aliases = new[] { "rn" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Usage = "/ReplaceNot (ExcludedBlock [AnotherOne]) ReplacementBlock",
            Help = "Replaces all blocks EXCEPT specified type(s) in an area.",
            Handler = ReplaceNotHandler
        };

        static void ReplaceNotHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            ReplaceHandlerInternal(ReplaceNotBrushFactory.Instance, player, cmd);
        }


        static readonly CommandDescriptor CdReplaceBrush = new CommandDescriptor {
            Name = "ReplaceBrush",
            Aliases = new[] { "rb" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Usage = "/ReplaceBrush Block BrushName [Params]",
            Help = "Replaces all blocks of specified type(s) in an area with output of a given brush. " +
                   "See &H/Help brush&S for a list of available brushes.",
            Handler = ReplaceBrushHandler
        };

        static void ReplaceBrushHandler( Player player, CommandReader cmd ) {
            ReplaceHandlerInternal(ReplaceBrushBrushFactory.Instance, player, cmd);
        }


        static readonly CommandDescriptor CdReplaceNotBrush = new CommandDescriptor {
            Name = "ReplaceNotBrush",
            Aliases = new[] { "rnb" },
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Usage = "/ReplaceNotBrush Block BrushName [Params]",
            Help = "Replaces all blocks except the specified type in an area with output of a given brush. " +
                   "See &H/Help brush&S for a list of available brushes.",
            Handler = ReplaceNotBrushHandler
        };

        static void ReplaceNotBrushHandler(Player player, CommandReader cmd) {
            ReplaceHandlerInternal(ReplaceNotBrushBrushFactory.Instance, player, cmd);
        }
        #endregion
        #region Undo / Redo

        static readonly CommandDescriptor CdUndo = new CommandDescriptor {
            Name = "Undo",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Selectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            Handler = UndoHandler
        };

        static void UndoHandler( Player player, CommandReader cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );
            if( cmd.HasNext ) {
                player.Message( "Undo command takes no parameters. Did you mean to do &H/UndoPlayer&S or &H/UndoArea&S?" );
                return;
            }

            string msg = "Undo: ";
            UndoState undoState = player.UndoPop();
            if( undoState == null ) {
                player.Message( "There is currently nothing to undo." );
                return;
            }

            // Cancel the last DrawOp, if still in progress
            if( undoState.Op != null && !undoState.Op.IsDone && !undoState.Op.IsCancelled ) {
                undoState.Op.Cancel();
                msg += String.Format( "Cancelled {0} (was {1}% done). ",
                                     undoState.Op.Description,
                                     undoState.Op.PercentDone );
            }

            // Check if command was too massive.
            if( undoState.IsTooLargeToUndo ) {
                if( undoState.Op != null ) {
                    player.Message( "Cannot undo {0}: too massive.", undoState.Op.Description );
                } else {
                    player.Message( "Cannot undo: too massive." );
                }
                return;
            }

            // no need to set player.drawingInProgress here because this is done on the user thread
            Logger.Log( LogType.UserActivity,
                        "Player {0} initiated /Undo affecting {1} blocks (on world {2})",
                        player.Name,
                        undoState.Buffer.Count,
                        playerWorld.Name );

            msg += String.Format( "Restoring {0} blocks. Type &H/Redo&S to reverse.",
                                  undoState.Buffer.Count );
            player.Message( msg );

            var op = new UndoDrawOperation( player, undoState, false );
            op.Prepare( new Vector3I[0] );
            op.Begin();
        }


        static readonly CommandDescriptor CdRedo = new CommandDescriptor {
            Name = "Redo",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Selectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            Handler = RedoHandler
        };

        static void RedoHandler( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                CdRedo.PrintUsage( player );
                return;
            }

            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            UndoState redoState = player.RedoPop();
            if( redoState == null ) {
                player.Message( "There is currently nothing to redo." );
                return;
            }

            string msg = "Redo: ";
            if( redoState.Op != null && !redoState.Op.IsDone ) {
                redoState.Op.Cancel();
                msg += String.Format( "Cancelled {0} (was {1}% done). ",
                                     redoState.Op.Description,
                                     redoState.Op.PercentDone );
            }

            // no need to set player.drawingInProgress here because this is done on the user thread
            Logger.Log( LogType.UserActivity,
                        "{0} {1} &Sinitiated /Redo affecting {2} blocks (on world {3})",
                        player.Info.Rank.Name,
                        player.Name,
                        redoState.Buffer.Count,
                        playerWorld.Name );

            msg += String.Format( "Restoring {0} blocks. Type &H/Undo&S to reverse.",
                                  redoState.Buffer.Count );
            player.Message( msg );

            var op = new UndoDrawOperation( player, redoState, true );
            op.Prepare( new Vector3I[0] );
            op.Begin();
        }

        #endregion
        #region Copy and Paste

        static readonly CommandDescriptor CdCopySlot = new CommandDescriptor {
            Name = "CopySlot",
            Aliases = new[] { "cs" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Usage = "/CopySlot [#]",
            Help = "Selects a slot to copy to/paste from. The maximum number of slots is limited per-rank.",
            Handler = CopySlotHandler
        };

        static void CopySlotHandler( Player player, CommandReader cmd ) {
            int slotNumber;
            if( cmd.NextInt( out slotNumber ) ) {
                if( cmd.HasNext ) {
                    CdCopySlot.PrintUsage( player );
                    return;
                }
                if( slotNumber < 1 || slotNumber > player.Info.Rank.CopySlots ) {
                    player.Message( "CopySlot: Select a number between 1 and {0}", player.Info.Rank.CopySlots );
                } else {
                    player.CopySlot = slotNumber - 1;
                    CopyState info = player.GetCopyState();
                    if( info == null ) {
                        player.Message( "Selected copy slot {0} (unused).", slotNumber );
                    } else {
                        player.Message( "Selected copy slot {0}: {1} blocks from {2}, {3} old.",
                                        slotNumber, info.Blocks.Length,
                                        info.OriginWorld, DateTime.UtcNow.Subtract( info.CopyTime ).ToMiniString() );
                    }
                }
            } else {
                CopyState[] slots = player.CopyStates;
                player.Message( "Using {0} of {1} slots. Selected slot: {2}",
                                slots.Count( info => info != null ), player.Info.Rank.CopySlots, player.CopySlot + 1 );
                for( int i = 0; i < slots.Length; i++ ) {
                    if( slots[i] != null ) {
                        player.Message( "  {0}: {1} blocks from {2}, {3} old",
                                        i + 1, slots[i].Blocks.Length,
                                        slots[i].OriginWorld,
                                        DateTime.UtcNow.Subtract( slots[i].CopyTime ).ToMiniString() );
                    }
                }
            }
        }



        static readonly CommandDescriptor CdCopy = new CommandDescriptor {
            Name = "Copy",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Copy blocks for pasting. " +
                   "Used together with &H/Paste&S and &H/PasteNot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/Copy&S from.",
            Handler = CopyHandler
        };

        static void CopyHandler( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                CdCopy.PrintUsage( player );
                return;
            }
            player.SelectionStart( 2, CopyCallback, null, CdCopy.Permissions );
            player.Message( "Copy: Click or &H/Mark&S 2 blocks." );
        }


        static void CopyCallback( Player player, Vector3I[] marks, object tag ) {
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sz = Math.Min( marks[0].Z, marks[1].Z );
            int ez = Math.Max( marks[0].Z, marks[1].Z );
            BoundingBox bounds = new BoundingBox( sx, sy, sz, ex, ey, ez );

            int volume = bounds.Volume;
            if( !player.CanDraw( volume ) ) {
                player.Message( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit, volume );
                return;
            }

            // remember dimensions and orientation
            CopyState copyInfo = new CopyState( marks[0], marks[1] );

            Map map = player.WorldMap;
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int z = sz; z <= ez; z++ ) {
                        copyInfo.Blocks[x - sx, y - sy, z - sz] = map.GetBlock( x, y, z );
                    }
                }
            }

            copyInfo.OriginWorld = playerWorld.Name;
            copyInfo.CopyTime = DateTime.UtcNow;
            player.SetCopyState( copyInfo );

            player.Message( "{0} blocks copied into slot #{1}, origin at {2} corner. You can now &H/Paste",
                               volume,
                               player.CopySlot + 1,
                               copyInfo.OriginCorner );

            Logger.Log( LogType.UserActivity,
                        "{0} copied {1} blocks from world {2} (between (X:{3} Y:{4} Z:{5}) and (X:{6} Y:{7} Z:{8})).",
                        player.Name, volume, playerWorld.Name,
                        bounds.MinVertex.X, bounds.MinVertex.Y, bounds.MinVertex.Z,
                        bounds.MaxVertex.X, bounds.MaxVertex.Y, bounds.MaxVertex.Z);
        }



        static readonly CommandDescriptor CdCut = new CommandDescriptor {
            Name = "Cut",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            RepeatableSelection = true,
            Help = "Copies and removes blocks for pasting. Unless a different block type is specified, the area is filled with air. " +
                   "Used together with &H/Paste&S and &H/PasteNot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/Cut&S from.",
            Usage = "/Cut [FillBlock]",
            Handler = CutHandler
        };

        static void CutHandler( Player player, CommandReader cmd ) {
            Block fillBlock = Block.Air;
            if( cmd.HasNext ) {
                if( !cmd.NextBlock( player, false, out fillBlock ) ) return;
                if( cmd.HasNext ) {
                    CdCut.PrintUsage( player );
                    return;
                }
            }

            CutDrawOperation op = new CutDrawOperation( player ) {
                Brush = new NormalBrush( fillBlock )
            };

            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw );
            player.Message( "{0}: Click 2 or &H/Mark&S 2 blocks.",
                            op.Description );
        }


        static readonly CommandDescriptor CdMirror = new CommandDescriptor {
            Name = "Mirror",
            Aliases = new[] { "Flip" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Flips copied blocks along specified axis/axes. " +
                   "The axes are: X = horizontal (east-west), Y = horizontal (north-south), Z = vertical. " +
                   "You can mirror more than one axis at a time, e.g. &H/Mirror X Y",
            Usage = "/Mirror [X] [Y] [Z]",
            Handler = MirrorHandler
        };


        static void MirrorHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            CopyState originalInfo = player.GetCopyState();
            if (originalInfo == null) {
                player.Message("Nothing to flip! Copy something first.");
                return;
            }

            // clone to avoid messing up any paste-in-progress
            CopyState info = new CopyState(originalInfo);

            bool flipX = false,
                 flipY = false,
                 flipH = false;
            string axis;
            while ((axis = cmd.Next()) != null) {
                foreach (char c in axis.ToLower()) {
                    if (c == 'x')
                        flipX = true;
                    if (c == 'y')
                        flipY = true;
                    if (c == 'z')
                        flipH = true;
                }
            }

            if (!flipX && !flipY && !flipH) {
                CdMirror.PrintUsage(player);
                return;
            }

            Block block;

            if (flipX) {
                int left = 0;
                int right = info.Bounds.Width - 1;
                while (left < right) {
                    for (int y = info.Bounds.Length - 1; y >= 0; y--) {
                        for (int z = info.Bounds.Height - 1; z >= 0; z--) {
                            block = info.Blocks[left, y, z];
                            info.Blocks[left, y, z] = info.Blocks[right, y, z];
                            info.Blocks[right, y, z] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if (flipY) {
                int left = 0;
                int right = info.Bounds.Length - 1;
                while (left < right) {
                    for (int x = info.Bounds.Width - 1; x >= 0; x--) {
                        for (int z = info.Bounds.Height - 1; z >= 0; z--) {
                            block = info.Blocks[x, left, z];
                            info.Blocks[x, left, z] = info.Blocks[x, right, z];
                            info.Blocks[x, right, z] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if (flipH) {
                int left = 0;
                int right = info.Bounds.Height - 1;
                while (left < right) {
                    for (int x = info.Bounds.Width - 1; x >= 0; x--) {
                        for (int y = info.Bounds.Length - 1; y >= 0; y--) {
                            block = info.Blocks[x, y, left];
                            info.Blocks[x, y, left] = info.Blocks[x, y, right];
                            info.Blocks[x, y, right] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            List<string> axes = new List<string>(3);
            if (flipX) axes.Add("X (east/west)");
            if (flipY) axes.Add("Y (north/south)");
            if (flipH) axes.Add("Z (vertical)");
            
            if (axes.Count == 3)
                player.Message("Flipped copy along all axes.");
            else if (axes.Count == 2)
                player.Message("Flipped copy along {0} and {1} axes.", axes[0], axes[1]);
            else
                player.Message("Flipped copy along {0} axis.", axes[0]);

            player.SetCopyState(info);
        }



        static readonly CommandDescriptor CdRotate = new CommandDescriptor {
            Name = "Rotate",
            Aliases = new[] { "spin" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Rotates copied blocks around specifies axis/axes. If no axis is given, rotates around Z (vertical).",
            Usage = "/Rotate (-90|90|180|270) (X|Y|Z)",
            Handler = RotateHandler
        };

        static void RotateHandler( Player player, CommandReader cmd ) {
            CopyState originalInfo = player.GetCopyState();
            if( originalInfo == null ) {
                player.Message( "Nothing to rotate! Copy something first." );
                return;
            }

            int degrees;
            if( !cmd.NextInt( out degrees ) || (degrees != 90 && degrees != -90 && degrees != 180 && degrees != 270) ) {
                CdRotate.PrintUsage( player );
                return;
            }

            string axisName = cmd.Next();
            Axis axis = Axis.Z;
            if( axisName != null ) {
                switch( axisName.ToLower() ) {
                    case "x":
                        axis = Axis.X;
                        break;
                    case "y":
                        axis = Axis.Y;
                        break;
                    case "z":
                    case "h":
                        axis = Axis.Z;
                        break;
                    default:
                        CdRotate.PrintUsage( player );
                        return;
                }
            }

            // allocate the new buffer
            Block[, ,] oldBuffer = originalInfo.Blocks;
            Block[, ,] newBuffer;

            if( degrees == 180 ) {
                newBuffer = new Block[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 2 )];

            } else if( axis == Axis.X ) {
                newBuffer = new Block[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 )];

            } else if( axis == Axis.Y ) {
                newBuffer = new Block[oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 )];

            } else { // axis == Axis.Z
                newBuffer = new Block[oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 )];
            }

            // clone to avoid messing up any paste-in-progress
            CopyState info = new CopyState( originalInfo, newBuffer );

            // construct the rotation matrix
            int[,] matrix = new[,]{
                {1,0,0},
                {0,1,0},
                {0,0,1}
            };

            int a, b;
            switch( axis ) {
                case Axis.X:
                    a = 1;
                    b = 2;
                    break;
                case Axis.Y:
                    a = 0;
                    b = 2;
                    break;
                default:
                    a = 0;
                    b = 1;
                    break;
            }

            switch( degrees ) {
                case 90:
                    matrix[a, a] = 0;
                    matrix[b, b] = 0;
                    matrix[a, b] = -1;
                    matrix[b, a] = 1;
                    break;
                case 180:
                    matrix[a, a] = -1;
                    matrix[b, b] = -1;
                    break;
                case -90:
                case 270:
                    matrix[a, a] = 0;
                    matrix[b, b] = 0;
                    matrix[a, b] = 1;
                    matrix[b, a] = -1;
                    break;
            }

            // apply the rotation matrix
            for( int x = oldBuffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = oldBuffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    for( int z = oldBuffer.GetLength( 2 ) - 1; z >= 0; z-- ) {
                        int nx = (matrix[0, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[0, 0] > 0 ? x : 0)) +
                                 (matrix[0, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[0, 1] > 0 ? y : 0)) +
                                 (matrix[0, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[0, 2] > 0 ? z : 0));
                        int ny = (matrix[1, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[1, 0] > 0 ? x : 0)) +
                                 (matrix[1, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[1, 1] > 0 ? y : 0)) +
                                 (matrix[1, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[1, 2] > 0 ? z : 0));
                        int nz = (matrix[2, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[2, 0] > 0 ? x : 0)) +
                                 (matrix[2, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[2, 1] > 0 ? y : 0)) +
                                 (matrix[2, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[2, 2] > 0 ? z : 0));
                        newBuffer[nx, ny, nz] = oldBuffer[x, y, z];
                    }
                }
            }

            player.Message( "Rotated copy (slot {0}) by {1} degrees around {2} axis.",
                            info.Slot + 1, degrees, axis );
            player.SetCopyState( info );
        }



        static readonly CommandDescriptor CdPasteX = new CommandDescriptor {
            Name = "PasteX",
            Aliases = new[] { "px" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            RepeatableSelection = true,
            Help = "Pastes previously copied blocks, aligned. Used together with &H/Copy&S command. " +
                   "If one or more optional IncludedBlock parameters are specified, ONLY pastes blocks of specified type(s). " +
                   "Takes 2 marks: first sets the origin of pasting, and second sets the direction where to paste.",
            Usage = "/PasteX [IncludedBlock [AnotherOne etc]]",
            Handler = PasteXHandler
        };

        static void PasteXHandler( Player player, CommandReader cmd ) {
            PasteOpHandler( player, cmd, 2, new PasteDrawOperation( player, false ) );
        }



        static readonly CommandDescriptor CdPasteNotX = new CommandDescriptor {
            Name = "PasteNotX",
            Aliases = new[] { "pnx", "pxn" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            RepeatableSelection = true,
            Help = "Pastes previously copied blocks, aligned, except the given block type(s). " +
                    "Used together with &H/Copy&S command. " +
                   "Takes 2 marks: first sets the origin of pasting, and second sets the direction where to paste.",
            Usage = "/PasteNotX ExcludedBlock [AnotherOne etc]",
            Handler = PasteNotXHandler
        };

        static void PasteNotXHandler( Player player, CommandReader cmd ) {
            PasteOpHandler( player, cmd, 2, new PasteDrawOperation( player, true ) );
        }



        static readonly CommandDescriptor CdPaste = new CommandDescriptor {
            Name = "Paste",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            RepeatableSelection = true,
            Help = "Pastes previously copied blocks. Used together with &H/Copy&S command. " +
                   "If one or more optional IncludedBlock parameters are specified, ONLY pastes blocks of specified type(s). " +
                   "Alignment semantics are... complicated.",
            Usage = "/Paste [IncludedBlock [AnotherOne etc]]",
            Handler = PasteHandler
        };

        static void PasteHandler( Player player, CommandReader cmd ) {
            PasteOpHandler( player, cmd, 1, new QuickPasteDrawOperation( player, false ) );
        }




        static readonly CommandDescriptor CdPasteNot = new CommandDescriptor {
            Name = "PasteNot",
            Aliases = new[] { "pn" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            RepeatableSelection = true,
            Help = "Pastes previously copied blocks, except the given block type(s). " +
                   "Used together with &H/Copy&S command. " +
                   "Alignment semantics are... complicated.",
            Usage = "/PasteNot ExcludedBlock [AnotherOne etc]",
            Handler = PasteNotHandler
        };

        static void PasteNotHandler( Player player, CommandReader cmd ) {
            PasteOpHandler( player, cmd, 1, new QuickPasteDrawOperation( player, true ) );
        }


        static void PasteOpHandler( Player player, CommandReader cmd, int expectedMarks, DrawOpWithBrush op ) {
            if( !op.ReadParams( cmd ) ) return;
            player.SelectionStart( expectedMarks, DrawOperationCallback, op, Permission.Draw, Permission.CopyAndPaste );
            CopyState copyInfo = player.GetCopyState();
            if( copyInfo != null ) {
                player.Message( "{0}: Click or &H/Mark&S the {1} corner.",
                                   op.Description, copyInfo.OriginCorner );
            } else {
                player.Message( "{0}: Click or &H/Mark&S a block.",
                                   op.Description );
            }
        }

        #endregion
        #region Restore

        const BlockChangeContext RestoreContext = BlockChangeContext.Drawn | BlockChangeContext.Restored;


        static readonly CommandDescriptor CdRestore = new CommandDescriptor {
            Name = "Restore",
            Category = CommandCategory.World,
            Permissions = new[] {
                Permission.Draw,
                Permission.DrawAdvanced,
                Permission.CopyAndPaste,
                Permission.ManageWorlds
            },
            RepeatableSelection = true,
            Usage = "/Restore FileName",
            Help = "Selectively restores/pastes part of mapfile into the current world. "+
                   "Map file must have the same dimensions as the current world. " +
                   "If the file name contains spaces, surround it with quote marks.",
            Handler = RestoreHandler
        };

        static void RestoreHandler( Player player, CommandReader cmd ) {
            string fileName = cmd.Next();
            if( fileName == null ) {
                CdRestore.PrintUsage( player );
                return;
            }
            if( cmd.HasNext ) {
                CdRestore.PrintUsage( player );
                return;
            }

            string fullFileName = WorldManager.FindMapFile( player, fileName );
            if( fullFileName == null ) return;

            Map map;
            if( !MapUtility.TryLoad( fullFileName, out map ) ) {
                player.Message( "Could not load the given map file ({0})", fileName );
                return;
            }

            Map playerMap = player.WorldMap;
            if( playerMap.Width != map.Width || playerMap.Length != map.Length || playerMap.Height != map.Height ) {
                player.Message( "Mapfile dimensions must match your current world's dimensions ({0}x{1}x{2})",
                                playerMap.Width,
                                playerMap.Length,
                                playerMap.Height );
                return;
            }

            map.Metadata["ProCraft.Temp", "FileName"] = fullFileName;
            player.SelectionStart( 2, RestoreCallback, map, CdRestore.Permissions );
            player.Message( "Restore: Click or &H/Mark&S 2 blocks." );
        }


        static void RestoreCallback( Player player, Vector3I[] marks, object tag ) {
            BoundingBox selection = new BoundingBox( marks[0], marks[1] );
            Map map = (Map)tag;

            if( !player.CanDraw( selection.Volume ) ) {
                player.Message(
                    "You are only allowed to restore up to {0} blocks at a time. This would affect {1} blocks.",
                    player.Info.Rank.DrawLimit,
                    selection.Volume );
                return;
            }

            int blocksDrawn = 0,
                blocksSkipped = 0;
            UndoState undoState = player.DrawBegin( null );

            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );
            Map playerMap = player.WorldMap;
            for( int x = selection.XMin; x <= selection.XMax; x++ ) {
                for( int y = selection.YMin; y <= selection.YMax; y++ ) {
                    for( int z = selection.ZMin; z <= selection.ZMax; z++ ) {
                        DrawOneBlock( player, playerMap, map.GetBlock( x, y, z ), new Vector3I( x, y, z ),
                                      RestoreContext,
                                      ref blocksDrawn, ref blocksSkipped, undoState );
                    }
                }
            }

            Logger.Log( LogType.UserActivity,
                        "{0} restored {1} blocks on world {2} (@{3},{4},{5} - {6},{7},{8}) from file {9}.",
                        player.Name, blocksDrawn,
                        playerWorld.Name,
                        selection.XMin, selection.YMin, selection.ZMin,
                        selection.XMax, selection.YMax, selection.ZMax,
                        map.Metadata["ProCraft.Temp", "FileName"] );

            DrawingFinished( player, "Restored", blocksDrawn, blocksSkipped );
        }

        #endregion
        #region Mark, Cancel

        static readonly CommandDescriptor CdMark = new CommandDescriptor {
            Name = "Mark",
            Aliases = new[] { "m" },
            Category = CommandCategory.Building,
            Usage = "/Mark&S or &H/Mark X Y Z",
            Help = "When making a selection (for drawing or zoning) use this to make a marker at your position in the world. " +
                   "If three numbers are given, those coordinates are used instead.",
            Handler = MarkHandler
        };

        static void MarkHandler( Player player, CommandReader cmd ) {
            Map map = player.WorldMap;
            int x, y, z;
            Vector3I coords;
            if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out z ) ) {
                if( cmd.HasNext ) {
                    CdMark.PrintUsage( player );
                    return;
                }
                coords = new Vector3I( x, y, z );
            } else {
                coords = player.Position.ToBlockCoords();
            }
            coords = map.Bounds.Clamp(coords);

            if( player.SelectionMarksExpected > 0 ) {
                player.SelectionAddMark( coords, true, true );
            } else {
                player.Message( "Cannot mark - no selection in progress." );
            }
        }

        static readonly CommandDescriptor CdMarkAll = new CommandDescriptor {
            Name = "MarkAll",
            Aliases = new[] { "ma", },
            Category = CommandCategory.New | CommandCategory.Building,
            Help = "When making a selection (for drawing or zoning) use this to mark the whole world.",
            Handler = MarkAllHandler
        };

        static void MarkAllHandler(Player player, CommandReader cmd) {
            Map map = player.WorldMap;
            Vector3I coordsMin = map.Bounds.MinVertex;
            Vector3I coordsMax = map.Bounds.MaxVertex;

            if (player.IsMakingSelection) {
                player.SelectionResetMarks();
                player.SelectionAddMark(coordsMin, false, false);
                player.SelectionAddMark(coordsMax, true, true);
            } else {
                player.Message("No selection in progress");
            }
        }

        static readonly CommandDescriptor CdDoNotMark = new CommandDescriptor {
            Name = "DoNotMark",
            Aliases = new[] { "dontmark", "dm", "dnm" },
            Category = CommandCategory.Building,
            Usage = "/DoNotMark",
            Help = "Toggles whether clicking blocks adds to a selection.",
            Handler = DoNotMarkHandler
        };

        static void DoNotMarkHandler( Player player, CommandReader cmd ) {
            player.DisableClickToMark = !player.DisableClickToMark;
            player.Message("Click to /Mark: " + (!player.DisableClickToMark ? "&2enabled" : "&4disabled"));
        }


        static readonly CommandDescriptor CdCancel = new CommandDescriptor {
            Name = "Cancel",
            Aliases = new[] { "Nvm" },
            Category = CommandCategory.Building | CommandCategory.Chat,
            NotRepeatable = true,
            Help = "If you are writing a partial/multiline message, it's cancelled. " +
                   "Otherwise, cancels current selection (for drawing or zoning).",
            Handler = CancelHandler
        };

        static void CancelHandler( Player player, CommandReader cmd ) {
            throw new NotSupportedException( "/Cancel handler may not be used directly. Use Player.SelectionCancel() instead." );
        }

        #endregion
        #region UndoPlayer and UndoArea

        sealed class BlockDBUndoArgs {
            public Player Player;
            public PlayerInfo[] Targets;
            public World World;
            public int CountLimit;
            public TimeSpan AgeLimit;
            public BlockDBEntry[] Entries;
            public BoundingBox Area;
            public bool Not;
        }


        // parses and checks command parameters (for both UndoPlayer and UndoArea)
        [CanBeNull]
        static BlockDBUndoArgs ParseBlockDBUndoParams( Player player, CommandReader cmd, CommandDescriptor cmdDesc, bool not ) {
            // check if command's being called by a worldless player (e.g. console)
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            // ensure that BlockDB is enabled
            if( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&W{0}: BlockDB is disabled on this server.", cmdDesc.Name );
                return null;
            }
            if( !playerWorld.BlockDB.IsEnabled ) {
                player.Message( "&W{0}: BlockDB is disabled in this world.", cmdDesc.Name );
                return null;
            }
            string action = cmdDesc == CdHighlight ? "highlight" : "undo";

            // parse the first parameter - either numeric or time limit
            string range = cmd.Next();
            if( range == null ) {
                cmdDesc.PrintUsage( player );
                return null;
            }
            int countLimit;
            TimeSpan ageLimit = TimeSpan.Zero;
            if( !Int32.TryParse( range, out countLimit ) && !range.TryParseMiniTimespan( out ageLimit ) ) {
                player.Message( "{0}: First parameter should be a number or a timespan.", cmdDesc.Name );
                return null;
            }
            if( ageLimit > DateTimeUtil.MaxTimeSpan ) {
                player.MessageMaxTimeSpan();
                return null;
            }

            // parse second and consequent parameters (player names)
            HashSet<PlayerInfo> targets = new HashSet<PlayerInfo>();
            bool allPlayers = false;
            while( true ) {
                string name = cmd.Next();
                if( name == null ) {
                    break;
                } else if( name == "*" ) {
                    // all players
                    if( not ) {
                        player.Message( "{0}: \"*\" not allowed (cannot {1} \"everyone except everyone\")", cmdDesc.Name, action );
                        return null;
                    }
                    if( allPlayers ) {
                        player.Message( "{0}: \"*\" was listed twice.", cmdDesc.Name );
                        return null;
                    }
                    allPlayers = true;

                } else {
                    // individual player
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, name, SearchOptions.IncludeSelf);
                    if( target == null ) {
                        return null;
                    }
                    if( targets.Contains( target ) ) {
                        player.Message( "{0}: Player {1}&S was listed twice.",
                                        target.ClassyName, cmdDesc.Name );
                        return null;
                    }
                    // make sure player has the permission
                    if( !not &&
                        player.Info != target && !player.Can( Permission.UndoAll ) &&
                        !player.Can( Permission.UndoOthersActions, target.Rank ) ) {
                        player.Message( "&W{0}: You may only {1} actions of players ranked {2}&S or lower.",
                                        cmdDesc.Name, action,
                                        player.Info.Rank.GetLimit( Permission.UndoOthersActions ).ClassyName );
                        player.Message( "Player {0}&S is ranked {1}",
                                        target.ClassyName, target.Rank.ClassyName );
                        return null;
                    }
                    targets.Add( target );
                }
            }
            if( targets.Count == 0 && !allPlayers ) {
                player.Message( "{0}: Specify at least one player name, or \"*\" to {1} everyone.", cmdDesc.Name, action );
                return null;
            }
            if( targets.Count > 0 && allPlayers ) {
                player.Message( "{0}: Cannot mix player names and \"*\".", cmdDesc.Name );
                return null;
            }

            // undoing everyone ('*' in place of player name) requires UndoAll permission
            if( ( not || allPlayers ) && !player.Can( Permission.UndoAll ) ) {
                player.MessageNoAccess( Permission.UndoAll );
                return null;
            }

            // Queue UndoPlayerCallback to run
            return new BlockDBUndoArgs {
                Player = player,
                AgeLimit = ageLimit,
                CountLimit = countLimit,
                Area = player.WorldMap.Bounds,
                World = playerWorld,
                Targets = targets.ToArray(),
                Not = not
            };
        }


        // called after player types "/ok" to the confirmation prompt.
        static void BlockDBUndoConfirmCallback( Player player, object tag, bool fromConsole ) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)tag;
            string cmdName = ( args.Area == null ? "UndoArea" : "UndoPlayer" );
            if( args.Not ) cmdName += "Not";

            // Produce 
            Vector3I[] coords;
            if( args.Area != null ) {
                coords = new[] { args.Area.MinVertex, args.Area.MaxVertex };
            } else {
                coords = new Vector3I[0];
            }

            // Produce a brief param description for BlockDBDrawOperation
            string description = BlockDBDescription( args );

            // start undoing (using DrawOperation infrastructure)
            var op = new BlockDBDrawOperation( player, cmdName, description, coords.Length );
            op.Prepare( coords, args.Entries );

            // log operation
            string targetList = BlockDBTargetList(args);
            Logger.Log( LogType.UserActivity,
                        "{0}: Player {1} will undo {2} changes (limit of {3}) by {4} on world {5}",
                        cmdName,
                        player.Name,
                        args.Entries.Length,
                        args.CountLimit == 0 ? args.AgeLimit.ToMiniString() : args.CountLimit.ToStringInvariant(),
                        targetList,
                        args.World.Name );

            op.Begin();
        }
        
        
        static string BlockDBDescription( BlockDBUndoArgs args ) {
            if( args.CountLimit > 0 ) {
                if( args.Targets.Length == 0 ) {
                    return args.CountLimit.ToStringInvariant();
                } else if( args.Not ) {
                    return String.Format( "{0} by everyone except {1}",
                                         args.CountLimit,
                                         args.Targets.JoinToString( p => p.Name ) );
                } else {
                    return String.Format( "{0} by {1}",
                                         args.CountLimit,
                                         args.Targets.JoinToString( p => p.Name ) );
                }
            } else {
                if( args.Targets.Length == 0 ) {
                    return args.AgeLimit.ToMiniString();
                } else if( args.Not ) {
                    return String.Format( "{0} by everyone except {1}",
                                         args.AgeLimit.ToMiniString(),
                                         args.Targets.JoinToString( p => p.Name ) );
                } else {
                    return String.Format( "{0} by {1}",
                                         args.AgeLimit.ToMiniString(),
                                         args.Targets.JoinToString( p => p.Name ) );
                }
            }
        }
        
        
        static string BlockDBTargetList( BlockDBUndoArgs args ) {
            if( args.Targets.Length == 0 ) {
                return "EVERYONE";
            } else if( args.Not ) {
                return "EVERYONE except " + args.Targets.JoinToClassyString();
            } else {
                return args.Targets.JoinToClassyString();
            }
        }

        
        static void UndoLookup( BlockDBUndoArgs args, string cmdName, string action, 
                               BlockDBEntry[] changes, ConfirmationCallback callback ) {
            // stop if there's nothing to undo
            if( changes.Length == 0 ) {
                args.Player.Message( "{0}: Found nothing to {1}.", cmdName, action );
                return;
            }
            Logger.Log( LogType.UserActivity,
                       "{0}: Asked {1} to confirm {3} on world {2}",
                       cmdName, args.Player.Name, args.World.Name, action );
            args.Entries = changes;
            
            string targetList = BlockDBTargetList( args );
            action = action.UppercaseFirst();
            
            if( args.CountLimit > 0 ) {
                args.Player.Confirm( callback, args,
                                    "{2} last {0} changes made here by {1}&S?",
                                    changes.Length, targetList, action );
            } else {
                args.Player.Confirm( callback, args,
                                    "{3} changes ({0}) made here by {1}&S in the last {2}?",
                                    changes.Length, targetList, args.AgeLimit.ToMiniString(), action );
            }            
        }
        
        
        #region UndoArea

        static readonly CommandDescriptor CdUndoArea = new CommandDescriptor {
            Name = "UndoArea",
            Aliases = new[] { "ua" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions },
            RepeatableSelection = true,
            Usage = "/UndoArea (TimeSpan|BlockCount) PlayerName [AnotherName]",
            Help = "Reverses changes made by the given player(s). " +
                   "Applies to a selected area in the current world. " +
                   "More than one player name can be given at a time. " +
                   "Players with UndoAll permission can use '*' in place of player name to undo everyone's changes at once.",
            Handler = UndoAreaHandler
        };

        static void UndoAreaHandler( Player player, CommandReader cmd ) {
            BlockDBUndoArgs args = ParseBlockDBUndoParams( player, cmd, CdUndoArea, false );
            if( args == null ) return;

            Permission permission;
            if( args.Targets.Length == 0 ) {
                permission = Permission.UndoAll;
            } else {
                permission = Permission.UndoOthersActions;
            }
            player.SelectionStart( 2, UndoAreaSelectionCallback, args, permission );
            player.Message( "UndoArea: Click or &H/Mark&S 2 blocks." );
        }


        static readonly CommandDescriptor CdUndoAreaNot = new CommandDescriptor {
            Name = "UndoAreaNot",
            Aliases = new[] { "uan", "una" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions, Permission.UndoAll },
            RepeatableSelection = true,
            Usage = "/UndoAreaNot (TimeSpan|BlockCount) PlayerName [AnotherName]",
            Help = "Reverses changes made by everyone EXCEPT the given player(s). " +
                   "Applies to a selected area in the current world. " +
                   "More than one player name can be given at a time.",
            Handler = UndoAreaNotHandler
        };

        static void UndoAreaNotHandler( Player player, CommandReader cmd ) {
            BlockDBUndoArgs args = ParseBlockDBUndoParams( player, cmd, CdUndoAreaNot, true );
            if( args == null ) return;

            player.SelectionStart( 2, UndoAreaSelectionCallback, args, CdUndoAreaNot.Permissions );
            player.Message( "UndoAreaNot: Click or &H/Mark&S 2 blocks." );
        }


        // Queues UndoAreaLookup to run in the background
        static void UndoAreaSelectionCallback( Player player, Vector3I[] marks, object tag ) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)tag;
            args.Area = new BoundingBox( marks[0], marks[1] );
            Scheduler.NewBackgroundTask( UndoAreaLookup )
                     .RunOnce( args, TimeSpan.Zero );
        }


        // Looks up the changes in BlockDB and prints a confirmation prompt. Runs on a background thread.
        static void UndoAreaLookup( SchedulerTask task ) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)task.UserState;
            string cmdName = ( args.Not ? "UndoAreaNot" : "UndoArea" );
            BlockDBEntry[] changes = UndoAreaGetChanges(args);
            UndoLookup( args, cmdName, "undo", 
                       changes, BlockDBUndoConfirmCallback );
        }
        

        static BlockDBEntry[] UndoAreaGetChanges( BlockDBUndoArgs args ) {
            if( args.CountLimit > 0 ) {
                // count-limited lookup
                if( args.Targets.Length == 0 ) {
                    return args.World.BlockDB.Lookup( args.CountLimit, args.Area );
                } else {
                    return args.World.BlockDB.Lookup( args.CountLimit, args.Area, args.Targets, args.Not );
                }
            } else {
                // time-limited lookup
                if( args.Targets.Length == 0 ) {
                    return args.World.BlockDB.Lookup( Int32.MaxValue, args.Area, args.AgeLimit );
                } else {
                    return args.World.BlockDB.Lookup( Int32.MaxValue, args.Area, args.Targets, args.Not, args.AgeLimit );
                }
            }
        }

        #endregion


        #region UndoPlayer

        static readonly CommandDescriptor CdUndoPlayer = new CommandDescriptor {
            Name = "UndoPlayer",
            Aliases = new[] { "up", "undox" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions },
            Usage = "/UndoPlayer (TimeSpan|BlockCount) PlayerName [AnotherName]",
            Help = "Reverses changes made by a given player in the current world. " +
                   "More than one player name can be given at a time. " +
                   "Players with UndoAll permission can use '*' in place of player name to undo everyone's changes at once.",
            Handler = UndoPlayerHandler
        };

        static void UndoPlayerHandler( Player player, CommandReader cmd ) {
            BlockDBUndoArgs args = ParseBlockDBUndoParams( player, cmd, CdUndoPlayer, false );
            if( args == null ) return;
            Scheduler.NewBackgroundTask( UndoPlayerLookup )
                     .RunOnce( args, TimeSpan.Zero );
        }


        static readonly CommandDescriptor CdUndoPlayerNot = new CommandDescriptor {
            Name = "UndoPlayerNot",
            Aliases = new[] { "upn", "unp" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions, Permission.UndoAll },
            Usage = "/UndoPlayerNot (TimeSpan|BlockCount) PlayerName [AnotherName...]",
            Help = "Reverses changes made by a given player in the current world. " +
                   "More than one player name can be given at a time.",
            Handler = UndoPlayerNotHandler
        };        

        static void UndoPlayerNotHandler( Player player, CommandReader cmd ) {
            BlockDBUndoArgs args = ParseBlockDBUndoParams( player, cmd, CdUndoPlayerNot, true );
            if( args == null ) return;
            Scheduler.NewBackgroundTask( UndoPlayerLookup )
                     .RunOnce( args, TimeSpan.Zero );
        }


        // Looks up the changes in BlockDB and prints a confirmation prompt. Runs on a background thread.
        static void UndoPlayerLookup( SchedulerTask task ) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)task.UserState;
            string cmdName = ( args.Not ? "UndoPlayerNot" : "UndoPlayer" );
            BlockDBEntry[] changes = UndoPlayerGetChanges( args );
            UndoLookup( args, cmdName, "undo", 
                       changes, BlockDBUndoConfirmCallback );
        }
        
        
        static BlockDBEntry[] UndoPlayerGetChanges( BlockDBUndoArgs args ) {
            if( args.CountLimit > 0 ) {
                // count-limited lookup
                if( args.Targets.Length == 0 ) {
                    return args.World.BlockDB.Lookup( args.CountLimit );
                } else {
                    return args.World.BlockDB.Lookup( args.CountLimit, args.Targets, args.Not );
                }
            } else {
                // time-limited lookup
                if( args.Targets.Length == 0 ) {
                    return args.World.BlockDB.Lookup( Int32.MaxValue, args.AgeLimit );
                } else {
                    return args.World.BlockDB.Lookup( Int32.MaxValue, args.Targets, args.Not, args.AgeLimit );
                }
            }
        }

        #endregion

        
        #region Highlight
        
        static readonly CommandDescriptor CdHighlight = new CommandDescriptor {
            Name = "Highlight",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions },
            Usage = "/Highlight (TimeSpan|BlockCount) PlayerName [AnotherName]",
            Help = "Highlights changes made by a given player in the current world. " +
                   "More than one player name can be given at a time. " +
                   "Players with UndoAll permission can use '*' in place of player name to highlight everyone's changes at once.",
            Handler = HighlightHandler
        };

        static void HighlightHandler( Player player, CommandReader cmd ) {
            BlockDBUndoArgs args = ParseBlockDBUndoParams( player, cmd, CdHighlight, false );
            if( args == null ) return;
            Scheduler.NewBackgroundTask( HighlightLookup )
                     .RunOnce( args, TimeSpan.Zero );
        }
        
        
        static void HighlightLookup( SchedulerTask task ) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)task.UserState;
            BlockDBEntry[] changes = HighlightGetChanges( args );
            UndoLookup( args, "Highlight", "highlight", 
                       changes, HighlightConfirmCallback );
        }
        
        
        // called after player types "/ok" to the confirmation prompt.
        static void HighlightConfirmCallback( Player player, object tag, bool fromConsole ) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)tag;
            string targetList = BlockDBTargetList( args );
            Logger.Log( LogType.UserActivity,
                        "Highlight: Player {0} will highlight {1} changes (limit of {2}) by {3} on world {4}",
                        player.Name, args.Entries.Length,
                        args.CountLimit == 0 ? args.AgeLimit.ToMiniString() : args.CountLimit.ToStringInvariant(),
                        targetList, args.World.Name );

            Vector3I coords;
            // iterate from oldest to newest
            for( int i = args.Entries.Length - 1; i >= 0; i-- ) {
                BlockDBEntry e = args.Entries[i];
                coords.X = e.X; coords.Y = e.Y; coords.Z = e.Z;
                
                Block block = e.NewBlock == Block.Air ? Block.Red : Block.Green;
                player.SendBlock( coords, block );
            }
            
            player.Message( "Highlight({0}): Highlighted {1} blocks.",
                           BlockDBDescription( args ), args.Entries.Length );
        }
        
        
        static BlockDBEntry[] HighlightGetChanges( BlockDBUndoArgs args ) {
            PlayerInfo[] infos = args.Targets;
            
            if( args.CountLimit > 0 ) {
                // count-limited lookup
                if( args.Targets.Length == 0 ) {
                    return args.World.BlockDB.Lookup( args.CountLimit, BlockDBSearchType.ReturnAll, 
                                                     entry => true );
                } else {
                    BlockDB.LookupID lookup = new BlockDB.LookupID { Infos = infos };
                    return args.World.BlockDB.Lookup( args.CountLimit, BlockDBSearchType.ReturnAll, lookup.Select );
                }
            } else {
                long ticks = DateTime.UtcNow.Subtract( args.AgeLimit ).ToUnixTime();               
                // time-limited lookup
                if( args.Targets.Length == 0 ) {
                    return args.World.BlockDB.Lookup( Int32.MaxValue, BlockDBSearchType.ReturnAll, 
                                                     entry => entry.Timestamp >= ticks );
                } else {
                    BlockDB.LookupTimeAndID lookup = new BlockDB.LookupTimeAndID { Infos = infos, Ticks = ticks };
                    return args.World.BlockDB.Lookup( Int32.MaxValue, BlockDBSearchType.ReturnAll, lookup.Select );
                }
            }
        }
        
        
        #endregion
        
        
        #endregion
        #region maze
        static readonly CommandDescriptor CdMazeCuboid = new CommandDescriptor
        {
            Name = "MazeCuboid",
            Aliases = new string[] { "Mc", "Mz", "Maze" },
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Draws a cuboid with the current brush and with a random maze inside.(C) 2012 Lao Tszy",
            Usage = "/MazeCuboid [block type]",
            Handler = MazeCuboidHandler,
        };

        private static void MazeCuboidHandler(Player p, CommandReader cmd)
        {
            try
            {
                MazeCuboidDrawOperation op = new MazeCuboidDrawOperation(p);
                BuildingCommands.DrawOperationBegin(p, cmd, op);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Error: " + e.Message);
            }
        }

        #endregion
        #region drawimg

        // New /drawimage implementation contributed by Matvei Stefarov <me@matvei.org>
        static readonly CommandDescriptor CdDrawImage = new CommandDescriptor
        {
            Name = "DrawImage",
            Aliases = new[] { "Drawimg", "Imgdraw", "ImgPrint", "DI" },
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new[] { Permission.DrawAdvanced },
            Usage = "/DrawImage [Imgur URL] [Palette]",
            Help = "Downloads and draws an image, using minecraft blocks. " +
                   "First mark specifies the origin (corner) block of the image. " +
                   "Second mark specifies direction (from origin block) in which image should be drawn. " +
                   "Optionally, a block palette name can be specified: " +
                   "Layered (default), Light, Dark, Gray, DarkGray, LayeredGray, or BW (black and white). " +
                   "If your image is from imgur.com, simply type '++' followed by the image code. " +
                   "Example: /DrawImage ++kbFRo",
            Handler = DrawImageHandler
        };

        static void DrawImageHandler(Player player, CommandReader cmd)
        {
            ImageDrawOperation op = new ImageDrawOperation(player);
            if (!op.ReadParams(cmd))
            {
                CdDrawImage.PrintUsage(player);
                return;
            }
            player.Message("DrawImage: Click 2 blocks or use &H/Mark&S to set direction.");
            player.SelectionStart(2, DrawImageCallback, op, Permission.DrawAdvanced);
        }

        private static void DrawImageCallback(Player player, Vector3I[] marks, object tag)
        {
            ImageDrawOperation op = (ImageDrawOperation)tag;
            player.Message("&HDrawImage: Downloading {0}", op.ImageUrl);
            try
            {
                op.Prepare(marks);
                if (!player.CanDraw(op.BlocksTotalEstimate))
                {
                    player.Message(
                        "DrawImage: You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                        player.Info.Rank.DrawLimit,
                        op.BlocksTotalEstimate);
                    return;
                }
                op.Begin();
            }
            catch (ArgumentException ex)
            {
                player.Message("&WDrawImage: Error setting up: " + ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Warning,
                            "{0}: Error downloading image from {1}: {2}",
                            op.Description,
                            op.ImageUrl,
                            ex);
                player.Message("&WDrawImage: Error downloading: " + ex.Message);
            }
        }

        #endregion
        #region static
        static readonly CommandDescriptor CdStatic = new CommandDescriptor {
            Name = "Static",
            Category = CommandCategory.Building,
            Help = "Toggles repetition of last selection on or off.",
            Handler = StaticHandler
        };

        static void StaticHandler( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                CdStatic.PrintUsage( player );
                return;
            }
            if( player.IsRepeatingSelection ) {
                player.Message( "Static: &4Off" );
                player.IsRepeatingSelection = false;
                player.SelectionCancel();
            } else {
                player.Message( "Static: &2On" );
                player.IsRepeatingSelection = true;
            }
        }
        #endregion
        #region snake
        private static readonly CommandDescriptor CdSnake = new CommandDescriptor {
            Name = "Snake",
            Category = CommandCategory.New | CommandCategory.Building,
            Permissions = new[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Usage = "/Snake (Length) [Block]",
            Help = "Builds a randomixed snake at a desired length with the specified block.",
            Handler = SnakeHandler
        };

        private static void SnakeHandler(Player player, CommandReader cmd) {
            int length;
            if (!cmd.NextInt(out length)) {
                CdSnake.PrintUsage(player);
                return;
            }
            if (length > 100000) {
                player.Message("Snake cannot be more than 100,000 blocks in length");
                return;
            }
            
            Block block = player.LastUsedBlockType;
            if (cmd.HasNext && !cmd.NextBlock(player, false, out block)) return;
            if (block == Block.None) {
                player.Message("&WCannot deduce desired block. Click a block or type out the block name.");
                return;
            }
            Random dir = new Random();
            Vector3I pos = new Vector3I(player.Position.BlockX, player.Position.BlockY, player.Position.BlockZ);

            if (player.World != null && player.World.Map != null) {
                int blocksDrawn = 0, blocksSkipped = 0;
                UndoState undoState = player.DrawBegin(null);
                for (int i = 0; i < length; i++) {
                    Vector3I nextX = pos; nextX.X += dir.Next(0, 2) * 2 - 1;
                    Vector3I nextY = pos; nextY.Y += dir.Next(0, 2) * 2 - 1;
                    Vector3I nextZ = pos; nextZ.Z += dir.Next(0, 2) * 2 - 1;
                    pos = new Vector3I(nextX.X, nextY.Y, nextZ.Z);
                    
                    DrawOneBlock(player, player.World.Map, block, nextX,
                          BlockChangeContext.Drawn,
                          ref blocksDrawn, ref blocksSkipped, undoState);
                    DrawOneBlock(player, player.World.Map, block, nextY,
                          BlockChangeContext.Drawn,
                          ref blocksDrawn, ref blocksSkipped, undoState);
                    DrawOneBlock(player, player.World.Map, block, nextZ,
                          BlockChangeContext.Drawn,
                          ref blocksDrawn, ref blocksSkipped, undoState);
                }
                DrawingFinished(player, "Placed", blocksDrawn, blocksSkipped);
            }
        }
        #endregion
        #region drawoneblock
        internal static void DrawOneBlock([NotNull] Player player, [NotNull] Map map, Block drawBlock, Vector3I coord,
                                 BlockChangeContext context, ref int blocks, ref int blocksDenied, UndoState undoState) {
            if (player == null) throw new ArgumentNullException("player");
            if (map == null) return;
            if (!map.InBounds(coord)) return;
            Block block = map.GetBlock(coord);
            if (block == drawBlock) return;

            if (player.CanPlace(map, coord, drawBlock, context) != CanPlaceResult.Allowed) {
                blocksDenied++;
                return;
            }

            map.QueueUpdate(new BlockUpdate(null, coord, drawBlock));
            Player.RaisePlayerPlacedBlockEvent(player, map, coord, block, drawBlock, context);

            if (!undoState.IsTooLargeToUndo) {
                if (!undoState.Add(coord, map, block)) {
                    player.Message("NOTE: This draw command is too massive to undo.");
                    player.LastDrawOp = null;
                }
            }
            blocks++;
        }


        static void DrawingFinished([NotNull] Player player, string verb, int blocks, int blocksDenied) {
            if (player == null)
                throw new ArgumentNullException("player");
            if (blocks == 0) {
                if (blocksDenied > 0) {
                    player.Message("No blocks could be {0} due to permission issues.", verb.ToLower());
                } else {
                    player.Message("No blocks were {0}.", verb.ToLower());
                }
            } else {
                if (blocksDenied > 0) {
                    player.Message("{0} {1} blocks ({2} blocks skipped due to permission issues)... " +
                                       "The map is now being updated.", verb, blocks, blocksDenied);
                } else {
                    player.Message("{0} {1} blocks... The map is now being updated.", verb, blocks);
                }
            }
            if (blocks > 0) {
                player.Info.ProcessDrawCommand(blocks);
                Server.RequestGC();
            }
        }
        #endregion
    }
}
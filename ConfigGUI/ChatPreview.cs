// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using fCraft.ConfigGUI.Properties;


namespace fCraft.ConfigGUI {
    sealed partial class ChatPreview : UserControl {

        struct ColorPair { public Brush Foreground, Shadow; }
        static readonly PrivateFontCollection Fonts;
        static readonly Font MinecraftFont;
        static Dictionary<char, ColorPair> brushes = new Dictionary<char, ColorPair>();

        unsafe static ChatPreview() {
            Fonts = new PrivateFontCollection();
            fixed( byte* fontPointer = Resources.MinecraftFont ) {
                Fonts.AddMemoryFont( (IntPtr)fontPointer, Resources.MinecraftFont.Length );
            }
            MinecraftFont = new Font( Fonts.Families[0], 12, FontStyle.Regular );
        }


        public ChatPreview() {
            InitializeComponent();
            DoubleBuffered = true;
        }


        sealed class TextSegment {
            public string Text;
            public char ColorCode;
            public int X, Y;

            public void Draw( Graphics g ) {
                ColorPair pair;
                if( !brushes.TryGetValue( ColorCode, out pair ) ) {
                    pair = MakeColorPair();
                    brushes[ColorCode] = pair;
                }
                
                g.DrawString( Text, MinecraftFont, pair.Shadow, X + 2, Y + 2 );
                g.DrawString( Text, MinecraftFont, pair.Foreground, X, Y );
            }
            
            ColorPair MakeColorPair() {
                ColorPair pair;
                System.Drawing.Color textCol;
                
                System.Drawing.Color c = ColorPicker.LookupColor( ColorCode, out textCol );
                pair.Foreground = new SolidBrush( System.Drawing.Color.FromArgb( c.R, c.G, c.B ) );
                
                // 25% opacity for shadow/background colour
                c = System.Drawing.Color.FromArgb( c.R / 4, c.G / 4, c.B / 4 );
                pair.Shadow = new SolidBrush( System.Drawing.Color.FromArgb( c.R, c.G, c.B ) );
                return pair;
            }
        }

        static readonly Regex SplitByColorRegex = new Regex( "(&[0-9a-zA-Z])", RegexOptions.Compiled );
        TextSegment[] segments;

        public void SetText( string[] lines ) {
            List<TextSegment> newSegments = new List<TextSegment>();
            using( Bitmap b = new Bitmap( 1, 1 ) ) {
                using( Graphics g = Graphics.FromImage( b ) ) { // graphics for string mesaurement
                    g.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;

                    int y = 5;
                    for( int i = 0; i < lines.Length; i++ ) {
                        if( lines[i] == null || lines[i].Length == 0 ) continue;
                        int x = 5;
                        string[] plainTextSegments = SplitByColorRegex.Split( lines[i] );

                        char colorCode = 'f';
                        for( int j = 0; j < plainTextSegments.Length; j++ ) {
                            if( plainTextSegments[j].Length == 0 ) continue;
                            if( plainTextSegments[j][0] == '&' ) {
                                colorCode = plainTextSegments[j][1];
                                // Conver system color codes into actual color codes
                                string converted = Color.Parse( colorCode );
                                if( converted != null ) colorCode = converted[1];
                            } else {
                                newSegments.Add( new TextSegment {
                                                    ColorCode = colorCode,
                                                    Text = plainTextSegments[j],
                                                    X = x,
                                                    Y = y
                                                } );
                                x += (int)g.MeasureString( plainTextSegments[j], MinecraftFont ).Width;
                            }
                        }
                        y += 20;
                    }

                }
            }
            segments = newSegments.ToArray();
            Invalidate();
        }


        protected override void OnPaint( PaintEventArgs e ) {
            e.Graphics.DrawImageUnscaledAndClipped( Resources.ChatBackground, e.ClipRectangle );

            e.Graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;

            if( segments != null && segments.Length > 0 ) {
                for( int i = 0; i < segments.Length; i++ ) {
                    segments[i].Draw( e.Graphics );
                }
            }

            base.OnPaint( e );
        }
    }
}
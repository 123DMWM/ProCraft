// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System.Collections.Generic;
using System.Windows.Forms;
using SysCol = System.Drawing.Color;
using fCraft;

namespace fCraft.ConfigGUI {
    internal sealed partial class ColorPicker : Form {
        public char ColorCode;

        internal static SysCol LookupColor( char colCode, out SysCol textCol ) {
            SysCol col = default(SysCol);
            CustomColor custom = Color.ExtColors[colCode];
            
            if( Color.IsStandardColorCode( colCode ) ) {
                int hex = Color.Hex( colCode );
                col = SysCol.FromArgb(
                    191 * ((hex >> 2) & 1) + 64 * (hex >> 3),
                    191 * ((hex >> 1) & 1) + 64 * (hex >> 3),
                    191 * ((hex >> 0) & 1) + 64 * (hex >> 3));
            } else if( custom.Undefined ) {
                col = SysCol.White;
            } else {
                col = SysCol.FromArgb( custom.R, custom.G, custom.B );
            }
            
            double r = Map( col.R ), g = Map( col.G ), b = Map( col.B );
            double L = 0.2126 * r + 0.7152 * g + 0.0722 * b;
            textCol = L > 0.179 ? SysCol.Black : SysCol.White;
            return col;
        }

        static double Map( double c ) {
            c /= 255.0;
            if ( c <= 0.03928 ) return c / 12.92;
            return System.Math.Pow( (c + 0.055) / 1.055, 2.4 );
        }
        
        
        internal struct ColorPair {
            public ColorPair( SysCol foreground, SysCol background ) {
                Foreground = foreground;
                Background = background;
            }
            public System.Drawing.Color Foreground;
            public System.Drawing.Color Background;
        }


        public ColorPicker( string title, char oldColorCode ) {
            ColorCode = oldColorCode;
            StartPosition = FormStartPosition.CenterParent;
            
            SuspendLayout();
            for( char code = '0'; code <= '9'; code++ )
                MakeButton(code);
            for ( char code = 'a'; code <= 'f'; code++ )
                MakeButton(code);
            for (int i = 0; i < Color.ExtColors.Length; i++) {
                if (!Color.ExtColors[i].Undefined) MakeButton(Color.ExtColors[i].Code);
            }
            MakeCancelButton();
            MakeWindow( title );
            ResumeLayout( false );
        }
        
        
        const int btnWidth = 130, btnHeight = 40, btnsPerCol = 8;
        int index = 0;
        void MakeButton( char colCode ) {
            int row = index / btnsPerCol, col = index % btnsPerCol;
            index++;
            
            Button btn = new Button();
            SysCol textCol;          
            btn.BackColor = LookupColor( colCode, out textCol );
            btn.ForeColor = textCol;
            btn.Location = new System.Drawing.Point( 9 + row * btnWidth, 7 + col * btnHeight );
            btn.Size = new System.Drawing.Size( btnWidth, btnHeight );
            btn.Name = "b" + index;
            btn.TabIndex = index;
            btn.Text = ColorName(colCode) + " - " + colCode;
            btn.Click += delegate { ColorCode = colCode; DialogResult = DialogResult.OK; Close(); };
            btn.Margin = new Padding( 0 );
            btn.UseVisualStyleBackColor = false;
            Controls.Add( btn );
        }
        
        
        void MakeCancelButton() {
            Button bCancel = new System.Windows.Forms.Button();
            bCancel.DialogResult = DialogResult.Cancel;
            bCancel.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0 );
            
            int rows = 1 + (index / btnsPerCol);
            int x = 0;
            // Centre if even count, align under row if odd count
            if ((rows & 1) == 0) {
                x = (rows * btnWidth) / 2 - (100 / 2);
            } else {
            	x = ((rows / 2) * btnWidth) + (btnWidth - 100) / 2;
            }
            
            bCancel.Location = new System.Drawing.Point( 8 + x, 10 + btnHeight * btnsPerCol );
            bCancel.Name = "bCancel";
            bCancel.Size = new System.Drawing.Size( 100, 25 );
            bCancel.TabIndex = 260;
            bCancel.Text = "Cancel";
            bCancel.UseVisualStyleBackColor = true;
            Controls.Add( bCancel );
        }
        
        
        void MakeWindow(string title) {
            AutoScaleDimensions = new System.Drawing.SizeF( 8F, 13F );
            AutoScaleMode = AutoScaleMode.Font;
            int rows = 1 + (index / btnsPerCol);
            ClientSize = new System.Drawing.Size( 18 + btnWidth * rows, 47 + btnHeight * btnsPerCol );
            Font = new System.Drawing.Font( "Lucida Console", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0 );
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding( 4, 3, 4, 3 );
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ColorPicker";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = title;
        }
        
        
        static string ColorName(char colCode) {
            char[] a = Color.GetName(colCode).ToCharArray();
            a[0] = char.ToUpper( a[0] );
            return new string( a );
        }
    }
}
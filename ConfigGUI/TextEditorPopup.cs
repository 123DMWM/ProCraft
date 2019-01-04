﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    public sealed partial class TextEditorPopup : Form {
        public string OriginalText { get; private set; }
        public string FileName { get; private set; }


        public TextEditorPopup( string fileName, string defaultValue ) {
            InitializeComponent();

            FileName = fileName;
            Text = "Editing " + FileName;

            if( File.Exists( fileName ) ) {
                OriginalText = File.ReadAllText( fileName );
            } else {
                OriginalText = defaultValue;
            }

            tText.Text = OriginalText;
            lWarning.Visible = ContainsLongLines();
        }

        bool ContainsLongLines() {
            return tText.Lines.Any( line => (line.Length > 62) );
        }


        private void tRules_KeyDown( object sender, KeyEventArgs e ) {
            lWarning.Visible = ContainsLongLines();
        }

        private void bOK_Click( object sender, EventArgs e ) {
            File.WriteAllText( FileName, tText.Text );
            Close();
        }

        ColorPicker colorPicker;
        private void bInsertColor_Click( object sender, EventArgs e ) {
            if( colorPicker == null ) colorPicker = new ColorPicker(" Insert color", 'f' );
            if( colorPicker.ShowDialog() == DialogResult.OK ) {
                string colorToInsert = "&" + colorPicker.ColorCode;
                int selectionStart = tText.SelectionStart;
                tText.Paste( colorToInsert );
                tText.Select( selectionStart, 2 );
                tText.Focus();
            }
        }

        KeywordPicker keywordPicker;
        private void bInsertKeyword_Click( object sender, EventArgs e ) {
            if( keywordPicker == null ) keywordPicker = new KeywordPicker();
            if( keywordPicker.ShowDialog() == DialogResult.OK ) {
                int selectionStart = tText.SelectionStart;
                tText.Paste( keywordPicker.Result );
                tText.Select( selectionStart, keywordPicker.Result.Length );
                tText.Focus();
            }
        }

        private void bReset_Click( object sender, EventArgs e ) {
            tText.Text = OriginalText;
        }
    }
}

// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    public sealed partial class DeleteRankPopup : Form {
        internal Rank SubstituteRank { get; private set; }

        public DeleteRankPopup( Rank deletedRank ) {
            InitializeComponent();
            foreach( Rank rank in RankManager.Ranks ) {
                if( rank != deletedRank ) {
                    cSubstitute.Items.Add( MainForm.ToComboBoxOption( rank ) );
                }
            }
            lWarning.Text = String.Format( lWarning.Text, deletedRank.Name );
            cSubstitute.SelectedIndex = cSubstitute.Items.Count - 1;
        }


        private void cSubstitute_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cSubstitute.SelectedIndex < 0 ) return;
            foreach( Rank rank in RankManager.Ranks ) {
                if( cSubstitute.SelectedItem.ToString() != MainForm.ToComboBoxOption( rank ) ) continue;
                SubstituteRank = rank;
                bDelete.Enabled = true;
                break;
            }
        }
    }
}

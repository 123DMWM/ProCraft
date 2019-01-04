﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    public sealed partial class PermissionLimitBox : UserControl {

        public Permission Permission { get; private set; }

        public string FirstItem { get; private set; }

        public Rank Rank { get; private set; }

        public PermissionLimitBox( string labelText, Permission permission, string firstItem ) {
            InitializeComponent();

            label.Text = labelText;
            label.Left = (comboBox.Left - comboBox.Margin.Left) - (label.Width + label.Margin.Right);

            Permission = permission;
            FirstItem = firstItem;
            RebuildList();

            comboBox.SelectedIndexChanged += OnPermissionLimitChanged;
        }


        void OnPermissionLimitChanged( object sender, EventArgs args ) {
            if( Rank == null ) return;
            Rank rankLimit = RankManager.FindRank( comboBox.SelectedIndex - 1 );
            if( rankLimit == null ) {
                Rank.ResetLimit( Permission );
            } else {
                Rank.SetLimit( Permission, rankLimit );
            }
        }


        public void Reset() {
            comboBox.SelectedIndex = 0;
        }


        public void RebuildList() {
            comboBox.Items.Clear();
            comboBox.Items.Add( FirstItem );
            foreach( Rank rank in RankManager.Ranks ) {
                comboBox.Items.Add( MainForm.ToComboBoxOption( rank ) );
            }
        }


        public void SelectRank( Rank rank ) {
            Rank = rank;
            if( rank == null ) {
                comboBox.SelectedIndex = -1;
                Visible = false;
            } else {
                comboBox.SelectedIndex = GetLimitIndex( rank, Permission );
                Visible = rank.Can( Permission );
            }
        }


        int GetLimitIndex( Rank rank, Permission permission ) {
            if( rank.HasLimitSet( permission ) ) {
                return rank.GetLimit( permission ).Index + 1;
            } else {
                return 0;
            }
        }


        public void PermissionToggled( bool isOn ) {
            Visible = isOn;
        }
    }
}
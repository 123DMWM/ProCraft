﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> A bounding box selection that is designated as a sub area within a world.
    /// Zones can have restriction just like worlds on access, and block modification. </summary>
    public sealed class Zone : IClassy, INotifiesOnChange {

        /// <summary> Zone boundaries. </summary>
        [NotNull]
        public BoundingBox Bounds { get; private set; }

        /// <summary> Zone build permission controller. </summary>
        [NotNull]
        public readonly SecurityController Controller = new SecurityController();

        /// <summary> Zone name (case-preserving but case-insensitive). </summary>
        [NotNull]
        public string Name { get; set; }

        /// <summary> Zone Sign Text (Only Shown if Zone is 1*1*1) </summary>
        [NotNull]
        public string Sign { get; set; }

        /// <summary> List of exceptions (included and excluded players). </summary>
        public PlayerExceptions ExceptionList {
            get { return Controller.ExceptionList; }
        }

        #region Packet.MakeSelection

        /// <summary> Wheather ot now to show the zone boundary </summary>
        [NotNull]
        public bool ShowZone = false;

        /// <summary> color of the zone boundary </summary>
        [NotNull]
        public string Color = "000";

        /// <summary> alpha of the zone boundry </summary>
        [NotNull]
        public short Alpha = 0;

        /// <summary> zone id for boundarys </summary>
        [NotNull]
        public byte ZoneID { get; set; }

        #endregion

        /// <summary> Zone creation date, UTC. </summary>
        public DateTime CreatedDate { get; private set; }

        /// <summary> Zone editing date, UTC. </summary>
        public DateTime EditedDate { get; private set; }

        /// <summary> Player who created this zone. May be null if unknown. </summary>
        [CanBeNull]
        public string CreatedBy { get; private set; }

        /// <summary> Classy name of the player who created this zone.
        /// Returns "?" if CreatedBy name is unknown, unrecognized, or null. </summary>
        public string CreatedByClassy {
            get {
                return PlayerDB.FindExactClassyName( CreatedBy );
            }
        }

        /// <summary> Player who was the last to edit this zone. May be null if unknown. </summary>
        [CanBeNull]
        public string EditedBy { get; private set; }

        /// <summary> Decorated name of the player who was the last to edit this zone.
        /// Returns "?" if EditedBy name is unknown, unrecognized, or null. </summary>
        public string EditedByClassy {
            get {
                return PlayerDB.FindExactClassyName( EditedBy );
            }
        }

        /// <summary> Map that this zone is on. </summary>
        [NotNull]
        public Map Map { get; set; }


        /// <summary> Creates the zone boundaries, and sets CreatedDate/CreatedBy. </summary>
        /// <param name="bounds"> New zone boundaries. </param>
        /// <param name="createdBy"> Player who created this zone. May not be null. </param>
        public void Create( [NotNull] BoundingBox bounds, [NotNull] PlayerInfo createdBy ) {
            if( bounds == null ) throw new ArgumentNullException( "bounds" );
            if( createdBy == null ) throw new ArgumentNullException( "createdBy" );
            CreatedDate = DateTime.UtcNow;
            Bounds = bounds;
            CreatedBy = createdBy.Name;
        }


        public void Edit( [NotNull] PlayerInfo editedBy ) {
            if( editedBy == null ) throw new ArgumentNullException( "editedBy" );
            EditedDate = DateTime.UtcNow;
            EditedBy = editedBy.Name;
            RaiseChangedEvent();
        }


        public Zone() {
            Controller.Changed += ( o, e ) => RaiseChangedEvent();
        }


        public Zone( [NotNull] string raw, [CanBeNull] World world )
            : this() {
            if( raw == null ) throw new ArgumentNullException( "raw" );
            string[] parts = raw.Split( ',' );

            string[] header = parts[0].Split( ' ' );
            Name = header[0];
            Bounds = new BoundingBox( Int32.Parse( header[1] ), Int32.Parse( header[2] ), Int32.Parse( header[3] ),
                                      Int32.Parse( header[4] ), Int32.Parse( header[5] ), Int32.Parse( header[6] ) );

            Rank buildRank = Rank.Parse( header[7] );


            if (header.Length > 8) {
                // Part 5: Zone color
                try {
                    bool zoneShow;
                    if (bool.TryParse(header[8], out zoneShow)) {
                        ShowZone = zoneShow;
                    }
                    Color = header[9];
                    short zoneAlpha;
                    if (short.TryParse(header[10], out zoneAlpha)) {
                        Alpha = zoneAlpha;
                    }
                } catch (Exception ex) {
                    Logger.Log(LogType.Error, "Could not load Zone Colors for {0}", Name);
                    Logger.Log(LogType.Error, ex.StackTrace);
                }
            }

            if (header[0].Contains(SpecialZone.Door)) {
                buildRank = RankManager.DefaultRank;
            }
            // if all else fails, fall back to lowest class... ignore door instances
            if (buildRank == null && !header[0].Contains(SpecialZone.Door)) {
                if (world != null) {
                    Controller.MinRank = world.BuildSecurity.MinRank;
                } else {
                    Controller.ResetMinRank();
                }
                Logger.Log(LogType.Error,
                            "Zone: Error parsing zone definition: unknown rank \"{0}\". Permission reset to default ({1}). Ignore this message if you have recently changed rank permissions.",
                            header[7], Controller.MinRank.Name);
            } else {
                Controller.MinRank = buildRank;
            }

            if( PlayerDB.IsLoaded ) {
                // Part 2:
                if( parts[1].Length > 0 ) {
                    foreach( string playerName in parts[1].Split( ' ' ) ) {
                        if (!Player.IsValidPlayerName(playerName))
                        {
                            Logger.Log( LogType.Warning,
                                        "Invalid entry in zone \"{0}\" whitelist: {1}", Name, playerName );
                            continue;
                        }
                        PlayerInfo info = PlayerDB.FindPlayerInfoExact( playerName );
                        if( info == null ) {
                            Logger.Log( LogType.Warning,
                                        "Unrecognized player in zone \"{0}\" whitelist: {1}", Name, playerName );
                            continue; // player name not found in the DB (discarded)
                        }
                        Controller.Include( info );
                    }
                }

                // Part 3: excluded list
                if( parts[2].Length > 0 ) {
                    foreach( string playerName in parts[2].Split( ' ' ) ) {
                        if (!Player.IsValidPlayerName(playerName))
                        {
                            Logger.Log( LogType.Warning,
                                        "Invalid entry in zone \"{0}\" blacklist: {1}", Name, playerName );
                            continue;
                        }
                        PlayerInfo info = PlayerDB.FindPlayerInfoExact( playerName );
                        if( info == null ) {
                            Logger.Log( LogType.Warning,
                                        "Unrecognized player in zone \"{0}\" whitelist: {1}", Name, playerName );
                            continue; // player name not found in the DB (discarded)
                        }
                        Controller.Exclude( info );
                    }
                }
            } else {
                RawWhitelist = parts[1];
                RawBlacklist = parts[2];
            }

            // Part 4: extended header
            if( parts.Length > 3 ) {
                string[] xheader = parts[3].Split( ' ' );                
                if( xheader[0] == "-" ) {
                    CreatedBy = null;
                    CreatedDate = DateTime.MinValue;
                } else {
                    CreatedBy = xheader[0];
                    CreatedDate = DateTime.Parse( xheader[1] );
                }

                if( xheader[2] == "-" ) {
                    EditedBy = null;
                    EditedDate = DateTime.MinValue;
                } else {
                    EditedBy = xheader[2];
                    EditedDate = DateTime.Parse( xheader[3] );
                }
            }
        }

        internal readonly string RawWhitelist,
                                 RawBlacklist;


        public string ClassyName {
            get {
                return Controller.MinRank.Color + Name;
            }
        }


        public event EventHandler Changed;

        void RaiseChangedEvent() {
            var h = Changed;
            if( h != null ) h( null, EventArgs.Empty );
        }
    }
}
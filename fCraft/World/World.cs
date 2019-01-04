﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using fCraft.MapConversion;
using JetBrains.Annotations;
using System.Collections;
using System.Threading;

namespace fCraft {
    /// <summary> World instance. Manages the player list, manages access/build security, stores metadata. </summary>
    public sealed class World : IClassy {
        /// <summary> World name (no formatting).
        /// Use WorldManager.RenameWorld() method to change this. </summary>
        [NotNull]
        public string Name { get; internal set; }


        
        /// <summary> Whether the world shows up on the /Worlds list.
        /// Can be assigned directly. </summary>
        public bool IsHidden { get; set; }


        /// <summary> Whether this world is currently pending unload 
        /// (waiting for block updates to finish processing before unloading). </summary>
        public bool IsPendingMapUnload { get; private set; }


        /// <summary> Controls which players may access this world. </summary>
        [NotNull]
        public SecurityController AccessSecurity { get; internal set; }

        /// <summary> Controls which players may build in this world. </summary>
        [NotNull]
        public SecurityController BuildSecurity { get; internal set; }



        /// <summary> Date (UTC) that this world was created on. </summary>
        public DateTime LoadedOn { get; internal set; }

        /// <summary> Name of the player/entity who created this world, null if the player is unknown. </summary>
        [CanBeNull]
        public string LoadedBy { get; internal set; }

        [NotNull]
        public string LoadedByClassy {
            get {
                return PlayerDB.FindExactClassyName( LoadedBy );
            }
        }


        /// <summary> Date (UTC) of the most recent map change. </summary>
        public DateTime MapChangedOn { get; internal set; }
        
        /// <summary> Name of the player/entity who last loaded this map,
        /// Null if the player is unknown or if map has never been changed. </summary>
        [CanBeNull]
        public string MapChangedBy { get; internal set; }

        [NotNull]
        public string MapChangedByClassy {
            get {
                return PlayerDB.FindExactClassyName( MapChangedBy );
            }
        }


        /// <summary> Message shown to players who join this map.
        /// Null if no message is set. </summary>
        [CanBeNull]
        public string Greeting { get; set; }

        /// <summary> Message shown to players who join this map.
        /// Null if no message is set. </summary>
        [CanBeNull]
        public string MOTD { get; set; }

        /// <summary> List of Signs in the specified world.
        /// Null if no signs are set. </summary>
        [CanBeNull]
        public Array Signs { get; set; }

        public Portals.PortalsList Portals = new Portals.PortalsList();
        
        public BlockDefinition[] BlockDefs;

        // used to synchronize player joining/parting with map loading/saving
        internal readonly object SyncRoot = new object();

        /// <summary> BlockDB instance used to store/lookup block changes for this world. </summary>
        public BlockDB BlockDB { get; private set; }

        /// <summary> Whether players can place any blocks in the world. </summary>
        public bool Buildable { get; set; }
        
        /// <summary> Whether players can delete any blocks in the world. </summary>
        public bool Deletable { get; set; }
        
        
        internal World( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if(!IsValidName( name ) ) {
                throw new ArgumentException( "Unacceptable world name." );
            }
            BlockDB = new BlockDB( this );
            AccessSecurity = new SecurityController();
            BuildSecurity = new SecurityController();
            Name = name;
            UpdatePlayerList();
            
            Buildable = true;
            Deletable = true;
            BlockDefs = new BlockDefinition[256];
            for (int i = 0; i < BlockDefs.Length; i++)
                BlockDefs[i] = BlockDefinition.GlobalDefs[i];
        }


        #region Map

        /// <summary> Map of this world. May be null if world is not loaded. </summary>
        [CanBeNull]
        public Map Map {
            get { return map; }
            set {
                if( map != null && value == null ) StopTasks();
                if( map == null && value != null ) StartTasks();
                if( value != null ) value.World = this;
                map = value;
            }
        }
        public Map map;

        /// <summary> Whether the map is currently loaded. </summary>
        public bool IsLoaded { get { return Map != null; } }
        
        /// <summary> Either retrieves map dimensions from loaded map, or loads map dimensions from disc. </summary>
        public Vector3I GetOrLoadDimensions() {
            var temp = Map;
            if( temp != null ) return temp.Bounds.Dimensions;

            lock( SyncRoot ) {
                if( Map != null ) return Map.Bounds.Dimensions;
                if( !File.Exists( MapFileName ) ) return Vector3I.Zero;
                
                try {
                    temp = MapUtility.LoadHeader( MapFileName );
                    return temp.Bounds.Dimensions;
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error,
                               "World.GetOrLoadDimensions: Failed to load map header ({0}): {1}",
                               MapFileName, ex );
                }
                return Vector3I.Zero;
            }
        }

        /// <summary> Loads the map file, if needed.
        /// Generates a default map if mapfile is missing or not loadable.
        /// Guaranteed to return a Map object. </summary>
        [NotNull]
        public Map LoadMap() {
            var tempMap = Map;
            if( tempMap != null ) return tempMap;

            lock( SyncRoot ) {
                if( Map != null ) return Map;

                if( File.Exists( MapFileName ) ) {
                    try {
                        Map = MapUtility.Load( MapFileName );
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "World.LoadMap: Failed to load map ({0}): {1}",
                                    MapFileName, ex );
                    }
                }

                // or generate a default one
                if( Map == null ) {
                    Server.Message( "&WMapfile is missing for world {0}&W. A new map has been created.", ClassyName );
                    Logger.Log( LogType.Warning,
                                "World.LoadMap: Map file missing for world {0}. Generating default flatgrass map.",
                                Name );
                    Map = MapGenerator.GenerateFlatgrass( 128, 128, 64 );
                }
                return Map;
            }
        }

        internal void UnloadMap( bool expectedPendingFlag ) {
            lock( SyncRoot ) {
                if( expectedPendingFlag != IsPendingMapUnload ) return;
                SaveMap();
                Map = null;
                IsPendingMapUnload = false;
            }
            Server.RequestGC();
        }


        /// <summary> Returns the map file name, including MapPath. </summary>
        public string MapFileName {
            get { return Path.Combine( Paths.MapPath, Name + ".fcm" ); }
        }


        /// <summary> Forces the map to be saved to file. Acquires SyncRoot. </summary>
        public void SaveMap() {
            lock( SyncRoot ) {
                if( Map != null ) {
                    if( Map.Save( MapFileName ) ) {
                        HasChangedSinceBackup = true;
                    }
                }
            }
        }


        /// <summary> Creates a new World for the given Map, with same properties as this world. </summary>
        /// <returns> Newly-created World object, with the new map. </returns>
        [NotNull]
        public World ChangeMap( [NotNull] Map newMap ) {
            if( newMap == null ) throw new ArgumentNullException( "newMap" );
            lock( SyncRoot ) {
                World newWorld = new World( Name ) {
                    AccessSecurity = (SecurityController)AccessSecurity.Clone(),
                    BuildSecurity = (SecurityController)BuildSecurity.Clone(),
                    IsHidden = IsHidden,
                    BlockDB = BlockDB,
                    lastBackup = lastBackup,
                    BackupInterval = BackupInterval,
                    IsLocked = IsLocked,
                    LockedBy = LockedBy,
                    UnlockedBy = UnlockedBy,
                    LockedOn = LockedOn,
                    UnlockedOn = UnlockedOn,
                    LoadedBy = LoadedBy,
                    LoadedOn = LoadedOn,
                    MapChangedBy = MapChangedBy,
                    MapChangedOn = DateTime.UtcNow,
                    FogColor = FogColor,
                    CloudColor = CloudColor,
                    SkyColor = SkyColor,
                    EdgeLevel = EdgeLevel,
                    SidesOffset = SidesOffset,
                    CloudsHeight = CloudsHeight,
                    MaxFogDistance = MaxFogDistance,
                    EdgeBlock = EdgeBlock,
                    HorizonBlock = HorizonBlock,
                    LightColor = LightColor,
                    ShadowColor = ShadowColor,
                    Weather = Weather,
                    Texture = Texture,
                    CloudsSpeed = CloudsSpeed,
                    WeatherSpeed = WeatherSpeed,
                    WeatherFade = WeatherFade,
                    SkyboxHorSpeed = SkyboxHorSpeed,
                    SkyboxVerSpeed = SkyboxVerSpeed,
                    Buildable = Buildable,
                    Deletable = Deletable,
                    BlockDefs = BlockDefs,
                };
                newMap.World = newWorld;
                newWorld.Map = newMap;
                newWorld.Preload = preload;
                WorldManager.ReplaceWorld( this, newWorld );
                using( BlockDB.GetWriteLock() ) {
                    BlockDB.Clear();
                    BlockDB.World = newWorld;
                }

                foreach ( Player player in Players ) {
                    player.JoinWorld( newWorld, WorldChangeReason.Rejoin );
                }
                return newWorld;
            }
        }


        /// <summary> Controls if the map should be loaded before players enter.
        /// Map is immediately loaded when Preload is set to true.
        /// Map is unloaded if Preloaded is set to false, and there are no players on this world. </summary>
        public bool Preload {
            get {
                return preload;
            }
            set {
                lock( SyncRoot ) {
                    if( preload == value ) return;
                    preload = value;
                    if( preload ) {
                        if( Map == null ) LoadMap();
                    } else {
                        if( Map != null && playerIndex.Count == 0 ) UnloadMap( false );
                    }
                }
            }
        }
        bool preload;

        #endregion


        #region Flush

        /// <summary> Whether this world is currently being flushed. </summary>
        public bool IsFlushing { get; private set; }


        /// <summary> Intiates a map flush, in which all block drawings are completed.
        /// All users are held in limbo until completion, and then resent the map.  </summary>
        public void Flush() {
            lock( SyncRoot ) {
                if( Map == null ) return;
                Players.Message( "&WMap is being flushed. Stay put, world will reload shortly." );
                IsFlushing = true;
            }
        }


        internal void EndFlushMapBuffer() {
            lock( SyncRoot ) {
                IsFlushing = false;
                Players.Message( "&WMap flushed. Reloading..." );
                foreach( Player player in Players ) {
                    player.JoinWorld( this, WorldChangeReason.Rejoin, player.Position );
                }
            }
        }

        #endregion

       
        #region PlayerList

        readonly Dictionary<string, Player> playerIndex = new Dictionary<string, Player>();
        public Player[] Players { get; private set; }

        [CanBeNull]
        public Map AcceptPlayer( [NotNull] Player player, bool announce ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            lock( SyncRoot ) {
                if( IsFull ) {
                    if( player.Info.Rank.HasReservedSlot ) {
                        Player idlestPlayer = Players.Where( p => p.Info.Rank.IdleKickTimer != 0 )
                                                     .OrderBy( p => p.LastActiveTime )
                                                     .FirstOrDefault();
                        if( idlestPlayer != null ) {
                            idlestPlayer.Kick( Player.Console, "Auto-kicked to make room (idle).",
                                               LeaveReason.IdleKick, false, false, false );

                            Server.Players
                                  .CanSee( player )
                                  .Message( "{1}&S was auto-kicked to make room for {2}",
                                            idlestPlayer.Info.Rank.Name, idlestPlayer.ClassyName, player.ClassyName );
                            Server.Players
                                  .CantSee( player )
                                  .Message( "{1}&S was kicked for being idle for {2} min",
                                            idlestPlayer.Info.Rank.Name, player.ClassyName, player.Info.Rank.IdleKickTimer);
                        } else {
                            return null;
                        }
                    } else {
                        return null;
                    }
                }

                if( playerIndex.ContainsKey( player.Name.ToLower() ) ) {
                    Logger.Log( LogType.Error,
                                "This world already contains the player by name ({0}). " +
                                "Some sort of state corruption must have occurred.",
                                player.Name );
                    playerIndex.Remove( player.Name.ToLower() );
                }

                playerIndex.Add( player.Name.ToLower(), player );

                // load the map, if it's not yet loaded
                IsPendingMapUnload = false;
                Map = LoadMap();

                if( ConfigKey.BackupOnJoin.Enabled() && (HasChangedSinceBackup || !ConfigKey.BackupOnlyWhenChanged.Enabled()) ) {
                    string backupFileName = String.Format( JoinBackupFormat,
                                                           Name, DateTime.Now, player.Name ); // localized
                    SaveBackup( Path.Combine( Paths.BackupPath, backupFileName ) );
                }

                UpdatePlayerList();

                if (announce && ConfigKey.ShowJoinedWorldMessages.Enabled()) 
                {
                    Server.Players.CanSee(player)
                                      .Message("{0}&S joined {1}",
                                                player.ClassyName, ClassyName);
                }

                Logger.Log( LogType.UserActivity,
                            "{0} &Sjoined world {1}.",
                            player.ClassyName, ClassyName );

                if( IsLocked ) {
                    player.Message( "&WThis map is currently locked (read-only)." );
                }

                if( player.Info.IsHidden ) {
                    player.Message( "&8Reminder: You are still hidden." );
                }

                return Map;
            }
        }

        

        public bool ReleasePlayer( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( SyncRoot ) {
                if( !playerIndex.Remove( player.Name.ToLower() ) ) {
                    return false;
                }

                // clear undo, selection, and confirmations
                if (player.LastDrawOp != null && player.Supports(CpeExt.MessageType))
                    player.Send(Packet.Message((byte)MessageType.Status3, "", player));
                player.LastDrawOp = null;
                player.UndoClear();
                player.RedoClear();
                player.IsRepeatingSelection = false;
                player.SelectionCancel();
                player.ConfirmCancel();

                // update player list
                UpdatePlayerList();

                // unload map (if needed)
                if( playerIndex.Count == 0 && !preload ) {
                    IsPendingMapUnload = true;
                }
                return true;
            }
        }
        
        public Player[] FindPlayers( [NotNull] Player player, [NotNull] string playerName ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Player[] tempList = Players;
            List<Player> results = new List<Player>();
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && player.CanSee( tempList[i] ) ) {
                    if( tempList[i].Name.CaselessEquals( playerName ) ) {
                        results.Clear();
                        results.Add( tempList[i] );
                        break;
                    } else if( tempList[i].Name.CaselessStarts( playerName ) ) {
                        results.Add( tempList[i] );
                    }
                }
            }
            return results.ToArray();
        }


        /// <summary> Gets player by name (without autocompletion) </summary>
        [CanBeNull]
        public Player FindPlayerExact( [NotNull] string playerName ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Player[] tempList = Players;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].Name.CaselessEquals( playerName ) ) {
                    return tempList[i];
                }
            }
            return null;
        }


        /// <summary> Caches the player list to an array (Players -> PlayerList) </summary>
        public void UpdatePlayerList() {
            lock( SyncRoot ) {
                Players = playerIndex.Values.ToArray();
            }
        }


        /// <summary> Counts all players (optionally includes all hidden players). </summary>
        public int CountPlayers( bool includeHiddenPlayers ) {
            if( includeHiddenPlayers ) {
                return Players.Length;
            } else {
                return Players.Count( player => !player.Info.IsHidden );
            }
        }


        /// <summary> Counts only the players who are not hidden from a given observer. </summary>
        public int CountVisiblePlayers( [NotNull] Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            return Players.Count( observer.CanSee );
        }


        /// <summary> Whether the current world is full, determined by ConfigKey.MaxPlayersPerWorld </summary>
        public bool IsFull {
            get {
                return (Players.Length >= ConfigKey.MaxPlayersPerWorld.GetInt());
            }
        }

        #endregion


        #region Lock / Unlock

        /// <summary> Whether the world is currently locked (in read-only mode). </summary>
        public bool IsLocked { get; internal set; }

        /// <summary> The name of player/entity who last locked this world. </summary>
        public string LockedBy { get; internal set; }

        /// <summary> The name of player/entity who last unlocked this world. </summary>
        public string UnlockedBy { get; internal set; }

        /// <summary> Date (UTC) when this world was last locked. </summary>
        public DateTime LockedOn { get; internal set; }

        /// <summary> Date (UTC) when this world was last locked. </summary>
        public DateTime UnlockedOn { get; internal set; }

        readonly object lockLock = new object();


        /// <summary> Locks the current world, which prevents blocks in the world from being updated. </summary>
        /// <param name="player"> Player who is issueing the lock. </param>
        /// <returns> True if the world was locked, or false if the world was already locked. </returns>
        public bool Lock( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( lockLock ) {
                if( IsLocked ) return false;

                LockedBy = player.Name;
                LockedOn = DateTime.UtcNow;
                IsLocked = true;
                WorldManager.SaveWorldList();
                Map mapCache = Map;
                if( mapCache != null ) {
                    mapCache.ClearUpdateQueue();
                    mapCache.CancelAllDrawOps();
                }
                
                Players.Message( "&WWorld was locked by {0}", player.ClassyName );
                Logger.Log( LogType.UserActivity,
                           "World {0} was locked by {1}",
                           Name, player.Name );
                return true;
            }
        }


        /// <summary> Unlocks the current world, which allows blocks in the world to be changed once more. </summary>
        /// <param name="player"> Player who is issueing the unlock. </param>
        /// <returns> True if the world was unlocked, or false if the world was already unlocked. </returns>
        public bool Unlock( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( lockLock ) {
                if( !IsLocked ) return false;
                
                UnlockedBy = player.Name;
                UnlockedOn = DateTime.UtcNow;
                IsLocked = false;
                WorldManager.SaveWorldList();
                
                Players.Message( "&WMap was unlocked by {0}", player.ClassyName );
                Logger.Log( LogType.UserActivity,
                           "World \"{0}\" was unlocked by {1}",
                           Name, player.Name );
                return true;
            }
        }

        #endregion


        #region Patrol

        readonly object patrolLock = new object();
        static readonly TimeSpan MinPatrolInterval = TimeSpan.FromSeconds( 20 );

        /// <summary> Selects the next player to teleport to while patroling.
        /// Sets target's LastPatrolTime automatically. </summary>
        /// <param name="observer"> Player who is patrolling </param>
        /// <returns> Player who has been selected to be patrolled </returns>
        /// <exception cref="ArgumentNullException"> If observer is null. </exception>
        public Player GetNextPatrolTarget( [NotNull] Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            lock( patrolLock ) {
                Player candidate = Players.RankedAtMost( RankManager.PatrolledRank )
                                          .CanBeSeen( observer )
                                          .Where( p => p.LastActiveTime > p.LastPatrolTime &&
                                                       p.HasFullyConnected &&
                                                       DateTime.UtcNow.Subtract( p.LastPatrolTime ) > MinPatrolInterval )
                                          .OrderBy( p => p.LastPatrolTime.Ticks )
                                          .FirstOrDefault();
                if( candidate != null ) {
                    candidate.LastPatrolTime = DateTime.UtcNow;
                }
                return candidate;
            }
        }

        /// <summary> Selects the next player to teleport to while patroling.
        /// Includes additional inclusion check (predicate). </summary>
        /// <param name="observer"> Player who is patrolling </param>
        /// <param name="predicate"> Additional inclusion check for patrol targets.
        /// Applied after the standard checks. Allows filtering out unwanted players. </param>
        /// <param name="setLastPatrolTime"> Whether to set target's LastPatrolTime. </param>
        /// <returns> Player who has been selected to be patrolled </returns>
        /// <exception cref="ArgumentNullException"> If observer or predicate is null. </exception>
        public Player GetNextPatrolTarget( [NotNull] Player observer,
                                           [NotNull] Predicate<Player> predicate,
                                           bool setLastPatrolTime ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            if( predicate == null ) throw new ArgumentNullException( "predicate" );
            lock( patrolLock ) {
                Player candidate = Players.RankedAtMost( RankManager.PatrolledRank )
                                          .CanBeSeen( observer )
                                          .Where( p => p.LastActiveTime > p.LastPatrolTime &&
                                                       p.HasFullyConnected &&
                                                       DateTime.UtcNow.Subtract( p.LastPatrolTime ) > MinPatrolInterval )
                                          .Where( p => predicate( p ) )
                                          .OrderBy( p => p.LastPatrolTime.Ticks )
                                          .FirstOrDefault();
                if( setLastPatrolTime && candidate != null ) {
                    candidate.LastPatrolTime = DateTime.UtcNow;
                }
                return candidate;
            }
        }

        #endregion


        #region Scheduled Tasks

        SchedulerTask updateTask, saveTask;
        readonly object taskLock = new object();


        void StopTasks() {
            lock( taskLock ) {
                if( updateTask != null ) {
                    updateTask.Stop();
                    updateTask = null;
                }
                if( saveTask != null ) {
                    saveTask.Stop();
                    saveTask = null;
                }
            }
        }


        void StartTasks() {
            lock( taskLock ) {
                updateTask = Scheduler.NewTask( UpdateTask );
                updateTask.RunForever( this,
                                       TimeSpan.FromMilliseconds( ConfigKey.TickInterval.GetInt() ),
                                       TimeSpan.Zero );

                if( ConfigKey.SaveInterval.GetInt() > 0 ) {
                    saveTask = Scheduler.NewBackgroundTask( SaveTask );
                    saveTask.IsCritical = true;
                    saveTask.RunForever( this,
                                         TimeSpan.FromSeconds( ConfigKey.SaveInterval.GetInt() ),
                                         TimeSpan.FromSeconds( ConfigKey.SaveInterval.GetInt() ) );
                }
            }
        }


        void UpdateTask( SchedulerTask task ) {
            Map tempMap = Map;
            if( tempMap != null ) {
                tempMap.ProcessUpdates();
            }
        }


        internal const string TimedBackupFormat = "{0}_{1:yyyy-MM-dd_HH-mm}.fcm",
                              JoinBackupFormat = "{0}_{1:yyyy-MM-dd_HH-mm}_{2}.fcm";


        void SaveTask( SchedulerTask task ) {
            if( !IsLoaded ) return;
            lock( SyncRoot ) {
                if( Map == null ) return;

                lock( BackupLock ) {
                    if( BackupsEnabled &&
                        DateTime.UtcNow.Subtract( lastBackup ) > BackupInterval &&
                        ( HasChangedSinceBackup || !ConfigKey.BackupOnlyWhenChanged.Enabled() ) ) {

                        string backupFileName = String.Format( TimedBackupFormat, Name, DateTime.Now ); // localized
                        SaveBackup( Path.Combine( Paths.BackupPath, backupFileName ) );
                        lastBackup = DateTime.UtcNow;
                    }
                }

                if( Map.HasChangedSinceSave ) {
                    SaveMap();
                }

                if( BlockDB.IsEnabledGlobally && BlockDB.IsEnabled ) {
                    BlockDB.Flush( true );
                }
            }
        }

        #endregion


        #region Backups

        DateTime lastBackup = DateTime.UtcNow;
        static readonly object BackupLock = new object();

        /// <summary> Whether timed backups are enabled (either manually or by default) on this world. </summary>
        public bool BackupsEnabled {
            get {
                switch( BackupEnabledState ) {
                    case YesNoAuto.Yes:
                        return BackupInterval > TimeSpan.Zero;
                    case YesNoAuto.No:
                        return false;
                    default: //case YesNoAuto.Auto:
                        return DefaultBackupsEnabled;
                }
            }
        }


        /// <summary> Whether the map was saved since last time it was backed up. </summary>
        public bool HasChangedSinceBackup { get; set; }


        /// <summary> Backup state. Use "Yes" to enable, "No" to disable, and "Auto" to use default settings.
        /// If setting to "Yes", make sure to set BackupInterval property value first. </summary>
        public YesNoAuto BackupEnabledState {
            get { return backupEnabledState; }
            set {
                lock( BackupLock ) {
                    if( value == backupEnabledState ) return;
                    if( value == YesNoAuto.Yes && backupInterval <= TimeSpan.Zero ) {
                        throw new InvalidOperationException( "To set BackupEnabledState to 'Yes,' set BackupInterval to the desired time interval." );
                    }
                    backupEnabledState = value;
                }
            }
        }
        YesNoAuto backupEnabledState = YesNoAuto.Auto;


        /// <summary> Timed backup interval.
        /// If BackupEnabledState is set to "Yes", value must be positive.
        /// If BackupEnabledState is set to "No" or "Auto", this property has no effect. </summary>
        public TimeSpan BackupInterval {
            get {
                switch( backupEnabledState ) {
                    case YesNoAuto.Yes:
                        return backupInterval;
                    case YesNoAuto.No:
                        return TimeSpan.Zero;
                    default: // case YesNoAuto.Auto:
                        return DefaultBackupInterval;
                }
            }
            set {
                lock( BackupLock ) {
                    backupInterval = value;
                    if( value > TimeSpan.Zero ) {
                        BackupEnabledState = YesNoAuto.Yes;
                    } else {
                        BackupEnabledState = YesNoAuto.No;
                    }
                }
            }
        }
        TimeSpan backupInterval;


        /// <summary> Default backup interval, for worlds that have BackupEnabledState set to "Auto". </summary>
        public static TimeSpan DefaultBackupInterval { get; set; }


        /// <summary> Whether timed backups are enabled by default for worlds that have BackupEnabledState set to "Auto". </summary>
        public static bool DefaultBackupsEnabled {
            get { return DefaultBackupInterval > TimeSpan.Zero; }
        }


        internal string BackupSettingDescription {
            get {
                switch( backupEnabledState ) {
                    case YesNoAuto.No:
                        return "disabled (manual)";
                    case YesNoAuto.Yes:
                        return String.Format( "every {0} (manual)", backupInterval.ToMiniString() );
                    default: //case YesNoAuto.Auto:
                        if( DefaultBackupsEnabled ) {
                            return String.Format( "every {0} (default)", DefaultBackupInterval.ToMiniString() );
                        } else {
                            return "disabled (default)";
                        }
                }
            }
        }



        public void SaveBackup( [NotNull] string targetName ) {
            if( targetName == null ) throw new ArgumentNullException( "targetName" );

            if( !File.Exists( MapFileName ) ) return;
            lock( BackupLock ) {
                DirectoryInfo directory = new DirectoryInfo( Paths.BackupPath );

                if( !directory.Exists ) {
                    try {
                        directory.Create();
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "Map.SaveBackup: Error occurred while trying to create backup directory: {0}", ex );
                        return;
                    }
                }

                try {
                    HasChangedSinceBackup = false;
                    File.Copy( MapFileName, targetName, true );
                } catch( Exception ex ) {
                    HasChangedSinceBackup = true;
                    Logger.Log( LogType.Error,
                                "Map.SaveBackup: Error occurred while trying to save backup to \"{0}\": {1}",
                                targetName, ex );
                    return;
                }

                if( ConfigKey.MaxBackups.GetInt() > 0 || ConfigKey.MaxBackupSize.GetInt() > 0 ) {
                    DeleteOldBackups( directory );
                }
            }

            Logger.Log( LogType.SystemActivity, "AutoBackup: {0}", targetName );
        }


        static void DeleteOldBackups( [NotNull] DirectoryInfo directory ) {
            if( directory == null ) throw new ArgumentNullException( "directory" );
            var backupList = directory.GetFiles( "*.fcm" ).OrderBy( fi => -fi.CreationTimeUtc.Ticks ).ToList();

            int maxFileCount = ConfigKey.MaxBackups.GetInt();

            if( maxFileCount > 0 ) {
                while( backupList.Count > maxFileCount ) {
                    FileInfo info = backupList[backupList.Count - 1];
                    backupList.RemoveAt( backupList.Count - 1 );
                    try {
                        File.Delete( info.FullName );
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "Map.SaveBackup: Error occurred while trying delete old backup \"{0}\": {1}",
                                    info.FullName, ex );
                        break;
                    }
                    Logger.Log( LogType.SystemActivity,
                                "Map.SaveBackup: Deleted old backup \"{0}\"", info.Name );
                }
            }

            int maxFileSize = ConfigKey.MaxBackupSize.GetInt();

            if( maxFileSize > 0 ) {
                while( true ) {
                    FileInfo[] fis = directory.GetFiles();
                    long size = fis.Sum( fi => fi.Length );

                    if( size / 16384 / 16384 > maxFileSize ) {
                        FileInfo info = backupList[backupList.Count - 1];
                        backupList.RemoveAt( backupList.Count - 1 );
                        try {
                            File.Delete( info.FullName );
                        } catch( Exception ex ) {
                            Logger.Log( LogType.Error,
                                        "Map.SaveBackup: Error occurred while trying delete old backup \"{0}\": {1}",
                                        info.Name, ex );
                            break;
                        }
                        Logger.Log( LogType.SystemActivity,
                                    "Map.SaveBackup: Deleted old backup \"{0}\"", info.Name );
                    } else {
                        break;
                    }
                }
            }
        }

        #endregion

        /*#region Block Hunt

        /// <summary> Position.X at which the hiders spawns. </summary>
        public short HiderPosX = -1;
        /// <summary> Position.Y at which the hiders spawns. </summary>
        public short HiderPosY = -1;
        /// <summary> Position.Z at which the hiders spawns. </summary>
        public short HiderPosZ = -1;

        /// <summary> Position.X at which the Seeker spawns. </summary>
        public short SeekerPosX = -1;
        /// <summary> Position.Y at which the Seeker spawns. </summary>
        public short SeekerPosY = -1;
        /// <summary> Position.Z at which the Seeker spawns. </summary>
        public short SeekerPosZ = -1;

        /// <summary> List of Blocks to be used in the game. </summary>
        public List<Block> GameBlocks = new List<Block>();

        #endregion*/

        #region Environment settings

        /// <summary> Color of the clouds (RGB packed into an int). Set to -1 to use client defaults. </summary>
        public string CloudColor = null;

        /// <summary> Color of the fog (RGB packed into an int). Set to -1 to use client defaults. </summary>
        public string FogColor = null;

        /// <summary> Color of the sky (RGB packed into an int). Set to -1 to use client defaults. </summary>
        public string SkyColor = null;

        /// <summary> Color of the blocks in shadows (RGB packed into an int). Set to -1 to use client defaults. </summary>
        public string ShadowColor = null;

        /// <summary> Color of the blocks in the light (RGB packed into an int). Set to -1 to use client defaults. </summary>
        public string LightColor = null;

        /// <summary> Elevation of the "ocean" that surrounds maps. Set to -1 to use client default (halfway up the map). </summary>
        public short EdgeLevel = -1;
        
        /// <summary> Offset of the "bedrock" borders of the map vertically from "ocean" elevation. </summary>
        public short SidesOffset = -2;

        public short GetEdgeLevel() {
            Vector3I dims = GetOrLoadDimensions();
            return (EdgeLevel == -1) ? (short)(dims.Z / 2) : EdgeLevel;
        }
                
        /// <summary> Elevation of the clouds height of the map. The default height is map.Height + 2. </summary>
        public short CloudsHeight = short.MinValue;
        
        public short GetCloudsHeight() {
            Vector3I dims = GetOrLoadDimensions();
            return (CloudsHeight == short.MinValue) ? (short)(dims.Z + 2) : CloudsHeight;
        }
        
        /// <summary> Maximum fog distance that can be seen by the player. The default value is 0. </summary>
        public short MaxFogDistance = 0;
        
        /// <summary> Clouds speed as seen by the player, in units of 256ths. The default value is 256 (1 speed). </summary>
        public short CloudsSpeed = 256;
        
        /// <summary> Rain/Snow speed as seen by the player, in units of 256ths. The default value is 256 (1 speed). </summary>
        public short WeatherSpeed = 256;
        
        /// <summary> Rain/Snow fade speed as seen by the player, in units of 128ths. The default value is 128 (1 speed). </summary>
        public short WeatherFade = 128;
        
        /// <summary> Skybox horizontal speed as seen by the player, in units of 1024ths. The default value is 0 (0 speed). </summary>
        public short SkyboxHorSpeed = 0;
        
        /// <summary> Skybox vertical speed as seen by the player, in units of 1024ths. The default value is 0 (0 speed). </summary>
        public short SkyboxVerSpeed = 0;

        /// <summary> The block which will be displayed on the horizon. </summary>
        public byte HorizonBlock = (byte)Block.Water;

        /// <summary> The block which will be displayed on the edge of the map. </summary>
        public byte EdgeBlock = (byte)Block.Admincrete;

        /// <summary> The texture that will be used for the blocks. </summary>
        public string Texture = "default";

        public string GetTexture() {
            return Texture.CaselessEquals("default") ? Server.DefaultTerrain : Texture;
        }

        /// <summary> The current weather in this world. </summary>
        public byte Weather = 0;

        #endregion

        /// <summary> The max reachdistance of the map. </summary>
        public short MaxReach = -1;


        /// <summary> Ensures that player name has the correct length (2-16 characters)
        /// and character set (alphanumeric chars and underscores allowed). </summary>
        public static bool IsValidName([NotNull] string name) {
            if (name == null) throw new ArgumentNullException("name");
            if (name.StartsWith("PW_")) return true;
            if (name.Length < 2 || name.Length > 16) return false;
            return name.All(ch => ch >= '0' && (ch <= '9' || ch >= 'A') && (ch <= 'Z' || ch >= '_') && (ch <= '_' || ch >= 'a') && ch <= 'z');
        }

        /// <summary> Returns a nicely formatted name, with optional color codes. </summary>
        public string ClassyName {
            get {
                if( ConfigKey.RankColorsInWorldNames.Enabled() ) {
                    Rank maxRank;
                    if( BuildSecurity.MinRank >= AccessSecurity.MinRank ) {
                        maxRank = BuildSecurity.MinRank;
                    } else {
                        maxRank = AccessSecurity.MinRank;
                    }
                    if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                        return maxRank.Color + maxRank.Prefix + Name;
                    } else {
                        return maxRank.Color + Name;
                    }
                } else {
                    return Name;
                }
            }
        }


        public override string ToString() {
            return String.Format( "World({0})", Name );
        }
    }
}

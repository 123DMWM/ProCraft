﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Persistent database of player information. </summary>
    public static class PlayerDB {
        static readonly Trie<PlayerInfo> Trie = new Trie<PlayerInfo>();
        static List<PlayerInfo> list = new List<PlayerInfo>();

        /// <summary> Cached list of all players in the database.
        /// May be quite long. Make sure to copy a reference to
        /// the list before accessing it in a loop, since this 
        /// array be frequently be replaced by an updated one. </summary>
        public static PlayerInfo[] PlayerInfoList { get; private set; }

        static int maxID = 255;
        const int BufferSize = 64 * 1024;

        /* 
         * Version 0 - before 0.530 - all dates/times are local
         * Version 1 - 0.530-0.536 - all dates and times are stored as UTC unix timestamps (milliseconds)
         * Version 2 - 0.600 dev - all dates and times are stored as UTC unix timestamps (seconds)
         * Version 3 - 0.600 dev - same as v2, but sorting by ID is enforced
         * Version 4 - 0.600 dev - added LastModified column, forced banned players to be unfrozen/unmuted/unhidden.
         * Version 5 - 0.600+ - removed FailedLoginCount column
         */
        public const int FormatVersion = 5;

        const string Header = "ProCraft PlayerDB | Row format: " +
                              "Name,IPAddress,Rank,RankChangeDate,RankChangedBy,Banned,BanDate,BannedBy," +
                              "UnbanDate,UnbannedBy,BanReason,UnbanReason,LastFailedLoginDate," +
                              "LastFailedLoginIP,UNUSED,FirstLoginDate,LastLoginDate,TotalTime," +
                              "BlocksBuilt,BlocksDeleted,TimesVisited,MessagesWritten,UNUSED,UNUSED," +
                              "PreviousRank,RankChangeReason,TimesKicked,TimesKickedOthers," +
                              "TimesBannedOthers,ID,RankChangeType,LastKickDate,LastSeen,BlocksDrawn," +
                              "LastKickBy,LastKickReason,BannedUntil,IsFrozen,FrozenBy,FrozenOn,MutedUntil,MutedBy," +
                              "Password,IsOnline,BandwidthUseMode,IsHidden,LastModified,DisplayedName,AccountType," +
                              "UNUSED,ReadIRC,PromoCount,TimesUseLeBot,UNUSED,HasRTR,TPDeny,JoinOnRankWorld" + 
                              "LastWorld, LastWorldPos, DemoCount";


        // used to ensure PlayerDB consistency when adding/removing PlayerDB entries
        static readonly object AddLocker = new object();

        // used to prevent concurrent access to the PlayerDB file
        static readonly object SaveLoadLocker = new object();


        public static bool IsLoaded { get; private set; }


        static void CheckIfLoaded() {
            if( !IsLoaded ) throw new InvalidOperationException( "PlayerDB is not loaded." );
        }

        [NotNull]
        public static PlayerInfo AddFakeEntry( [NotNull] string name, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            CheckIfLoaded();

            PlayerInfo info;
            lock( AddLocker ) {
                info = Trie.Get( name );
                if( info != null ) {
                    throw new ArgumentException( "A PlayerDB entry already exists for this name.", "name" );
                }

                var e = new PlayerInfoBeingCreatedEventArgs( name, IPAddress.None, RankManager.DefaultRank, true );
                PlayerInfo.RaiseCreatingEvent( e );
                if( e.Cancel ) {
                    throw new OperationCanceledException( "Cancelled by a plugin." );
                }

                info = new PlayerInfo( name, e.StartingRank, false, rankChangeType );

                list.Add( info );
                Trie.Add( info.Name, info );
                UpdateCache();
            }
            PlayerInfo.RaiseCreatedEvent( info, false );
            return info;
        }


        #region Saving/Loading

        internal static void Load() {
            lock( SaveLoadLocker ) {
                if( File.Exists( Paths.PlayerDBFileName ) ) {
                    Stopwatch sw = Stopwatch.StartNew();
                    using( FileStream fs = OpenRead( Paths.PlayerDBFileName ) ) {
                        using( StreamReader reader = new StreamReader( fs, Encoding.UTF8, true, BufferSize ) ) {

                            string header = reader.ReadLine();

                            // if PlayerDB is an empty file
                            if( header == null ) {
                                Logger.Log( LogType.Warning, "PlayerDB.Load: PlayerDB file is empty." );
                            } else {
                                lock( AddLocker ) {
                                    LoadInternal( reader, header );
                                }
                            }
                        }
                    }
                    sw.Stop();
                    Logger.Log( LogType.Debug,
                                "PlayerDB.Load: Done loading player DB ({0} records) in {1}ms. MaxID={2}",
                                Trie.Count, sw.ElapsedMilliseconds, maxID );
                } else {
                    Logger.Log( LogType.Warning, "PlayerDB.Load: No player DB file found." );
                }
                UpdateCache();
                IsLoaded = true;
            }
        }


        static void LoadInternal( StreamReader reader, string header ) {
            int version = IdentifyFormatVersion( header );
            if( version > FormatVersion ) {
                Logger.Log( LogType.Warning,
                            "PlayerDB.Load: Attempting to load unsupported PlayerDB format ({0}). Errors may occur.",
                            version );
            } else if( version < FormatVersion ) {
                Logger.Log( LogType.Warning,
                            "PlayerDB.Load: Converting PlayerDB to a newer format (version {0} to {1}).",
                            version, FormatVersion );
            }

            int emptyRecords = 0, count = 0;
            string[] fields = new string[100];
            while( true ) {
                string line = reader.ReadLine();
                if( line == null ) break;
                Split(line, ref fields, out count);

                if( fields.Length >= PlayerInfo.MinFieldCount ) {
#if !DEBUG
                    try {
#endif
                        PlayerInfo info;
                        switch( version ) {
                            case 0:
                                info = PlayerInfo.LoadFormat0( fields, count, true );
                                break;
                            case 1:
                                info = PlayerInfo.LoadFormat1( fields, count );
                                break;
                            default:
                                // Versions 2-5 differ in semantics only, not in actual serialization format.
                                info = PlayerInfo.LoadFormat2( fields, count );
                                break;
                        }

                        if( info.ID > maxID ) {
                            Logger.Log( LogType.Warning,
                                        "PlayerDB.Load: Adjusting wrongly saved MaxID ({0} to {1}).",
                                        maxID, info.ID );
                            maxID = info.ID;
                        }

                        // A record is considered "empty" if the player has never logged in.
                        // Empty records may be created by /Import, /Ban, and /Rank commands on typos.
                        // Deleting such records should have no negative impact on DB completeness.
                        if( (info.LastIP.Equals( IPAddress.None ) || info.LastIP.Equals( IPAddress.Any ) || info.TimesVisited == 0) &&
                            !info.IsBanned && info.Rank == RankManager.DefaultRank ) {

                            Logger.Log( LogType.SystemActivity,
                                        "PlayerDB.Load: Skipping an empty record for player \"{0}\"",
                                        info.Name );
                            emptyRecords++;
                            continue;
                        }

                        // Check for duplicates. Unless PlayerDB.txt was altered externally, this does not happen.
                        if( Trie.ContainsKey( info.Name ) ) {
                            Logger.Log( LogType.Error,
                                        "PlayerDB.Load: Duplicate record for player \"{0}\" skipped.",
                                        info.Name );
                        } else {
                            Trie.Add( info.Name, info );
                            list.Add( info );
                        }
#if !DEBUG
                    } catch( Exception ex ) {
                        Logger.LogAndReportCrash( "Error while parsing PlayerInfo record: " + line,
                                                  "ProCraft",
                                                  ex,
                                                  false );
                    }
#endif
                } else {
                    Logger.Log( LogType.Error,
                                "PlayerDB.Load: Unexpected field count ({0}), expecting at least {1} fields for a PlayerDB entry.",
                                fields.Length, PlayerInfo.MinFieldCount );
                }
            }
            
            for (int i = 0; i < fields.Length; i++)
                fields[i] = null;
            if( emptyRecords > 0 ) {
                Logger.Log( LogType.Warning,
                            "PlayerDB.Load: Skipped {0} empty records.", emptyRecords );
            }

            RunCompatibilityChecks( version );
        }
        
        static void Split(string line, ref string[] fields, out int count) {
            // Effectively string.split(',') but without the two passes and memory allocations.
            count = 0;
            int start = 0, len = 0;
            for (int i = 0; i < line.Length; i++) {
                if (line[i] != ',') { len++; continue; }
                if (count == fields.Length)
                    Array.Resize(ref fields, count + 10);
                
                fields[count] = line.Substring(start, len); count++;
                // Start next string at character after the ,
                start = i + 1;
                len = 0;
            }
            
            // We will always have at least one string, even if it just empty.
            if (count == fields.Length)
                Array.Resize(ref fields, count + 10);
            fields[count] = line.Substring(start, len); count++;
        }


        static void RunCompatibilityChecks( int loadedVersion ) {
            // Sorting the list allows finding players by ID using binary search.
            list.Sort( PlayerIDComparer.Instance );

            if( loadedVersion < 4 ) {
                int unhid = 0, unfroze = 0, unmuted = 0;
                Logger.Log( LogType.SystemActivity, "PlayerDB: Checking consistency of banned player records..." );
                for( int i = 0; i < list.Count; i++ ) {
                    if( list[i].IsBanned ) {
                        if( list[i].IsHidden ) {
                            unhid++;
                            list[i].IsHidden = false;
                        }

                        if( list[i].IsFrozen ) {
                            list[i].Unfreeze();
                            unfroze++;
                        }

                        if( list[i].IsMuted ) {
                            list[i].Unmute();
                            unmuted++;
                        }
                    }
                }
                Logger.Log( LogType.SystemActivity,
                            "PlayerDB: Unhid {0}, unfroze {1}, and unmuted {2} banned accounts.",
                            unhid, unfroze, unmuted );
            }
        }


        static int IdentifyFormatVersion( [NotNull] string header ) {
            if( header == null ) throw new ArgumentNullException( "header" );
            if( header.StartsWith( "playerName" ) ) return 0;
            string[] headerParts = header.Split( ' ' );
            if( headerParts.Length < 2 ) {
                throw new FormatException( "Invalid PlayerDB header format: " + header );
            }
            int maxIDField;
            if( Int32.TryParse( headerParts[0], out maxIDField ) ) {
                if( maxIDField >= 255 ) {// IDs start at 256
                    maxID = maxIDField;
                }
            }
            int version;
            if( Int32.TryParse( headerParts[1], out version ) ) {
                return version;
            } else {
                return 0;
            }
        }


        public static void Save() {
            CheckIfLoaded();
            const string tempFileName = Paths.PlayerDBFileName + ".temp";

            lock( SaveLoadLocker ) {
                PlayerInfo[] listCopy = PlayerInfoList;
                Stopwatch sw = Stopwatch.StartNew();
                using( FileStream fs = OpenWrite( tempFileName ) ) {
                    using( StreamWriter writer = new StreamWriter( fs, Encoding.UTF8, BufferSize ) ) {
                        writer.WriteLine( "{0} {1} {2}", maxID, FormatVersion, Header );

                        StringBuffer sb = new StringBuffer(2048);
                        for( int i = 0; i < listCopy.Length; i++ ) {
                            listCopy[i].Serialize( sb );
                            writer.WriteLine( sb.value, 0, sb.length );
                            sb.length = 0;
                        }
                    }
                }
                sw.Stop();
                Logger.Log( LogType.Debug,
                            "PlayerDB.Save: Saved player database ({0} records) in {1}ms",
                            Trie.Count, sw.ElapsedMilliseconds );

                try {
                    Paths.MoveOrReplaceFile( tempFileName, Paths.PlayerDBFileName );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error,
                                "PlayerDB.Save: An error occurred while trying to save PlayerDB: {0}", ex );
                }
            }
        }


        static FileStream OpenRead( string fileName ) {
            return new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan );
        }


        static FileStream OpenWrite( string fileName ) {
            return new FileStream( fileName, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize );
        }

        #endregion


        #region Scheduled Saving

        static SchedulerTask saveTask;

        /// <summary> Interval at which PlayerDB should be saved. Default is 90s. </summary>
        public static TimeSpan SaveInterval {
            get { return saveInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException( "Save interval may not be negative" );
                saveInterval = value;
                if( saveTask != null ) saveTask.Interval = value;
            }
        }
        static TimeSpan saveInterval = TimeSpan.FromSeconds( 120 );

        internal static void StartSaveTask() {
            saveTask = Scheduler.NewBackgroundTask( SaveTask );
            saveTask.IsCritical = true;
            saveTask.RunForever( SaveInterval, SaveInterval + TimeSpan.FromSeconds( 15 ) );
        }

        static void SaveTask( SchedulerTask task ) {
            Save();
        }

        #endregion


        #region Lookup

        [NotNull]
        public static PlayerInfo FindOrCreateInfoForPlayer( [NotNull] string givenName, [NotNull] IPAddress lastIP ) {
            if( givenName == null ) throw new ArgumentNullException( "givenName" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            CheckIfLoaded();
            PlayerInfo info;

            // this flag is used to avoid raising PlayerInfoCreated event inside the lock
            bool raiseCreatedEvent = false;

            bool isEmail = false;
            string name = givenName;

            // handle email accounts
            if( name.Contains( '@' ) ) {
                isEmail = true;
                name = EmailToPlayerName( name );
            }

            lock( AddLocker ) {
                if( isEmail ) {
                    // special treatment for email accounts
                    int i = 1;
                    while( true ) {
                        string newName = name + (i > 1 ? ("@" + i) : "@");
                        info = Trie.Get( newName );
                        if( info == null ) {
                            // found a new player, not in the database
                            name = newName;
                            break;
                        } else if( givenName.CaselessEquals( info.Email ) ) {
                            // player with matching email found
                            return info;
                        } else {
                            // increment number and retry
                            i++;
                            if( (name + "@" + i).Length > 16 ) {
                                name = name.Substring( 0, name.Length - 1 );
                            }
                        }
                    }
                } else {
                    info = Trie.Get( name );
                }

                if( info == null ) {
                    var e = new PlayerInfoBeingCreatedEventArgs( name, lastIP, RankManager.DefaultRank, false );
                    PlayerInfo.RaiseCreatingEvent( e );
                    if( e.Cancel ) throw new OperationCanceledException( "Cancelled by a plugin." );

                    info = new PlayerInfo( name, lastIP, e.StartingRank );
                    if( isEmail ) {
                        info.Email = givenName;
                        info.AccountType = AccountType.Free;
                    }
                    Trie.Add( name, info );
                    list.Add( info );
                    UpdateCache();

                    raiseCreatedEvent = true;
                }
            }

            if( raiseCreatedEvent ) {
                PlayerInfo.RaiseCreatedEvent( info, false );
            }
            return info;
        }


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] IPAddress address ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            return FindPlayers( address, Int32.MaxValue );
        }


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] IPAddress address, int limit ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( limit < 0 ) throw new ArgumentOutOfRangeException( "limit" );
            CheckIfLoaded();
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            
            PlayerInfo[] cache = PlayerInfoList;
            for( int i = 0; i < cache.Length; i++ ) {
                if( cache[i].LastIP.Equals( address ) ) {
                    result.Add( cache[i] );
                    count++;
                    if( count >= limit ) return result.ToArray();
                }
            }
            return result.ToArray();
        }


        [NotNull]
        public static PlayerInfo[] FindPlayersCidr( [NotNull] IPAddress address, byte range ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            return FindPlayersCidr( address, range, Int32.MaxValue );
        }


        [NotNull]
        public static PlayerInfo[] FindPlayersCidr( [NotNull] IPAddress address, byte range, int limit ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            if( limit < 0 ) throw new ArgumentOutOfRangeException( "limit" );
            CheckIfLoaded();
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            uint addressInt = address.AsUInt();
            uint netMask = IPAddressUtil.NetMask( range );
            
            PlayerInfo[] cache = PlayerInfoList;
            for( int i = 0; i < cache.Length; i++ ) {
                if( cache[i].LastIP.Match( addressInt, netMask ) ) {
                    result.Add( cache[i] );
                    count++;
                    if( count >= limit ) return result.ToArray();
                }
            }
            return result.ToArray();
        }


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] Regex regex ) {
            if( regex == null ) throw new ArgumentNullException( "regex" );
            return FindPlayers( regex, Int32.MaxValue );
        }


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] Regex regex, int limit ) {
            if( regex == null ) throw new ArgumentNullException( "regex" );
            CheckIfLoaded();
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            
            PlayerInfo[] cache = PlayerInfoList;
            for( int i = 0; i < cache.Length; i++ ) {
                if( regex.IsMatch( cache[i].Name ) ) {
                    result.Add( cache[i] );
                    count++;
                    if( count >= limit ) break;
                }
            }
            return result.ToArray();
        }


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] string namePart ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            return FindPlayers( namePart, Int32.MaxValue );
        }


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] string namePart, int limit ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            CheckIfLoaded();
            lock( AddLocker ) {
                return Trie.GetList( namePart, limit ).ToArray();
            }
        }


        /// <summary> Searches for player names starting with namePart, returning just one or none of the matches. </summary>
        /// <param name="namePart"> Partial or full player name. </param>
        /// <param name="info"> PlayerInfo to output (will be set to null if no single match was found) </param>
        /// <returns> true if one or zero matches were found, false if multiple matches were found </returns>
        internal static bool FindPlayerInfo( [NotNull] string namePart, out PlayerInfo info ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            CheckIfLoaded();
            lock( AddLocker ) {
                return Trie.GetOneMatch( namePart, out info );
            }
        }


        /// <summary> Finds player by exact name. </summary>
        /// <param name="fullName"> Full, case-insensitive name of the player. </param>
        /// <returns> PlayerInfo object if the player was found. Null if not found. </returns>
        [CanBeNull]
        public static PlayerInfo FindPlayerInfoExact( [NotNull] string fullName ) {
            if( fullName == null ) throw new ArgumentNullException( "fullName" );
            CheckIfLoaded();
            lock( AddLocker ) {
                return Trie.Get( fullName );
            }
        }

        const int MatchesToPrint = 25;

        /// <summary> Searches for players whose names start with <paramref name="namePart"/>.
        /// If exactly one player matched, returns the corresponding PlayerInfo object.
        /// If name format is incorrect, or if no matches were found, an appropriate message is printed to the player.
        /// If multiple players were found matching the namePart, first 25 matches are printed. </summary>
        /// <param name="player"> Player to print feedback to. Also used to determine visibility, for sorting. </param>
        /// <param name="namePart"> Partial or full player name. </param>
        /// <param name="options"> Search options.
        /// IncludeSelf and ReturnSelfIfOnlyMatch flags are applicable, other flags are ignored. </param>
        /// <returns> PlayerInfo object if one player was found. Null if no or multiple matches were found. 
        /// Results are sorted using PlayerInfoComparer. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="player"/> or <paramref name="namePart"/> is null. </exception>
        [CanBeNull]
        public static PlayerInfo FindPlayerInfoOrPrintMatches([NotNull] Player player, [NotNull] string namePart,
                                                              SearchOptions options)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (namePart == null) throw new ArgumentNullException("namePart");
            CheckIfLoaded();

            bool includeSelf = (options & SearchOptions.IncludeSelf) != 0;
            bool returnSelf = (options & SearchOptions.ReturnSelfIfOnlyMatch) != 0;

            // If name starts with '!', return matches for online players only
            if (namePart.Length > 1 && namePart[0] == '!')
            {
                namePart = namePart.Substring(1);
                Player targetPlayer = Server.FindPlayerOrPrintMatches(player, namePart, options);
                if (targetPlayer != null)
                {
                    return targetPlayer.Info;
                }
                else
                {
                    player.Message("No online players found matching \"{0}\"", namePart);
                    return null;
                }
            }

            // Repeat last-used player name
            if (namePart == "-")
            {
                if (player.LastUsedPlayerName != null)
                {
                    namePart = player.LastUsedPlayerName;
                }
                else
                {
                    player.Message("Cannot repeat player name: you haven't used any names yet.");
                    return null;
                }
            }

            // Make sure player name is valid
            if (!Player.ContainsValidCharacters(namePart))
            {
                player.MessageInvalidPlayerName(namePart);
                return null;
            }

            // Search for exact matches first
            PlayerInfo target = FindPlayerInfoExact(namePart);

            // If no exact match was found, look for partial matches
            if (target == null || target == player.Info && !includeSelf)
            {
                PlayerInfo[] targets = FindPlayers(namePart);

                if (!includeSelf)
                {
                    // special handling if IncludeSelf flag is not set
                    if (targets.Length > 1)
                    {
                        targets = targets.Where(p => p != player.Info).ToArray();
                    }
                    else if (!returnSelf && targets.Length == 1 && targets[0] == player.Info)
                    {
                        // special handling if ReturnSelfIfOnlyMatch flag is not set
                        targets = new PlayerInfo[0];
                    }
                }

                if (targets.Length == 0)
                {
                    // No matches
                    player.MessageNoPlayer(namePart);
                    return null;

                }
                else if (targets.Length > 1)
                {
                    // More than one match
                    Array.Sort(targets, new PlayerInfoComparer(player));
                    player.MessageManyMatches("player", targets.Take(MatchesToPrint).ToArray());
                    return null;

                } // else: one match!
                target = targets[0];
            }

            // If a single name has been found, set it as LastUsedPlayerName
            if (includeSelf || target != player.Info)
            {
                player.LastUsedPlayerName = target.Name;
            }
            return target;
        }


        [NotNull]
        public static string FindExactClassyName( [CanBeNull] string name ) {
            if( string.IsNullOrEmpty( name ) ) return "?";
            PlayerInfo info = FindPlayerInfoExact( name );
            if( info == null ) return name;
            else return info.ClassyName;
        }

        #endregion


        #region Stats

        public static int BannedCount {
            get {
                return PlayerInfoList.Count( t => t.IsBanned );
            }
        }


        public static float BannedPercentage {
            get {
                var listCache = PlayerInfoList;
                if( listCache.Length == 0 ) {
                    return 0;
                } else {
                    return listCache.Count( t => t.IsBanned ) * 100f / listCache.Length;
                }
            }
        }


        public static int Size {
            get {
                return Trie.Count;
            }
        }

        #endregion


        public static int GetNextID() {
            return Interlocked.Increment( ref maxID );
        }


        /// <summary> Finds PlayerInfo by ID. Returns null of not found. </summary>
        [CanBeNull]
        public static PlayerInfo FindPlayerInfoByID( int id ) {
            if( id < 0 ) throw new ArgumentOutOfRangeException( "id" );
            CheckIfLoaded();
            PlayerInfo dummy = new PlayerInfo( id );
            lock( AddLocker ) {
                int index = list.BinarySearch( dummy, PlayerIDComparer.Instance );
                if( index >= 0 ) {
                    return list[index];
                } else {
                    return null;
                }
            }
        }


        public static int MassRankChange( [NotNull] Player player, [NotNull] Rank from, [NotNull] Rank to, [NotNull] string reason ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( from == null ) throw new ArgumentNullException( "from" );
            if( to == null ) throw new ArgumentNullException( "to" );
            if( reason == null ) throw new ArgumentNullException( "reason" );
            CheckIfLoaded();
            int affected = 0;
            string fullReason = reason + "~MassRank";
            lock( AddLocker ) {
                for( int i = 0; i < PlayerInfoList.Length; i++ ) {
                    if( PlayerInfoList[i].Rank == from ) {
                        try {
                            list[i].ChangeRank( player, to, fullReason, false, true, false );
                        } catch( PlayerOpException ex ) {
                            player.Message( ex.MessageColored );
                        }
                        affected++;
                    }
                }
                return affected;
            }
        }


        static void UpdateCache() {
            lock( AddLocker ) {
                PlayerInfoList = list.ToArray();
            }
        }


        #region Experimental & Debug things

        internal static int CountInactivePlayers() {
            lock( AddLocker ) {
                Dictionary<IPAddress, List<PlayerInfo>> playersByIP = new Dictionary<IPAddress, List<PlayerInfo>>();
                PlayerInfo[] cache = PlayerInfoList;
                for( int i = 0; i < cache.Length; i++ ) {
                    if( !playersByIP.ContainsKey( cache[i].LastIP ) ) {
                        playersByIP[cache[i].LastIP] = new List<PlayerInfo>();
                    }
                    playersByIP[cache[i].LastIP].Add( PlayerInfoList[i] );
                }

                return cache.Count( t => PlayerIsInactive( playersByIP, t, true ) );
            }
        }


        internal static int RemoveInactivePlayers() {
            int count = 0;
            lock( AddLocker ) {
                Dictionary<IPAddress, List<PlayerInfo>> playersByIP = new Dictionary<IPAddress, List<PlayerInfo>>();
                PlayerInfo[] playerInfoListCache = PlayerInfoList;
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    if( !playersByIP.ContainsKey( playerInfoListCache[i].LastIP ) ) {
                        playersByIP[playerInfoListCache[i].LastIP] = new List<PlayerInfo>();
                    }
                    playersByIP[playerInfoListCache[i].LastIP].Add( PlayerInfoList[i] );
                }
                List<PlayerInfo> newList = new List<PlayerInfo>();
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    PlayerInfo p = playerInfoListCache[i];
                    if( PlayerIsInactive( playersByIP, p, true ) ) {
                        count++;
                    } else {
                        newList.Add( p );
                    }
                }

                list = newList;
                Trie.Clear();
                foreach( PlayerInfo p in list ) {
                    Trie.Add( p.Name, p );
                }

                list.TrimExcess();
                UpdateCache();
            }
            return count;
        }


        static bool PlayerIsInactive( [NotNull] IDictionary<IPAddress, List<PlayerInfo>> playersByIP, [NotNull] PlayerInfo player, bool checkIP ) {
            if( playersByIP == null ) throw new ArgumentNullException( "playersByIP" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( player.BanStatus != BanStatus.NotBanned || player.UnbanDate != DateTime.MinValue ||
                player.IsFrozen || player.IsMuted || player.TimesKicked != 0 ||
                player.Rank != RankManager.DefaultRank || player.PreviousRank != null ) {
                return false;
            }
            if( player.TotalTime.TotalMinutes > 30 || player.TimeSinceLastSeen.TotalDays < 30 ) {
                return false;
            }
            if( IPBanList.Get( player.LastIP ) != null ) {
                return false;
            }
            if( checkIP ) {
                return playersByIP[player.LastIP].All( other => (other == player) || PlayerIsInactive( playersByIP, other, false ) );
            }
            return true;
        }


        internal static void SwapPlayerInfo( [NotNull] PlayerInfo p1, [NotNull] PlayerInfo p2 ) {
            if( p1 == null ) throw new ArgumentNullException( "p1" );
            if( p2 == null ) throw new ArgumentNullException( "p2" );
            lock( AddLocker ) {
                lock( SaveLoadLocker ) {
                    if( p1.IsOnline || p2.IsOnline ) {
                        throw new Exception( "Both players must be offline to swap info." );
                    }
                    Swap( ref p1.BanDate, ref p2.BanDate );
                    Swap( ref p1.BandwidthUseMode, ref p2.BandwidthUseMode );
                    Swap( ref p1.BanStatus, ref p2.BanStatus );
                    Swap( ref p1.BannedBy, ref p2.BannedBy );
                    Swap( ref p1.BannedUntil, ref p2.BannedUntil );
                    Swap( ref p1.BanReason, ref p2.BanReason );
                    Swap( ref p1.BlocksBuilt, ref p2.BlocksBuilt );
                    Swap( ref p1.BlocksDeleted, ref p2.BlocksDeleted );
                    Swap( ref p1.BlocksDrawn, ref p2.BlocksDrawn );
                    Swap( ref p1.DisplayedName, ref p2.DisplayedName );
                    Swap( ref p1.FirstLoginDate, ref p2.FirstLoginDate );
                    Swap( ref p1.FrozenBy, ref p2.FrozenBy );
                    Swap( ref p1.FrozenOn, ref p2.FrozenOn );
                    Swap( ref p1.ID, ref p2.ID );
                    Swap( ref p1.IsFrozen, ref p2.IsFrozen );
                    //Swap( ref p1.IsHidden, ref p2.IsHidden );
                    Swap( ref p1.LastFailedLoginDate, ref p2.LastFailedLoginDate );
                    Swap( ref p1.LastFailedLoginIP, ref p2.LastFailedLoginIP );
                    //Swap( ref p1.LastIP, ref p2.LastIP );
                    Swap( ref p1.LastKickBy, ref p2.LastKickBy );
                    Swap( ref p1.LastKickDate, ref p2.LastKickDate );
                    Swap( ref p1.LastKickReason, ref p2.LastKickReason );
                    //Swap( ref p1.LastLoginDate, ref p2.LastLoginDate );
                    //Swap( ref p1.LastSeen, ref p2.LastSeen );
                    //Swap( ref p1.LeaveReason, ref p2.LeaveReason );
                    Swap( ref p1.MessagesWritten, ref p2.MessagesWritten );
                    Swap( ref p1.MutedBy, ref p2.MutedBy );
                    Swap( ref p1.MutedUntil, ref p2.MutedUntil );
                    //Swap( ref p1.Name, ref p2.Name );
                    //Swap( ref p1.Online, ref p2.Online );
                    Swap( ref p1.Password, ref p2.Password );
                    //Swap( ref p1.PlayerObject, ref p2.PlayerObject );
                    Swap( ref p1.PreviousRank, ref p2.PreviousRank );

                    Rank p1Rank = p1.Rank;
                    p1.Rank = p2.Rank;
                    p2.Rank = p1Rank;

                    Swap( ref p1.RankChangeDate, ref p2.RankChangeDate );
                    Swap( ref p1.RankChangedBy, ref p2.RankChangedBy );
                    Swap( ref p1.RankChangeReason, ref p2.RankChangeReason );
                    Swap( ref p1.RankChangeType, ref p2.RankChangeType );
                    Swap( ref p1.TimesBannedOthers, ref p2.TimesBannedOthers );
                    Swap( ref p1.TimesKicked, ref p2.TimesKicked );
                    Swap( ref p1.TimesKickedOthers, ref p2.TimesKickedOthers );
                    Swap( ref p1.TimesVisited, ref p2.TimesVisited );
                    Swap( ref p1.TotalTime, ref p2.TotalTime );
                    Swap( ref p1.UnbanDate, ref p2.UnbanDate );
                    Swap( ref p1.UnbannedBy, ref p2.UnbannedBy );
                    Swap( ref p1.UnbanReason, ref p2.UnbanReason );
                    Swap( ref p1.TimesUsedBot, ref p2.TimesUsedBot );
                    Swap( ref p1.PromoCount, ref p2.PromoCount );
                    Swap( ref p1.DemoCount, ref p2.DemoCount );
                    Swap( ref p1.HasRTR, ref p2.HasRTR );
                    Swap( ref p1.ReadIRC, ref p2.ReadIRC );
                    Swap( ref p1.JoinOnRankWorld, ref p2.JoinOnRankWorld );
                    Swap( ref p1.LastWorld, ref p2.LastWorld );
                    Swap( ref p1.LastWorldPos, ref p2.LastWorldPos );

                    list.Sort( PlayerIDComparer.Instance );
                }
            }
        }


        static void Swap<T>( ref T t1, ref T t2 ) {
            var temp = t2;
            t2 = t1;
            t1 = temp;
        }

        #endregion


        sealed class PlayerIDComparer : IComparer<PlayerInfo> {
            public static readonly PlayerIDComparer Instance = new PlayerIDComparer();
            private PlayerIDComparer() { }

            public int Compare( PlayerInfo x, PlayerInfo y ) {
                return x.ID - y.ID;
            }
        }


        #region Escaping

        public static StringBuilder AppendEscaped( [NotNull] this StringBuilder sb, [CanBeNull] string str ) {
            if( sb == null ) throw new ArgumentNullException( "sb" );
            if( !String.IsNullOrEmpty( str ) ) {
                if( str.IndexOf( ',' ) > -1 ) {
                    int startIndex = sb.Length;
                    sb.Append( str );
                    sb.Replace( ',', '\xFF', startIndex, str.Length );
                } else {
                    sb.Append( str );
                }
            }
            return sb;
        }

        static readonly char[] EscapableChars = new[] { ',', '\n', '\r' };
        static readonly char[] EscapedChars = new[] { '\xFF', '\xFE', '\xFD' };

        /// <summary> Escapes characters not suitable for PlayerDB serialization
        /// (commas, newlines \n, and carriage-returns \r). </summary>
        [NotNull]
        public static string Escape( [CanBeNull] string str ) {
            if( String.IsNullOrEmpty( str ) ) {
                return "";
            } else if( str.IndexOfAny( EscapableChars ) > -1 ) {
                str = str.Replace( ',', '\xFF' );
                str = str.Replace( '\n', '\xFE' );
                return str.Replace( '\r', '\xFD' );
            } else {
                return str;
            }
        }


        [NotNull]
        internal static string UnescapeOldFormat( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            return str.Replace( '\xFF', ',' ).Replace( "\'", "'" ).Replace( @"\\", @"\" );
        }


        [NotNull]
        public static string Unescape( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.IndexOfAny( EscapedChars ) > -1 ) {
                str = str.Replace( '\xFF', ',' );
                str = str.Replace( '\xFE', '\n' );
                return str.Replace( '\xFD', '\r' );
            } else {
                return str;
            }
        }

        #endregion

        /// <summary> Creates a regular expression pattern from a wildcard pattern (like the one /Info takes).
        /// Accepts single-character ("?") and zero-or-more-characters ("*") wildcards. </summary>
        [NotNull]
        public static Regex WildcardToRegex( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            string regexString = "^" +
                                 WildcardStripRegex.Replace( name, "" )
                                                   .Replace( ".", "\\." )
                                                   .Replace( "*", ".*" )
                                                   .Replace( "?", "." ) +
                                 "$";
            return new Regex( regexString, RegexOptions.IgnoreCase );
        }

        static readonly Regex WildcardStripRegex = new Regex( @"[^a-zA-Z0-9_\.\*\?@]" );


        // Extracts name from an email address, replaces non-printable characters with underscores,
        // and truncates name to 14 characters. Used on Mojang accounts.
        [NotNull]
        static string EmailToPlayerName( [NotNull] string email ) {
            if( email == null ) throw new ArgumentNullException( "email" );
            string name = email.Substring( 0, email.LastIndexOf( '@' ) );
            name = NonNameCharsRegex.Replace( name, "_" );
            if( name.Length == 0 ) {
                name = "_";
            } else if( name.Length > 15 ) {
                name = name.Substring( 0, 15 );
            }
            return name;
        }

        static readonly Regex NonNameCharsRegex = new Regex( @"[^a-zA-Z0-9_.]" );
    }
}
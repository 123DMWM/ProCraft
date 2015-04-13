// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using fCraft.AutoRank;
using fCraft.Drawing;
using fCraft.Events;
using JetBrains.Annotations;
using ThreadState = System.Threading.ThreadState;
using fCraft.Portals;

namespace fCraft {
    /// <summary> Core of an fCraft server. Manages startup/shutdown, online player
    /// sessions, global events and scheduled tasks. </summary>
    public static partial class Server {
        /// <summary> Time when the server started (UTC). Used to check uptime. </summary>
        public static DateTime StartTime { get; private set; }

        /// <summary> Internal IP address that the server's bound to (0.0.0.0 if not explicitly specified by the user). </summary>
        public static IPAddress InternalIP { get; private set; }

        /// <summary> External IP address of this machine, as reported by http://www.classicube.net/api/myip/ </summary>
        public static IPAddress ExternalIP { get; private set; }

        /// <summary> Number of the local listening port. </summary>
        public static int Port { get; private set; }

        /// <summary> Minecraft.net connection URL. </summary>
        public static Uri Uri { get; internal set; }

        /// <summary> Number for determining color of day. </summary>
        public static byte ColorTime = 1;

        /// <summary> Number for determining color of day. </summary>
        public static Dictionary<byte, String> SkyColorHex = new Dictionary<byte, string> {
            {1,  "000000"},
            {2,  "000000"},
            {3,  "000003"},
            {4,  "000108"},
            {5,  "000110"},
            {6,  "000117"},
            {7,  "01011F"},
            {8,  "010128"},
            {9,  "02002E"},
            {10, "020133"},
            {11, "03053C"},
            {12, "070D50"},
            {13, "0F196A"},
            {14, "1A2883"},
            {15, "243694"},
            {16, "2F4499"},
            {17, "354F95"},
            {18, "385693"},
            {19, "3A5E98"},
            {20, "3F66A1"},
            {21, "4670AB"},
            {22, "4E7AB5"},
            {23, "5585C1"},
            {24, "5D90CC"},
            {25, "649AD6"},
            {26, "6BA2DF"},
            {27, "71A8E6"},
            {28, "75ACEA"},
            {29, "78AFEC"},
            {30, "79B0EC"},
            {31, "79B0EC"},
            {32, "78AFEC"},
            {33, "75ACEA"},
            {34, "71A8E6"},
            {35, "6BA2DF"},
            {36, "649AD6"},
            {37, "5D90CC"},
            {38, "5585C1"},
            {39, "4E7AB5"},
            {40, "4670AB"},
            {41, "3F66A1"},
            {42, "3A5E98"},
            {43, "385693"},
            {44, "354F95"},
            {45, "2F4499"},
            {46, "243694"},
            {47, "1A2883"},
            {48, "0F196A"},
            {49, "070D50"},
            {50, "03053C"},
            {51, "020133"},
            {52, "02002E"},
            {53, "010128"},
            {54, "01011F"},
            {55, "000117"},
            {56, "000110"},
            {57, "000108"},
            {58, "000003"},
            {59, "000000"},
            {60, "000000"}
        };

        /// <summary> Number for determining color of day. </summary>
        public static Dictionary<byte, String> CloudAndFogColorHex = new Dictionary<byte, string> {
            {1, "444444"},
            {2, "444444"},
            {3, "444444"},
            {4, "444444"},
            {5, "444444"},
            {6, "444444"},
            {7, "444444"},
            {8, "444444"},
            {9, "444444"},
            {10, "444444"},
            {11, "515151"},
            {12, "5D5D5D"},
            {13, "686868"},
            {14, "717171"},
            {15, "797979"},
            {16, "7F7F7F"},
            {17, "858585"},
            {18, "8A8A8A"},
            {19, "919191"},
            {20, "999999"},
            {21, "A2A2A2"},
            {22, "AEAEAE"},
            {23, "BBBBBB"},
            {24, "C8C8C8"},
            {25, "D5D5D5"},
            {26, "E0E0E0"},
            {27, "EAEAEA"},
            {28, "F1F1F1"},
            {29, "F5F5F5"},
            {30, "F7F7F7"},
            {31, "F7F7F7"},
            {32, "F5F5F5"},
            {33, "F1F1F1"},
            {34, "EAEAEA"},
            {35, "E0E0E0"},
            {36, "D5D5D5"},
            {37, "C8C8C8"},
            {38, "BBBBBB"},
            {39, "AEAEAE"},
            {40, "A2A2A2"},
            {41, "999999"},
            {42, "919191"},
            {43, "8A8A8A"},
            {44, "858585"},
            {45, "7F7F7F"},
            {46, "797979"},
            {47, "717171"},
            {48, "686868"},
            {49, "5D5D5D"},
            {50, "515151"},
            {51, "444444"},
            {52, "444444"},
            {53, "444444"},
            {54, "444444"},
            {55, "444444"},
            {56, "444444"},
            {57, "444444"},
            {58, "444444"},
            {59, "444444"},
            {60, "444444"}
        };
        


    internal static int MaxUploadSpeed, // set by Config.ApplyConfig
                            BlockUpdateThrottling; // used when there are no players in a world
        internal const int MaxSessionPacketsPerTick = 128, // used when there are no players in a world
                           MaxBlockUpdatesPerTick = 100000; // used when there are no players in a world
        internal static float TicksPerSecond;

        static TcpListener listener;


        #region Command-line args

        static readonly Dictionary<ArgKey, string> Args = new Dictionary<ArgKey, string>();


        /// <summary> Returns value of a given command-line argument (if present). Use HasArg to check flag arguments. </summary>
        /// <param name="key"> Command-line argument name (enumerated) </param>
        /// <returns> Value of the command-line argument, or null if this argument was not set or argument is a flag. </returns>
        [CanBeNull]
        public static string GetArg( ArgKey key ) {
            if( Args.ContainsKey( key ) ) {
                return Args[key];
            } else {
                return null;
            }
        }


        /// <summary> Checks whether a command-line argument was set. </summary>
        /// <param name="key"> Command-line argument name (enumerated) </param>
        /// <returns> True if given argument was given. Otherwise false. </returns>
        public static bool HasArg( ArgKey key ) {
            return Args.ContainsKey( key );
        }


        /// <summary> Produces a string containing all recognized arguments that wereset/passed to this instance of fCraft. </summary>
        /// <returns> A string containing all given arguments, or an empty string if none were set. </returns>
        public static string GetArgString() {
            return String.Join( " ", GetArgList() );
        }


        /// <summary> Produces a list of arguments that were passed to this instance of fCraft. </summary>
        /// <returns> An array of strings, formatted as --key="value" (or, for flag arguments, --key).
        /// Returns an empty string array if no arguments were set. </returns>
        public static string[] GetArgList() {
            List<string> argList = new List<string>();
            foreach( var pair in Args ) {
                if( pair.Value != null ) {
                    argList.Add( String.Format( "--{0}=\"{1}\"", pair.Key.ToString().ToLower(), pair.Value ) );
                } else {
                    argList.Add( String.Format( "--{0}", pair.Key.ToString().ToLower() ) );
                }
            }
            return argList.ToArray();
        }

        #endregion


        #region Initialization and Startup

        // flags used to ensure proper initialization order
        static bool libraryInitialized,
                    serverInitialized;

        public static bool IsRunning { get; private set; }


        /// <summary> Reads command-line switches and sets up paths and logging.
        /// This should be called before any other library function.
        /// Note to frontend devs: Subscribe to log-related events before calling this.
        /// Does not raise any events besides Logger.Logged.
        /// Throws exceptions on failure. </summary>
        /// <param name="rawArgs"> string arguments passed to the frontend (if any). </param>
        /// <exception cref="System.InvalidOperationException"> If library is already initialized. </exception>
        /// <exception cref="System.IO.IOException"> Working path, log path, or map path could not be set. </exception>
        public static void InitLibrary( [NotNull] IEnumerable<string> rawArgs ) {
            if( rawArgs == null ) throw new ArgumentNullException( "rawArgs" );
            if( libraryInitialized ) {
                throw new InvalidOperationException( "ProCraft library is already initialized" );
            }

            ServicePointManager.Expect100Continue = false;

            // try to parse arguments
            foreach( string arg in rawArgs ) {
                if( arg.StartsWith( "--" ) ) {
                    string argKeyName, argValue;
                    if( arg.Contains( '=' ) ) {
                        argKeyName = arg.Substring( 2, arg.IndexOf( '=' ) - 2 ).ToLower().Trim();
                        argValue = arg.Substring( arg.IndexOf( '=' ) + 1 ).Trim();
                        if( argValue.StartsWith( "\"" ) && argValue.EndsWith( "\"" ) ) {
                            argValue = argValue.Substring( 1, argValue.Length - 2 );
                        }

                    } else {
                        argKeyName = arg.Substring( 2 );
                        argValue = null;
                    }
                    ArgKey key;
                    if( EnumUtil.TryParse( argKeyName, out key, true ) ) {
                        Args.Add( key, argValue );
                    } else {
                        Console.Error.WriteLine( "Unknown argument: {0}", arg );
                    }
                } else {
                    Console.Error.WriteLine( "Unknown argument: {0}", arg );
                }
            }

            // before we do anything, set path to the default location
            Directory.SetCurrentDirectory( Paths.WorkingPath );

            // set custom working path (if specified)
            string path = GetArg( ArgKey.Path );
            if( path != null && Paths.TestDirectory( "WorkingPath", path, true ) ) {
                Paths.WorkingPath = Path.GetFullPath( path );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else if( Paths.TestDirectory( "WorkingPath", Paths.WorkingPathDefault, true ) ) {
                Paths.WorkingPath = Path.GetFullPath( Paths.WorkingPathDefault );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else {
                throw new IOException( "Could not set the working path." );
            }

            // set log path
            string logPath = GetArg( ArgKey.LogPath );
            if( logPath != null && Paths.TestDirectory( "LogPath", logPath, true ) ) {
                Paths.LogPath = Path.GetFullPath( logPath );
            } else if( Paths.TestDirectory( "LogPath", Paths.LogPathDefault, true ) ) {
                Paths.LogPath = Path.GetFullPath( Paths.LogPathDefault );
            } else {
                throw new IOException( "Could not set the log path." );
            }

            if( HasArg( ArgKey.NoLog ) ) {
                Logger.Enabled = false;
            } else {
                Logger.MarkLogStart();
            }

            // set map path
            string mapPath = GetArg( ArgKey.MapPath );
            if( mapPath != null && Paths.TestDirectory( "MapPath", mapPath, true ) ) {
                Paths.MapPath = Path.GetFullPath( mapPath );
                Paths.IgnoreMapPathConfigKey = true;
            } else if( Paths.TestDirectory( "MapPath", Paths.MapPathDefault, true ) ) {
                Paths.MapPath = Path.GetFullPath( Paths.MapPathDefault );
            } else {
                throw new IOException( "Could not set the map path." );
            }

            // set config path
            Paths.ConfigFileName = Paths.ConfigFileNameDefault;
            string configFile = GetArg( ArgKey.Config );
            if( configFile != null ) {
                if( Paths.TestFile( "config.xml", configFile, false, FileAccess.Read ) ) {
                    Paths.ConfigFileName = new FileInfo( configFile ).FullName;
                }
            }

            if( MonoCompat.IsMono ) {
                Logger.Log( LogType.Debug, "Running on Mono {0}", MonoCompat.MonoVersion );
            }

#if DEBUG_EVENTS
            Logger.PrepareEventTracing();
#endif

            Logger.Log( LogType.Debug, "&3Working directory: {0}", Directory.GetCurrentDirectory() );
            Logger.Log( LogType.Debug, "&3Log path: {0}", Path.GetFullPath( Paths.LogPath ) );
            Logger.Log( LogType.Debug, "&3Map path: {0}", Path.GetFullPath( Paths.MapPath ) );
            Logger.Log( LogType.Debug, "&3Config path: {0}", Path.GetFullPath( Paths.ConfigFileName ) );

            libraryInitialized = true;
        }


        /// <summary> Initialized various server subsystems. This should be called after InitLibrary and before StartServer.
        /// Loads config, PlayerDB, IP bans, AutoRank settings, builds a list of commands, and prepares the IRC bot.
        /// Raises Server.Initializing and Server.Initialized events, and possibly Logger.Logged events.
        /// Throws exceptions on failure. </summary>
        /// <exception cref="System.InvalidOperationException"> Library is not initialized, or server is already initialzied. </exception>
        public static void InitServer() {
            if( serverInitialized ) {
                throw new InvalidOperationException( "Server is already initialized" );
            }
            if( !libraryInitialized ) {
                throw new InvalidOperationException( "Server.InitLibrary must be called before Server.InitServer" );
            }
            RaiseEvent( Initializing );

            // Instantiate DeflateStream to make sure that libMonoPosix is present.
            // This allows catching misconfigured Mono installs early, and stopping the server.
            using( var testMemStream = new MemoryStream() ) {
                using( new DeflateStream( testMemStream, CompressionMode.Compress ) ) {}
            }


            if( MonoCompat.IsMono && !MonoCompat.IsSGenCapable ) {
                Logger.Log( LogType.Warning,
                            "You are using a relatively old version of the Mono runtime ({0}). " +
                            "It is recommended that you upgrade to at least 2.8+",
                            MonoCompat.MonoVersion );
            }

#if DEBUG
            Config.RunSelfTest();
#else
            // delete the old updater, if exists
            File.Delete( Paths.UpdaterFileName );
            File.Delete( "fCraftUpdater.exe" ); // pre-0.600
#endif

            // try to load the config
            if( !Config.Load( false, false ) ) {
                throw new Exception( "ProCraft Config failed to initialize" );
            }

            if( ConfigKey.VerifyNames.GetEnum<NameVerificationMode>() == NameVerificationMode.Never ) {
                Logger.Log( LogType.Warning,
                            "Name verification is currently OFF. Your server is at risk of being hacked. " +
                            "Enable name verification as soon as possible." );
            }

            // load player DB
            PlayerDB.Load();
            IPBanList.Load();

            // prepare the list of commands
            CommandManager.Init();

            // prepare the brushes
            BrushManager.Init();

            // Init IRC
            IRC.Init();

            if( ConfigKey.AutoRankEnabled.Enabled() ) {
                AutoRankManager.Init();
            }

            RaiseEvent( Initialized );

            serverInitialized = true;
        }


        /// <summary> Starts the server:
        /// Creates Console pseudoplayer, loads the world list, starts listening for incoming connections,
        /// sets up scheduled tasks and starts the scheduler, starts the heartbeat, and connects to IRC.
        /// Raises Server.Starting and Server.Started events.
        /// May throw an exception on hard failure. </summary>
        /// <returns> True if server started normally, false on soft failure. </returns>
        /// <exception cref="System.InvalidOperationException"> Server is already running, or server/library have not been initailized. </exception>
        public static bool StartServer() {
            if( IsRunning ) {
                throw new InvalidOperationException( "Server is already running" );
            }
            if( !libraryInitialized || !serverInitialized ) {
                throw new InvalidOperationException(
                    "Server.InitLibrary and Server.InitServer must be called before Server.StartServer" );
            }

            StartTime = DateTime.UtcNow;
            cpuUsageStartingOffset = Process.GetCurrentProcess().TotalProcessorTime;
            Players = new Player[0];

            RaiseEvent( Starting );

            if( ConfigKey.BackupDataOnStartup.Enabled() ) {
                BackupData();
            }

            Player.Console = new Player( ConfigKey.ConsoleName.GetString() );
            Player.AutoRank = new Player( "(AutoRank)" );

            // Back up server data (PlayerDB, worlds, bans, config)
            if( ConfigKey.BlockDBEnabled.Enabled() ) {
                BlockDB.Init();
            }

            // Load the world list
            if( !WorldManager.LoadWorldList() ) return false;
            WorldManager.SaveWorldList();

            // Back up all worlds (if needed)
            if( ConfigKey.BackupOnStartup.Enabled() ) {
                foreach( World world in WorldManager.Worlds ) {
                    string backupFileName = String.Format( World.TimedBackupFormat,
                                                           world.Name, DateTime.Now ); // localized
                    world.SaveBackup( Path.Combine( Paths.BackupPath, backupFileName ) );
                }
            }

            // open the port
            Port = ConfigKey.Port.GetInt();
            InternalIP = IPAddress.Parse( ConfigKey.IP.GetString() );

            try {
                listener = new TcpListener( InternalIP, Port );
                listener.Start();

            } catch( Exception ex ) {
                // if the port is unavailable
                Logger.Log( LogType.Error,
                            "Could not start listening on port {0}, stopping. ({1})",
                            Port, ex.Message );
                if( !ConfigKey.IP.IsDefault() ) {
                    Logger.Log( LogType.Warning,
                                "Do not use the \"Designated IP\" setting unless you have multiple NICs or IPs." );
                }
                return false;
            }

            // Resolve internal and external IP addresses
            InternalIP = ( (IPEndPoint)listener.LocalEndpoint ).Address;
            ExternalIP = CheckExternalIP();

            if( ExternalIP == null ) {
                Logger.Log( LogType.SystemActivity,
                            "&3Server.Run: now accepting connections on port {0}", Port );
            } else {
                Logger.Log( LogType.SystemActivity,
                            "&3Server.Run: now accepting connections at {0}:{1}",
                            ExternalIP, Port );
            }

            // list loaded worlds
            WorldManager.UpdateWorldList();
            Logger.Log( LogType.SystemActivity,
                        "&3All available worlds: {0}",
                        WorldManager.Worlds.JoinToString( ", ", w => w.ClassyName ) );

            Logger.Log( LogType.SystemActivity,
                        "&3Main world: {0}&3; default rank: {1}",
                        WorldManager.MainWorld.ClassyName, RankManager.DefaultRank.ClassyName );

            // Check for incoming connections (every 250ms)
            checkConnectionsTask = Scheduler.NewTask( CheckConnections ).RunForever( CheckConnectionsInterval );

            // Check for idles (every 1s)
            checkIdlesTask = Scheduler.NewTask( CheckIdles ).RunForever( CheckIdlesInterval );// Check for idles (every 30s)
            tabListTask = Scheduler.NewTask( TabList ).RunForever( CheckIdlesInterval );

            // Monitor CPU usage (every 30s)
            try {
                MonitorProcessorUsage( null );
                Scheduler.NewTask( MonitorProcessorUsage ).RunForever( MonitorProcessorUsageInterval,
                                                                       MonitorProcessorUsageInterval );
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "Server.StartServer: Could not start monitoring CPU use: {0}", ex );
            }

            // PlayerDB saving (every 90s)
            PlayerDB.StartSaveTask();

            // Announcements
            if( ConfigKey.AnnouncementInterval.GetInt() > 0 ) {
                TimeSpan announcementInterval = TimeSpan.FromMinutes( ConfigKey.AnnouncementInterval.GetInt() );
                Scheduler.NewTask( ShowRandomAnnouncement ).RunForever( announcementInterval );
                Scheduler.NewTask( RemoveRandomAnnouncement ).RunForever( announcementInterval, new TimeSpan(0, 0, 5));
            }
            Scheduler.NewTask(ChangeWorldColors).RunForever(TimeSpan.FromMinutes(1));

            #region LoadTimers
            try
            {
                //Load Timers.
                if (Directory.Exists("./Timers"))
                {
                    string[] TimersFileList = Directory.GetFiles("./Timers");
                    foreach (string filename in TimersFileList)
                    {
                        if (Path.GetExtension("./Timers/" + filename) == ".txt")
                        {
                            string[] TimerData = File.ReadAllLines(filename);
                            DateTime StartDate = DateTime.UtcNow;
                            DateTime EndDate = StartDate;
                            PlayerInfo CreatedBy = Player.Console.Info;
                            string TimerMessage = null;
                            foreach (string line in TimerData)
                            {
                                if (line.Contains("StartDate: "))
                                {
                                    string date = line.Remove(0, "StartDate: ".Length);
                                    if (DateTime.TryParse(date, out StartDate))
                                    {
                                        StartDate = DateTime.Parse(date);
                                    }
                                }
                                else if (line.Contains("EndDate: "))
                                {
                                    string date = line.Remove(0, "EndDate: ".Length);
                                    if (DateTime.TryParse(date, out EndDate))
                                    {
                                        EndDate = DateTime.Parse(date);
                                    }
                                }
                                else if (line.Contains("CreatedBy: "))
                                {
                                    string creator = line.Remove(0, "CreatedBy: ".Length);
                                    if (PlayerDB.FindPlayerInfoExact(creator) != null)
                                    {
                                        CreatedBy = PlayerDB.FindPlayerInfoExact(creator);
                                    }
                                }
                                else if (line.Contains("Message: "))
                                {
                                    TimerMessage = line.Remove(0, "Creator: ".Length);
                                }
                            }
                            if (StartDate == null || EndDate == null || CreatedBy == null || TimerMessage == null)
                            {
                                Player.Console.Message("Error starting a Timer: {0}, {1}, {2}, {3}", StartDate.ToString(), EndDate.ToString(), CreatedBy.Name, TimerMessage);
                                continue;
                            }
                            if (DateTime.Compare(EndDate, DateTime.UtcNow) <= 0)
                            {
                                Player.Console.Message("Timer Expired: {0}, {1}, {2}, {3} Time Now: {4}", StartDate.ToString(), EndDate.ToString(), CreatedBy.Name, TimerMessage, DateTime.UtcNow.ToString());
                                if (Directory.Exists("./Timers"))
                                {
                                    Player.Console.Message(filename);
                                    if (File.Exists(filename))
                                    {
                                        File.Delete(filename);
                                    }
                                }
                                continue;
                            }
                            if ((StartDate != EndDate) && (CreatedBy != null) && (TimerMessage != null))
                            {
                                ChatTimer.Start((EndDate - DateTime.UtcNow), TimerMessage, CreatedBy.Name);
                                continue;
                            }
                        }
                    }
                    if (TimersFileList.Length > 0) Player.Console.Message("All Timers Loaded. ({0})", TimersFileList.Length);
                    else Player.Console.Message("No Timers Were Loaded.");
                }
            }
            catch (Exception ex)
            {
                Player.Console.Message("Timer Loader Has Crashed: {0}", ex);
            }
            #endregion

            #region LoadReports

            try {
                if (Directory.Exists("./Reports")) {
                    string[] ReportFileList = Directory.GetFiles("./Reports");
                    int created = 0;
                    foreach (string filename in ReportFileList) {
                        Report rCreate = new Report();
                        if (Path.GetExtension("./Reports/" + filename) == ".txt") {
                            string[] reportData = File.ReadAllLines(filename);
                            string idString = filename.Replace("./Reports\\", "").Replace(".txt", "").Split('-')[0];
                            string sender = reportData[0];
                            DateTime dateSent = DateTime.MinValue;
                            long dateSentBinary;
                            if (long.TryParse(reportData[1], out dateSentBinary)) {
                                dateSent = DateTime.FromBinary(dateSentBinary);
                            }
                            string message = reportData[2];
                            int id;
                            if (int.TryParse(idString, out id)) {
                                rCreate.addReport(id, sender, dateSent, message);
                                created++;
                            }

                        }

                    }
                    if (created > 0)
                        Player.Console.Message("All Reports Loaded. ({0})", created);
                    else
                        Player.Console.Message("No reports were loaded.");
                }
            } catch (Exception ex) {
                Player.Console.Message("Report Loader Has Crashed: {0}", ex);
            }

            #endregion

            #region LoadFilters

            try {
                if (Directory.Exists("./Filters")) {
                    string[] FilterFileList = Directory.GetFiles("./Filters");
                    int created = 0;
                    foreach (string filename in FilterFileList) {
                        Filter filterCreate = new Filter();
                        if (Path.GetExtension("./Filters/" + filename) == ".txt") {
                            string[] filterData = File.ReadAllLines(filename);
                            string idString = filename.Replace("./Filters\\", "").Replace(".txt", "");
                            string wordString = filterData[0];
                            string replacementString = filterData[1];
                            int id;
                            if (int.TryParse(idString, out id)) {
                                filterCreate.addFilter(id, wordString, replacementString);
                                created++;
                            }

                        }

                    }
                    if (created > 0)
                        Player.Console.Message("All Filters Loaded. ({0})", created);
                    else
                        Player.Console.Message("No filters were loaded.");
                }
            } catch (Exception ex) {
                Player.Console.Message("Filter Loader Has Crashed: {0}", ex);
            }

            #endregion

            #region LoadEntities

            try {
                if (Directory.Exists("./Entities")) {
                    string[] EntityFileList = Directory.GetFiles("./Entities");
                    foreach (string filename in EntityFileList) {
                        Bot botCreate = new Bot();
                        if (Path.GetExtension("./Entities/" + filename) == ".txt") {
                            string[] entityData = File.ReadAllLines(filename);
                            sbyte idString;
                            Position posString;
                            string nameString = entityData[0];
                            string skinString = entityData[1];
                            string modelString = entityData[2];
                            if (!ModerationCommands.validEntities.Contains(modelString)) {
                                Block block;
                                if (Map.GetBlockByName(modelString, false, out block)) {
                                    modelString = block.GetHashCode().ToString();
                                } else {
                                    modelString = "humanoid";
                                }
                            }
                            if (!sbyte.TryParse(entityData[3], out idString)) { }
                            World worldString = WorldManager.FindWorldExact(entityData[4]) ??
                                                WorldManager.FindMainWorld(RankManager.LowestRank);
                            if (!short.TryParse(entityData[5], out posString.X)) {
                                posString.X = worldString.map.Spawn.X;
                            }
                            if (!short.TryParse(entityData[6], out posString.Y)) {
                                posString.Y = worldString.map.Spawn.Y;
                            }
                            if (!short.TryParse(entityData[7], out posString.Z)) {
                                posString.Z = worldString.map.Spawn.Z;
                            }
                            if (!byte.TryParse(entityData[8], out posString.L)) {
                                posString.L = worldString.map.Spawn.L;
                            }
                            if (!byte.TryParse(entityData[9], out posString.R)) {
                                posString.R = worldString.map.Spawn.R;
                            }

                            botCreate.setBot(nameString, skinString ?? nameString, modelString, worldString, posString, idString);
                        }

                    }
                    if (EntityFileList.Length > 0)
                        Player.Console.Message("All Entities Loaded. ({0})", EntityFileList.Length);
                    else
                        Player.Console.Message("No Entities Were Loaded.");
                }
            } catch (Exception ex) {
                Player.Console.Message("Entity Loader Has Crashed: {0}", ex);
            }

            #endregion

            #region BotFiles

            if (!Directory.Exists("./Bot")) {
                Directory.CreateDirectory("./Bot");
            }
            if (!File.Exists("Bot/Funfacts.txt")) {
                string[] factStrings = {
                        "Every time you lick a stamp, you're consuming 1/10 of a calorie.",
                        "Banging your head against a wall uses 150 calories an hour",
                        "The average person falls asleep in seven minutes.",
                        "Your stomach has to produce a new layer of mucus every two weeks otherwise it will digest itself.",
                        "Polar bears are left handed.", "An ostrich's eye is bigger than it's brain.",
                        "Bullet proof vests, fire escapes, windshield wipers, and laser printers were all invented by women.",
                        "Pearls melt in vinegar.", "Coca Cola was originally green.",
                        "The youngest Pope was 11 years old.",
                        "A 'jiffy' is an actual unit of time: 1/100th of a second.",
                        "Venus is the only planet that rotates clockwise.", "Most lipstick contains fish scales.",
                        "Women blink nearly twice as much as men.", "Ketchup was sold in the 1830s as medicine.",
                        "Stewardesses' is the longest word that is typed with only the left hand.",
                        "The only 15 letter word that can be spelled without repeating a letter is \"uncopyrightable\".",
                        "Maine is the only state in the USA that is one syllable.",
                        "The adult human brain weighs about 3 pounds (1,300-1,400 g).",
                        "Pirates of old spoke just like everyone else. The 'pirate accent' was invented for the 1950 Disney movie, Treasure Island.",
                        "Almonds are a member of the peach family.",
                        "Paraguay's flag is the only national flag where the front and the back are different.",
                        "In 1999, Furbies were banned by the Pentagon, based on the fear that the dolls would mimic top-secret discussions.",
                        "A dime has 118 ridges around the edge.", "Peanuts are one of the ingredients of dynamite.",
                        "Two thirds of the world's eggplant is grown in New Jersey.",
                        "No piece of paper can be folded in half more than 7 times.",
                        "American Airlines saved $40,000 in 1987 by eliminating 1 olive from each salad served in first-class.",
                        "In 1998, more fast-food employees were murdered on the job than police officers.",
                        "Fortune cookies were actually invented in America, in 1918, by Charles Jung.",
                        "TYPEWRITER is the longest word that can be made using the letters only on one row of the keyboard."
                    };
                File.WriteAllLines("Bot/Funfacts.txt", factStrings);
            }
            if (!File.Exists("Bot/Jokes.txt")) {
                string[] jokeStrings = {
                        "What do you call a fat psychic? A four chin teller.",
                        "If con is the opposite of pro, it must mean Congress is the opposite of progress?",
                        "What did the fish say when he swam into the wall? -- Damn",
                        "What do you call a sheep with no legs? A cloud.",
                        "How do you make holy water? You boil the hell out of it.",
                        "Fat people are harder to kidnap.",
                        "What's the best thing about being 100 y/o? -- No peer pressure",
                        "Failure is not an option -- it comes bundled with Windows.",
                        "Yo moma is like HTML: Tiny head, huge body.",
                        "1f u c4n r34d th1s u r34lly n33d t0 g37 l41d",
                        "As long as there are tests, there will be prayer in schools.",
                        "For Sale: Parachute. Only used once, never opened.",
                        "When everything's coming your way, you're in the wrong lane.",
                        "If at first you don't succeed, destroy all evidence that you tried.",
                        "The last thing I want to do is hurt you. But it's still on the list.",
                        "If I agreed with you we'd both be wrong.",
                        "Energizer Bunny was arrested, charged with battery.",
                        "I usually take steps to avoid elevators.", "Schrodinger's Cat: Wanted dead and alive.",
                        "If at first you don't succeed; call it version 1.0",
                        "CONGRESS.SYS Corrupted: Re-boot Washington D.C (Y/n)?",
                        "We live in a society where pizza gets to your house before the police.",
                        "Light travels faster than sound. This is why some people appear bright until you hear them speak.",
                        "Evening news is where they begin with 'Good evening', and then proceed to tell you why it isn't.",
                        "You do not need a parachute to skydive. You only need a parachute to skydive twice.",
                        "When in doubt, mumble.", "War does not determine who is right – only who is left...",
                        "I wondered why the frisbee was getting bigger - then it hit me.",
                        "Never argue with a fool, they will lower you to their level, and then beat you with experience.",
                        "There are 3 kinds of people in the world: Those who can count and those who can't.",
                        "Never hit a man with glasses. Hit him with a baseball bat instead.",
                        "Nostalgia isn't what it used to be."
                    };
                File.WriteAllLines("Bot/Jokes.txt", jokeStrings);
            }
            if (!File.Exists("Bot/Protips.txt")) {
                string[] tipStrings = {
                        "Store cheese curls bag in freezer, the colder they are the better they taste.",
                        "Take a screen-shot on the payment confirmation screen of every purchase you make online.",
                        "Use an underscore in front of folder names and other sortable items on your computer to keep those at the top of the list.",
                        "Don't use \"lol\" as a filler word.",
                        "Press F2 to immediately rename a file, no more slow double clicks.",
                        "Take one minute to record your phone's serial number. This may be the one identifying factor if your phone is ever stolen and reset.",
                        "Create a life binder and keep in it copies of things like your: medical records, SSN, birth photos, etc.",
                        "When applying for a job online, save the job description in an email/pdf. You'll be able to prepare even if the post is removed.",
                        "Try the microwave challenge: when microwaving see how much of the kitchen you can clean up.",
                        "Give your shower walls a quick wipe-down after each shower and you'll never fight mildew and hard water stains again.",
                        "Open your dishwasher as soon as it has finished to allow additional water to evaporate (and get rid of water spots)",
                        "Have a separate account on your laptop for performing presentations.",
                        "When going on long roadtrips and don't want to pay for a motel? All Wal-Marts allow people to park over night without being kicked out.",
                        "Sprinkle cinnamon on the carpet. When you vacuum, the room will smell like cinnamon instead of your nasty vacuum.",
                        "Use a window squeegee to remove pet hair from carpet.",
                        "If you see someone griefing, don't try to fix it, tell an admin we have commands that can fix it instantly with no work.",
                        "Want a higher rank? Be nice, don't argue, try your best to get a better impression toward the admins.",
                        "Use travel delay as opportunity to stop rather than get stressed. When the world stands still, let it.",
                        "Stop clinging and embrace change as a constant.",
                        "Try and give people the benefit of the doubt if they snap at you. Might be something going on you don't know about.",
                        "Friendship is a gift, not a possession.",
                        "Before you go to bed, write down only 3 things that you want to do the following day. This is how to prioritize.",
                        "Do something relaxing before going to bed. No electronics.",
                        "When in doubt, take a deep breath.", "Define what's necessary; say no to the rest.",
                        "Expect nothing. Welcome everything.",
                        "Good things come to those who wait… greater things come to those who get off their ass and do anything to make it happen.",
                        "Ends are not bad things, they just mean that something else is about to begin.",
                        "Beat your alarm to wake up? Don't go back to bed, you will feel worse.",
                        "Orgasms cure hiccups.",
                        "Telemarketers aren't allowed to hang up first. The possibilities are endless.",
                        "Apples are 10 times more effective at keeping people awake than coffee."
                    };
                File.WriteAllLines("Bot/Protips.txt", tipStrings);
            }
            if (!File.Exists("Bot/Adjectives.txt")) {
                string[] adjectiveStrings = {
                    "adorable", "adventurous", "aggressive", "agreeable", "alert", "alive",
                    "amused", "angry", "annoyed", "annoying", "anxious", "arrogant", "ashamed", "attractive", "average",
                    "awful", "bad", "beautiful", "better", "bewildered", "black", "bloody", "blue", "blue-eyed",
                    "blushing", "bored", "brainy", "brave", "breakable", "bright", "busy", "calm", "careful", "cautious",
                    "charming", "cheerful", "clean", "clear", "clever", "cloudy", "clumsy", "colorful", "combative",
                    "comfortable", "concerned", "condemned", "confused", "cooperative", "courageous", "crazy", "creepy",
                    "crowded", "cruel", "curious", "cute", "dangerous", "dark", "dead", "defeated", "defiant",
                    "delightful", "depressed", "determined", "different", "difficult", "disgusted", "distinct",
                    "disturbed", "dizzy", "doubtful", "drab", "dull", "eager", "easy", "elated", "elegant",
                    "embarrassed", "enchanting", "encouraging", "energetic", "enthusiastic", "envious", "evil",
                    "excited", "expensive", "exuberant", "fair", "faithful", "famous", "fancy", "fantastic", "fierce",
                    "filthy", "fine", "foolish", "fragile", "frail", "frantic", "friendly", "frightened", "funny",
                    "gentle", "gifted", "glamorous", "gleaming", "glorious", "good", "gorgeous", "graceful", "grieving",
                    "grotesque", "grumpy", "handsome", "happy", "healthy", "helpful", "helpless", "hilarious",
                    "homeless", "homely", "horrible", "hungry", "hurt", "ill", "important", "impossible", "inexpensive",
                    "innocent", "inquisitive", "itchy", "jealous", "jittery", "jolly", "joyous", "kind", "lazy", "light",
                    "lively", "lonely", "long", "lovely", "lucky", "magnificent", "misty", "modern", "motionless",
                    "muddy", "mushy", "mysterious", "nasty", "naughty", "nervous", "nice", "nutty", "obedient",
                    "obnoxious", "odd", "old-fashioned", "open", "outrageous", "outstanding", "panicky", "perfect",
                    "plain", "pleasant", "poised", "poor", "powerful", "precious", "prickly", "proud", "puzzled",
                    "quaint", "real", "relieved", "repulsive", "rich", "scary", "selfish", "shiny", "shy", "silly",
                    "sleepy", "smiling", "smoggy", "sore", "sparkling", "splendid", "spotless", "stormy", "strange",
                    "stupid", "successful", "super", "talented", "tame", "tender", "tense", "terrible", "testy",
                    "thankful", "thoughtful", "thoughtless", "tired", "tough", "troubled", "ugliest", "ugly",
                    "uninterested", "unsightly", "unusual", "upset", "uptight", "vast", "victorious", "vivacious",
                    "wandering", "weary", "wicked", "wide-eyed", "wild", "witty", "worrisome", "worried", "wrong",
                    "zany", "zealous"
                };
                File.WriteAllLines("Bot/Adjectives.txt", adjectiveStrings);
            }
            if (!File.Exists("Bot/Nouns.txt")) {
                string[] nounStrings = {
                    "account", "achiever", "acoustics", "act", "action", "activity", "actor",
                    "addition", "adjustment", "advertisement", "advice", "aftermath", "afternoon", "afterthought",
                    "agreement", "air", "airplane", "airport", "alarm", "amount", "amusement", "anger", "angle",
                    "animal", "answer", "ant", "ants", "apparatus", "apparel", "apple", "apples", "appliance",
                    "approval", "arch", "argument", "arithmetic", "arm", "army", "art", "attack", "attempt", "attention",
                    "attraction", "aunt", "authority", "babies", "baby", "back", "badge", "bag", "bait", "balance",
                    "ball", "balloon", "balls", "banana", "band", "base", "baseball", "basin", "basket", "basketball",
                    "bat", "bath", "battle", "bead", "beam", "bean", "bear", "bears", "beast", "bed", "bedroom", "beds",
                    "bee", "beef", "beetle", "beggar", "beginner", "behavior", "belief", "believe", "bell", "bells",
                    "berry", "bike", "bikes", "bird", "birds", "birth", "birthday", "bit", "bite", "blade", "blood",
                    "blow", "board", "boat", "boats", "body", "bomb", "bone", "book", "books", "boot", "border",
                    "bottle", "boundary", "box", "boy", "boys", "brain", "brake", "branch", "brass", "bread",
                    "breakfast", "breath", "brick", "bridge", "brother", "brothers", "brush", "bubble", "bucket",
                    "building", "bulb", "bun", "burn", "burst", "bushes", "business", "butter", "button", "cabbage",
                    "cable", "cactus", "cake", "cakes", "calculator", "calendar", "camera", "camp", "can", "cannon",
                    "canvas", "cap", "caption", "car", "card", "care", "carpenter", "carriage", "cars", "cart", "cast",
                    "cat", "cats", "cattle", "cause", "cave", "celery", "cellar", "cemetery", "cent", "chain", "chair",
                    "chairs", "chalk", "chance", "change", "channel", "cheese", "cherries", "cherry", "chess", "chicken",
                    "chickens", "children", "chin", "church", "circle", "clam", "class", "clock", "clocks", "cloth",
                    "cloud", "clouds", "clover", "club", "coach", "coal", "coast", "coat", "cobweb", "coil", "collar",
                    "color", "comb", "comfort", "committee", "company", "comparison", "competition", "condition",
                    "connection", "control", "cook", "copper", "copy", "cord", "cork", "corn", "cough", "country",
                    "cover", "cow", "cows", "crack", "cracker", "crate", "crayon", "cream", "creator", "creature",
                    "credit", "crib", "crime", "crook", "crow", "crowd", "crown", "crush", "cry", "cub", "cup",
                    "current", "curtain", "curve", "cushion", "dad", "daughter", "day", "death", "debt", "decision",
                    "deer", "degree", "design", "desire", "desk", "destruction", "detail", "development", "digestion",
                    "dime", "dinner", "dinosaurs", "direction", "dirt", "discovery", "discussion", "disease", "disgust",
                    "distance", "distribution", "division", "dock", "doctor", "dog", "dogs", "doll", "dolls", "donkey",
                    "door", "downtown", "drain", "drawer", "dress", "drink", "driving", "drop", "drug", "drum", "duck",
                    "ducks", "dust", "ear", "earth", "earthquake", "edge", "education", "effect", "egg", "eggnog",
                    "eggs", "elbow", "end", "engine", "error", "event", "example", "exchange", "existence", "expansion",
                    "experience", "expert", "eye", "eyes", "face", "fact", "fairies", "fall", "family", "fan", "fang",
                    "farm", "farmer", "father", "father", "faucet", "fear", "feast", "feather", "feeling", "feet",
                    "fiction", "field", "fifth", "fight", "finger", "finger", "fire", "fireman", "fish", "flag", "flame",
                    "flavor", "flesh", "flight", "flock", "floor", "flower", "flowers", "fly", "fog", "fold", "food",
                    "foot", "force", "fork", "form", "fowl", "frame", "friction", "friend", "friends", "frog", "frogs",
                    "front", "fruit", "fuel", "furniture", "alley", "game", "garden", "gate", "geese", "ghost", "giants",
                    "giraffe", "girl", "girls", "glass", "glove", "glue", "goat", "gold", "goldfish", "good-bye",
                    "goose", "government", "governor", "grade", "grain", "grandfather", "grandmother", "grape", "grass",
                    "grip", "ground", "group", "growth", "guide", "guitar", "gun 	H", "", "hair", "haircut", "hall",
                    "hammer", "hand", "hands", "harbor", "harmony", "hat", "hate", "head", "health", "hearing", "heart",
                    "heat", "help", "hen", "hill", "history", "hobbies", "hole", "holiday", "home", "honey", "hook",
                    "hope", "horn", "horse", "horses", "hose", "hospital", "hot", "hour", "house", "houses", "humor",
                    "hydrant", "ice", "icicle", "idea", "impulse", "income", "increase", "industry", "ink", "insect",
                    "instrument", "insurance", "interest", "invention", "iron", "island", "jail", "jam", "jar", "jeans",
                    "jelly", "jellyfish", "jewel", "join", "joke", "journey", "judge", "juice", "jump", "kettle", "key",
                    "kick", "kiss", "kite", "kitten", "kittens", "kitty", "knee", "knife", "knot", "knowledge",
                    "laborer", "lace", "ladybug", "lake", "lamp", "land", "language", "laugh", "lawyer", "lead", "leaf",
                    "learning", "leather", "leg", "legs", "letter", "letters", "lettuce", "level", "library", "lift",
                    "light", "limit", "line", "linen", "lip", "liquid", "list", "lizards", "loaf", "lock", "locket",
                    "look", "loss", "love", "low", "lumber", "lunch", "lunchroom", "machine", "magic", "maid", "mailbox",
                    "man", "manager", "map", "marble", "mark", "market", "mask", "mass", "match", "meal", "measure",
                    "meat", "meeting", "memory", "men", "metal", "mice", "middle", "milk", "mind", "mine", "minister",
                    "mint", "minute", "mist", "mitten", "mom", "money", "monkey", "month", "moon", "morning", "mother",
                    "motion", "mountain", "mouth", "move", "muscle", "music", "nail", "name", "nation", "neck", "need",
                    "needle", "nerve", "nest", "net", "news", "night", "noise", "north", "nose", "note", "notebook",
                    "number", "nut", "oatmeal", "observation", "ocean", "offer", "office", "oil", "operation", "opinion",
                    "orange", "oranges", "order", "organization", "ornament", "oven", "owl", "owner", "page", "pail",
                    "pain", "paint", "pan", "pancake", "paper", "parcel", "parent", "park", "part", "partner", "party",
                    "passenger", "paste", "patch", "payment", "peace", "pear", "pen", "pencil", "person", "pest", "pet",
                    "pets", "pickle", "picture", "pie", "pies", "pig", "pigs", "pin", "pipe", "pizzas", "place", "plane",
                    "planes", "plant", "plantation", "plants", "plastic", "plate", "play", "playground", "pleasure",
                    "plot", "plough", "pocket", "point", "poison", "police", "polish", "pollution", "popcorn", "porter",
                    "position", "pot", "potato", "powder", "power", "price", "print", "prison", "process", "produce",
                    "profit", "property", "prose", "protest", "pull", "pump", "punishment", "purpose", "push", "quarter",
                    "quartz", "queen", "question", "quicksand", "quiet", "quill", "quilt", "quince", "quiver", "rabbit",
                    "rabbits", "rail", "railway", "rain", "rainstorm", "rake", "range", "rat", "rate", "ray", "reaction",
                    "reading", "reason", "receipt", "recess", "record", "regret", "relation", "religion",
                    "representative", "request", "respect", "rest", "reward", "rhythm", "rice", "riddle", "rifle",
                    "ring", "rings", "river", "road", "robin", "rock", "rod", "roll", "roof", "room", "root", "rose",
                    "route", "rub", "rule", "run", "sack", "sail", "salt", "sand", "scale", "scarecrow", "scarf",
                    "scene", "scent", "school", "science", "scissors", "screw", "sea", "seashore", "seat", "secretary",
                    "seed", "selection", "self", "sense", "servant", "shade", "shake", "shame", "shape", "sheep",
                    "sheet", "shelf", "ship", "shirt", "shock", "shoe", "shoes", "shop", "show", "side", "sidewalk",
                    "sign", "silk", "silver", "sink", "sister", "sisters", "size", "skate", "skin", "skirt", "sky",
                    "slave", "sleep", "sleet", "slip", "slope", "smash", "smell", "smile", "smoke", "snail", "snails",
                    "snake", "snakes", "sneeze", "snow", "soap", "society", "sock", "soda", "sofa", "son", "song",
                    "songs", "sort", "sound", "soup", "space", "spade", "spark", "spiders", "sponge", "spoon", "spot",
                    "spring", "spy", "square", "squirrel", "stage", "stamp", "star", "start", "statement", "station",
                    "steam", "steel", "stem", "step", "stew", "stick", "sticks", "stitch", "stocking", "stomach",
                    "stone", "stop", "store", "story", "stove", "stranger", "straw", "stream", "street", "stretch",
                    "string", "structure", "substance", "sugar", "suggestion", "suit", "summer", "sun", "support",
                    "surprise", "sweater", "swim", "swing", "system", "table", "tail", "talk", "tank", "taste", "tax",
                    "teaching", "team", "teeth", "temper", "tendency", "tent", "territory", "test", "texture", "theory",
                    "thing", "things", "thought", "thread", "thrill", "throat", "throne", "thumb", "thunder", "ticket",
                    "tiger", "time", "tin", "title", "toad", "toe", "toes", "tomatoes", "tongue", "tooth", "toothbrush",
                    "toothpaste", "top", "touch", "town", "toy", "toys", "trade", "trail", "train", "trains", "tramp",
                    "transport", "tray", "treatment", "tree", "trees", "trick", "trip", "trouble", "trousers", "truck",
                    "trucks", "tub", "turkey", "turn", "twig", "twist", "umbrella", "uncle", "underwear", "unit", "use ",
                    "vacation", "value", "van", "vase", "vegetable", "veil", "vein", "verse", "vessel", "vest", "view",
                    "visitor", "voice", "volcano", "volleyball", "voyage", "walk", "wall", "war", "wash", "waste",
                    "watch", "water", "wave", "waves", "wax", "way", "wealth", "weather", "week", "weight", "wheel",
                    "whip", "whistle", "wilderness", "wind", "window", "wine", "wing", "winter", "wire", "wish", "woman",
                    "women", "wood", "wool", "word", "work", "worm", "wound", "wren", "wrench", "wrist", "writer",
                    "writing", "yak", "yam", "yard", "yarn", "year", "yoke", "zebra", "zephyr", "zinc", "zipper", "zoo"
                };
                File.WriteAllLines("Bot/Nouns.txt", nounStrings);
            }

            #endregion


            PortalHandler.GetInstance();
            PortalDB.Load();

            // garbage collection (every 60s)
            gcTask = Scheduler.NewTask( DoGC ).RunForever( GCInterval, TimeSpan.FromSeconds( 45 ) );

            Heartbeat.Start();

            if( ConfigKey.IRCBotEnabled.Enabled() ) {
                IRC.Start();
            }

            if( ConfigKey.AutoRankEnabled.Enabled() ) {
                Scheduler.NewTask( AutoRankManager.TaskCallback ).RunForever( AutoRankManager.TickInterval );
            }

            if( ConfigKey.RestartInterval.GetInt() > 0 ) {
                TimeSpan restartIn = TimeSpan.FromSeconds( ConfigKey.RestartInterval.GetInt() );
                Shutdown( new ShutdownParams( ShutdownReason.RestartTimer, restartIn, true ), false );
                ChatTimer.Start( restartIn, "Automatic Server Restart", Player.Console.Name );
            }

            // start the main loop - server is now connectible
            Scheduler.Start();
            IsRunning = true;

            RaiseEvent( Started );
            return true;
        }

        #endregion


        #region Shutdown

        /// <summary> Whether the server is currently being shut down. </summary>
        public static volatile bool IsShuttingDown;

        static readonly object ShutdownLock = new object();
        static readonly AutoResetEvent ShutdownWaiter = new AutoResetEvent( false );
        static Thread shutdownThread;
        static ChatTimer shutdownTimer;


        static void ShutdownNow( [NotNull] ShutdownParams shutdownParams ) {
            if( shutdownParams == null ) throw new ArgumentNullException( "shutdownParams" );
            if( IsShuttingDown ) return; // to avoid starting shutdown twice
            IsShuttingDown = true;
#if !DEBUG
            try {
#endif
                RaiseShutdownBeganEvent( shutdownParams );

                Scheduler.BeginShutdown();

                Logger.Log( LogType.SystemActivity,
                            "Server shutting down ({0})",
                            shutdownParams.ReasonString );

                // stop accepting new players
                if( listener != null ) {
                    listener.Stop();
                    listener = null;
                }

                // kill IRC bot
                IRC.Disconnect("Shutting down");

                // kick all players
                Player[] kickedPlayers = null;
                lock( PlayerListLock ) {
                    if( PlayerIndex.Count > 0 ) {
                        Logger.Log( LogType.SystemActivity, "Shutdown: Kicking players..." );
                        foreach( Player p in PlayerIndex ) {
                            // NOTE: kick packet delivery here is not currently guaranteed
                            p.Kick( "Server shutting down (" + shutdownParams.ReasonString + Color.White + ")",
                                    LeaveReason.ServerShutdown );
                        }
                        kickedPlayers = PlayerIndex.ToArray();
                    }
                }

                if( kickedPlayers != null ) {
                    Logger.Log( LogType.SystemActivity, "Shutdown: Waiting for players to disconnect..." );
                    foreach( Player p in kickedPlayers ) {
                        p.WaitForDisconnect();
                    }
                }

                if( WorldManager.Worlds != null ) {
                    Logger.Log( LogType.SystemActivity, "Shutdown: Saving worlds..." );
                    lock( WorldManager.SyncRoot ) {
                        // unload all worlds (includes saving)
                        foreach( World world in WorldManager.Worlds ) {
                            if( BlockDB.IsEnabledGlobally && world.BlockDB.IsEnabled ) {
                                world.BlockDB.Flush( false );
                            }
                            world.SaveMap();
                        }
                    }
                }

                if( Scheduler.CriticalTaskCount > 0 ) {
                    Logger.Log( LogType.SystemActivity,
                                "Shutdown: Waiting for {0} background tasks to finish...",
                                Scheduler.CriticalTaskCount );
                }
                Scheduler.EndShutdown();

                if( IsRunning ) {
                    Logger.Log( LogType.SystemActivity, "Shutdown: Saving databases..." );
                    if( PlayerDB.IsLoaded ) PlayerDB.Save();
                    if( IPBanList.IsLoaded ) IPBanList.Save();
                }
                IsRunning = false;

                Environment.ExitCode = (int)shutdownParams.Reason;

                Logger.Log( LogType.SystemActivity, "Shutdown: Complete" );
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Error in Server.Shutdown", "ProCraft", ex, true );
            }
#endif
        }


        /// <summary> Initiates the server shutdown with given parameters. </summary>
        /// <param name="shutdownParams"> Shutdown parameters </param>
        /// <param name="waitForShutdown"> If true, blocks the calling thread until shutdown is complete or cancelled. </param>
        public static void Shutdown( [NotNull] ShutdownParams shutdownParams, bool waitForShutdown ) {
            if( shutdownParams == null ) throw new ArgumentNullException( "shutdownParams" );
            lock( ShutdownLock ) {
                if( !CancelShutdown() ) return;
                shutdownThread = new Thread( ShutdownThread ) {
                    Name = "fCraft.Shutdown",
                    CurrentCulture = new CultureInfo( "en-US" )
                };
                if( shutdownParams.Delay >= ChatTimer.MinDuration ) {
                    string timerMsg = String.Format( "Server {0} ({1})",
                                                     shutdownParams.Restart ? "restart" : "shutdown",
                                                     shutdownParams.ReasonString );
                    string nameOnTimer;
                    if( shutdownParams.InitiatedBy == null ) {
                        nameOnTimer = Player.Console.Name;
                    } else {
                        nameOnTimer = shutdownParams.InitiatedBy.Name;
                    }
                    shutdownTimer = ChatTimer.Start( shutdownParams.Delay, timerMsg, nameOnTimer );
                }
                shutdownThread.Start( shutdownParams );
            }
            if( waitForShutdown ) {
                ShutdownWaiter.WaitOne();
            }
        }


        /// <summary> Attempts to cancel the shutdown timer. </summary>
        /// <returns> True if a shutdown timer was cancelled, false if no shutdown is in progress.
        /// Also returns false if it's too late to cancel (shutdown has begun). </returns>
        public static bool CancelShutdown() {
            lock( ShutdownLock ) {
                if( shutdownThread != null ) {
                    if( IsShuttingDown || shutdownThread.ThreadState != ThreadState.WaitSleepJoin ) {
                        return false;
                    }
                    if( shutdownTimer != null ) {
                        shutdownTimer.Abort();
                        shutdownTimer = null;
                    }
                    ShutdownWaiter.Set();
                    shutdownThread.Abort();
                    shutdownThread = null;
                }
            }
            return true;
        }


        static void ShutdownThread( [NotNull] object obj ) {
            if( obj == null ) throw new ArgumentNullException( "obj" );
            ShutdownParams param = (ShutdownParams)obj;
            Thread.Sleep( param.Delay );
            ShutdownNow( param );
            ShutdownWaiter.Set();

            bool doRestart = ( param.Restart && !HasArg( ArgKey.NoRestart ) );
            string assemblyExecutable = Assembly.GetEntryAssembly().Location;

            if( doRestart ) {
                MonoCompat.StartDotNetProcess( assemblyExecutable, GetArgString(), true );
            }

            RaiseShutdownEndedEvent( param );
        }

        #endregion


        #region Messaging / Packet Sending

        /// <summary> Broadcasts a message to all online players.
        /// Shorthand for Server.Players.Message </summary>
        [StringFormatMethod( "message" )]
        public static void Message( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            Players.Message( message, formatArgs );
        }


        /// <summary> Broadcasts a message to all online players except one.
        /// Shorthand for Server.Players.Except(except).Message </summary>
        [StringFormatMethod( "message" )]
        public static void Message( [CanBeNull] Player except, [NotNull] string message,
                                    [NotNull] params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            Players.Except( except ).Message( message, formatArgs );
        }

        #endregion


        #region Scheduled Tasks

        // checks for incoming connections
        static SchedulerTask checkConnectionsTask;
        static TimeSpan checkConnectionsInterval = TimeSpan.FromMilliseconds( 250 );

        public static TimeSpan CheckConnectionsInterval {

            get { return checkConnectionsInterval; }
            set
            {
                if (value.Ticks < 0) throw new ArgumentException("CheckConnectionsInterval may not be negative.");
                checkConnectionsInterval = value;
                if (checkConnectionsTask != null) checkConnectionsTask.Interval = value;
            }
        }


        static void CheckConnections( SchedulerTask param ) {
            TcpListener listenerCache = listener;
            if( listenerCache != null && listenerCache.Pending() ) {
                try {
                    Player.StartSession( listenerCache.AcceptTcpClient() );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error,
                                "Server.CheckConnections: Could not accept incoming connection: {0}", ex );
                }
            }
        }


        // checks for idle players
        static SchedulerTask checkIdlesTask;
        static SchedulerTask tabListTask;
        static TimeSpan checkIdlesInterval = TimeSpan.FromSeconds( 1 );

        /// <summary> Interval at which Server checks for idle players (to kick idlers). </summary>
        public static TimeSpan CheckIdlesInterval {
            get { return checkIdlesInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException( "CheckIdlesInterval may not be negative." );
                checkIdlesInterval = value;
                if( checkIdlesTask != null ) checkIdlesTask.Interval = checkIdlesInterval;
                if( tabListTask != null ) tabListTask.Interval = checkIdlesInterval;
            }
        }


        private static void CheckIdles(SchedulerTask task) {

            Player[] tempPlayerList = Players;
            for (int i = 0; i < tempPlayerList.Length; i++) {
                Player player = tempPlayerList[i];

                if (player.lastSolidPos != null && !player.Info.IsAFK && player.Supports(CpeExtension.MessageType) &&
                    !player.IsPlayingCTF) {
                    double speed = (Math.Sqrt(player.Position.DistanceSquaredTo(player.lastSolidPos))/32);
                    player.Send(Packet.Message((byte)MessageType.BottomRight3, String.Format("&eSpeed: &f{0:N2} &eBlocks/s", speed)));
                    player.Send(Packet.Message((byte)MessageType.BottomRight2,
                        player.Position.ToBlockCoordsExt().ToString() +
                        InfoCommands.GetCompassStringType(player.Position.R)));
                }
                if (player.IsPlayingCTF && player.Supports(CpeExtension.MessageType)) {
                    player.Send(Packet.Message((byte)MessageType.BottomRight1, ""));
                    if (((CTF.redRoundsWon*5) + CTF.redScore) > ((CTF.blueRoundsWon*5) + CTF.blueScore)) {
                        player.Send(Packet.Message((byte)MessageType.BottomRight3,
                            "&4Red &a" + CTF.redRoundsWon + "&4:&f" + CTF.redScore + " &c<-- &1Blue &a" +
                            CTF.blueRoundsWon + "&1:&f" + CTF.blueScore));
                    } else if (((CTF.redRoundsWon*5) + CTF.redScore) < ((CTF.blueRoundsWon*5) + CTF.blueScore)) {
                        player.Send(Packet.Message((byte)MessageType.BottomRight3,
                            "&4Red &a" + CTF.redRoundsWon + "&4:&f" + CTF.redScore + " &9--> &1Blue &a" +
                            CTF.blueRoundsWon + "&1:&f" + CTF.blueScore));
                    } else {
                        player.Send(Packet.Message((byte)MessageType.BottomRight3,
                            "&4Red &a" + CTF.redRoundsWon + "&4:&f" + CTF.redScore + " &d<=> &1Blue &a" +
                            CTF.blueRoundsWon + "&1:&f" + CTF.blueScore));
                    }
                    var flagholder = player.World.Players.Where(p => p.IsHoldingFlag);
                    if (flagholder != null) {
                        if (CTF.redHasFlag) {
                            player.Send(Packet.Message((byte)MessageType.BottomRight2,
                                flagholder.Take(1)
                                    .JoinToString((r => String.Format("&4{0} &ehas the &1Blue&e flag!", r.Name)))));
                        } else if (CTF.blueHasFlag) {
                            player.Send(Packet.Message((byte)MessageType.BottomRight2,
                                flagholder.Take(1)
                                    .JoinToString((r => String.Format("&1{0} &ehas the &4Red&e flag!", r.Name)))));
                        } else {
                            player.Send(Packet.Message((byte)MessageType.BottomRight2, "&eNo one has the flag!"));
                        }

                    }
                    if (player.Team == "Red") {
                        player.Send(Packet.Message((byte)MessageType.Status3, "&eTeam: &4Red"));
                    } else if (player.Team == "Blue") {
                        player.Send(Packet.Message((byte)MessageType.Status3, "&eTeam: &1Blue"));
                    } else
                        player.Send(Packet.Message((byte)MessageType.Status3, "&eTeam: &0None"));
                }
                if (player.IsPlayingCTF && player.Supports(CpeExtension.EnvColors)) {
                    if (((CTF.redRoundsWon*5) + CTF.redScore) > ((CTF.blueRoundsWon*5) + CTF.blueScore)) {
                        player.Send(Packet.MakeEnvSetColor((byte)EnvVariable.FogColor, "AA0000"));
                    } else if (((CTF.redRoundsWon*5) + CTF.redScore) < ((CTF.blueRoundsWon*5) + CTF.blueScore)) {
                        player.Send(Packet.MakeEnvSetColor((byte)EnvVariable.FogColor, "0000AA"));
                    } else {
                        player.Send(Packet.MakeEnvSetColor((byte)EnvVariable.FogColor, "AA00AA"));
                    }
                }
                player.lastSolidPos = player.Position;

                if (player.Info.Rank.IdleKickTimer <= 0) continue;
                TimeSpan TimeLeft = new TimeSpan(0, player.Info.Rank.IdleKickTimer, 0) - player.IdBotTime;

                if (player.IdBotTime.ToSeconds()%300 == 0 && player.IdBotTime.ToSeconds() >= 300) {
                    if (player.Info.IsAFK == false) {
                        Players.CanSee(player).Message("&S{0} is now AFK (Auto)", player.Name);
                        player.Info.IsAFK = true;
                        player.Info.oldafkMob = player.Info.afkMob;
                        player.Info.afkMob = "chicken";

                    }
                    UpdateTabList();
                    player.Message("You have " + TimeLeft.ToMiniString() + " left before being kicked for idleing");
                }

                if (player.IdBotTime.Minutes >= player.Info.Rank.IdleKickTimer) {
                    Message("{0}&S was kicked for being idle for {1} min", player.ClassyName,
                        player.Info.Rank.IdleKickTimer);
                    string kickReason = "Idle for " + player.Info.Rank.IdleKickTimer + " minutes";
                    player.Kick(Player.Console, kickReason, LeaveReason.IdleKick, false, true, false);
                    player.Info.TotalTime = player.Info.TotalTime - player.IdBotTime;
                    player.Info.IsAFK = false;
                    player.Info.oldafkMob = player.Info.afkMob;
                    player.Info.afkMob = player.Info.Mob;
                    UpdateTabList();
                    player.ResetIdBotTimer(); // to prevent kick from firing more than once
                }
            }
        }

        static void TabList(SchedulerTask task)
        {

            Player[] tempPlayerList = Players;
            for (int i = 0; i < tempPlayerList.Length; i++)
            {
                Player player = tempPlayerList[i];
                if (!player.Supports(CpeExtension.ExtPlayerList) && !player.Supports(CpeExtension.ExtPlayerList2))
                    continue;
                var canBeSeen = Players.Where(a => player.CanSee(a)).ToArray();
                var canBeSeenW = player.World.Players.Where(a => player.CanSee(a)).ToArray();
                if (!player.IsPlayingCTF)
                {
                    foreach (Player p2 in canBeSeen)
                    {
                        player.Send(Packet.MakeExtAddPlayerName(p2.NameID, p2.Name, p2.ListName,
                            p2.World.ClassyName + " &e(&f" + p2.World.CountVisiblePlayers(player) + "&e)", 0));
                    }
                }
                else
                {
                    foreach (Player p2 in canBeSeenW)
                    {
                        if (p2.IsPlayingCTF && p2.Team == "Red")
                        {
                            player.Send(Packet.MakeExtAddPlayerName(p2.NameID, p2.Name, "&c" + p2.Name, "&eTeam &4Red",
                                0));
                        }
                        else if (p2.IsPlayingCTF && p2.Team == "Blue")
                        {
                            player.Send(Packet.MakeExtAddPlayerName(p2.NameID, p2.Name, "&9" + p2.Name, "&eTeam &1Blue",
                                0));
                        }
                    }
                }
            }
        }

        #region SaveEntity

        /// <summary>
        /// Saves the entity data to be used when restarting the server
        /// </summary>
        /// <param name="bot">entity being saved</param>
        public static void SaveEntity(Bot bot) {
            try {
                String[] entityData = {
                    bot.Name, bot.SkinName ?? bot.Name, bot.Model ?? "humanoid", bot.ID.ToString(CultureInfo.InvariantCulture),
                    bot.World.Name, bot.Position.X.ToString(CultureInfo.InvariantCulture),
                    bot.Position.Y.ToString(CultureInfo.InvariantCulture),
                    bot.Position.Z.ToString(CultureInfo.InvariantCulture),
                    bot.Position.L.ToString(CultureInfo.InvariantCulture),
                    bot.Position.R.ToString(CultureInfo.InvariantCulture)
                };
                if (!Directory.Exists("./Entities")) {
                    Directory.CreateDirectory("./Entities");
                }
                File.WriteAllLines("./Entities/" + bot.Name.ToLower() + ".txt", entityData);
            } catch (Exception ex) {
                Player.Console.Message("Entity Saver Has Crashed: {0}", ex);
            }
        }

        #endregion

        /*// checks for idle players
        static SchedulerTask setWeatherTask;
        static TimeSpan setWeatherInterval = TimeSpan.FromMinutes(1);

        /// <summary> Interval at which Server checks for idle players (to kick idlers). </summary>
        public static TimeSpan SetWeatherInterval
        {
            get { return checkIdlesInterval; }
            set
            {
                if (value.Ticks < 0) throw new ArgumentException("CheckIdlesInterval may not be negative.");
                checkIdlesInterval = value;
                if (checkIdlesTask != null) checkIdlesTask.Interval = checkIdlesInterval;
            }
        }


        static void CheckIdles(SchedulerTask task)
        {

            Player[] tempPlayerList = Players;
            for (int i = 0; i < tempPlayerList.Length; i++)
            {
                Player player = tempPlayerList[i];
                if (player.Info.Rank.IdleKickTimer <= 0) continue;

                if (player.IdBotTime.ToSeconds() % 300 == 0 && player.IdBotTime.ToSeconds() >= 300)
                {
                    if (player.Info.IsAFK == false)
                    {
                        Server.Players.CanSee(player).Message("&S{0} is now AFK (Auto)", player.Name);
                        player.Info.IsAFK = true;
                        player.Info.Mob = "chicken";
                        int TimeLeft = (player.Info.Rank.IdleKickTimer - (int)player.IdBotTime.ToMinutes());
                        player.Message("You have " + TimeLeft + "m left before you get kicked for being AFK");
                    }
                    else
                    {
                        player.Info.IsAFK = true;
                        int TimeLeft = (player.Info.Rank.IdleKickTimer - (int)player.IdBotTime.ToMinutes());
                        player.Message("You have " + TimeLeft + "m left before you get kicked for being AFK");
                    }
                }

                if (player.IdBotTime.Minutes >= player.Info.Rank.IdleKickTimer)
                {
                    Message("{0}&S was kicked for being idle for {1} min",
                             player.ClassyName,
                             player.Info.Rank.IdleKickTimer);
                    string kickReason = "Idle for " + player.Info.Rank.IdleKickTimer + " minutes";
                    player.Kick(Player.Console, kickReason, LeaveReason.IdleKick, false, true, false);
                    player.Info.TotalTime = player.Info.TotalTime - player.IdBotTime;
                    player.Info.IsAFK = false;
                    player.ResetIdBotTimer(); // to prevent kick from firing more than once
                }
            }
        }
        */


                //static void CheckIdles(SchedulerTask task)
                //{
                //
                //    Player[] tempPlayerList = Players;
                //    for (int i = 0; i < tempPlayerList.Length; i++)
                //    {
                //        Player player = tempPlayerList[i];
                //        int fail;
                //        if (player.IdBotTime.ToSeconds() % 5 == 0 && player.IdBotTime.ToSeconds() == 5 && int.TryParse(player.Info.Mob, out fail)) //&& player.isPlayingAsHider && player.isPlayingGame)
                //        {
                //            short x = (short)(player.Position.X / 32 * 32 + 16);
                //            short y = (short)(player.Position.Y / 32 * 32 + 16);
                //            short z = (short)(player.Position.Z / 32 * 32);
                //            Vector3I Pos = new Vector3I(player.Position.X / 32, player.Position.Y / 32, (player.Position.Z - 32) / 32);
                //            player.solidPosBlock = player.WorldMap.GetBlock(Pos);
                //            player.WorldMap.SetBlock(Pos, player.inGameBlock);
                //            BlockUpdate blockUpdate = new BlockUpdate(null, Pos, player.inGameBlock);
                //            player.World.Map.QueueUpdate(blockUpdate);
                //            player.Message("&7You are now a solid block ({0}) Don't walk around or you will be normal again.", player.inGameBlock);
                //            player.isSolid = true;
                //            player.Info.IsHidden = true;
                //            player.lastSolidPos = Pos;
                //            Player.RaisePlayerHideChangedEvent(player, true, true);
                //        }                
                //    }
                //}

            static
            SchedulerTask gcTask;
        static TimeSpan gcInterval = TimeSpan.FromSeconds( 60 );

        /// <summary> Interval at which Server checks whether forced garbage collection is needed. </summary>
        public static TimeSpan GCInterval {
            get { return gcInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException( "GCInterval may not be negative." );
                gcInterval = value;
                if( gcTask != null ) gcTask.Interval = gcInterval;
            }
        }


        static void DoGC( SchedulerTask task ) {
            if( !gcRequested ) return;
            gcRequested = false;

            Process proc = Process.GetCurrentProcess();
            proc.Refresh();
            long usageBefore = proc.PrivateMemorySize64 / ( 1024 * 1024 );

            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );

            proc.Refresh();
            long usageAfter = proc.PrivateMemorySize64 / ( 1024 * 1024 );

            Logger.Log( LogType.Debug,
                        "Server.DoGC: Collected on schedule ({0}->{1} MB).",
                        usageBefore, usageAfter );
        }


        // shows announcements
        static void ShowRandomAnnouncement( SchedulerTask task ) {
            if( !File.Exists( Paths.AnnouncementsFileName ) ) return;
            string[] lines = File.ReadAllLines( Paths.AnnouncementsFileName );
            if( lines.Length == 0 ) return;
            string line = lines[new Random().Next( 0, lines.Length )].Trim();
            if( line.Length == 0 ) return;
            var visiblePlayers = Server.Players
                                            .Where(p => p.Info.IsHidden == false)
                                            .OrderBy(p => p.Name)
                                            .ToArray();
            if (visiblePlayers.Count() > 0)
            {
                foreach (Player sendtome in Server.Players)
                {
                    if (sendtome.Supports(CpeExtension.MessageType))
                    {
                        if (line.StartsWith("&d", StringComparison.OrdinalIgnoreCase))
                        {
                            line = line.Remove(0, 2);
                        }
                        sendtome.Send(Packet.Message((byte)MessageType.Announcement, "&d" + Chat.ReplaceTextKeywords(Player.Console, line)));
                    }
                    else
                    {
                        sendtome.Message("&d" + Chat.ReplaceTextKeywords(Player.Console, line));
                    }
                }
            }
        }

        // removes announcements
        static void RemoveRandomAnnouncement(SchedulerTask task)
        {
            if (!File.Exists(Paths.AnnouncementsFileName)) return;
            string[] lines = File.ReadAllLines(Paths.AnnouncementsFileName);
            if (lines.Length == 0) return;
            string line = lines[new Random().Next(0, lines.Length)].Trim();
            if (line.Length == 0) return;
            var visiblePlayers = Server.Players
                                            .Where(p => p.Info.IsHidden == false)
                                            .OrderBy(p => p.Name)
                                            .ToArray();
            if (visiblePlayers.Count() > 0)
            {
                foreach (Player sendtome in Server.Players)
                {
                    if (sendtome.Supports(CpeExtension.MessageType))
                    {
                        sendtome.Message((byte)MessageType.Announcement, " ");
                    }
                }
            }
        }

        // Changes day color
        private static void ChangeWorldColors(SchedulerTask task) {
            string hex;
            foreach (Player sendtome in Players.Where(w => w.Supports(CpeExtension.EnvColors) && w.World != null && w.World.SkyLightEmulator)) {
                if (SkyColorHex.TryGetValue(ColorTime, out hex)) {
                    sendtome.Send(Packet.MakeEnvSetColor((byte)EnvVariable.SkyColor, hex));
                }
                if (CloudAndFogColorHex.TryGetValue(ColorTime, out hex)) {
                    sendtome.Send(Packet.MakeEnvSetColor((byte)EnvVariable.CloudColor, hex));
                    sendtome.Send(Packet.MakeEnvSetColor((byte)EnvVariable.FogColor, hex));
                }
            }
            if (ColorTime >= 60) {
                ColorTime = 1;
            } else {
                ColorTime++;
            }
        }


        // measures CPU usage
        public static bool IsMonitoringCPUUsage { get; private set; }
        static TimeSpan cpuUsageStartingOffset;
        public static double CPUUsageTotal { get; private set; }
        public static double CPUUsageLastMinute { get; private set; }

        static TimeSpan oldCPUTime = new TimeSpan( 0 );
        static readonly TimeSpan MonitorProcessorUsageInterval = TimeSpan.FromSeconds( 30 );
        static DateTime lastMonitorTime = DateTime.UtcNow;


        static void MonitorProcessorUsage( SchedulerTask task ) {
            TimeSpan newCPUTime = Process.GetCurrentProcess().TotalProcessorTime - cpuUsageStartingOffset;
            CPUUsageLastMinute = ( newCPUTime - oldCPUTime ).TotalSeconds /
                                 ( Environment.ProcessorCount * DateTime.UtcNow.Subtract( lastMonitorTime ).TotalSeconds );
            lastMonitorTime = DateTime.UtcNow;
            CPUUsageTotal = newCPUTime.TotalSeconds /
                            ( Environment.ProcessorCount * DateTime.UtcNow.Subtract( StartTime ).TotalSeconds );
            oldCPUTime = newCPUTime;
            IsMonitoringCPUUsage = true;
        }

        #endregion


        #region Utilities

        static bool gcRequested;

        /// <summary> Informs the server that garbage collection should be performed.
        /// Actual collection is done on the background task thread, asynchronously, as needed. </summary>
        public static void RequestGC() {
            gcRequested = true;
        }


        internal static bool VerifyName( [NotNull] string name, [NotNull] string hash, [NotNull] string salt ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( hash == null ) throw new ArgumentNullException( "hash" );
            if( salt == null ) throw new ArgumentNullException( "salt" );
            if (hash.Length < 32) hash = hash.PadLeft(32, '0');
            MD5 hasher = MD5.Create();
            StringBuilder sb = new StringBuilder( 32 );
            foreach( byte b in hasher.ComputeHash( Encoding.ASCII.GetBytes( salt + name ) ) ) {
                sb.AppendFormat( "{0:x2}", b );
            }
            return sb.ToString().Equals( hash, StringComparison.OrdinalIgnoreCase );
        }


        internal static int CalculateMaxPacketsPerUpdate( [NotNull] World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            int packetsPerTick = (int)( BlockUpdateThrottling / TicksPerSecond );
            int maxPacketsPerUpdate = (int)( MaxUploadSpeed / TicksPerSecond * 128 );

            int playerCount = world.Players.Length;
            if( playerCount > 0 && !world.IsFlushing ) {
                maxPacketsPerUpdate /= playerCount;
                if( maxPacketsPerUpdate > packetsPerTick ) {
                    maxPacketsPerUpdate = packetsPerTick;
                }
            } else {
                maxPacketsPerUpdate = MaxBlockUpdatesPerTick;
            }

            return maxPacketsPerUpdate;
        }


        public static void BackupData() {
            if( !Paths.TestDirectory( "DataBackup", Paths.DataBackupDirectory, true ) ) {
                Logger.Log( LogType.Error, "Unable to create a data backup." );
                return;
            }
            string backupFileName = String.Format( Paths.DataBackupFileNameFormat, DateTime.Now ); // localized
            backupFileName = Path.Combine( Paths.DataBackupDirectory, backupFileName );
            using( FileStream fs = File.Create( backupFileName ) ) {
                string fileComment = String.Format( "Backup of fCraft data for server \"{0}\", saved on {1}",
                                                    ConfigKey.ServerName.GetString(),
                                                    DateTime.Now );
                using( ZipStorer backupZip = ZipStorer.Create( fs, fileComment ) ) {
                    foreach( string dataFileName in Paths.DataFilesToBackup ) {
                        if( File.Exists( dataFileName ) ) {
                            backupZip.AddFile( ZipStorer.Compression.Deflate,
                                               dataFileName,
                                               dataFileName,
                                               "" );
                        }
                    }
                }
            }
            Logger.Log( LogType.SystemActivity,
                        "&3Backed up server data to \"{0}\"",
                        backupFileName );
        }


        /// <summary> Returns a cryptographically secure random string of given length. </summary>
        [NotNull]
        public static string GetRandomString( int chars ) {
            RandomNumberGenerator prng = RandomNumberGenerator.Create();
            StringBuilder sb = new StringBuilder();
            byte[] oneChar = new byte[1];
            while( sb.Length < chars ) {
                prng.GetBytes( oneChar );
                if( oneChar[0] >= 48 && oneChar[0] <= 57 ||
                    oneChar[0] >= 65 && oneChar[0] <= 90 ||
                    oneChar[0] >= 97 && oneChar[0] <= 122 ) {
                    //if( oneChar[0] >= 33 && oneChar[0] <= 126 ) {
                    sb.Append( (char)oneChar[0] );
                }
            }
            return sb.ToString();
        }


        static readonly Uri IPCheckUri = new Uri("http://fcraft.net/ipcheck.php");
        const int IPCheckTimeout = 20000;


        /// <summary> Checks server's external IP, as reported by fCraft.net. </summary>
        [CanBeNull]
        static IPAddress CheckExternalIP()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(IPCheckUri);
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            request.ReadWriteTimeout = IPCheckTimeout;
            request.ServicePoint.BindIPEndPointDelegate = BindIPEndPointCallback;
            request.Timeout = IPCheckTimeout;
            request.UserAgent = Updater.UserAgent;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Logger.Log(LogType.Warning,
                                    "Could not check external IP: {0}",
                                    response.StatusDescription);
                        return null;
                    }
                    // ReSharper disable AssignNullToNotNullAttribute
                    using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                    {
                        // ReSharper restore AssignNullToNotNullAttribute
                        string responseString = responseReader.ReadToEnd();
                        IPAddress result;
                        if (IPAddress.TryParse(responseString, out result))
                        {
                            return result;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                Logger.Log(LogType.Warning,
                            "Could not check external IP: {0}",
                            ex);
                return null;
            }
        }


        // Callback for setting the local IP binding. Implements System.Net.BindIPEndPoint delegate.
        public static IPEndPoint BindIPEndPointCallback( ServicePoint servicePoint, IPEndPoint remoteEndPoint,
                                                         int retryCount ) {
            return new IPEndPoint( InternalIP, 0 );
        }
        internal static readonly RequestCachePolicy CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);

        #endregion


        #region Player and Session Management

        /// <summary> List of online players.
        /// This property is volatile and can change when players connect/disconnect,
        /// so cache a reference if you need to refer to the same snapshot of the
        /// playerlist more than once. </summary>
        public static Player[] Players { get; private set; }

        // list of all player sessions currently registered with the server
        static readonly List<Player> PlayerIndex = new List<Player>();

        // lock shared by RegisterPlayer/UnregisterPlayer/UpdatePlayerList
        static readonly object PlayerListLock = new object();


        // Registers a player and checks if the server is full.
        // Also kicks any existing connections for this player account.
        // Returns true if player was registered succesfully.
        // Returns false if the server was full.
        internal static bool RegisterPlayer( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            lock( PlayerListLock ) {
                // Kick other sessions with same player name
                Player ghost = PlayerIndex.FirstOrDefault( p => p.Name.Equals( player.Name,
                                                                               StringComparison.OrdinalIgnoreCase ) );
                if( ghost != null ) {
                    // Wait for other session to exit/unregister
                    Logger.Log( LogType.SuspiciousActivity,
                                "Server.RegisterPlayer: Player {0} logged in twice. Ghost from {1} was kicked.",
                                ghost.Name, ghost.IP );
                    ghost.KickSynchronously( "Connected from elsewhere!", LeaveReason.ClientReconnect );
                }

                int maxSessions = ConfigKey.MaxConnectionsPerIP.GetInt();
                // check the number of connections from this IP.
                if( !player.IP.Equals( IPAddress.Loopback ) && maxSessions > 0 ) {
                    int connections = PlayerIndex.Count( p => p.IP.Equals( player.IP ) );
                    if( connections >= maxSessions ) {
                        Logger.Log( LogType.SuspiciousActivity,
                                    "Player.LoginSequence: Denied player {0}: maximum number of connections was reached for {1}",
                                    player.Name, player.IP );
                        player.Kick( "Max connections reached for " + player.IP, LeaveReason.LoginFailed );
                        return false;
                    }
                }

                // check if server is full
                if( PlayerIndex.Count >= ConfigKey.MaxPlayers.GetInt() && !player.Info.Rank.HasReservedSlot ) {
                    player.Kick( "Server is full!", LeaveReason.ServerFull );
                    return false;
                }
                PlayerIndex.Add( player );
                player.HasRegistered = true;
                UpdateTabList();              
                return true;
            }
        }


        public static string MakePlayerConnectedMessage( [NotNull] Player player, bool firstTime, [NotNull] World world ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if (world == null)
                throw new ArgumentNullException( "world" );
            UpdateTabList();
            if( firstTime ) {
                return String.Format("&sPlease welcome {0}&S to the server!\n" + 
                                     "&sThis is their first visit",
                                      player.ClassyName);
            } else {
                return String.Format("&sPlease welcome back {0}&S to the server!\n" +
                                     "&sThey joined {1} times for a total of {2:F1}h",
                                      player.ClassyName,
                                      player.Info.TimesVisited,
                                      player.Info.TotalTime.TotalHours );
            }


        }

        public static string MakePlayerDisconnectedMessage([NotNull] Player player)
        {
            if (player == null) throw new ArgumentNullException("player");
            return String.Format("{0}&s left the server.", player.ClassyName);

            UpdateTabList();
        }


        // Removes player from the list, and announced them leaving
        internal static void UnregisterPlayer( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            lock( PlayerListLock ) {
                if( !player.HasRegistered ) {
                    return;
                }
                player.ResetIdBotTimer();
                player.Info.ProcessLogout( player );

                Logger.Log( LogType.UserActivity,
                            "{0} left the server ({1}).", player.Name, player.LeaveReason );
                if (player.HasFullyConnected && ConfigKey.ShowConnectionMessages.Enabled())
                {
                    if (player.usedquit == false)
                    {
                        Players.CanSee(player).Message("{0}&s left the server.", player.ClassyName);
                    }
                    else if (player.usedquit == true)
                    {
                        Players.CanSee(player).Message("{0}&s left the server (Reason: {1})", player.ClassyName, player.quitmessage);
                    }
                }

                if( player.World != null ) {
                    player.World.ReleasePlayer( player );
                }
                foreach (Player p1 in Players)
                {
                    if (p1.Supports(CpeExtension.ExtPlayerList) || p1.Supports(CpeExtension.ExtPlayerList2))
                    {
                        p1.Send(Packet.MakeExtRemovePlayerName(player.NameID));
                    }
                }
                PlayerIndex.Remove( player );
                player.Info.IsAFK = false;
                UpdatePlayerList();
                UpdateTabList();
            }
        }


        internal static void UpdateTabList() {
            foreach (Player p1 in Players) {
                if (!p1.Supports(CpeExtension.ExtPlayerList) && !p1.Supports(CpeExtension.ExtPlayerList2))
                    continue;
                var canBeSeen = Players.Where(i => p1.CanSee(i)).ToArray();
                var canBeSeenW = p1.World.Players.Where(i => p1.CanSee(i)).ToArray();
                if (!p1.IsPlayingCTF)
                {
                    foreach (Player p2 in canBeSeen)
                    {
                        p1.Send(Packet.MakeExtAddPlayerName(p2.NameID, p2.Name, p2.ListName,
                            p2.World.ClassyName + " &e(&f" + p2.World.CountVisiblePlayers(p1) + "&e)", 0));
                    }
                }
                else
                {
                    foreach (Player p2 in canBeSeenW)
                    {
                        if (p2.IsPlayingCTF && p2.Team == "Red")
                        {
                            p1.Send(Packet.MakeExtAddPlayerName(p2.NameID, p2.Name, "&c" + p2.Name, "&eTeam &4Red", 0));
                        } else if (p2.IsPlayingCTF && p2.Team == "Blue")
                        {
                            p1.Send(Packet.MakeExtAddPlayerName(p2.NameID, p2.Name, "&8" + p2.Name, "&eTeam &1Blue", 0));
                        }
                    }
                }
            }
        }

        internal static void UpdatePlayerList()
        {
            lock (PlayerListLock)
            {
                Players = PlayerIndex.Where(p => p.IsOnline)
                                     .OrderBy(player => player.Name)
                                     .ToArray();
                RaiseEvent(PlayerListChanged);
            }
        }

        /// <summary> Finds a player by name, using autocompletion.
        /// Returns ALL matching players, including hidden ones. </summary>
        /// <param name="namePart"> Full or partial player name. </param>
        /// <param name="options"> Search options (recognizes SuppressEvent). </param>
        /// <returns> An array of matches. List length of 0 means "no matches";
        /// 1 is an exact match; over 1 for multiple matches. </returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Player[] FindPlayers([NotNull] string namePart, SearchOptions options)
        {
            if (namePart == null) throw new ArgumentNullException("namePart");
            bool suppressEvent = (options & SearchOptions.SuppressEvent) != 0;
            Player[] tempList = Players;
            List<Player> results = new List<Player>();
            for (int i = 0; i < tempList.Length; i++)
            {
                if (tempList[i] == null) continue;
                if (tempList[i].Name.Equals(namePart, StringComparison.OrdinalIgnoreCase))
                {
                    results.Clear();
                    results.Add(tempList[i]);
                    break;
                }
                else if (tempList[i].Name.StartsWith(namePart, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(tempList[i]);
                }
            }
            if (!suppressEvent)
            {
                var h = SearchingForPlayer;
                if (h != null)
                {
                    var e = new SearchingForPlayerEventArgs(null, namePart, results, options);
                    h(null, e);
                }
            }
            return results.ToArray();
        }

        /// <summary> Finds a player by name, using autocompletion. Does not include hidden players. 
        /// Raises Player.SearchingForPlayer event, which may modify search results, unless SuppressEvent option is set.
        /// Does not include self unless IncludeSelf search option is set. </summary>
        /// <param name="player"> Player who initiated the search.
        /// Used to determine which hidden players to show in results. </param>
        /// <param name="name"> Full or partial name of the search target. </param>
        /// <param name="options"> Search options.
        /// All flags (IncludeHidden, IncludeSelf, SuppressEvent, and ReturnSelfIfOnlyMatch) are applicable. </param>
        /// <returns> An array of matches. Array length of 0 means "no matches";
        /// 1 means an exact match or a single partial match; over 1 means multiple matches. </returns>
        [NotNull]
        public static Player[] FindPlayers([NotNull] Player player, [NotNull] string name, SearchOptions options)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (name == null) throw new ArgumentNullException("name");

            bool includeHidden = (options & SearchOptions.IncludeHidden) != 0;
            bool includeSelf = (options & SearchOptions.IncludeSelf) != 0;
            bool suppressEvent = (options & SearchOptions.SuppressEvent) != 0;
            bool returnSelf = (options & SearchOptions.ReturnSelfIfOnlyMatch) != 0;

            // Repeat last-used player name
            if (name == "-")
            {
                if (player.LastUsedPlayerName != null)
                {
                    name = player.LastUsedPlayerName;
                }
                else
                {
                    return new Player[0];
                }
            }

            // in case someone tries to use the "!" prefix in an online-only search
            if (name.Length > 0 && name[0] == '!')
            {
                name = name.Substring(1);
            }

            bool foundSelf = false;
            List<Player> results = new List<Player>();
            Player[] tempList = Players;
            foreach (Player otherPlayer in tempList)
            {
                if (otherPlayer == null ||
                    !includeHidden && !player.CanSee(otherPlayer))
                {
                    continue;
                }
                if (otherPlayer.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (!includeSelf && otherPlayer == player)
                    {
                        foundSelf = true;
                    }
                    else
                    {
                        results.Clear();
                        results.Add(otherPlayer);
                        break;
                    }
                }
                else if (otherPlayer.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (!includeSelf && otherPlayer == player)
                    {
                        foundSelf = true;
                    }
                    else
                    {
                        results.Add(otherPlayer);
                    }
                }
            }

            // set LastUsedPlayerName if we found one result
            if (results.Count == 1)
            {
                player.LastUsedPlayerName = results[0].Name;
            }

            // raise the SearchingForPlayer event
            if (!suppressEvent)
            {
                var h = SearchingForPlayer;
                if (h != null)
                {
                    var e = new SearchingForPlayerEventArgs(player, name, results, options);
                    h(null, e);
                }
            }

            // special behavior for ReturnSelfIfOnlyMatch flag
            if (results.Count == 0 && !includeSelf && foundSelf && returnSelf)
            {
                results.Add(player);
            }
            return results.ToArray();
        }

        /// <summary> Finds player by name without autocompletion.
        /// Returns null if no player with the given name is online. </summary>
        /// <param name="player"> Player from whose perspective search is performed. Used to determine whether others are hidden. </param>
        /// <param name="name"> Full player name. Case-insensitive. </param>
        /// <param name="options"> Search options (IncludeHidden and IncludeSelf are applicable, other flags are ignored). </param>
        /// <returns> Player object if player was found online; otherwise null. </returns>
        public static Player FindPlayerExact([NotNull] Player player, [NotNull] string name, SearchOptions options)
        {
            Player target = Players.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            bool includeHidden = (options & SearchOptions.IncludeHidden) != 0;
            bool includeSelf = (options & SearchOptions.IncludeSelf) != 0;
            if (target != null && !includeHidden && !player.CanSee(target) || // hide players whom player cant see
                target == player && !includeSelf)
            { // hide self, if applicable
                target = null;
            }
            return target;
        }


        /// <summary> Find player by name using autocompletion.
        /// Returns null and prints message if none or multiple players matched.
        /// Raises Player.SearchingForPlayer event, which may modify search results, unless SuppressEvent option is set. </summary>
        /// <param name="player"> Player who initiated the search. This is where messages are sent. </param>
        /// <param name="namePart"> Full or partial name of the search target. </param>
        /// <param name="options"> Search options.
        /// All flags (IncludeHidden, IncludeSelf, SuppressEvent, and ReturnSelfIfOnlyMatch) are applicable. </param>
        /// <returns> Player object, or null if no player was found. </returns>
        [CanBeNull]
        public static Player FindPlayerOrPrintMatches([NotNull] Player player,
                                                      [NotNull] string namePart,
                                                      SearchOptions options)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (namePart == null) throw new ArgumentNullException("namePart");

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

            // in case someone tries to use the "!" prefix in an online-only search
            if (namePart.Length > 0 && namePart[0] == '!')
            {
                namePart = namePart.Substring(1);
            }

            // Make sure player name is valid
            if (!Player.ContainsValidCharacters(namePart))
            {
                player.MessageInvalidPlayerName(namePart);
                return null;
            }

            Player[] matches = FindPlayers(namePart, options);

            if (matches.Length == 0)
            {
                player.MessageNoPlayer(namePart);
                return null;

            }
            else if (matches.Length > 1)
            {
                player.MessageManyMatches("player", matches);
                return null;

            }
            else
            {
                player.LastUsedPlayerName = matches[0].Name;
                return matches[0];
            }
        }


        /// <summary> Counts online players, optionally including hidden ones. </summary>
        public static int CountPlayers( bool includeHiddenPlayers ) {
            if( includeHiddenPlayers ) {
                return Players.Length;
            } else {
                return Players.Count( player => !player.Info.IsHidden );
            }
        }


        /// <summary> Counts online players whom the given observer can see. </summary>
        public static int CountVisiblePlayers( [NotNull] Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            return Players.Count( observer.CanSee );
        }

        #endregion
    }
}

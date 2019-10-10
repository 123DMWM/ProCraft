﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
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
using fCraft.Games;
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

        /// <summary> External IP address of this machine, as reported by http://fcraft.net/ipcheck.php </summary>
        public static IPAddress ExternalIP { get; private set; }

        /// <summary> Number of the local listening port. </summary>
        public static int Port { get; private set; }

        /// <summary> Minecraft.net connection URL. </summary>
        public static Uri Uri { get; internal set; }

        /// <summary> Default terrain file for each world.</summary>
        public static string DefaultTerrain { get; set; }

        /// <summary> Software name that shows up on the server list on classicube.</summary>
        public static string Software = "&cP&4R&6O&eC&aR&2A&bF&3T";

        internal static int MaxUploadSpeed, // set by Config.ApplyConfig
                            BlockUpdateThrottling; // used when there are no players in a world
        internal const int MaxSessionPacketsPerTick = 128, MaxBlockPacketsPerTick = 256,
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

            Logger.Log( LogType.Debug, "Working directory: {0}", Directory.GetCurrentDirectory() );
            Logger.Log( LogType.Debug, "Log path: {0}", Path.GetFullPath( Paths.LogPath ) );
            Logger.Log( LogType.Debug, "Map path: {0}", Path.GetFullPath( Paths.MapPath ) );
            Logger.Log( LogType.Debug, "Config path: {0}", Path.GetFullPath( Paths.ConfigFileName ) );

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
            Color.LoadExtColors();

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
            PluginManager.Init();

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
            string dstPath = Path.Combine(Paths.BlockDefsDirectory, Paths.GlobalDefsFile);
            if (!Directory.Exists(Paths.BlockDefsDirectory))
                Directory.CreateDirectory(Paths.BlockDefsDirectory);
            if (File.Exists(Paths.GlobalDefsFile)) {
                File.Copy(Paths.GlobalDefsFile, dstPath, true);
                File.Delete(Paths.GlobalDefsFile);
            }
            
            BlockDefinition.LoadGlobalDefinitions();
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
                            "Server.Run: now accepting connections on port {0}", Port );
            } else {
                Logger.Log( LogType.SystemActivity,
                            "Server.Run: now accepting connections at {0}:{1}",
                            ExternalIP, Port );
            }

            // list loaded worlds
            WorldManager.UpdateWorldList();
            Logger.Log( LogType.SystemActivity,
                        "All available worlds: {0}",
                        WorldManager.Worlds.JoinToString( ", ", w => w.ClassyName ) );

            Logger.Log( LogType.SystemActivity,
                        "Main world: {0}&3; default rank: {1}",
                        WorldManager.MainWorld.ClassyName, RankManager.DefaultRank.ClassyName );

            // Check for incoming connections (every 250ms)
            checkConnectionsTask = Scheduler.NewTask( CheckConnections ).RunForever( CheckConnectionsInterval );

            // Check for idles (every 1s)
            checkIdlesTask = Scheduler.NewTask( CheckIdles ).RunForever( CheckIdlesInterval );// Check for idles (every 30s)

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
            }
            if (ConfigKey.IRCBotEnabled.Enabled() || ConfigKey.IRCBotChannels.GetString().Length >= 1 || ConfigKey.IRCBotNetwork.GetString().Length >= 1) {
                Scheduler.NewTask( SendIrcPing ).RunForever( TimeSpan.FromMinutes( 15 ) );
            }

            ChatTimer.LoadAll();
            Report.LoadAll();
            ChatFilter.Load();
            Entity.LoadAll();
            DownloadResources();
            
            PortalHandler.Init();
            PortalDB.Load();
            EnvPresets.LoadAll();

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
        
        static void DownloadResources() {
            if (!Directory.Exists("./Bot")) Directory.CreateDirectory("./Bot");
            if (!Directory.Exists("./fonts")) Directory.CreateDirectory("./fonts");            
       
            DownloadResource("https://123DMWM.com/ProCraft/resources/Funfacts.txt", "./Bot/Funfacts.txt");
            DownloadResource("https://123DMWM.com/ProCraft/resources/Jokes.txt", "./Bot/Jokes.txt");
            DownloadResource("https://123DMWM.com/ProCraft/resources/Protips.txt", "./Bot/Protips.txt");
            DownloadResource("https://123DMWM.com/ProCraft/resources/Adjectives.txt", "./Bot/Adjectives.txt");
            DownloadResource("https://123DMWM.com/ProCraft/resources/Nouns.txt", "./Bot/Nouns.txt");
            
            DownloadResource("https://123DMWM.com/ProCraft/resources/MOTDList.txt", "./MOTDList.txt");
            DownloadResource("https://123DMWM.com/ProCraft/resources/comicsans.ttf", "./fonts/comicsans.ttf");
            DownloadResource("https://123DMWM.com/ProCraft/resources/mcclassic.ttf", "./fonts/mcclassic.ttf");
            DownloadResource("https://123DMWM.com/ProCraft/resources/microsoft.ttf", "./fonts/microsoft.ttf");
            DownloadResource("https://123DMWM.com/ProCraft/resources/minecraft.ttf", "./fonts/minecraft.ttf");
        }
        
        static void DownloadResource(string url, string file) {
            if (File.Exists(file)) return;
            
            try {
                TimeSpan timeout = TimeSpan.FromSeconds(20);
                using (WebClient c = HttpUtil.CreateWebClient(timeout)) {
                    c.DownloadFile(url, file);
                }
                Logger.Log(LogType.SystemActivity, "Succesfully download resource {0} to {1}. ", url, file);
            } catch (Exception ex) {
                WebException webEx = ex as WebException;
                if (webEx != null) {
                    WebResponse resp = webEx.Response;
                    if (resp != null) resp.Close();
                }
                
                Logger.Log(LogType.Warning, "Error downloading resource {0}: {1}", file, ex);
            }
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

        public static void BotMessage( string message, [CanBeNull] params object[] formatArgs) {
            if (formatArgs.Length > 0) {
                int count = 0;
                foreach (object obj in formatArgs) {
                    if (obj is int) {
                        formatArgs[count] = string.Format("{0:#,##0}", obj);
                    }
                    count++;
                }
                message = String.Format(message, formatArgs);
            }
            Server.Players.Message("&6Bot&f: " + message);
            Logger.Log(LogType.UserActivity, "&6Bot&f: " + message);
            IRC.SendChannelMessage("\u212C&6Bot\u211C: " + Color.StripColors(message, true));
        }

        #endregion


        #region Scheduled Tasks

        // checks for incoming connections
        static SchedulerTask checkConnectionsTask;
        static TimeSpan checkConnectionsInterval = TimeSpan.FromMilliseconds( 125 );

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
        static TimeSpan checkIdlesInterval = TimeSpan.FromSeconds( 1 );

        /// <summary> Interval at which Server checks for idle players (to kick idlers). </summary>
        public static TimeSpan CheckIdlesInterval {
            get { return checkIdlesInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException( "CheckIdlesInterval may not be negative." );
                checkIdlesInterval = value;
                if( checkIdlesTask != null ) checkIdlesTask.Interval = checkIdlesInterval;
            }
        }


        private static void CheckIdles(SchedulerTask task) {
            Player[] tempPlayerList = Players;
            for (int i = 0; i < tempPlayerList.Length; i++) {
                Player player = tempPlayerList[i];

                if (player.Supports(CpeExt.MessageType)) {
                    UpdateStatusMessages(player);
                }
                CTF.PrintCtfState(player);
                player.lastSolidPos = player.Position;

                if (player.Info.Rank.IdleKickTimer <= 0) continue;
                TimeSpan TimeLeft = new TimeSpan(0, player.Info.Rank.IdleKickTimer, 0) - player.IdleTime;

                if (player.IdleTime.ToSeconds()%300 == 0 && player.IdleTime.ToSeconds() >= 300) {
                    if (!player.IsAFK) {
                        Players.CanSee(player).Message("{0} is now AFK (Auto)", player.Name);
                        player.IsAFK = true;
                        player.oldafkMob = player.afkMob;
                        player.afkMob = player.AFKModel;

                    }
                    player.Message("You have " + TimeLeft.ToMiniString() + " left before being kicked for idleing");
                }

                if (player.IdleTime.TotalMinutes >= player.Info.Rank.IdleKickTimer) {
                    Message("{0}&S was kicked for being idle for {1} min", player.ClassyName,
                        player.Info.Rank.IdleKickTimer);
                    string kickReason = "Idle for " + player.Info.Rank.IdleKickTimer + " minutes";
                    player.Kick(Player.Console, kickReason, LeaveReason.IdleKick, false, true, false);
                    player.Info.TotalTime = player.Info.TotalTime - player.IdleTime;
                    player.IsAFK = false;
                    player.oldafkMob = player.afkMob;
                    player.afkMob = player.Info.Model;
                    player.ResetIdleTimer(); // to prevent kick from firing more than once
                }
            }
            UpdateTabList(false);
        }
        
        static void UpdateStatusMessages(Player player) {
            //double speed = (Math.Sqrt(player.Position.DistanceSquaredTo(player.lastSolidPos)) / 32);
            //player.Send(Packet.Message((byte)MessageType.Announcement, string.Format("&eSpeed: &f{0:N2} &eBlocks/s", speed), player.UseFallbackColors));
            string bottomRight2 = player.Position.ToBlockCoordsRaw() + "&S[" + compassString(player.Position.R) + "&S]";
            if (bottomRight2 != player.lastBottomRight2) {
                player.lastBottomRight2 = bottomRight2;
                player.Send(Packet.Message((byte)MessageType.BottomRight2, bottomRight2, player));
            }
            
            if (player.LastDrawOp != null && !player.IsPlayingCTF) {
                if (player.LastDrawOp.PercentDone < 100) {
                    player.Send(Packet.Message((byte)MessageType.Status3, player.LastDrawOp.Description +
                                               " percent done: &f" + player.LastDrawOp.PercentDone + "&S%", player));
                } else if (player.LastDrawOp.PercentDone == 100 || player.LastDrawOp.IsDone) {
                    if (!player.AnnouncedLastDrawOpFinished) {
                        player.Send(Packet.Message((byte)MessageType.Status3, "", player));
                        player.AnnouncedLastDrawOpFinished = true;
                    }
                }
            }
        }
        
        public static string compassString(int rot) {
            if (rot > 240 || rot < 15) {
                return "&1S";
            } else if (16 <= rot && rot <= 47) {
                return "&9S&fW";
            } else if (48 <= rot && rot <= 79) {
                return "&fW";
            } else if (80 <= rot && rot <= 111) {
                return "&cN&fW";
            } else if (112 <= rot && rot <= 143) {
                return "&4N";
            } else if (144 <= rot && rot <= 175) {
                return "&cN&fE";
            } else if (176 <= rot && rot <= 207) {
                return "&fE";
            } else if (208 <= rot && rot <= 239) {
                return "&9S&fE";
            } else
                return "&fN/A";
        }

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

                if (player.IdleTime.ToSeconds() % 300 == 0 && player.IdleTime.ToSeconds() >= 300)
                {
                    if (player.Info.IsAFK == false)
                    {
                        Server.Players.CanSee(player).Message("{0} is now AFK (Auto)", player.Name);
                        player.Info.IsAFK = true;
                        player.Info.Mob = player.AFKModel;
                        int TimeLeft = (player.Info.Rank.IdleKickTimer - (int)player.IdleTime.ToMinutes());
                        player.Message("You have " + TimeLeft + "m left before you get kicked for being AFK");
                    }
                    else
                    {
                        player.Info.IsAFK = true;
                        int TimeLeft = (player.Info.Rank.IdleKickTimer - (int)player.IdleTime.ToMinutes());
                        player.Message("You have " + TimeLeft + "m left before you get kicked for being AFK");
                    }
                }

                if (player.IdleTime.Minutes >= player.Info.Rank.IdleKickTimer)
                {
                    Message("{0}&S was kicked for being idle for {1} min",
                             player.ClassyName,
                             player.Info.Rank.IdleKickTimer);
                    string kickReason = "Idle for " + player.Info.Rank.IdleKickTimer + " minutes";
                    player.Kick(Player.Console, kickReason, LeaveReason.IdleKick, false, true, false);
                    player.Info.TotalTime = player.Info.TotalTime - player.IdleTime;
                    player.Info.IsAFK = false;
                    player.ResetIdleTimer(); // to prevent kick from firing more than once
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
        //        if (player.IdleTime.ToSeconds() % 5 == 0 && player.IdleTime.ToSeconds() == 5 && int.TryParse(player.Info.Mob, out fail)) //&& player.isPlayingAsHider && player.isPlayingGame)
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
            
            Player[] players = Server.Players;
            if ( players.Length == 0 ) return;
            
            foreach (Player pl in players) {
                if (pl.Supports(CpeExt.MessageType)) {
                    if (line.CaselessStarts("&d"))
                        line = line.Remove(0, 2);
                    pl.Send(Packet.Message((byte)MessageType.Announcement,
                                           "&d" + Chat.ReplaceTextKeywords(pl, line), pl));
                } else {
                    pl.Message("&d" + Chat.ReplaceTextKeywords(pl, line));
                }
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
        

        static void SendIrcPing( SchedulerTask task ) {
            IRC.SendRawMessage(IRCCommands.Ping(ConfigKey.IRCBotChannels.GetString()), "", "");
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
            if( hash.Length < 32 ) hash = hash.PadLeft( 32, '0' );
            
            byte[] md5 = MD5.Create().ComputeHash( Encoding.ASCII.GetBytes( salt + name ) );
            string computedHash = ToHexString( md5 );
            return computedHash.CaselessEquals( hash );
        }
        
        static string ToHexString( byte[] array ) {
            char[] hex = new char[array.Length * 2];
            for( int i = 0; i < array.Length; i++ ) {
                int value = array[i];
                int hi = value >> 4, lo = value & 0x0F;
                
                // 48 = index of 0, 55 = index of (A - 10).
                hex[i * 2 + 0] = hi < 10 ? (char)(hi + 48) : (char)(hi + 55);
                hex[i * 2 + 1] = lo < 10 ? (char)(lo + 48) : (char)(lo + 55);
            }
            return new String(hex);
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
                string fileComment = String.Format( "Backup of ProCraft data for server \"{0}\", saved on {1}",
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
                        "Backed up server data to \"{0}\"",
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


        const string ipCheckUri = "http://www.classicube.net/api/myip/";
        const int IPCheckTimeout = 20000;


        /// <summary> Checks server's external IP, as reported by fCraft.net. </summary>
        [CanBeNull]
        static IPAddress CheckExternalIP() {
            string data = HttpUtil.DownloadString(ipCheckUri, "check external IP", IPCheckTimeout);
            IPAddress ip = null;
            
            if (data == null || !IPAddress.TryParse(data, out ip)) return null;
            return ip;
        }

        // Callback for setting the local IP binding. Implements System.Net.BindIPEndPoint delegate.
        public static IPEndPoint BindIPEndPointCallback( ServicePoint servicePoint, IPEndPoint remoteEndPoint,
                                                         int retryCount ) {
            // InternalIP is ipv4 address, so we can't use it when connecting to a website via ipv6
            // Otherwise it gets stuck trying to bind to an ipv4 address with this:
            /* System.Net.Sockets Error: 0 : [1696] Socket#13869071::UpdateStatusAfterSocketError() - Fault
               System.Net.Sockets Error: 0 : [1696] Exception in Socket#13869071::DoBind - The system detected an invalid pointer address in attempting to use a pointer argument in a call.
               System.Net.Sockets Verbose: 0 : [1696] Socket#13869071::InternalBind(0.0.0.0:0#0) 
             */
            if (remoteEndPoint.AddressFamily != AddressFamily.InterNetwork) return null;
            
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
                Player ghost = PlayerIndex.FirstOrDefault( p => p.Name.CaselessEquals( player.Name ) );
                if( ghost != null ) {
                    // Wait for other session to exit/unregister
                    Logger.Log( LogType.SuspiciousActivity,
                                "Server.RegisterPlayer: Player {0} logged in twice. Ghost from {1} was kicked.",
                                ghost.Name, ghost.IP );
                    ghost.Kick( "Connected from elsewhere!", LeaveReason.ClientReconnect, false );
                    Server.UnregisterPlayer( ghost );
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
                UpdateTabList(true);              
                return true;
            }
        }


        public static string MakePlayerConnectedMessage([NotNull] Player player, bool firstTime) {
            if (player == null) throw new ArgumentNullException("player");
            UpdateTabList(true);
            string ip = player.Info.LastIP.ToString();
            if (IPAddress.Parse(ip).IsLocal() && ExternalIP != null)
                ip = ExternalIP.ToString();
            return string.Format("&2(&A{0}&2) Connected{1}." + "{2}", player.ClassyName, 
                player.Info.TimesVisited == 1 ? " for the first time" : 
                (ip != player.Info.GeoIP || string.IsNullOrEmpty(player.Info.CountryName)) ? "" :
                " from " + player.Info.CountryName, string.IsNullOrEmpty(player.ClientName) ? "" :
                "&N&BUsing: " + player.ClientName);
        }


        public static string MakePlayerDisconnectedMessage([NotNull] Player player)
        {
            if (player == null)
                throw new ArgumentNullException("player");
            UpdateTabList(true);
            return string.Format("&4(&C{0}&4) Disconnected.", 
                (player.Info.TimeSinceFirstLogin <= TimeSpan.FromDays(1) ? Chat.newPlayerPrefix.ToString() : "") + player.ClassyName);

        }


        // Removes player from the list, and announced them leaving
        internal static void UnregisterPlayer( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            lock( PlayerListLock ) {
                if( !player.HasRegistered ) {
                    return;
                }
                player.ResetIdleTimer();
                player.Info.ProcessLogout( player );

                Logger.Log( LogType.UserActivity,
                            "{0} &4disconnected &S({1}).", player.Name, player.LeaveReason );
                if (player.HasFullyConnected && ConfigKey.ShowConnectionMessages.Enabled())
                {
                    Players.Where(p => !p.IsStaff).CanSee(player).Message("{0}{1}", MakePlayerDisconnectedMessage(player), player.usedquit ? " &cReason: " + player.quitmessage : "");
                    Players.Where(p => p.IsStaff).CanSee(player).Message("{0} &cReason: {1}", MakePlayerDisconnectedMessage(player), player.usedquit ? player.quitmessage : player.LeaveReason.ToString());
                }

                if( player.World != null ) {
                    player.World.ReleasePlayer( player );
                }
                foreach (Player p1 in Players)
                {
                    if (p1.Supports(CpeExt.ExtPlayerList) || p1.Supports(CpeExt.ExtPlayerList2))
                    {
                        p1.Send(Packet.MakeExtRemovePlayerName(player.NameID));
                    }
                }
                PlayerIndex.Remove( player );
                player.IsAFK = false;
                UpdatePlayerList();
                UpdateTabList(true);
            }
        }
        
        internal static void UpdateTabList(bool force) {
            foreach (Player p in Players) {
                IEnumerable<Player> canSee = p.IsPlayingCTF ? null : Players.Where(pl => pl.CanSee(p)).ToArray();
                string nick = GetNick(p), group = GetGroup(p, canSee);
                
                if (p.Supports(CpeExt.MessageType)) {
                    string status2 = p.World == null ? p.Name :
                        p.Name + " on world " + p.World.ClassyName;
                    if (status2 != p.lastStatus2) {
                        p.lastStatus2 = status2;
                        p.Send(Packet.Message((byte)MessageType.Status2, status2, p));
                    }
                }
                
                if (!force && (nick == p.lastDisplayName && group == p.lastGroupName)) continue;
                p.lastDisplayName = nick;
                p.lastGroupName = group;
                
                if (!p.IsPlayingCTF) {
                    foreach (Player pl in canSee) {
                        if (!pl.Supports(CpeExt.ExtPlayerList) && !pl.Supports(CpeExt.ExtPlayerList2)) continue;
                        
                        IEnumerable<Player> plCanSee = Players.Where(other => pl.CanSee(other)).ToArray();
                        pl.Send(Packet.MakeExtAddPlayerName(
                            p.NameID, p.Name, nick, GetGroup(p, plCanSee), (byte)p.Info.Rank.Index, pl.FallbackColors, pl.HasCP437));
                    }
                } else {
                    var canBeSeenW = p.World.Players.Where(pl => pl.CanSee(p)).ToArray();
                    foreach (Player pl in canBeSeenW) {
                        if (!pl.Supports(CpeExt.ExtPlayerList) && !pl.Supports(CpeExt.ExtPlayerList2)) continue;
                        if (pl.IsPlayingCTF)
                            pl.Send(Packet.MakeExtAddPlayerName(p.NameID, p.Name, nick, group, 0, p.FallbackColors, pl.HasCP437));
                    }
                }
            }
        }
        
        static string GetNick(Player p) {
            if (p.IsPlayingCTF) return "&f" + p.Name;
            if (p.Info.DisplayedName == null) return p.Name;
            
            string nick = Color.StripColors(p.Info.DisplayedName, true);
            bool nickSame = nick.CaselessEquals(p.Info.Name);
            return nickSame ? Color.White + p.Name : Color.White + p.Name + " &S(&7" + nick + "&S)";
        }
        
        static string GetGroup(Player p, IEnumerable<Player> canBeSeen) {
            if (p.IsPlayingCTF) return "&STeam " + p.Team.ClassyName;
            if (p.IsAFK) return "&SAway From Keyboard (&f" + canBeSeen.Where(pl => pl.IsAFK).Count() + "&S)";
            return "&S" + p.World.Name + " (&f" + canBeSeen.Where(pl => !pl.IsAFK && pl.World == p.World).Count() + "&S)";
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
        public static Player[] FindPlayers([NotNull] string namePart, SearchOptions options) {
            if (namePart == null) throw new ArgumentNullException("namePart");
            bool suppressEvent = (options & SearchOptions.SuppressEvent) != 0;
            bool includeHidden = (options & SearchOptions.IncludeHidden) != 0;
            List<Player> matches = null;
            
            if (includeHidden)
                matches = NameMatcher.Find(Players, namePart, p => p.Name);
            else
                matches = NameMatcher.Find(Players, namePart, p => p.Info.IsHidden ? null : p.Name);
            
            var h = SearchingForPlayer;
            if (!suppressEvent && h != null) {
                var e = new SearchingForPlayerEventArgs(null, namePart, matches, options);
                h(null, e);
                matches = e.Matches;
            }
            return matches.ToArray();
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
            if (name == "-") {
                if (player.LastUsedPlayerName != null) {
                    name = player.LastUsedPlayerName;
                } else {
                    return new Player[0];
                }
            }

            // in case someone tries to use the "!" prefix in an online-only search
            if (name.Length > 0 && name[0] == '!') {
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
                if (otherPlayer.Name.CaselessEquals(name))
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
                else if (otherPlayer.Name.CaselessStarts(name))
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
            Player target = Players.FirstOrDefault(p => p.Name.CaselessEquals(name));
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
            return NameMatcher.FindPlayerMatches(player, namePart, options);
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

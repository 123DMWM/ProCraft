// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using fCraft.Events;
using fCraft.GUI;

namespace fCraft.ServerGUI {

    public sealed partial class MainForm : Form {
        volatile bool shutdownPending, startupComplete, shutdownComplete;
        const int MaxLinesInLog = 2000,
                  LinesToTrimWhenExceeded = 50;

        public MainForm() {
            InitializeComponent();
            Shown += BeginStartup;
        }

        #region Startup
        Thread startupThread;

        void BeginStartup( object sender, EventArgs e ) {
            // check for assembly mismatch
            if( typeof( Server ).Assembly.GetName().Version != typeof( Program ).Assembly.GetName().Version ) {
                MessageBox.Show( "fCraft.dll version does not match ServerGUI.exe version." );
                Application.Exit();
                return;
            }

            // force form handle to be created to make sure that InvokeRequire returns correct results.
#pragma warning disable 168
            var forcedHandle = Handle;
#pragma warning restore 168

            // put fCraft version into the title
            Text = "ProCraft " + Updater.CurrentRelease.VersionString + " - starting...";

            // set up event hooks
            Logger.Logged += OnLogged;
            Heartbeat.UriChanged += OnHeartbeatUriChanged;
            Server.PlayerListChanged += OnPlayerListChanged;
            Server.ShutdownEnded += OnServerShutdownEnded;
            console.OnCommand += console_Enter;

            // set up a context menu for logBox (thanks Jonty)
            logBox.ContextMenu = new ContextMenu( new[] {
                new MenuItem( "Copy", CopyMenuOnClickHandler )
            } );
            logBox.ContextMenu.Popup += CopyMenuPopupHandler;

            // start fCraft from a separate thread (to keep UI responsive)
            startupThread = new Thread( StartupThread ) {
                Name = "fCraft.ServerGUI.Startup",
                CurrentCulture = new CultureInfo( "en-US" )
            };
            startupThread.Start();
        }


        void StartupThread() {
#if !DEBUG
            try {
#endif
                Server.InitLibrary( Environment.GetCommandLineArgs() );
                if( shutdownPending ) return;

                Server.InitServer();
                if( shutdownPending ) return;

                BeginInvoke( (Action)OnInitSuccess );

                // set process priority
                if( !ConfigKey.ProcessPriority.IsBlank() ) {
                    try {
                        Process.GetCurrentProcess().PriorityClass = ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                    } catch( Exception ) {
                        Logger.Log( LogType.Warning,
                                    "MainForm.StartServer: Could not set process priority, using defaults." );
                    }
                }

                if( shutdownPending ) return;
                if( Server.StartServer() ) {
                    startupComplete = true;
                    BeginInvoke( (Action)OnStartupSuccess );
                } else {
                    BeginInvoke( (Action)OnStartupFailure );
                }
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in ServerGUI.StartUp", "ServerGUI", ex, true );
                Shutdown( ShutdownReason.Crashed );
            }
#endif
        }


        void OnInitSuccess() {
            Text = "ProCraft " + Updater.CurrentRelease.VersionString + " - " + Color.StripColors(ConfigKey.ServerName.GetString());
        }


        void OnStartupSuccess() {
            if( !ConfigKey.HeartbeatEnabled.Enabled() ) {
                uriDisplay.Text = "Heartbeat disabled. See externalurl.txt";
            }
            console.Enabled = true;
            console.Text = "";
        }


        void OnStartupFailure() {
            Shutdown( ShutdownReason.FailedToStart );
        }

        #endregion


        #region Shutdown

        protected override void OnFormClosing( FormClosingEventArgs e ) {
            if( startupThread != null && !shutdownComplete ) {
                Shutdown( ShutdownReason.ProcessClosing );
                e.Cancel = true;
            } else {
                base.OnFormClosing( e );
            }
        }


        void Shutdown( ShutdownReason reason ) {
            if( shutdownPending ) return;
            shutdownPending = true;
            console.Enabled = false;
            console.Text = "Shutting down...";
            Text = "ProCraft " + Updater.CurrentRelease.VersionString + " - shutting down...";
            uriDisplay.Enabled = false;
            if( !startupComplete ) {
                startupThread.Join();
            }
            Server.Shutdown( new ShutdownParams( reason, TimeSpan.Zero, false ), false );
        }


        void OnServerShutdownEnded( object sender, ShutdownEventArgs e ) {
            try {
                BeginInvoke( (Action)delegate {
                    shutdownComplete = true;
                    switch( e.ShutdownParams.Reason ) {
                        case ShutdownReason.FailedToInitialize:
                        case ShutdownReason.FailedToStart:
                        case ShutdownReason.Crashed:
                            if( Server.HasArg( ArgKey.ExitOnCrash ) ) {
                                Application.Exit();
                            }
                            break;
                        default:
                            Application.Exit();
                            break;
                    }
                } );
            } catch( ObjectDisposedException ) {
            } catch( InvalidOperationException ) { }
        }

        #endregion


        [DebuggerStepThrough]
        void OnLogged( object sender, LogEventArgs e ) {
            if( !e.WriteToConsole ) return;
            try {
                if( shutdownComplete ) return;
                if( InvokeRequired ) {
                    BeginInvoke( (EventHandler<LogEventArgs>)OnLogged, sender, e );
                } else {
                    // store user's selection
                    int userSelectionStart = logBox.SelectionStart;
                    int userSelectionLength = logBox.SelectionLength;
                    bool userSelecting = (logBox.SelectionStart != logBox.TextLength && logBox.Focused ||
                                          logBox.SelectionLength > 0);

                    // insert and color a new message
                    int oldLength = logBox.TextLength;
                    char initialcolor = 'f';
                    switch( e.MessageType ) {
                        case LogType.Warning:
                            initialcolor = 'e'; break;
                        case LogType.Debug:
                            initialcolor = '8'; break;
                        case LogType.Error:
                        case LogType.SeriousError:
                            initialcolor = 'c'; break;
                        case LogType.ConsoleInput:
                        case LogType.ConsoleOutput:
                            initialcolor = 'f'; break;
                        default:
                            initialcolor = '7'; break;
                    }
                    
                    int index = 0;
                    while( index < e.Message.Length )
                        AppendNextPart( ref initialcolor, ref index, e.Message, logBox );
                    logBox.AppendText( Environment.NewLine );

                    // cut off the log, if too long
                    if( logBox.Lines.Length > MaxLinesInLog ) {
                        logBox.SelectionStart = 0;
                        logBox.SelectionLength = logBox.GetFirstCharIndexFromLine( LinesToTrimWhenExceeded );
                        userSelectionStart -= logBox.SelectionLength;
                        if( userSelectionStart < 0 ) userSelecting = false;
                        string textToAdd = "&3----- cut off, see " + Logger.CurrentLogFileName + " for complete log -----" + Environment.NewLine;
                        logBox.SelectedText = textToAdd;
                        userSelectionStart += textToAdd.Length;
                        logBox.SelectionColor = System.Drawing.Color.DarkGray;
                    }

                    // either restore user's selection, or scroll to end
                    if( userSelecting ) {
                        logBox.Select( userSelectionStart, userSelectionLength );
                    } else {
                        logBox.SelectionStart = logBox.TextLength;
                        logBox.ScrollToCaret();
                    }
                }
            } catch( ObjectDisposedException ) {
            } catch( InvalidOperationException ) { }
        }

        static void AppendNextPart( ref char code, ref int start, string message, RichTextBox logBox ) {
            int nextAnd = message.IndexOf( '&', start );
            int total = logBox.TextLength;
            
            if( nextAnd == -1 || nextAnd == (message.Length - 1) ) {
                string part = message.Substring( start );
                logBox.AppendText( part );
                logBox.Select( total, part.Length );                
                SetSelectionColor( code, logBox );
                start = message.Length;
            } else {
                string part = message.Substring( start, nextAnd - start );
                logBox.AppendText( part );
                logBox.Select( total, part.Length );            
                SetSelectionColor( code, logBox );
                start = nextAnd + 2;                
                code = message[nextAnd + 1];
            }
        }
        
        static void SetSelectionColor( char code, RichTextBox logBox ) {
            string name = Color.GetName( code );
            if( code == '0' ) {
                logBox.SelectionColor = System.Drawing.Color.FromArgb( 64, 64, 64 );
            } else if( name != null ) {
                logBox.SelectionColor = System.Drawing.Color.FromName( name );
            } else {
                logBox.SelectionColor = System.Drawing.Color.White;
            }            
        }

        void OnHeartbeatUriChanged( object sender, UrlChangedEventArgs e ) {
            try {
                if( shutdownPending ) return;
                if( uriDisplay.InvokeRequired ) {
                    BeginInvoke( (EventHandler<UrlChangedEventArgs>)OnHeartbeatUriChanged,
                            sender, e );
                } else {
                    uriDisplay.Text = e.NewUrl.ToString();
                    uriDisplay.Enabled = true;
                    bPlay.Enabled = true;
                }
            } catch( ObjectDisposedException ) {
            } catch( InvalidOperationException ) { }
        }


        void OnPlayerListChanged( object sender, EventArgs e ) {
            try {
                if( shutdownPending ) return;
                if( playerList.InvokeRequired ) {
                    BeginInvoke( (EventHandler)OnPlayerListChanged, null, EventArgs.Empty );
                } else {
                    playerList.Items.Clear();
                    Player[] playerListCache = Server.Players.OrderBy( p => p.Info.Rank.Index ).ToArray();
                    foreach( Player player in playerListCache ) {
                        playerList.Items.Add( player.Info.Rank.Name + " - " + player.Name );
                    }
                }
            } catch( ObjectDisposedException ) {
            } catch( InvalidOperationException ) { }
        }


        private void console_Enter() {
            string[] separator = { Environment.NewLine };
            string[] lines = console.Text.Trim().Split( separator, StringSplitOptions.RemoveEmptyEntries );
            foreach( string line in lines ) {
#if !DEBUG
                try {
#endif
                    if( line.Equals( "/Clear", StringComparison.OrdinalIgnoreCase ) ) {
                        logBox.Clear();
                    } else if( line.Equals( "/credits", StringComparison.OrdinalIgnoreCase ) ) {
                        new AboutWindow().Show();
                    } else {
                        Player.Console.ParseMessage( line, true );
                    }
#if !DEBUG
                } catch( Exception ex ) {
                    Logger.LogToConsole( "Error occurred while trying to execute last console command: " );
                    Logger.LogToConsole( ex.GetType().Name + ": " + ex.Message );
                    Logger.LogAndReportCrash( "Exception executing command from console", "ServerGUI", ex, false );
                }
#endif
            }
            console.Text = "";
        }


        private void bPlay_Click( object sender, EventArgs e ) {
            try {
                Process.Start( uriDisplay.Text );
            } catch( Exception ) {
                Clipboard.SetText( uriDisplay.Text, TextDataFormat.Text );
                MessageBox.Show( "Server URL has been copied to clipboard." );
            }
        }


        // CopyMenuOnClickHandler and CopyMenuPopupHandler by Jonty800
        private void CopyMenuOnClickHandler( object sender, EventArgs e ) {
            if( logBox.SelectedLength > 0 ) {
                Clipboard.SetText( logBox.SelectedText, TextDataFormat.Text );
            }
        }


        private void CopyMenuPopupHandler( object sender, EventArgs e ) {
            ContextMenu menu = sender as ContextMenu;
            if( menu != null ) {
                menu.MenuItems[0].Enabled = (logBox.SelectedLength > 0);
            }
        }
    }
}
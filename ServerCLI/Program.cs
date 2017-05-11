﻿/*
 *  Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 *
 */
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft.ServerCLI {
    static class Program {
        static bool useColor = true,
                    exitOnShutdown = true;


        static void Main( string[] args ) {
            // Check fCraft.dll version
            if( typeof( Server ).Assembly.GetName().Version != typeof( Program ).Assembly.GetName().Version ) {
                Console.Error.WriteLine( "fCraft.dll version does not match ServerCLI.exe version." );
                Environment.ExitCode = (int)ShutdownReason.FailedToInitialize;
                return;
            }

            Console.Title = "ProCraft " + Updater.CurrentRelease.VersionString + " - starting...";

            Logger.Logged += OnLogged;
            Heartbeat.UriChanged += OnHeartbeatUriChanged;
            Server.ShutdownEnded += OnShutdownEnded;

#if !DEBUG
            try {
#endif
                Server.InitLibrary( args );
                useColor = !Server.HasArg( ArgKey.NoConsoleColor );

                Server.InitServer();

                Console.Title = "ProCraft " + Updater.CurrentRelease.VersionString + " - " +
                                ConfigKey.ServerName.GetString();

                if( !ConfigKey.ProcessPriority.IsBlank() ) {
                    try {
                        Process.GetCurrentProcess().PriorityClass =
                            ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                    } catch( Exception ) {
                        Logger.Log( LogType.Warning, "Program.Main: Could not set process priority, using defaults." );
                    }
                }

                // dont hook up Ctrl+C handler until the server's about to start
                Console.CancelKeyPress += OnCancelKeyPress;

                if( Server.StartServer() ) {
                    Console.WriteLine( "** Running fCraft version {0}. **", Updater.CurrentRelease.VersionString );
                    Console.WriteLine( "** Server is now ready. Type /Shutdown to exit safely. **" );

                    while( !Server.IsShuttingDown ) {
                        string cmd = Console.ReadLine();
                        if( cmd == null ) {
                            Console.WriteLine(
                                "*** Received EOF from console. You will not be able to type anything in console any longer. ***" );
                            break;
                        }
                        if( cmd.Equals( "/Clear", StringComparison.OrdinalIgnoreCase ) ) {
                            Console.Clear();
                        } else {
#if !DEBUG
                            try {
                                Player.Console.ParseMessage( cmd, true );
                            } catch( Exception ex ) {
                                Logger.LogAndReportCrash( "Error while executing a command from console",
                                                          "ServerCLI",
                                                          ex, false );
                            }
#else
                            Player.Console.ParseMessage( cmd, true );
#endif
                        }
                    }

                } else {
                    ReportFailure( ShutdownReason.FailedToStart );
                }
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in ServerCLI", "ServerCLI", ex, true );
                ReportFailure( ShutdownReason.Crashed );
            } finally {
                Console.ResetColor();
            }
#endif
        }


        static void OnShutdownEnded( object sender, ShutdownEventArgs e ) {
            if( exitOnShutdown ) {
                Environment.Exit( Environment.ExitCode );
            }
        }


        static void OnCancelKeyPress( object sender, ConsoleCancelEventArgs e ) {
            switch( e.SpecialKey ) {
                case ConsoleSpecialKey.ControlBreak:
                    Console.WriteLine( "*** Shutting down (Ctrl-Break) ***" );
                    Thread t = new Thread( OnControlShutdown );
                    t.Start();
                    t.Join();
                    break;
                case ConsoleSpecialKey.ControlC:
                    e.Cancel = true;
                    Console.WriteLine( "*** Shutting down (Ctrl-C) ***" );
                    new Thread( OnControlShutdown ).Start();
                    break;
            }
        }


        static void OnControlShutdown() {
            Server.Shutdown( new ShutdownParams( ShutdownReason.ProcessClosing, TimeSpan.Zero, false ), true );
            Environment.Exit( (int)ShutdownReason.ProcessClosing );
        }


        static void ReportFailure( ShutdownReason reason ) {
            Console.Title = String.Format( "ProCraft {0} {1}", Updater.CurrentRelease.VersionString, reason );
            if( useColor ) Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine( "** {0} **", reason );
            if( useColor ) Console.ResetColor();

            exitOnShutdown = Server.HasArg( ArgKey.ExitOnCrash );
            Server.Shutdown( new ShutdownParams( reason, TimeSpan.Zero, false ), false );
            if( !exitOnShutdown ) {
                Console.ReadLine();
            }
        }


        [DebuggerStepThrough]
        static void OnLogged( object sender, LogEventArgs e ) {
            if( !e.WriteToConsole ) return;
            string text = Color.StripColors( e.Message, false );
            switch( e.MessageType ) {
                case LogType.Error:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( text );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.SeriousError:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.White;
                    if( useColor ) Console.BackgroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( text );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.Warning:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine( text );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.Debug:
                case LogType.Trace:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine( text );
                    if( useColor ) Console.ResetColor();
                    return;

                default:
                    Console.WriteLine( text );
                    return;
            }
        }


        static void OnHeartbeatUriChanged([CanBeNull] object sender, [NotNull] UrlChangedEventArgs e)
        {
            File.WriteAllText( "externalurl.txt", e.NewUrl.ToString(), Encoding.ASCII );
            Console.WriteLine( "** URL: {0} **", e.NewUrl );
            Console.WriteLine( "URL is also saved to file externalurl.txt" );
        }


        #region Updates

        static readonly AutoResetEvent UpdateDownloadWaiter = new AutoResetEvent( false );

        static readonly object ProgressReportLock = new object();


        static void OnUpdateDownloadProgress( object sender, DownloadProgressChangedEventArgs e ) {
            lock( ProgressReportLock ) {
                Console.CursorLeft = 0;
                int maxProgress = Console.WindowWidth - 9;
                int progress = (int)Math.Round( ( e.ProgressPercentage / 100f ) * ( maxProgress - 1 ) );
                Console.Write( "{0,3}% |", e.ProgressPercentage );
                Console.Write( new String( '=', progress ) );
                Console.Write( '>' );
                Console.Write( new String( ' ', maxProgress - progress ) );
                Console.Write( '|' );
            }
        }


        static void OnUpdateDownloadCompleted( object sender, AsyncCompletedEventArgs e ) {
            Console.WriteLine();
            if( e.Error != null ) {
                if( useColor ) Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( "Downloading the updater failed: {0}", e.Error );
                if( useColor ) Console.ResetColor();
            } else {
                Console.WriteLine( "Update download finished." );
            }
            UpdateDownloadWaiter.Set();
        }


        static void RestartForUpdate() {
            string restartArgs = String.Format( "{0} --restart=\"{1}\"",
                                                Server.GetArgString(),
                                                MonoCompat.PrependMono( "ServerCLI.exe" ) );
            MonoCompat.StartDotNetProcess( Paths.UpdaterFileName, restartArgs, true );
        }

        #endregion
    }
}
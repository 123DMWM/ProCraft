﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using fCraft.Events;
using JetBrains.Annotations;
#if DEBUG_EVENTS
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace fCraft {
    /// <summary> Central logging class. Logs to file, relays messages to the frontend, submits crash reports. </summary>
    public static class Logger {
        /// <summary> Gets or sets whether logging is globally enabled/disabled.
        /// If "--nolog" command-line argument is given, logging is disabled. </summary>
        public static bool Enabled { get; set; }

        public static readonly bool[] ConsoleOptions;
        public static readonly bool[] LogFileOptions;

        static readonly object LogLock = new object();
        const string DefaultLogFileName = "ProCraft.log",
                     LongDateFormat = "yyyy'-'MM'-'dd'_'HH'-'mm'-'ss",
                     ShortDateFormat = "yyyy'-'MM'-'dd",
                     TimeFormat = "HH':'mm':'ss";

        static readonly string SessionStart = DateTime.Now.ToString( LongDateFormat ); // localized
        static readonly Queue<string> RecentMessages = new Queue<string>();
        static DateTimeFormatInfo formatter = CultureInfo.InvariantCulture.DateTimeFormat;
        const int MaxRecentMessages = 25;

        /// <summary> Name of the file that log messages are currently being written to.
        /// Does not include path to the log folder (see Paths.LogPath for that). </summary>
        public static string CurrentLogFileName {
            get {
                switch( SplittingType ) {
                    case LogSplittingType.SplitBySession:
                        return SessionStart + ".log";
                    case LogSplittingType.SplitByDay:
                        return DateTime.Now.ToString( ShortDateFormat, formatter ) + ".log"; // localized
                    default:
                        return DefaultLogFileName;
                }
            }
        }


        public static LogSplittingType SplittingType { get; set; }


        static Logger() {
            // initialize defaults
            SplittingType = LogSplittingType.OneFile;
            Enabled = true;
            int typeCount = Enum.GetNames( typeof( LogType ) ).Length;
            ConsoleOptions = new bool[typeCount];
            LogFileOptions = new bool[typeCount];
            for( int i = 0; i < typeCount; i++ ) {
                ConsoleOptions[i] = true;
                LogFileOptions[i] = true;
            }
        }


        internal static void MarkLogStart() {
            // Mark start of logging
            Log( LogType.SystemActivity, "------ Log Starts {0} ({1}) ------",
                 DateTime.Now.ToLongDateString(), DateTime.Now.ToShortDateString() ); // localized
        }


        static string[] split = new string[] { "&N" };
        /// <summary> Logs a message of type ConsoleOutput, strips colors,
        /// and splits into multiple messages at newlines.
        /// Use this method for all messages of LogType.ConsoleOutput </summary>
        public static void LogToConsole( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( message.Contains( "&N" ) ) {
                foreach( string line in message.Split( split, StringSplitOptions.RemoveEmptyEntries ) ) {
                    LogToConsole( line );
                }
                return;
            }

            message = "# " + "&S" + message;
            Log( LogType.ConsoleOutput, message );
        }


        /// <summary> Adds a message to the server log.
        /// Depending on server configuration and log category, message can be shown in console, logged to file, both, or neither. </summary>
        /// <param name="type"> Type of message. </param>
        /// <param name="message"> Format string for the message. Uses same syntax as String.Format. </param>
        /// <param name="args"> An System.Object array containing zero or more objects to format. </param>
        /// <exception cref="ArgumentNullException"> Message or args is null. </exception>
        /// <exception cref="FormatException"> String.Format rejected formatting. </exception>
        [DebuggerStepThrough]
        [StringFormatMethod( "message" )]
        public static void Log( LogType type, [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( args.Length > 0 ) {
                try
                {
                    message = String.Format(message, args);
                }
                catch (Exception e) {
                    message = e.StackTrace + "\n" + message;
                }
            }
            if( !Enabled ) return;
            
            message = Color.StripColors( message, true );
            string line = DateTime.Now.ToString( TimeFormat, formatter ) + " > " + GetPrefix( type ) + message; // localized

            lock( LogLock ) {
                RaiseLoggedEvent( message, line, type );

                RecentMessages.Enqueue( line );
                while( RecentMessages.Count > MaxRecentMessages ) {
                    RecentMessages.Dequeue();
                }

                if( LogFileOptions[(int)type] ) {
                    try {
                        File.AppendAllText( Path.Combine( Paths.LogPath, CurrentLogFileName ),
                                            line + Environment.NewLine );
                    } catch( Exception ex ) {
                        string errorMessage = "Logger.Log: " + ex;
                        line = String.Format( "{0} > {1}{2}",
                                              DateTime.Now.ToString( TimeFormat, formatter ),// localized
                                              GetPrefix( LogType.Error ),
                                              errorMessage );
                        RaiseLoggedEvent( errorMessage,
                                          line, 
                                          LogType.Error );
                    }
                }
            }
        }


        [DebuggerStepThrough]
        static string GetPrefix( LogType level ) {
            switch( level ) {
                case LogType.SeriousError:
                case LogType.Error:
                    return "&cERROR: ";
                case LogType.Warning:
                    return "&SWarning: ";
                case LogType.IrcStatus:
                    return "&IIRC: ";
                default:
                    return "";
            }
        }


        #region Crash Handling


        /// <summary> Logs and reports a crash or an unhandled exception.
        /// Details are logged, and a crash report may be submitted to fCraft.net.
        /// Note that this method may take several seconds to finish,
        /// since it gathers system information and possibly communicates to fCraft.net. </summary>
        /// <param name="message"> Description/context of the crash. May be null if unknown. </param>
        /// <param name="assembly"> Assembly or component where the crash/exception was caught. May be null if unknown. </param>
        /// <param name="exception"> Exception. May be null. </param>
        /// <param name="shutdownImminent"> Whether this crash will likely report in a server shutdown.
        /// Used for Logger.Crashed event. </param>
        public static void LogAndReportCrash( [CanBeNull] string message, [CanBeNull] string assembly,
                                              [CanBeNull] Exception exception, bool shutdownImminent ) {
            if( message == null ) message = "(none)";
            if( assembly == null ) assembly = "(none)";
            if( exception == null ) exception = new Exception( "(none)" );

            Log( LogType.SeriousError, "{0}: {1}", message, exception );
            bool isCommon = CheckForCommonErrors( exception );

            try {
                var eventArgs = new CrashedEventArgs( message,
                                                      assembly,
                                                      exception,
                                                      !isCommon,
                                                      isCommon,
                                                      shutdownImminent );
                RaiseCrashedEvent( eventArgs );
                isCommon = eventArgs.IsCommonProblem;
            } catch { }

            if( isCommon ) {
                return;
            }
        }


        // Called by the Logger in case of serious errors to print troubleshooting advice.
        // Returns true if this type of error is common, and crash report should NOT be submitted.
        static bool CheckForCommonErrors( [CanBeNull] Exception ex ) {
            if( ex == null ) throw new ArgumentNullException( "ex" );
            string message = null;
            try {
                if( ex is FileNotFoundException && ex.Message.Contains( "Version=3.5" ) ) {
                    message = "Your crash was likely caused by using a wrong version of .NET or Mono runtime. " +
                              "Please update to Microsoft .NET Framework 3.5 (Windows) OR Mono 2.6.4+ (Linux, Unix, Mac OS X).";
                    return true;

                } else if( ex.Message.Contains( "libMonoPosixHelper" ) ||
                           ex is EntryPointNotFoundException && ex.Message.Contains( "CreateZStream" ) ) {
                    message = "ProCraft could not locate Mono's compression functionality. " +
                              "Please make sure that you have zlib (sometimes called \"libz\" or just \"z\") installed. " +
                              "Some versions of Mono may also require \"libmono-posix-2.0-cil\" package to be installed.";
                    return true;

                } else if( ex is MissingMemberException || ex is TypeLoadException ) {
                    message = "Something is incompatible with the current revision of fCraft. " +
                              "If you installed third-party modifications, " +
                              "make sure to use the correct revision (as specified by mod developers). " +
                              "If your own modifications stopped working, your may need to make some updates.";
                    return true;

                } else if( ex is UnauthorizedAccessException ) {
                    message = "ProCraft was blocked from accessing a file or resource. " +
                              "Make sure that correct permissions are set for the ProCraft files, folders, and processes.";
                    return true;

                } else if( ex is OutOfMemoryException ) {
                    message = "ProCraft ran out of memory. Make sure there is enough RAM to run.";
                    return true;

                } else if( ex is SystemException && ex.Message == "Can't find current process" ) {
                    // Ignore Mono-specific bug in MonitorProcessorUsage()
                    return true;

                } else if( ex is InvalidOperationException && ex.StackTrace.Contains( "MD5CryptoServiceProvider" ) ) {
                    message = "Some Windows settings are preventing ProCraft from doing player name verification. " +
                              "See http://support.microsoft.com/kb/811833";
                    return true;

                } else if( ex.StackTrace.Contains( "__Error.WinIOError" ) ) {
                    message = "A filesystem-related error has occurred. Make sure that only one instance of ProCraft is running, " +
                              "and that no other processes are using server's files or directories.";
                    return true;

                } else if( ex.Message.Contains( "UNSTABLE" ) ) {
                    return true;

                } else {
                    return false;
                }
            } finally {
                if( message != null ) {
                    Log( LogType.Warning, message );
                }
            }
        }

        #endregion


        #region Event Tracing
#if DEBUG_EVENTS

        // list of events in this assembly
        static readonly Dictionary<int, EventInfo> eventsMap = new Dictionary<int, EventInfo>();


        static List<string> eventWhitelist = new List<string>();
        static List<string> eventBlacklist = new List<string>();
        const string TraceWhitelistFile = "traceonly.txt",
                     TraceBlacklistFile = "notrace.txt";
        static bool useEventWhitelist, useEventBlacklist;

        static void LoadTracingSettings() {
            if( File.Exists( TraceWhitelistFile ) ) {
                useEventWhitelist = true;
                eventWhitelist.AddRange( File.ReadAllLines( TraceWhitelistFile ) );
            } else if( File.Exists( TraceBlacklistFile ) ) {
                useEventBlacklist = true;
                eventBlacklist.AddRange( File.ReadAllLines( TraceBlacklistFile ) );
            }
        }


        // adds hooks to all compliant events in current assembly
        internal static void PrepareEventTracing() {

            LoadTracingSettings();

            // create a dynamic type to hold our handler methods
            AppDomain myDomain = AppDomain.CurrentDomain;
            var asmName = new AssemblyName( "fCraftEventTracing" );
            AssemblyBuilder myAsmBuilder = myDomain.DefineDynamicAssembly( asmName, AssemblyBuilderAccess.RunAndSave );
            ModuleBuilder myModule = myAsmBuilder.DefineDynamicModule( "DynamicHandlersModule" );
            TypeBuilder typeBuilder = myModule.DefineType( "EventHandlersContainer", TypeAttributes.Public );

            int eventIndex = 0;
            Assembly asm = Assembly.GetExecutingAssembly();
            List<EventInfo> eventList = new List<EventInfo>();

            // find all events in current assembly, and create a handler for each one
            foreach( Type type in asm.GetTypes() ) {
                foreach( EventInfo eventInfo in type.GetEvents() ) {
                    // Skip non-static events
                    if( (eventInfo.GetAddMethod().Attributes & MethodAttributes.Static) != MethodAttributes.Static ) {
                        continue;
                    }
                    if( eventInfo.EventHandlerType.FullName.StartsWith( typeof( EventHandler<> ).FullName ) ||
                        eventInfo.EventHandlerType.FullName.StartsWith( typeof( EventHandler ).FullName ) ) {

                        if( useEventWhitelist && !eventWhitelist.Contains( type.Name + "." + eventInfo.Name, StringComparer.OrdinalIgnoreCase ) ||
                            useEventBlacklist && eventBlacklist.Contains( type.Name + "." + eventInfo.Name, StringComparer.OrdinalIgnoreCase ) ) continue;

                        MethodInfo method = eventInfo.EventHandlerType.GetMethod( "Invoke" );
                        var parameterTypes = method.GetParameters().Select( info => info.ParameterType ).ToArray();
                        AddEventHook( typeBuilder, parameterTypes, method.ReturnType, eventIndex );
                        eventList.Add( eventInfo );
                        eventsMap.Add( eventIndex, eventInfo );
                        eventIndex++;
                    }
                }
            }

            // hook up the handlers
            Type handlerType = typeBuilder.CreateType();
            for( int i = 0; i < eventList.Count; i++ ) {
                MethodInfo notifier = handlerType.GetMethod( "EventHook" + i );
                var handlerDelegate = Delegate.CreateDelegate( eventList[i].EventHandlerType, notifier );
                try {
                    eventList[i].AddEventHandler( null, handlerDelegate );
                } catch( TargetException ) {
                    // There's no way to tell if an event is static until you
                    // try adding a handler with target=null.
                    // If it wasn't static, TargetException is thrown
                }
            }
        }


        // create a static handler method that matches the given signature, and calls EventTraceNotifier
        static void AddEventHook( TypeBuilder typeBuilder, Type[] methodParams, Type returnType, int eventIndex ) {
            string methodName = "EventHook" + eventIndex;
            MethodBuilder methodBuilder = typeBuilder.DefineMethod( methodName,
                                                                    MethodAttributes.Public | MethodAttributes.Static,
                                                                    returnType,
                                                                    methodParams );

            ILGenerator generator = methodBuilder.GetILGenerator();
            generator.Emit( OpCodes.Ldc_I4, eventIndex );
            generator.Emit( OpCodes.Ldarg_1 );
            MethodInfo min = typeof( Logger ).GetMethod( "EventTraceNotifier" );
            generator.EmitCall( OpCodes.Call, min, null );
            generator.Emit( OpCodes.Ret );
        }


        // Invoked when events fire
        public static void EventTraceNotifier( int eventIndex, EventArgs e ) {
            if( (e is LogEventArgs) && ((LogEventArgs)e).MessageType == LogType.Trace ) return;
            var eventInfo = eventsMap[eventIndex];

            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( var prop in e.GetType().GetProperties() ) {
                if( !first ) sb.Append( ", " );
                if( prop.Name != prop.PropertyType.Name ) {
                    sb.Append( prop.Name ).Append( '=' );
                }
                object val = prop.GetValue( e, null );
                if( val == null ) {
                    sb.Append( "null" );
                } else if( val is string ) {
                    sb.AppendFormat( "\"{0}\"", val );
                } else {
                    sb.Append( val );
                }
                first = false;
            }

            Log( LogType.Trace,
                 "TraceEvent: {0}.{1}( {2} )",
                 eventInfo.DeclaringType.Name, eventInfo.Name, sb.ToString() );

        }

#endif
        #endregion


        #region Events

        /// <summary> Occurs after a message has been logged. </summary>
        public static event EventHandler<LogEventArgs> Logged;


        /// <summary> Occurs when the server "crashes" (has an unhandled exception).
        /// Note that such occurences will not always cause shutdowns - check ShutdownImminent property.
        /// Reporting of the crash may be suppressed. </summary>
        public static event EventHandler<CrashedEventArgs> Crashed;


        [DebuggerStepThrough]
        static void RaiseLoggedEvent( [NotNull] string rawMessage, [NotNull] string line, LogType logType ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            if( line == null ) throw new ArgumentNullException( "line" );
            var h = Logged;
            if( h != null ) h( null, new LogEventArgs( rawMessage,
                                                       line,
                                                       logType,
                                                       LogFileOptions[(int)logType],
                                                       ConsoleOptions[(int)logType] ) );
        }


        static void RaiseCrashedEvent( CrashedEventArgs e ) {
            var h = Crashed;
            if( h != null ) h( null, e );
        }

        #endregion
    }


    #region Enums

    /// <summary> Category of a log event. </summary>
    public enum LogType {
        /// <summary> System activity (loading/saving of data, shutdown and startup events, etc). </summary>
        SystemActivity,

        /// <summary> Warnings (missing files, config discrepancies, minor recoverable errors, etc). </summary>
        Warning,

        /// <summary> Recoverable errors (loading/saving problems, connection problems, etc). </summary>
        Error,

        /// <summary> Critical non-recoverable errors and crashes. </summary>
        SeriousError,

        /// <summary> Routine user activity (command results, kicks, bans, etc). </summary>
        UserActivity,

        /// <summary> Raw commands entered by the player. </summary>
        UserCommand,

        /// <summary> Permission and hack related activity (name verification failures, banned players logging in, detected hacks, etc). </summary>
        SuspiciousActivity,

        /// <summary> Normal (white) chat written by the players. </summary>
        GlobalChat,

        /// <summary> Private messages exchanged by players. </summary>
        PrivateChat,

        /// <summary> Rank chat messages. </summary>
        RankChat,

        /// <summary> Messages and commands entered from console. </summary>
        ConsoleInput,

        /// <summary> Messages printed to the console (typically as the result of commands called from console). </summary>
        ConsoleOutput,

        /// <summary> Information related to IRC activity. </summary>
        IrcStatus,

        /// <summary> IRC chatter and join/part messages. </summary>
        IrcChat,

        /// <summary> Information useful for debugging (error details, routine events, system information). </summary>
        Debug,

        /// <summary> Special-purpose messages related to event tracing (never logged). </summary>
        Trace
    }


    /// <summary> Log splitting type. </summary>
    public enum LogSplittingType {
        /// <summary> All logs are written to one file. </summary>
        OneFile,

        /// <summary> A new timestamped logfile is made every time the server is started. </summary>
        SplitBySession,

        /// <summary> A new timestamped logfile is created every 24 hours. </summary>
        SplitByDay
    }

    #endregion
}


namespace fCraft.Events {
    /// <summary> Provides data for Logger.Logged event. Immutable. </summary>
    public sealed class LogEventArgs : EventArgs {
        [DebuggerStepThrough]
        internal LogEventArgs( string rawMessage, string message, LogType messageType, bool writeToFile, bool writeToConsole ) {
            RawMessage = rawMessage;
            Message = message;
            MessageType = messageType;
            WriteToFile = writeToFile;
            WriteToConsole = writeToConsole;
        }
        public string RawMessage { get; private set; }
        public string Message { get; private set; }
        public LogType MessageType { get; private set; }
        public bool WriteToFile { get; private set; }
        public bool WriteToConsole { get; private set; }
    }


    /// <summary> Provides for Logger.Crashed event. Crash reporting can be canceled. </summary>
    public sealed class CrashedEventArgs : EventArgs {
        internal CrashedEventArgs( string message, string location, Exception exception, bool submitCrashReport, bool isCommonProblem, bool shutdownImminent ) {
            Message = message;
            Location = location;
            Exception = exception;
            IsCommonProblem = isCommonProblem;
            ShutdownImminent = shutdownImminent;
        }
        public string Message { get; private set; }
        public string Location { get; private set; }
        public Exception Exception { get; private set; }
        public bool IsCommonProblem { get; private set; }
        public bool ShutdownImminent { get; private set; }
    }
}

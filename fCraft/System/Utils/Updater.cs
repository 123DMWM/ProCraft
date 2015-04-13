// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Checks for updates, and keeps track of current version/revision. </summary>
    public static class Updater {
        /// <summary> The current release information of this version/revision. </summary>
        public static readonly ReleaseInfo CurrentRelease = new ReleaseInfo(
            1,
            23,
            new DateTime( 2013, 8, 26, 11, 0, 0, DateTimeKind.Utc ),
            "",
            "",
            ReleaseFlags.Dev
#if DEBUG
            | ReleaseFlags.Dev
#endif
            );

        /// <summary> User-agent value used for HTTP requests (heartbeat, updater, external IP check, etc). 
        /// Defaults to "ProCraft + VersionString of the current release. </summary>
        [NotNull]
        public static string UserAgent { get; set; }

        /// <summary> The latest stable branch/version of fCraft. </summary>
        public const string LatestStable = "1.23";

        /// <summary> Url to update fCraft from. Use "{0}" as a placeholder for CurrentRelease.Version.Revision </summary>
        [NotNull]
        public static string UpdateUri { get; set; }
        
        /// <summary> Amount of time in milliseconds before the updater will consider the connection dead.
        /// Default: 4000ms </summary>
        public static TimeSpan UpdateCheckTimeout { get; set; }
              

        #region Events

        /// <summary> Occurs when fCraft is about to check for updates (cancelable).
        /// The update Url may be overridden. </summary>
        public static event EventHandler<CheckingForUpdatesEventArgs> CheckingForUpdates;


        /// <summary> Occurs when fCraft has just checked for updates. </summary>
        public static event EventHandler<CheckedForUpdatesEventArgs> CheckedForUpdates;


        static bool RaiseCheckingForUpdatesEvent( [NotNull] ref string updateUrl ) {
            var h = CheckingForUpdates;
            if( h == null ) return false;
            var e = new CheckingForUpdatesEventArgs( updateUrl );
            h( null, e );
            updateUrl = e.Url;
            return e.Cancel;
        }


        static void RaiseCheckedForUpdatesEvent( [NotNull] string url, [NotNull] UpdaterResult result ) {
            var h = CheckedForUpdates;
            if( h != null ) h( null, new CheckedForUpdatesEventArgs( url, result ) );
        }

        #endregion
    }


    /// <summary> Result of an update check. </summary>
    public sealed class UpdaterResult {
        [NotNull]
        internal static UpdaterResult NoUpdate {
            get { return new UpdaterResult( false, null, new ReleaseInfo[0] ); }
        }


        internal UpdaterResult( bool updateAvailable, [CanBeNull] Uri downloadUri, [NotNull] ReleaseInfo[] releases ) {
            UpdateAvailable = updateAvailable;
            DownloadUri = downloadUri;
            History = releases.OrderByDescending( r => r.Revision ).ToArray();
            LatestRelease = releases.FirstOrDefault();
        }


        public bool UpdateAvailable { get; private set; }
        public Uri DownloadUri { get; private set; }
        public ReleaseInfo[] History { get; private set; }
        public ReleaseInfo LatestRelease { get; private set; }
    }


    /// <summary> Used to describe a particular release version of fCraft. Includes date released, version </summary>
    public sealed class ReleaseInfo {
        internal ReleaseInfo( int version, int revision, DateTime releaseDate, [NotNull] string summary,
                              [NotNull] string changeLog, ReleaseFlags releaseType ) {
            Version = version;
            Revision = revision;
            Date = releaseDate;
            Summary = summary;
            ChangeLog = changeLog.Split( new[] {
                '\n'
            } );
            Flags = releaseType;
        }


        public ReleaseFlags Flags { get; private set; }

        public string[] FlagsList {
            get { return ReleaseFlagsToStringArray( Flags ); }
        }

        public int Version { get; private set; }

        public int Revision { get; private set; }

        public DateTime Date { get; private set; }

        public TimeSpan Age {
            get { return DateTime.UtcNow.Subtract( Date ); }
        }

        public string VersionString {
            get {
                string formatString = "{0}.{1}";
                return String.Format( CultureInfo.InvariantCulture,
                                      formatString,
                                      Decimal.Divide( Version, 1 ),
                                      Revision );
            }
        }

        public string Summary { get; private set; }

        public string[] ChangeLog { get; private set; }


        public static ReleaseFlags StringToReleaseFlags( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            ReleaseFlags flags = ReleaseFlags.None;
            for( int i = 0; i < str.Length; i++ ) {
                switch( Char.ToUpper( str[i] ) ) {
                    case 'A':
                        flags |= ReleaseFlags.APIChange;
                        break;
                    case 'B':
                        flags |= ReleaseFlags.Bugfix;
                        break;
                    case 'C':
                        flags |= ReleaseFlags.ConfigFormatChange;
                        break;
                    case 'D':
                        flags |= ReleaseFlags.Dev;
                        break;
                    case 'F':
                        flags |= ReleaseFlags.Feature;
                        break;
                    case 'M':
                        flags |= ReleaseFlags.MapFormatChange;
                        break;
                    case 'P':
                        flags |= ReleaseFlags.PlayerDBFormatChange;
                        break;
                    case 'S':
                        flags |= ReleaseFlags.Security;
                        break;
                    case 'U':
                        flags |= ReleaseFlags.Unstable;
                        break;
                    case 'O':
                        flags |= ReleaseFlags.Optimized;
                        break;
                }
            }
            return flags;
        }


        [NotNull]
        public static string[] ReleaseFlagsToStringArray( ReleaseFlags flags ) {
            List<string> list = new List<string>();
            if( (flags & ReleaseFlags.APIChange) == ReleaseFlags.APIChange ) list.Add( "API Changes" );
            if( (flags & ReleaseFlags.Bugfix) == ReleaseFlags.Bugfix ) list.Add( "Fixes" );
            if( (flags & ReleaseFlags.ConfigFormatChange) == ReleaseFlags.ConfigFormatChange ) list.Add( "Config Changes" );
            if( (flags & ReleaseFlags.Dev) == ReleaseFlags.Dev ) list.Add( "Developer" );
            if( (flags & ReleaseFlags.Feature) == ReleaseFlags.Feature ) list.Add( "New Features" );
            if( (flags & ReleaseFlags.MapFormatChange) == ReleaseFlags.MapFormatChange ) list.Add( "Map Format Changes" );
            if( (flags & ReleaseFlags.PlayerDBFormatChange) == ReleaseFlags.PlayerDBFormatChange ) list.Add( "PlayerDB Changes" );
            if( (flags & ReleaseFlags.Security) == ReleaseFlags.Security ) list.Add( "Security Patch" );
            if( (flags & ReleaseFlags.Unstable) == ReleaseFlags.Unstable ) list.Add( "Unstable" );
            if( (flags & ReleaseFlags.Optimized) == ReleaseFlags.Optimized ) list.Add( "Optimized" );
            return list.ToArray();
        }


        public bool IsFlagged( ReleaseFlags flag ) {
            return (Flags & flag) == flag;
        }
    }

    #region Enums

    /// <summary> Updater behavior. </summary>
    public enum UpdaterMode {
        /// <summary> Does not check for updates. </summary>
        Disabled,

        /// <summary> Checks for updates and notifies of availability (in console/log). </summary>
        Notify,

        /// <summary> Checks for updates, downloads them if available, and prompts to install.
        /// Behavior is frontend-specific: in ServerGUI, a dialog is shown with the list of changes and
        /// options to update immediately or next time. In ServerCLI, asks to type in 'y' to confirm updating
        /// or press any other key to skip. '''Note: Requires user interaction
        /// (if you restart the server remotely while unattended, it may get stuck on this dialog).''' </summary>
        Prompt,

        /// <summary> Checks for updates, automatically downloads and installs the updates, and restarts the server. </summary>
        Auto,
    }


    /// <summary> A list of release flags/attributes.
    /// Use binary flag logic (value &amp; flag == flag) or Release.IsFlagged() to test for flags. </summary>
    [Flags]
    public enum ReleaseFlags {
        None = 0,

        /// <summary> The API was notably changed in this release. </summary>
        APIChange = 1,

        /// <summary> Bugs were fixed in this release. </summary>
        Bugfix = 2,

        /// <summary> Config.xml format was changed (and version was incremented) in this release. </summary>
        ConfigFormatChange = 4,

        /// <summary> This is a developer-only release, not to be used on live servers.
        /// Untested/undertested releases are often marked as such. </summary>
        Dev = 8,

        /// <summary> A notable new feature was added in this release. </summary>
        Feature = 16,

        /// <summary> The map format was changed in this release (rare). </summary>
        MapFormatChange = 32,

        /// <summary> The PlayerDB format was changed in this release. </summary>
        PlayerDBFormatChange = 64,

        /// <summary> A security issue was addressed in this release. </summary>
        Security = 128,

        /// <summary> There are known or likely stability issues in this release. </summary>
        Unstable = 256,

        /// <summary> This release contains notable optimizations. </summary>
        Optimized = 512
    }

    #endregion
}

namespace fCraft.Events {
    /// <summary> Provides data for Updater.CheckingForUpdates event. Allows changing the URL. Cancelable. </summary>
    public sealed class CheckingForUpdatesEventArgs : EventArgs, ICancelableEvent {
        internal CheckingForUpdatesEventArgs( [NotNull] string url ) {
            if( url == null ) throw new ArgumentNullException( "url" );
            Url = url;
        }

        [NotNull]
        public string Url { get; set; }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Updater.CheckedForUpdates event. Immutable. </summary>
    public sealed class CheckedForUpdatesEventArgs : EventArgs {
        internal CheckedForUpdatesEventArgs( [NotNull] string url, [NotNull] UpdaterResult result ) {
            if( url == null ) throw new ArgumentNullException( "url" );
            if( result == null ) throw new ArgumentNullException( "result" );
            Url = url;
            Result = result;
        }

        [NotNull]
        public string Url { get; private set; }

        [NotNull]
        public UpdaterResult Result { get; private set; }
    }
}

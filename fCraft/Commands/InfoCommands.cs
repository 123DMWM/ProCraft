// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.Text;
using JetBrains.Annotations;
namespace fCraft {
    /// <summary> Contains commands that don't do anything besides displaying some information or text.
    /// Includes several chat commands. </summary>
    static class InfoCommands {
        const int PlayersPerPage = 30;
        internal static void Init() {
            CommandManager.RegisterCommand( CdInfo );
            CommandManager.RegisterCommand( CdWhoIs );
            CommandManager.RegisterCommand( CdBanInfo );
            CommandManager.RegisterCommand( CdRankInfo );
            CommandManager.RegisterCommand( CdServerInfo );
            CommandManager.RegisterCommand( CdRanks );
            CommandManager.RegisterCommand( CdListStaff );
            CommandManager.RegisterCommand( CdOnlineStaff);
            CommandManager.RegisterCommand( CdRules );
            CommandManager.RegisterCommand( CdMeasure );
            CommandManager.RegisterCommand( CdPlayers );
            CommandManager.RegisterCommand( CdPlayersAdvanced );
            CommandManager.RegisterCommand( CdWhere );
            CommandManager.RegisterCommand( CdHelp );
            CommandManager.RegisterCommand( CdCommands );
            CommandManager.RegisterCommand( CdColors );
            CommandManager.RegisterCommand( CdEmotes );
            CommandManager.RegisterCommand( CdBum );
            CommandManager.RegisterCommand( CdBDBDB );
            CommandManager.RegisterCommand( cdTaskDebug );
            CommandManager.RegisterCommand( CdMost );
            CommandManager.RegisterCommand( CdLRP );
            CommandManager.RegisterCommand( CdIPInfo );
            CommandManager.RegisterCommand( CdSeen );
            CommandManager.RegisterCommand( Cdclp );
            CommandManager.RegisterCommand( CdGeoip );
            CommandManager.RegisterCommand( CdGeoipNp );
            CommandManager.RegisterCommand( CdApi );
            CommandManager.RegisterCommand( CdPlugin );
            CommandManager.RegisterCommand( CdPingList );

        }
        #region Debug

        static readonly CommandDescriptor CdBum = new CommandDescriptor
        {
                Name = "BUM",
                IsHidden = true,
                Category = CommandCategory.New | CommandCategory.Info,
                Permissions = new[] { Permission.Chat },
                Help = "Bandwidth Use Mode statistics.",
                Handler = BumHandler
        };

        static void BumHandler(Player player, CommandReader cmd)
        {
            string newModeName = cmd.Next();
            if (newModeName == null)
            {
                player.Message("Bytes Sent: {0}  Per Second: {1:0.0}", player.BytesSent, player.BytesSentRate);
                player.Message("Bytes Received: {0}  Per Second: {1:0.0}", player.BytesReceived, player.BytesReceivedRate);
                player.Message("Bandwidth mode: {0}",player.BandwidthUseMode);
                                
                                
                                
                return;
            }
            else if (player.Can(Permission.EditPlayerDB))
            {
                var newMode = (BandwidthUseMode)Enum.Parse(typeof(BandwidthUseMode), newModeName, true);
                player.Message("Bandwidth mode: {0} --> {1}", player.BandwidthUseMode, newMode.ToString());
                player.BandwidthUseMode = newMode;
                player.Info.BandwidthUseMode = newMode;
                return;
            }
            else
            {
                player.Message("You need {0}&s to change your BandwidthUseMode", RankManager.GetMinRankWithAnyPermission(Permission.EditPlayerDB).ClassyName);
                return;
            }
            
        }

        static readonly CommandDescriptor CdBDBDB = new CommandDescriptor
        {
                Name = "BDBDB",
                IsHidden = true,
                Category = CommandCategory.New | CommandCategory.Info,
                Permissions = new[] { Permission.ViewOthersInfo },
                Help = "BlockDB Debug",
                Handler = BDBDBHandler
        };

        static void BDBDBHandler(Player player, CommandReader cmd)
        {
                    if( player.World == null ) PlayerOpException.ThrowNoWorld( player );
                    BlockDB db = player.World.BlockDB;
                    lock( db.SyncRoot ) {
                        player.Message( "BlockDB: CAP={0} SZ={1} FI={2}",
                                        db.CacheCapacity, db.CacheSize, db.LastFlushedIndex );
                    }
        }

        static CommandDescriptor cdTaskDebug = new CommandDescriptor
        {
            Name = "TaskDebug",
            Category = CommandCategory.New | CommandCategory.Info,
            Permissions = new[] { Permission.ShutdownServer },
            IsConsoleSafe = true,
            IsHidden = true,
            Handler = (player, cmd) => Scheduler.PrintTasks(player)
        };
                 
        #endregion
        #region Info

        const int MaxAltsToPrint = 15;
        static readonly Regex RegexNonNameChars = new Regex(@"[^a-zA-Z0-9_\*\?]", RegexOptions.Compiled);

        static readonly TimeSpan InfoIdleThreshold = TimeSpan.FromMinutes(1);

        static readonly CommandDescriptor CdInfo = new CommandDescriptor {
            Name = "Info",
            Aliases = new[] { "i", "whowis" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Info [PlayerName or IP [Offset]]",
            Help = "Prints information and stats for a given player. " +
                   "Prints your own stats if no name is given. " +
                   "Prints a list of names if a partial name or an IP is given. ",
            Handler = InfoHandler
        };

        static void InfoHandler( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            PlayerInfo info = FindPlayerInfo(player, cmd, true);
            if (info == null) return;
            Player target = info.PlayerObject;

            // hide online status when hidden
            if( target != null && !player.CanSee( target ) ) {
                target = null;
            }

            if( info.LastIP.Equals( IPAddress.None ) ) {
                player.Message( "About {0}&S: Never seen before.",
                                info.ClassyName);

            } else {
                StringBuilder firstLine = new StringBuilder();
                if( info.DisplayedName != null ) {
                    firstLine.AppendFormat("About {0}&S ({1}): ", info.ClassyName, info.Name );
                } else {
                    firstLine.AppendFormat("About {0}&S: ", info.ClassyName );
                }
                if( target != null ) {
                    if( info.IsHidden ) {
                        firstLine.AppendFormat( "HIDDEN" );
                    } else {
                        firstLine.AppendFormat( "Online now" );
                    }
                    if( target.IsDeaf ) {
                        firstLine.Append( " (deaf)" );
                    }                    
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        firstLine.AppendFormat( " from {0}", info.LastIP );
                    }
                    if( target.IdleTime > InfoIdleThreshold ) {
                        firstLine.AppendFormat( " (idle {0})", target.IdleTime.ToMiniString() );
                    }

                } else {
                    firstLine.AppendFormat( "Last seen {0} ago", info.TimeSinceLastSeen.ToMiniString());
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        firstLine.AppendFormat(" from &F{0}", info.LastIP );
                    }
                    if( info.LeaveReason != LeaveReason.Unknown ) {
                        firstLine.AppendFormat( " ({0})", info.LeaveReason );
                    }
                }
                player.Message( firstLine.ToString() );


                if (info.Email != null && (player == Player.Console || player.Info == info))
                {
                    // Show login information
                    player.Message("  <{0}> {1} logins since {2:d MMM yyyy}.",
                                    Color.StripColors(info.Email),
                                    info.TimesVisited,
                                    info.FirstLoginDate);
                }
                else
                {
                    // Show login information
                    player.Message("  {0} logins since {1:d MMM yyyy}.",
                                    info.TimesVisited,
                                    info.FirstLoginDate);
                }
                             
            }

            if( info.IsFrozen ) {
                player.Message("  Frozen {0} ago by {1}",
                                info.TimeSinceFrozen.ToMiniString(),
                                info.FrozenByClassy );
            }

            if (info.IsMuted)
            {
                player.Message( "  Muted for {0} by {1}",
                                info.TimeMutedLeft.ToMiniString(),
                                info.MutedByClassy );
            }

            // Show ban information
            IPBanInfo ipBan = IPBanList.Get( info.LastIP );
            switch( info.BanStatus ) {
                case BanStatus.Banned:
                    if( ipBan != null ) {
                        player.Message( "  Account and IP are &CBANNED" );
                    } else if( String.IsNullOrEmpty( info.BanReason ) ) {
                        player.Message( "  Account is &CBANNED" );
                    } else {
                        player.Message("  Account is &CBANNED&S ({0})", info.BanReason );
                    }
                    break;
                case BanStatus.IPBanExempt:
                    if( ipBan != null ) {
                        player.Message( "  IP is &CBANNED&S, but account is exempt." );
                    } else {
                        player.Message( "  IP is not banned, and account is exempt." );
                    }
                    break;
                case BanStatus.NotBanned:
                    if( ipBan != null ) {
                        if( String.IsNullOrEmpty( ipBan.BanReason ) ) {
                            player.Message( "  IP is &CBANNED" );
                        } else {
                            player.Message("  IP is &CBANNED&S ({0})", ipBan.BanReason );
                        }
                    }
                    break;
            }


            if( !info.LastIP.Equals( IPAddress.None ) ) {
                // Show alts
                List<PlayerInfo> altNames = new List<PlayerInfo>();
                int bannedAltCount = 0;
                foreach( PlayerInfo playerFromSameIP in PlayerDB.FindPlayers( info.LastIP ) ) {
                    if( playerFromSameIP == info ) continue;
                    altNames.Add( playerFromSameIP );
                    if( playerFromSameIP.IsBanned ) {
                        bannedAltCount++;
                    }
                }

                if( altNames.Count > 0 ) {
                    altNames.Sort( new PlayerInfoComparer( player ) );
                    if( altNames.Count > MaxAltsToPrint ) {
                        if( bannedAltCount > 0 ) {
                            player.Message("  Over {0} accounts ({1} banned) on IP: {2}  &Setc",
                                                    MaxAltsToPrint,
                                                    bannedAltCount,
                                                    altNames.Take( 15 ).ToArray().JoinToClassyString() );
                        } else {
                            player.Message("  Over {0} accounts on IP: {1} &Setc",
                                                    MaxAltsToPrint,
                                                    altNames.Take( 15 ).ToArray().JoinToClassyString() );
                        }
                    } else {
                        if( bannedAltCount > 0 ) {
                            player.Message("  {0} accounts ({1} banned) on IP: {2}",
                                                    altNames.Count,
                                                    bannedAltCount,
                                                    altNames.ToArray().JoinToClassyString() );
                        } else {
                            player.Message("  {0} accounts on IP: {1}",
                                                    altNames.Count,
                                                    altNames.ToArray().JoinToClassyString() );
                        }
                    }
                }
            }


            // Stats

            if (info.BlocksDrawn > 0)
            {
                player.Message("  Built {0} Deleted {1} Drew {2}",
                                Server.GetNumberString(info.BlocksBuilt),
                                Server.GetNumberString(info.BlocksDeleted),
                                Server.GetNumberString(info.BlocksDrawn));
            }
            else
            {
                player.Message("  Built {0} Deleted {1}",
                                Server.GetNumberString(info.BlocksBuilt),
                                Server.GetNumberString(info.BlocksDeleted));
            }
            float blocks = ((info.BlocksBuilt) - info.BlocksDeleted);
            player.Message("  Wrote {0} messages.", Server.GetNumberString(info.MessagesWritten));
            // More stats
            if (info.TimesBannedOthers > 0 || info.TimesKickedOthers > 0)
            {
                player.Message( "  Kicked {0}, banned {1}",
                                info.TimesKickedOthers,
                                info.TimesBannedOthers);
            }

            if( info.TimesKicked > 0 ) {
                if( info.LastKickDate != DateTime.MinValue ) {
                    player.Message("  Got kicked {0} times. Last kick {1} ago by {2}",
                                    info.TimesKicked,
                                    info.TimeSinceLastKick.ToMiniString(),
                                    info.LastKickByClassy );
                } else {
                    player.Message("  Got kicked {0} times.", info.TimesKicked );
                }
                if( info.LastKickReason != null ) {
                    player.Message("  Kick reason: {0}", info.LastKickReason );
                }
            }


            // Promotion/demotion
            if( info.PreviousRank == null ) {
                if( info.RankChangedBy == null ) {
                    player.Message("  Rank is {0}&S (default).",
                                    info.Rank.ClassyName );
                } else {
                    player.Message("  Promoted to {0}&S by {1}&S {2} ago.",
                                    info.Rank.ClassyName,
                                    info.RankChangedByClassy,
                                    info.TimeSinceRankChange.ToMiniString() );
                    if( info.RankChangeReason != null ) {
                        player.Message("  Promotion reason: {0}", info.RankChangeReason );
                    }
                }
            } else if( info.PreviousRank <= info.Rank ) {
                player.Message("  Promoted from {0}&S to {1}&S by {2}&S {3} ago.",
                                info.PreviousRank.ClassyName,
                                info.Rank.ClassyName,
                                info.RankChangedByClassy,
                                info.TimeSinceRankChange.ToMiniString() );
                if( info.RankChangeReason != null ) {
                    player.Message("  Promotion reason: {0}", info.RankChangeReason );
                }
            } else {
                player.Message("  Demoted from {0}&S to {1}&S by {2}&S {3} ago.",
                                info.PreviousRank.ClassyName,
                                info.Rank.ClassyName,
                                info.RankChangedByClassy,
                                info.TimeSinceRankChange.ToMiniString() );
                if( info.RankChangeReason != null ) {
                    player.Message("  Demotion reason: {0}", info.RankChangeReason );
                }
            }

            

            if (!info.LastIP.Equals(IPAddress.None))
            {
                // Time on the server
                TimeSpan totalTime = info.TotalTime;
                if (target != null)
                {
                    totalTime = totalTime.Add(info.TimeSinceLastLogin);
                }
                if (info.IsOnline && target != null)
                {
                    player.Message("  Total time: {0:F1} hours. This session: {1:F1} hours.",
                                    totalTime.TotalHours,
                                    target.Info.TimeSinceLastLogin.TotalHours);
                }
                else
                {
                    player.Message("  Total time: {0:F1} hours",
                                    totalTime.TotalHours);
                }
            }
        }

        #endregion
        #region BanInfo

        static readonly CommandDescriptor CdBanInfo = new CommandDescriptor {
            Name = "BanInfo",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/BanInfo [PlayerName|IPAddress]",
            Help = "Prints information about past and present bans/unbans associated with the PlayerName or IP. " +
                   "If no name is given, this prints your own ban info.",
            Handler = BanInfoHandler
        };

        static void BanInfoHandler( Player player, CommandReader cmd ) {
            string name = cmd.Next();
            if( cmd.HasNext ) {
                CdBanInfo.PrintUsage( player );
                return;
            }

            IPAddress address;
            PlayerInfo info = null;

            if( name == null ) {
                name = player.Name;
            } else if( !player.Can( Permission.ViewOthersInfo ) ) {
                player.MessageNoAccess( Permission.ViewOthersInfo );
                return;
            }

            if( IPAddressUtil.IsIP( name ) && IPAddress.TryParse( name, out address ) ) {
                IPBanInfo banInfo = IPBanList.Get( address );
                if( banInfo != null ) {
                    player.Message( "{0} was banned by {1}&S on {2:dd MMM yyyy} ({3} ago)",
                                    banInfo.Address,
                                    banInfo.BannedByClassy,
                                    banInfo.BanDate,
                                    banInfo.TimeSinceLastAttempt );
                    if( !String.IsNullOrEmpty( banInfo.PlayerName ) ) {
                        player.Message( "  Banned by association with {0}",
                                        banInfo.PlayerNameClassy );
                    }
                    if( banInfo.Attempts > 0 ) {
                        player.Message( "  There have been {0} attempts to log in, most recently {1} ago by {2}",
                                        banInfo.Attempts,
                                        banInfo.TimeSinceLastAttempt.ToMiniString(),
                                        banInfo.LastAttemptNameClassy );
                    }
                    if( banInfo.BanReason != null ) {
                        player.Message( "  Ban reason: {0}", banInfo.BanReason );
                    }
                } else {
                    player.Message( "{0} is currently NOT banned.", address );
                }

            } else {
                SearchOptions flags = SearchOptions.IncludeHidden | SearchOptions.IncludeSelf;
                info = PlayerDB.FindPlayerInfoOrPrintMatches( player, name, flags );
                if( info == null ) return;

                address = info.LastIP;

                IPBanInfo ipBan = IPBanList.Get( info.LastIP );
                switch( info.BanStatus ) {
                    case BanStatus.Banned:
                        if( ipBan != null ) {
                            player.Message( "Player {0}&S and their IP are &CBANNED", info.ClassyName );
                        } else {
                            player.Message( "Player {0}&S is &CBANNED&S (but their IP is not).", info.ClassyName );
                        }
                        break;
                    case BanStatus.IPBanExempt:
                        if( ipBan != null ) {
                            player.Message( "Player {0}&S is exempt from an existing IP ban.", info.ClassyName );
                        } else {
                            player.Message( "Player {0}&S is exempt from IP bans.", info.ClassyName );
                        }
                        break;
                    case BanStatus.NotBanned:
                        if( ipBan != null ) {
                            player.Message( "Player {0}&s is not banned, but their IP is.", info.ClassyName );
                        } else {
                            player.Message( "Player {0}&s is not banned.", info.ClassyName );
                        }
                        break;
                }

                if( info.BanDate != DateTime.MinValue ) {
                    player.Message( "  Last ban by {0}&S on {1:dd MMM yyyy} ({2} ago).",
                                    info.BannedByClassy,
                                    info.BanDate,
                                    info.TimeSinceBan.ToMiniString() );
                    if( info.BanReason != null ) {
                        player.Message( "  Last ban reason: {0}", info.BanReason );
                    }
                } else {
                    player.Message( "No past bans on record." );
                }

                if( info.UnbanDate != DateTime.MinValue && !info.IsBanned ) {
                    player.Message( "  Unbanned by {0}&S on {1:dd MMM yyyy} ({2} ago).",
                                    info.UnbannedByClassy,
                                    info.UnbanDate,
                                    info.TimeSinceUnban.ToMiniString() );
                    if( info.UnbanReason != null ) {
                        player.Message( "  Last unban reason: {0}", info.UnbanReason );
                    }
                }

                if( info.BanDate != DateTime.MinValue ) {
                    TimeSpan banDuration;
                    if( info.IsBanned ) {
                        banDuration = info.TimeSinceBan;
                        player.Message( "  Ban duration: {0} so far",
                                        banDuration.ToMiniString() );
                    } else {
                        banDuration = info.UnbanDate.Subtract( info.BanDate );
                        player.Message( "  Previous ban's duration: {0}",
                                        banDuration.ToMiniString() );
                    }
                }
            }

            // Show alts
            if( !address.Equals( IPAddress.None ) ) {
                List<PlayerInfo> altNames = new List<PlayerInfo>();
                int bannedAltCount = 0;
                foreach( PlayerInfo playerFromSameIP in PlayerDB.FindPlayers( address ) ) {
                    if( playerFromSameIP == info ) continue;
                    altNames.Add( playerFromSameIP );
                    if( playerFromSameIP.IsBanned ) {
                        bannedAltCount++;
                    }
                }

                if( altNames.Count > 0 ) {
                    altNames.Sort( new PlayerInfoComparer( player ) );
                    if( altNames.Count > MaxAltsToPrint ) {
                        if( bannedAltCount > 0 ) {
                            player.Message("  Over {0} accounts ({1} banned) on IP: {2} &Setc",
                                                    MaxAltsToPrint,
                                                    bannedAltCount,
                                                    altNames.Take( 15 ).ToArray().JoinToClassyString() );
                        } else {
                            player.Message("  Over {0} accounts on IP: {1} &Setc",
                                                    MaxAltsToPrint,
                                                    altNames.Take( 15 ).ToArray().JoinToClassyString() );
                        }
                    } else {
                        if( bannedAltCount > 0 ) {
                            player.Message("  {0} accounts ({1} banned) on IP: {2}",
                                                    altNames.Count,
                                                    bannedAltCount,
                                                    altNames.ToArray().JoinToClassyString() );
                        } else {
                            player.Message("  {0} accounts on IP: {1}",
                                                    altNames.Count,
                                                    altNames.ToArray().JoinToClassyString() );
                        }
                    }
                }
            }
        }

        #endregion
        #region RankInfo

        static readonly CommandDescriptor CdRankInfo = new CommandDescriptor {
            Name = "RankInfo",
            Aliases = new[] { "rinfo" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/RankInfo RankName",
            Help = "Shows a list of permissions granted to a rank. To see a list of all ranks, use &H/Ranks",
            Handler = RankInfoHandler
        };

        // Shows general information about a particular rank.
        static void RankInfoHandler( Player player, CommandReader cmd ) {
            Rank rank;

            string rankName = cmd.Next();
            if( cmd.HasNext ) {
                CdRankInfo.PrintUsage( player );
                return;
            }

            if( rankName == null ) {
                rank = player.Info.Rank;
            } else {
                rank = RankManager.FindRank( rankName );
                if( rank == null ) {
                    player.MessageNoRank( rankName );
                    return;
                }
            }

            List<Permission> permissions = new List<Permission>();
            for( int i = 0; i < rank.Permissions.Length; i++ ) {
                if( rank.Permissions[i] ) {
                    permissions.Add( (Permission)i );
                }
            }

            Permission[] sortedPermissionNames =
                permissions.OrderBy( s => s.ToString(), StringComparer.OrdinalIgnoreCase ).ToArray();
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat( "Players of rank {0}&S can: ", rank.ClassyName );
                bool first = true;
                for( int i = 0; i < sortedPermissionNames.Length; i++ ) {
                    Permission p = sortedPermissionNames[i];
                    if( !first ) sb.Append( ',' ).Append( ' ' );
                    Rank permissionLimit = rank.PermissionLimits[(int)p];
                    sb.Append( p );
                    if( permissionLimit != null ) {
                        sb.AppendFormat( "({0}&S)", permissionLimit.ClassyName );
                    }
                    first = false;
                }
                player.Message( sb.ToString() );
            }

            if( rank.Can( Permission.Draw ) ) {
                StringBuilder sb = new StringBuilder();
                if( rank.DrawLimit > 0 ) {
                    sb.AppendFormat( "Draw limit: {0} blocks.", rank.DrawLimit );
                } else {
                    sb.AppendFormat( "Draw limit: None (unlimited)." );
                }
                if( rank.Can( Permission.CopyAndPaste ) ) {
                    sb.AppendFormat( " Copy/paste slots: {0}", rank.CopySlots );
                }
                player.Message( sb.ToString() );
            }

            if( rank.IdleKickTimer > 0 ) {
                player.Message( "Idle kick after {0}", TimeSpan.FromMinutes( rank.IdleKickTimer ).ToMiniString() );
            }
            if (Directory.Exists(Paths.RankReqDirectory) && player.IsStaff) {
                string rankReqFileName = null;
                string[] sectionFiles = Directory.GetFiles(Paths.RankReqPath,rank.Name.ToLower() + ".txt", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < sectionFiles.Length; i++) {
                    string sectionFullName = Path.GetFileNameWithoutExtension(sectionFiles[i]).ToLower();
                    if (rank.Name.ToLower().Equals(sectionFullName)) {
                        rankReqFileName = sectionFiles[i];
                        break;
                    }
                }
                if (rankReqFileName != null) {
                    string sectionFullName = Path.GetFileNameWithoutExtension(rankReqFileName);
                    FileInfo rankReqFile = new FileInfo(rankReqFileName);
                    try {
                        string[] ruleLines = File.ReadAllLines(rankReqFile.FullName);
                        player.Message("&RRank requirements:");
                        foreach (string ruleLine in ruleLines) {
                            if (ruleLine.Trim().Length > 0) {
                                player.Message("&R{0}", Chat.ReplaceTextKeywords(player, ruleLine));
                            }
                        }
                    } catch (Exception ex) {
                        Logger.Log(LogType.Error,
                                    "InfoCommands.PrintRankReq: An error occurred while trying to read {0}: {1}",
                                    rankReqFile.FullName, ex);
                        player.Message("&WError reading the rank requirement file.");
                    }
                }
            }
        }

        #endregion
        #region ServerInfo

        static readonly CommandDescriptor CdServerInfo = new CommandDescriptor {
            Name = "ServerInfo",
            Aliases = new[] { "ServerReport", "Version", "SInfo" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Help = "Shows server stats",
            Handler = ServerInfoHandler
        };

        static void ServerInfoHandler( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                CdServerInfo.PrintUsage( player );
                return;
            }
            Process.GetCurrentProcess().Refresh();

            player.Message( ConfigKey.ServerName.GetString() );
            player.Message( "Servers status: Up for {0:0.0} hours, using {1:0} MB",
                            DateTime.UtcNow.Subtract( Server.StartTime ).TotalHours,
                            (Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024)) );

            if( Server.IsMonitoringCPUUsage ) {
                player.Message( "  Averaging {0:0.0}% CPU now, {1:0.0}% overall",
                                Server.CPUUsageLastMinute * 100,
                                Server.CPUUsageTotal * 100 );
            }

            if( MonoCompat.IsMono ) {
                player.Message( "  Running ProCraft 1.23, under Mono {0}",
                                MonoCompat.MonoVersionString );
            } else {
                player.Message( "  Running ProCraft 1.23, under .NET {0}",
                                Environment.Version );
            }

            double bytesReceivedRate = Server.Players.Aggregate( 0d, ( i, p ) => i + p.BytesReceivedRate );
            double bytesSentRate = Server.Players.Aggregate( 0d, ( i, p ) => i + p.BytesSentRate );
            player.Message( "  Bandwidth: {0:0.0} KB/s up, {1:0.0} KB/s down",
                            bytesSentRate / 1000, bytesReceivedRate / 1000 );

            player.Message( "  Tracking {0:N0} players ({1} online, {2} banned ({3:0.0}%), {4} IP-banned).",
                            PlayerDB.PlayerInfoList.Length,
                            Server.CountVisiblePlayers( player ),
                            PlayerDB.BannedCount,
                            PlayerDB.BannedPercentage,
                            IPBanList.Count );

            player.Message("  Players built {0:N0}; deleted {1:N0}; drew {2:N0} blocks; wrote {3:N0} messages; issued {4:N0} kicks; spent {5:N0} hours total (Average: {6:N0} hours); joined {7:N0} times (Average: {8:N0} times)",
                            PlayerDB.PlayerInfoList.Sum( p => p.BlocksBuilt ),
                            PlayerDB.PlayerInfoList.Sum( p => p.BlocksDeleted ),
                            PlayerDB.PlayerInfoList.Sum( p => p.BlocksDrawn ),
                            PlayerDB.PlayerInfoList.Sum( p => p.MessagesWritten ),
                            PlayerDB.PlayerInfoList.Sum( p => p.TimesKickedOthers ),
                            PlayerDB.PlayerInfoList.Sum(p => p.TotalTime.TotalHours),
                            PlayerDB.PlayerInfoList.Average(p => p.TotalTime.TotalHours),
                            PlayerDB.PlayerInfoList.Sum(p => p.TimesVisited),
                            PlayerDB.PlayerInfoList.Average(p => p.TimesVisited));

            player.Message( "  There are {0} worlds available ({1} loaded, {2} hidden).",
                            WorldManager.Worlds.Length,
                            WorldManager.CountLoadedWorlds( player ),
                            WorldManager.Worlds.Count( w => w.IsHidden ) );
        }

        #endregion
        #region Ranks

        static readonly CommandDescriptor CdRanks = new CommandDescriptor
        {
            Name = "Ranks",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Help = "Shows a list of all defined ranks.",
            Handler = RanksHandler
        };

        private static void RanksHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            player.Message("Below is a list of ranks. For detail see &H{0}", CdRankInfo.Usage);
            foreach (Rank rank in RankManager.Ranks) {
                player.Message("    {0}  &s(&f{1}&s)", rank.ClassyName, rank.PlayerCount);
            }
        }

        #endregion
        #region WhoIs
        static readonly CommandDescriptor CdWhoIs = new CommandDescriptor
        {
            Name = "WhoIs",
            Aliases = new[] { "realname" },
            Category = CommandCategory.New | CommandCategory.Info,
            Permissions = new[] { Permission.ViewOthersInfo },
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/WhoIs [DisplayedName]",
            Help = "Prints a list of players using the specified displayed name.",
            Handler = WhoIsHandler
        };

        static void WhoIsHandler(Player player, CommandReader cmd)
        {
            string TargetDisplayedName = cmd.Next();
            if (TargetDisplayedName == null)
            {
                CdWhoIs.PrintUsage(player);
                return;
            }
            string whoislist = "";
            //string offsetstring = cmd.Next();
            //int offset = 0;
            //if (offsetstring != null) {
                //Int32.TryParse(offsetstring, out offset);
            //}

            List<PlayerInfo> Results = new List<PlayerInfo>();
            PlayerInfo[] CachedList = PlayerDB.PlayerInfoList;
            foreach (PlayerInfo playerinfo in CachedList)
            {
                if (playerinfo.DisplayedName == null)
                {
                    if (Color.StripColors(playerinfo.Name.ToLower()) == Color.StripColors(TargetDisplayedName.ToLower())) Results.Add(playerinfo);
                }
                else
                {
                    if (Color.StripColors(playerinfo.DisplayedName.ToLower()) == Color.StripColors(TargetDisplayedName.ToLower())) Results.Add(playerinfo);
                }
            }
            if (Results.Count <= 0)
            {
                player.Message("No players have the displayed name \"" + TargetDisplayedName + "\"");
            }
            if (Results.Count == 1)
            {
                player.Message("{0} &shas the displayed name \"" + TargetDisplayedName + "\"", Results.ToArray()[0].Rank.Color + Results.ToArray()[0].Name);
            }
            if (Results.Count > 1)
            {
                foreach (PlayerInfo thisplayer in Results)
                {
                    whoislist += thisplayer.Rank.Color + thisplayer.Name + ", ";
                }
                whoislist = whoislist.Remove(whoislist.Length - 2);
                whoislist += ".";
                player.Message("The following players have the displayed name \"" + TargetDisplayedName + "\"&s: {0}", whoislist);
            }
        }
        #endregion
        #region OnlineStaff

        static readonly CommandDescriptor CdOnlineStaff = new CommandDescriptor
        {
            Name = "OnlineStaff",
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Help = "Shows a list of currently online staff members.",
            Handler = OnlineStaffHandler
        };

        static void OnlineStaffHandler(Player player, CommandReader cmd)
        {
            Player[] onlineStaff = Server.Players.Where(p => p.IsStaff && player.CanSee(p)).ToArray();
            if (!onlineStaff.Any())
            {
                player.Message("There are no online staff at the moment");
                return;
            }

            player.Message("Below is a list of online staff members:");
            foreach (Rank rank in RankManager.Ranks.Where(r => r.IsStaff)) {
                string StaffListTemporary = "";
                foreach (Player stafflistplayer in onlineStaff.Where(r => r.Info.Rank == rank)) {
                    StaffListTemporary += stafflistplayer.ClassyName + ", ";
                }
                if (StaffListTemporary.Length > 2) {
                    StaffListTemporary = StaffListTemporary.Remove((StaffListTemporary.Length - 2), 2);
                    StaffListTemporary += ".";
                    StaffListTemporary = rank.ClassyName + ": " + StaffListTemporary;
                    player.Message(StaffListTemporary);
                }
            }
        }

        #endregion
        #region ListStaff

        static readonly CommandDescriptor CdListStaff = new CommandDescriptor
        {
            Name = "ListStaff",
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Help = "Shows a list of ALL staff members.",
            Handler = ListStaffHandler
        };

        static void ListStaffHandler(Player player, CommandReader cmd)
        {
            Rank ranktarget;
            if (cmd.HasNext)
            {
                string rankName = cmd.Next();
                ranktarget = RankManager.FindRank( rankName );
                if (ranktarget == null)
                {
                    player.MessageNoRank(rankName);
                    return;
                }
            }
            else ranktarget = null;

            PlayerInfo[] infos;

            if (ranktarget == null)
            {
                player.Message("Below is a list of ALL staff members.");
                foreach (Rank rank in RankManager.Ranks)
                {
                    if (rank.IsStaff)
                    {
                        infos = PlayerDB.PlayerInfoList
                                        .Where(info => info.Rank == rank)
                                        .ToArray();
                        if (infos != null && rank.PlayerCount > 0)
                        {
                            Array.Sort(infos, new PlayerInfoComparer(player));
                            IClassy[] itemsEnumerated = infos;
                            string nameList = itemsEnumerated.Take( 15 ).JoinToString( "&s, ", p => p.ClassyName );
                            if (rank.PlayerCount > 15) {
                                player.Message( " {0} &s(&f{1}&s): {2}{3} &s{4} more", rank.ClassyName, rank.PlayerCount, rank.Color, nameList, (rank.PlayerCount - 15) );
                            } else {
                                player.Message( " {0} &s(&f{1}&s): {2}{3}", rank.ClassyName, rank.PlayerCount, rank.Color, nameList );
                            }
                        }
                    }
                }
            }
            else
            {
                player.Message("Below is a list of ALL staff members of rank '" + ranktarget.ClassyName + "&s':");
                infos = PlayerDB.PlayerInfoList
                                .Where(info => info.Rank == ranktarget)
                                .ToArray();
                if (infos != null)
                {
                    Array.Sort(infos, new PlayerInfoComparer(player));
                    IClassy[] itemsEnumerated = infos;
                    string nameList = itemsEnumerated.Take(30).JoinToString(", ", p => p.ClassyName);
                    if (ranktarget.PlayerCount > 15) {
                        player.Message( " {0} &s(&f{1}&s): {2}{3} &s{4} more", ranktarget.ClassyName, ranktarget.PlayerCount, ranktarget.Color, nameList, (ranktarget.PlayerCount - 30) );
                    } else {
                        player.Message( " {0} &s(&f{1}&s): {2}{3}", ranktarget.ClassyName, ranktarget.PlayerCount, ranktarget.Color, nameList );
                    }
                }
            }
        }

        #endregion
        #region Rules

        const string DefaultRules = "Rules: Use common sense!";

        static readonly CommandDescriptor CdRules = new CommandDescriptor {
            Name = "Rules",
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Help = "Shows a list of rules defined by server operator(s).",
            Handler = RulesHandler
        };

        static void RulesHandler( Player player, CommandReader cmd ) {
            string sectionName = cmd.Next();
            if (!player.Info.HasRTR) {
                Server.Players.Can( Permission.ReadStaffChat ).Message( player.ClassyName + " &sread the rules!" );
                player.Info.HasRTR = true;
                player.Info.ReadIRC = true;
            }

            // if no section name is given
            if( sectionName == null ) {
                FileInfo ruleFile = new FileInfo( Paths.RulesFileName );

                if( ruleFile.Exists ) {
                    PrintRuleFile( player, ruleFile );
                } else {
                    player.Message( DefaultRules );
                }

                // print a list of available sections
                string[] sections = GetRuleSectionList();
                if( sections != null ) {
                    player.Message( "Rule sections: {0}. Type &H/Rules SectionName&S to read.", sections.JoinToString() );
                }
                return;
            }

            // if a section name is given, but no section files exist
            if( !Directory.Exists( Paths.RulesDirectory ) ) {
                player.Message( "There are no rule sections defined." );
                return;
            }

            string ruleFileName = null;
            string[] sectionFiles = Directory.GetFiles( Paths.RulesPath,
                                                        "*.txt",
                                                        SearchOption.TopDirectoryOnly );

            for( int i = 0; i < sectionFiles.Length; i++ ) {
                string sectionFullName = Path.GetFileNameWithoutExtension( sectionFiles[i] );
                if( sectionFullName == null ) continue;
                if( sectionFullName.StartsWith( sectionName, StringComparison.OrdinalIgnoreCase ) ) {
                    if( sectionFullName.Equals( sectionName, StringComparison.OrdinalIgnoreCase ) ) {
                        // if there is an exact match, break out of the loop early
                        ruleFileName = sectionFiles[i];
                        break;

                    } else if( ruleFileName == null ) {
                        // if there is a partial match, keep going to check for multiple matches
                        ruleFileName = sectionFiles[i];

                    } else {
                        var matches = sectionFiles.Select( f => Path.GetFileNameWithoutExtension( f ) )
                                                  .Where( sn => sn != null && sn.StartsWith( sectionName, StringComparison.OrdinalIgnoreCase ) );
                        // if there are multiple matches, print a list
                        player.Message( "Multiple rule sections matched \"{0}\": {1}",
                                        sectionName, matches.JoinToString() );
                        return;
                    }
                }
            }

            if( ruleFileName != null ) {
                string sectionFullName = Path.GetFileNameWithoutExtension( ruleFileName );
                if (sectionFullName.IndexOf("Admin") > -1) {
                    if (!player.IsStaff) {
                        player.Message("You need to be an Admin to read the Admin Rules.");
                    } else {
                        PrintRuleFile(player, new FileInfo(ruleFileName));
                    }
                } else {
                    PrintRuleFile(player, new FileInfo(ruleFileName));
                }

            } else {
                var sectionList = GetRuleSectionList();
                if( sectionList == null ) {
                    player.Message( "There are no rule sections defined." );
                } else {
                    player.Message( "No rule section defined for \"{0}\". Available sections: {1}",
                                    sectionName, sectionList.JoinToString() );
                }
            }
        }


        [CanBeNull]
        static string[] GetRuleSectionList() {
            if( Directory.Exists( Paths.RulesDirectory ) ) {
                string[] sections = Directory.GetFiles( Paths.RulesPath, "*.txt", SearchOption.TopDirectoryOnly )
                                             .Select( name => Path.GetFileNameWithoutExtension( name ) )
                                             .Where( name => !String.IsNullOrEmpty( name ) )
                                             .ToArray();
                if( sections.Length != 0 ) {
                    return sections;
                }
            }
            return null;
        }


        static void PrintRuleFile( Player player, FileSystemInfo ruleFile ) {
            try {
                string[] ruleLines = File.ReadAllLines( ruleFile.FullName );
                foreach( string ruleLine in ruleLines ) {
                    if( ruleLine.Trim().Length > 0 ) {
                        player.Message( "&R{0}", Chat.ReplaceTextKeywords( player, ruleLine ) );
                    }
                }
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "InfoCommands.PrintRuleFile: An error occurred while trying to read {0}: {1}",
                            ruleFile.FullName, ex );
                player.Message( "&WError reading the rule file." );
            }
        }

        #endregion
        #region Measure

        static readonly CommandDescriptor CdMeasure = new CommandDescriptor {
            Name = "Measure",
            Category = CommandCategory.Info | CommandCategory.Building,
            RepeatableSelection = true,
            Help = "Shows information about a selection: width/length/height and volume.",
            Handler = MeasureHandler
        };

        static void MeasureHandler( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                CdMeasure.PrintUsage( player );
                return;
            }
            player.SelectionStart( 2, MeasureCallback, null );
            player.Message( "Measure: Select the area to be measured" );
        }

        const int TopBlocksToList = 5;

        static void MeasureCallback( Player player, Vector3I[] marks, object tag ) {
            BoundingBox box = new BoundingBox( marks[0], marks[1] );
            player.Message( "Measure: {0} x {1} wide, {2} tall, {3} blocks.",
                            box.Width,
                            box.Length,
                            box.Height,
                            box.Volume );
            player.Message( "  Located between {0} and {1}",
                            box.MinVertex,
                            box.MaxVertex );

            Map map = player.WorldMap;
            Dictionary<byte, int> blockCounts = new Dictionary<byte, int>();
            for( int i = 0; i < 256; i++ )
            	blockCounts[(byte)i] = 0;            
            
            for( int z = box.ZMin; z <= box.ZMax; z++ )
                for( int y = box.YMin; y <= box.YMax; y++ )
                    for( int x = box.XMin; x <= box.XMax; x++ )
            {
            	Block block = map.GetBlock( x, y, z );
            	blockCounts[(byte)block]++;
            }
            var topBlocks = blockCounts.Where( p => p.Value > 0 )
                                       .OrderByDescending( p => p.Value )
                                       .Take( TopBlocksToList )
                                       .ToArray();
            var blockString = topBlocks.JoinToString( p => String.Format( "{0}: {1} ({2}%)",
                                                                          (Block)p.Key,
                                                                          p.Value,
                                                                          (p.Value * 100L) / box.Volume ) );
            player.Message( "  Top {0} block types: {1}",
                            topBlocks.Length, blockString );
        }

        #endregion
        #region Players

        static readonly CommandDescriptor CdPlayers = new CommandDescriptor {
            Name = "Players",
            Aliases = new[] { "who" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/Players [WorldName] [Offset]",
            Help = "Lists all players on the server (in all worlds). " +
                   "If a WorldName is given, only lists players on that one world.",
            Handler = PlayersHandler
        };
        
        static readonly CommandDescriptor CdPlayersAdvanced = new CommandDescriptor {
            Name = "List",
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/List [WorldName] [Offset]",
            Help = "Lists all real names of players on the server (in all worlds). " +
                   "If a WorldName is given, only lists players on that one world.",
            Handler = PlayersAdvancedHandler
        };

        static void PlayersHandler( Player player, CommandReader cmd ) {
        	ListPlayersHandler( player, cmd, false );
        }
        
        static void PlayersAdvancedHandler( Player player, CommandReader cmd ) {
        	ListPlayersHandler( player, cmd, true );
        }

        static void ListPlayersHandler( Player player, CommandReader cmd, bool realNames ) {
            string param = cmd.Next();
            Player[] players;
            string worldName = null;
            string qualifier;
            int offset = 0;

            if( param == null || Int32.TryParse( param, out offset ) ) {
                // No world name given; Start with a list of all players.
                players = Server.Players;
                qualifier = "online";
                if( cmd.HasNext ) {
                	CommandDescriptor desc = realNames ? CdPlayersAdvanced : CdPlayers;
                	desc.PrintUsage( player );
                    return;
                }

            } else {
                // Try to find the world
                World world = WorldManager.FindWorldOrPrintMatches( player, param );
                if( world == null ) return;

                worldName = param;
                // If found, grab its player list
                players = world.Players;
                qualifier = String.Format( "in world {0}&S", world.ClassyName );

                if( cmd.HasNext && !cmd.NextInt( out offset ) ) {
                    CdPlayers.PrintUsage( player );
                    return;
                }
            }

            if( players.Length > 0 ) {
                // Filter out hidden players, and sort
                Player[] visiblePlayers = players.Where( player.CanSee )
                                                 .OrderBy( p => p, PlayerListSorter.Instance )
                                                 .ToArray();


                if( visiblePlayers.Length == 0 ) {
                    player.Message( "There are no players {0}", qualifier );

                } else if( visiblePlayers.Length <= PlayersPerPage || player.IsSuper ) {
                	string names = realNames ? visiblePlayers.JoinToRealString() : visiblePlayers.JoinToClassyString();
                    player.Message("  There are {0} players {1}: {2}",
                                            visiblePlayers.Length, qualifier, names );

                } else {
                    if( offset >= visiblePlayers.Length ) {
                        offset = Math.Max( 0, visiblePlayers.Length - PlayersPerPage );
                    }
                    Player[] playersPart = visiblePlayers.Skip( offset ).Take( PlayersPerPage ).ToArray();
                    string names = realNames ? playersPart.JoinToRealString() : playersPart.JoinToClassyString();
                    player.Message("  Players {0}: {1}", qualifier, names);

                    if( offset + playersPart.Length < visiblePlayers.Length ) {
                        player.Message( "Showing {0}-{1} (out of {2}). Next: &H/Players {3}{1}",
                                        offset + 1, offset + playersPart.Length,
                                        visiblePlayers.Length,
                                        (worldName == null ? "" : worldName + " ") );
                    } else {
                        player.Message( "Showing players {0}-{1} (out of {2}).",
                                        offset + 1, offset + playersPart.Length,
                                        visiblePlayers.Length );
                    }
                }
            } else {
                player.Message( "There are no players {0}", qualifier );
            }
        }
        #endregion
        #region Where
        const string Compass = "N.......ne......E.......se......S.......sw......W.......nw......" +
                               "N.......ne......E.......se......S.......sw......W.......nw......";
        static readonly CommandDescriptor CdWhere = new CommandDescriptor {
            Name = "Where",
            Aliases = new[] { "compass", "whereis", "whereami", "position", "pos" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Where [PlayerName]",
            Help = "Shows information about the location and orientation of a player. " +
                   "If no name is given, shows player's own info.",
            Handler = WhereHandler
        };

        static void WhereHandler( Player player, CommandReader cmd ) {
            string name = cmd.Next();
            if( cmd.HasNext ) {
                CdWhere.PrintUsage( player );
                return;
            }
            Player target = player;

            if( name != null ) {
                if( !player.Can( Permission.ViewOthersInfo ) ) {
                    player.MessageNoAccess( Permission.ViewOthersInfo );
                    return;
                }
                target = Server.FindPlayerOrPrintMatches(player, name, SearchOptions.IncludeSelf);
                if( target == null ) return;
            } else if( target.World == null ) {
                player.Message( "When called from console, &H/Where&S requires a player name." );
                return;
            }

            if( target.World == null ) {
                // Chances of this happening are miniscule
                player.Message( "Player {0}&S is not in any world.", target.Name );
                return;
            } else {
                player.Message( "Player {0}&S is on world {1}&S:",
                                target.ClassyName,
                                target.World.ClassyName );
            }

            Vector3I targetBlockCoords = target.Position.ToBlockCoords();
            player.Message( "{0}{1} - {2}",
                            Color.Silver,
                            targetBlockCoords,
                            GetCompassString( target.Position.R ) );
            //player.Message("Yaw: " + player.Position.R.ToString() + "Pitch: " + player.Position.L.ToString());
        }


        public static string GetCompassString( byte rotation ) {
            int offset = (int)(rotation / 255f * 64f) + 32;

            return String.Format( "&s[&f{0}&c{1}&f{2}&s]",
                                  Compass.Substring(offset - 9, 8),
                                  Compass.Substring(offset - 1, 3),
                                  Compass.Substring(offset + 2, 8));
        }
        #endregion
        #region Help

        static readonly CommandDescriptor CdHelp = new CommandDescriptor {
            Name = "Help",
            Aliases = new[] { "herp", "man" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Help [CommandName]",
            Help = "Derp.",
            Handler = HelpHandler
        };

        static void HelpHandler( Player player, CommandReader cmd ) {
            string commandName = cmd.Next();

            if( commandName == "commands" ) {
                CdCommands.Call( player, cmd, false );

            } else if( commandName != null ) {
                CommandDescriptor descriptor = CommandManager.GetDescriptor( commandName, true );
                if( descriptor == null ) {
                    player.Message( "Unknown command: \"{0}\"", commandName );
                    return;
                }

                string sectionName = cmd.Next();
                if( sectionName != null ) {
                    string sectionHelp;
                    if( descriptor.HelpSections != null && descriptor.HelpSections.TryGetValue( sectionName.ToLower(), out sectionHelp ) ) {
                        player.Message("  " + sectionHelp);
                    } else {
                        player.Message( "No help found for \"{0}\"", sectionName );
                    }
                } else {
                    StringBuilder sb = new StringBuilder( Color.Help );
                    sb.Append( descriptor.Usage ).Append( "&N&S" );

                    if( descriptor.Aliases != null ) {
                        sb.Append( "Aliases: &H" );
                        sb.Append( descriptor.Aliases.JoinToString() );
                        sb.Append( "&N&S" );
                    }

                    if( String.IsNullOrEmpty( descriptor.Help ) ) {
                        sb.Append( "No help is available for this command." );
                    } else {
                        sb.Append( descriptor.Help );
                    }

                    player.Message("  " + sb.ToString() );

                    if( descriptor.Permissions != null && descriptor.Permissions.Length > 0 ) {
                        player.MessageNoAccess( descriptor );
                    }
                }

            } else {
                player.Message( "  To see a list of all commands, write &H/Commands" );
                player.Message( "  To see detailed help for a command, write &H/Help Command" );
                if( player != Player.Console ) {
                    player.Message( "  To see your stats, write &H/Info" );
                }
                player.Message( "  To list available worlds, write &H/Worlds" );
                player.Message( "  To join a world, write &H/Join WorldName" );
                player.Message( "  To send private messages, write &H@PlayerName Message" );
            }
        }

        #endregion
        #region Commands

        static readonly CommandDescriptor CdCommands = new CommandDescriptor {
            Name = "Commands",
            Aliases = new[] { "cmds", "cmdlist" },
            Category = CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Commands [Category]",
            Help = "Shows a list of commands by category" +
                   "Categories are: Building, Chat, Info, Maintenance, Moderation, New, World, Zone, New, and All." +
                   "You can also search by command name using \"*\" as a wildcard(needed).",
            Handler = CommandsHandler
        };

        private static void CommandsHandler(Player player, CommandReader cmd) {
            string param = cmd.Next();
            CommandDescriptor[] cd;
            CommandCategory category;
            string prefix;
            if (param == null) {
                player.Message("Command Categories:");
                player.Message("&h  /cmds All");
                player.Message("&h  /cmds Building");
                player.Message("&h  /cmds Chat");
                player.Message("&h  /cmds CPE");
                player.Message("&h  /cmds Info");
                player.Message("&h  /cmds Maintenance");
                player.Message("&h  /cmds Moderation");
                player.Message("&h  /cmds New");
                player.Message("&h  /cmds World");
                return;
			}
			Array items = CommandManager.GetCommands(player.Info.Rank, false);
			string output = "";
            if (param.StartsWith("*") && param.EndsWith("*")) {
                foreach (CommandDescriptor item in items) {
                    if (item.Name.ToLower().Contains(param.ToLower().Trim('*'))) {
                        output += item.MinRank.Color + item.Name + "&s, ";
                    }
                }
                player.Message("Commands containing \"{0}\":", param.Trim('*'));
				if (output.EndsWith(", ")) {
					player.Message(output.Remove(output.Length - 2) + ".");
				} else {
					player.Message("&cThere are no commands containing \"{0}\"", param.Trim('*'));
				}
                return;
            } else if (param.EndsWith("*")) {
                foreach (CommandDescriptor item in items) {
                    if (item.Name.ToLower().StartsWith(param.ToLower().Trim('*'))) {
                        output += item.MinRank.Color + item.Name + "&s, ";
                    }
                }
                player.Message("Commands starting with \"{0}\":", param.Trim('*'));
				if (output.EndsWith(", ")) {
					player.Message(output.Remove(output.Length - 2) + ".");
				} else {
					player.Message("&cThere are no commands starting with \"{0}\"", param.Trim('*'));
				}
                return;
			} else if (param.StartsWith("*")) {
                foreach (CommandDescriptor item in items) {
                    if (item.Name.ToLower().EndsWith(param.ToLower().Trim('*'))) {
                        output += item.MinRank.Color + item.Name + "&s, ";
                    }
                }
                player.Message("Commands ending with \"{0}\":", param.Trim('*'));
				if (output.EndsWith(", ")) {
					player.Message(output.Remove(output.Length - 2) + ".");
				} else {
					player.Message("&cThere are no commands ending with \"{0}\"", param.Trim('*'));
				}
				return;
			} else if (param.StartsWith("@")) {
                string rankName = param.Substring(1);
                Rank rank = RankManager.FindRank(rankName);
                if (rank == null) {
                    player.MessageNoRank(rankName);
                    return;
                }
                prefix = string.Format("Commands available to {0}&S", rank.ClassyName);
                cd = CommandManager.GetCommands(rank, false);
            } else if (param.Equals("all", StringComparison.OrdinalIgnoreCase)) {
                prefix = "All commands";
                cd = CommandManager.GetCommands();
            } else if (param.Equals("hidden", StringComparison.OrdinalIgnoreCase)) {
                prefix = "Hidden commands";
                cd = CommandManager.GetCommands(true);
            } else if (EnumUtil.TryComplete(param, out category, true)) {
                prefix = string.Format("{0} commands", category);
                cd = CommandManager.GetCommands(category, false);
            } else {
                CdCommands.PrintUsage(player);
                return;
            }
            player.Message("{0}: {1}", prefix, cd.JoinToClassyString());
        }

        #endregion
        #region Most

        static readonly CommandDescriptor CdMost = new CommandDescriptor {
            Name = "Most",
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/Most <stat> [Rank] and/or [Offset]",
            Help = "Lists all players in order of a specefied statistic. Stats:" +
                   "Banned, Built, Chat, Deleted, Demoted, Drawn, Hours, Kicked, Logins, Promoted",
            HelpSections = new Dictionary<string, string>{
                { "Banned",     "/Most Banned [Args]" +
                                    "Lists the top players by playeres banned" },
                { "Built",      "/Most Built [Args]" +
                                    "Lists the top players by blocks built" },
                { "Chat",       "/Most Chat [Args]" +
                                    "Lists the top players by lines of chat sent" },
                { "Deleted",    "/Most Deleted [Args]" +
                                    "Lists the top players by blocks deleted" },
                { "Demoted",    "/Most Demoted [Args]" +
                                    "Lists the top players by players demoted" },
                { "Drawn",      "/Most Drawn [Args]" +
                                    "Lists the top players by blocks drawn" },
                { "Hours",      "/Most Hours [Args]" +
                                    "Lists the top players by total hours" },
                { "Kicked",     "/Most Kicked [Args]" +
                                    "Lists the top players by players kicked" },
                { "Logins",     "/Most Logins [Args]" +
                                    "Lists the top players by total logins" },
                { "Promoted",   "/Most Promoted [Args]" +
                                    "Lists the top players by players promoted" },
            },
            Handler = MostHandler
        };

        private static void MostHandler(Player player, CommandReader cmd) {
            string stat = cmd.Next();
            if (string.IsNullOrEmpty(stat)) {
                CdMost.PrintUsage(player); return;
            }
            string rankStr = cmd.Next();
            string offsetStr = cmd.Next();
            bool noRank = false;
            int offset = 0;
            Rank rank = RankManager.FindRank(rankStr);
            if (string.IsNullOrEmpty(rankStr)) {
                noRank = true;
            } else {
                if (rank == null) {
                    if (!int.TryParse(rankStr, out offset)) {
                        player.MessageNoRank(rankStr);
                        return;
                    } else {
                        noRank = true;
                    }
                }
                if (offsetStr != null) {
                    if (!int.TryParse(offsetStr, out offset)) {
                        offset = 0;
                    }
                }
            }
            
            Func<PlayerInfo, long> orderer;
            Func<PlayerInfo, string> formatter;
            switch (stat.ToLower()) {
                case "bans":
                case "banned":
                    formatter = p => string.Format("{0:N0}", p.TimesBannedOthers);
                    orderer = p => p.TimesBannedOthers; break;
                case "built":
                    formatter = p => string.Format("{0:N0}", p.BlocksBuilt); 
                    orderer = p => p.BlocksBuilt; break;
                case "chat":
                case "messages":
                    formatter = p => string.Format("{0:N0}", p.MessagesWritten);
                    orderer = p => p.MessagesWritten; break;
                case "deleted":
                    formatter = p => string.Format("{0:N0}", p.BlocksDeleted); 
                    orderer = p => p.BlocksDeleted; break;
                case "demoted":
                    formatter = p => string.Format("{0:N0}", p.DemoCount);
                    orderer = p => p.DemoCount; break;
                case "drawn":
                    formatter = p => string.Format("{0:N0}&sK", p.BlocksDrawn / 1000.0);
                    orderer = p => p.BlocksDrawn; break;
                case "time":
                case "hours":
                    formatter = p => string.Format("{0:N2}&sH", p.TotalTime.TotalHours);
                    orderer = p => p.TotalTime.ToHours(); break;
                case "kicks":
                case "kicked":
                    formatter = p => string.Format("{0:N0}", p.TimesKickedOthers);
                    orderer = p => p.TimesKickedOthers; break;
                case "logins":
                    formatter = p => string.Format("{0:N0}", p.TimesVisited); 
                    orderer = p => p.TimesVisited; break;
                case "promoted":
                    formatter = p => string.Format("{0:N0}", p.PromoCount);
                    orderer = p => p.PromoCount; break;
                default:
                    player.Message("No stats for: {0}", stat);
                    return;
            }
            
            PlayerInfo[] all = PlayerDB.PlayerInfoList;
            if (noRank)
                all = all.Where(p => orderer(p) >= 1).OrderBy(orderer).ToArray();
            else
                all = all.Where(p => orderer(p) >= 1 && p.Rank == rank).OrderBy(orderer).ToArray();
            Array.Reverse(all);
            
            offset = offset < all.Length ? offset : Math.Max(0, all.Length - 10);
            int count = Math.Min(offset + 10, all.Length) - offset;
            int pad = formatter(all[offset]).Length;
            
            player.Message("Top Players ({0}):", stat);
            for (int i = offset; i < offset + count; i++) {
                PlayerInfo p = all[i];
                player.Message(" &7{1}&s - {0}", p.ClassyName, formatter(p).PadLeft(pad, '0'));
            }
             
            int total = noRank ? PlayerDB.PlayerInfoList.Length : rank.PlayerCount;
            player.Message("Showing players{3}{0}-{1} (out of {2}).", offset + 1, 
                           offset + count, total, 
                           (rank != null ? " in rank (" + rank.ClassyName + "&s)" : " "));
        }

        #endregion
        #region ListRanks

        static readonly CommandDescriptor CdLRP = new CommandDescriptor
        {
            Name = "ListRank",
            Aliases = new[] { "lr", "listrankplayers", "lrp", "listr" },
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/Listr [Rank] [Offset]",
            Help = "Lists all players of a certain rank",
            Handler = LRPHandler
        };

        static void LRPHandler(Player player, CommandReader cmd)
        {
            string name = cmd.Next();
            PlayerInfo[] infos;
            Rank rank = RankManager.FindRank(player.Info.Rank.Name);
            if (name != null)
            {
                rank = RankManager.FindRank(name);
                if (rank == null)
                {
                    player.MessageNoRank(name);
                    return;
                }
            }
            infos = PlayerDB.PlayerInfoList.Where(i => i.Rank == rank).OrderBy(c => c.TimeSinceRankChange).ToArray();
            int offset;
            if (!cmd.NextInt(out offset)) offset = 0;
            if (offset >= infos.Count())
            {
                offset = Math.Max(0, infos.Count() - PlayersPerPage);
            }
            var playersPart = infos.Skip(offset).Take(10).ToArray();
            player.Message("  Players in rank ({1}&s): {0}", playersPart.JoinToString((r => String.Format("&n{0}&S (Had rank for: {1})", r.ClassyName, r.TimeSinceRankChange.ToMiniString()))), rank.ClassyName);
            player.Message("Showing players {0}-{1} (out of {2}).", offset + 1, offset + playersPart.Length, infos.Count());
        }

        #endregion
        #region ListPreviousRanks

        static readonly CommandDescriptor CdLPR = new CommandDescriptor
        {
            Name = "PreviousRank",
            Aliases = new[] { "pr", "Listpreviousrank", "lpr", "listpr" },
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/Lprp [Rank] [Offset]",
            Help = "Lists all players who previously had that certain rank.",
            Handler = LPRHandler
        };

        static void LPRHandler(Player player, CommandReader cmd)
        {
            string name = cmd.Next();
            PlayerInfo[] infos;
            Rank rank = RankManager.FindRank(player.Info.Rank.Name);
            if (name != null)
            {
                rank = RankManager.FindRank(name);
                if (rank == null)
                {
                    player.MessageNoRank(name);
                    return;
                }
            }
            infos = PlayerDB.PlayerInfoList.Where(info => info.PreviousRank == rank).OrderBy(c => c.TimeSinceRankChange).ToArray();
            int offset;
            if (!cmd.NextInt(out offset)) offset = 0;            
            if (offset >= infos.Count())
            {
                offset = Math.Max(0, infos.Count() - PlayersPerPage);
            }
            var playersPart = infos.Skip(offset).Take(10).ToArray();
            player.Message("  Players who previously had rank ({1}&s): {0}", playersPart.JoinToString((r => String.Format("&n{0}&S (Had current rank ({2}&s) for: {1})", r.ClassyName, r.TimeSinceRankChange.ToMiniString(), r.Rank.ClassyName))), rank.ClassyName);
            player.Message("Showing players {0}-{1} (out of {2}).", offset + 1, offset + playersPart.Length, infos.Count());
        }

        #endregion
        #region Colors and Emotes

        static readonly CommandDescriptor CdColors = new CommandDescriptor
        {
            Name = "Colors",
            Aliases = new[] { "color" },
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Colors [Color]",
            Help = "Shows a list of all available color codes and some extra information about that color.",
            Handler = ColorHandler
        };

        static void ColorHandler(Player player, CommandReader cmd) {
            string color = cmd.Next() ?? "";
            switch (color.ToLower()) {
                case "black":
                case "0":
                    player.Message("Color: &fBlack");
                    player.Message("    Color Code: &f%0");
                    player.Message("    HEX Code: &f#000000");
                    player.Message("    RGB: &4R &f0 &2G &f0 &1B &f0");
                    player.Message("    Example: &0The quick brown fox jumps over the lazy dog");
                    break;
                case "navy":
                case "1":
                    player.Message("Color: &fNavy");
                    player.Message("    Color Code: &f%1");
                    player.Message("    HEX Code: &f#0000AA");
                    player.Message("    RGB: &4R &f0 &2G &f0 &1B &f170");
                    player.Message("    Example: &1The quick brown fox jumps over the lazy dog");
                    break;
                case "green":
                case "2":
                    player.Message("Color: &fGreen");
                    player.Message("    Color Code: &f%2");
                    player.Message("    HEX Code: &f#00AA00");
                    player.Message("    RGB: &4R &f0 &2G &f170 &1B &f0");
                    player.Message("    Example: &2The quick brown fox jumps over the lazy dog");
                    break;
                case "teal":
                case "3":
                    player.Message("Color: &fTeal");
                    player.Message("    Color Code: &f%3");
                    player.Message("    HEX Code: &f#00AAAA");
                    player.Message("    RGB: &4R &f0 &2G &f170 &1B &f170");
                    player.Message("    Example: &3The quick brown fox jumps over the lazy dog");
                    break;
                case "gray":
                case "8":
                    player.Message("Color: &fGray");
                    player.Message("    Color Code: &f%8");
                    player.Message("    HEX Code: &f#555555");
                    player.Message("    RGB: &4R &f85 &2G &f85 &1B &f85");
                    player.Message("    Example: &8The quick brown fox jumps over the lazy dog");
                    break;
                case "blue":
                case "9":
                    player.Message("Color: &fBlue");
                    player.Message("    Color Code: &f%9");
                    player.Message("    HEX Code: &f#55555FF");
                    player.Message("    RGB: &4R &f85 &2G &f85 &1B &f255");
                    player.Message("    Example: &9The quick brown fox jumps over the lazy dog");
                    break;
                case "lime":
                case "a":
                    player.Message("Color: &fLime");
                    player.Message("    Color Code: &f%a");
                    player.Message("    HEX Code: &f#55FF55");
                    player.Message("    RGB: &4R &f85 &2G &f255 &1B &f85");
                    player.Message("    Example: &aThe quick brown fox jumps over the lazy dog");
                    break;
                case "aqua":
                case "b":
                    player.Message("Color: &fAqua");
                    player.Message("    Color Code: &f%b");
                    player.Message("    HEX Code: &f#55FFFF");
                    player.Message("    RGB: &4R &f85 &2G &f255 &1B &f255");
                    player.Message("    Example: &bThe quick brown fox jumps over the lazy dog");
                    break;
                case "maroon":
                case "4":
                    player.Message("Color: &fMaroon");
                    player.Message("    Color Code: &f%4");
                    player.Message("    HEX Code: &f#AA0000");
                    player.Message("    RGB: &4R &f170 &2G &f0 &1B &f0");
                    player.Message("    Example: &4The quick brown fox jumps over the lazy dog");
                    break;
                case "purple":
                case "5":
                    player.Message("Color: &fPurple");
                    player.Message("    Color Code: &f%5");
                    player.Message("    HEX Code: &f#AA00AA");
                    player.Message("    RGB: &4R &f170 &2G &f0 &1B &f170");
                    player.Message("    Example: &5The quick brown fox jumps over the lazy dog");
                    break;
                case "olive":
                case "6":
                    player.Message("Color: &fOlive");
                    player.Message("    Color Code: &f%6");
                    player.Message("    HEX Code: &f#FFAA00");
                    player.Message("    RGB: &4R &f255 &2G &f170 &1B &f0");
                    player.Message("    Example: &6The quick brown fox jumps over the lazy dog");
                    break;
                case "silver":
                case "7":
                    player.Message("Color: &fSilver");
                    player.Message("    Color Code: &f%7");
                    player.Message("    HEX Code: &f#AAAAAA");
                    player.Message("    RGB: &4R &f170 &2G &f170 &1B &f170");
                    player.Message("    Example: &7The quick brown fox jumps over the lazy dog");
                    break;
                case "red":
                case "c":
                    player.Message("Color: &fRed");
                    player.Message("    Color Code: &f%c");
                    player.Message("    HEX Code: &f#FF5555");
                    player.Message("    RGB: &4R &f255 &2G &f85 &1B &f85");
                    player.Message("    Example: &cThe quick brown fox jumps over the lazy dog");
                    break;
                case "magenta":
                case "d":
                    player.Message("Color: &fMagenta");
                    player.Message("    Color Code: &f%d");
                    player.Message("    HEX Code: &f#FF55FF");
                    player.Message("    RGB: &4R &f255 &2G &f85 &1B &f255");
                    player.Message("    Example: &dThe quick brown fox jumps over the lazy dog");
                    break;
                case "yellow":
                case "e":
                    player.Message("Color: &fYellow");
                    player.Message("    Color Code: &f%e");
                    player.Message("    HEX Code: &f#FFFF55");
                    player.Message("    RGB: &4R &f255 &2G &f255 &1B &f85");
                    player.Message("    Example: &eThe quick brown fox jumps over the lazy dog");
                    break;
                case "white":
                case "f":
                    player.Message("Color: &fWhite");
                    player.Message("    Color Code: &f%f");
                    player.Message("    HEX Code: &f#FFFFFF");
                    player.Message("    RGB: &4R &f255 &2G &f255 &1B &f255");
                    player.Message("    Example: &fThe quick brown fox jumps over the lazy dog");
                    break;
                default:
                    foreach (CustomColor col in Color.ExtColors.Where(c => !c.Undefined)) {
                        if (color.ToLower() == col.Name.ToLower() || color == col.Code.ToString()) {
                            player.Message("Color: &{0}{1}", col.Code, col.Name);
                            player.Message("    Color Code: &f%{0}", col.Code);
                            player.Message("    Fallback Color Code: &f%{0}", col.Fallback);
                            player.Message("    HEX Code: &f#{0}", string.Format("{0:X2}{1:X2}{2:X2}", col.R, col.G, col.B));
                            player.Message("    RGB: &4R &f{0} &2G &f{1} &1B &f{2}", col.R, col.G, col.B);
                            player.Message("    Example: &{0}The quick brown fox jumps over the lazy dog", col.Code);
                            return;
                        }
                    }
                    player.Message("List of Colors:");
                    player.Message(" &0%0 Black &8%8 Gray");
                    player.Message(" &1%1 Navy &9%9 Blue");
                    player.Message(" &2%2 Green &a%a Lime");
                    player.Message(" &3%3 Teal &b%b Aqua");
                    player.Message(" &4%4 Maroon &c%c Red");
                    player.Message(" &5%5 Purple &d%d Magenta");
                    player.Message(" &6%6 Olive &e%e Yellow");
                    player.Message(" &7%7 Silver &f%f White");
                    if (Color.ExtColors.Where(c => !c.Undefined).Count() >= 1) {
                        player.Message("List of Custom Colors:");
                        string list = "";
                        foreach (CustomColor col in Color.ExtColors.Where(c => !c.Undefined)) {
                            list = list + string.Format(" &{0}%{0}-{1}", col.Code, col.Name.ToLower().UppercaseFirst());
                        }
                        player.Message(list);
                    }
                    if (player.IsStaff) {
                        player.Message("Server colors:");
                        player.Message(" &r%r Announcement &h%h Help &i%i IRC &m%m Me");
                        player.Message(" &p%p PM &y%y Say &s%s System &w%w Warning");
                    }
                    if (!player.Can(Permission.UseColorCodes)) {
                        Rank reqRank = RankManager.GetMinRankWithAllPermissions(Permission.UseColorCodes);
                        if (reqRank == null) {
                            player.Message("None of the ranks have permission to use colors in chat.");
                        } else {
                            player.Message("Only {0}+&S can use colors in chat.",
                                     reqRank.ClassyName);
                        }
                    }
                    break;
            }
        }


        static readonly CommandDescriptor CdEmotes = new CommandDescriptor
        {
            Name = "Emotes",
            Usage = "/Emotes [Page]",
            Category = CommandCategory.Info | CommandCategory.Chat,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Help = "Shows a list of all available emotes and their keywords. " +
                   "There are 34 emotes, spanning 3 pages. Use &h/emotes 2&s and &h/emotes 3&s to see pages 2 and 3.",
            Handler = EmotesHandler
        };

        const int EmotesPerPage = 12;

        static void EmotesHandler(Player player, CommandReader cmd)
        {
            int page = 1;
            if (cmd.HasNext)
            {
                if (!cmd.NextInt(out page))
                {
                    CdEmotes.PrintUsage(player);
                    return;
                }
            }
            if (page < 1 || page > 3)
            {
                CdEmotes.PrintUsage(player);
                return;
            }

            var emoteChars = Chat.EmoteKeywords
                                 .Values
                                 .Distinct()
                                 .Skip((page - 1) * EmotesPerPage)
                                 .Take(EmotesPerPage);

            player.Message("List of emotes, page {0} of 3:", page);
            foreach (char ch in emoteChars)
            {
                char ch1 = ch;
                string keywords = Chat.EmoteKeywords
                                      .Where(pair => pair.Value == ch1)
                                      .Select(kvp => "{&F" + kvp.Key.UppercaseFirst() + "&7}")
                                      .JoinToString(" ");
                player.Message("&F  {0} &7= {1}", ch, keywords);
            }

            if (!player.Can(Permission.UseEmotes))
            {
                Rank reqRank = RankManager.GetMinRankWithAllPermissions(Permission.UseEmotes);
                if (reqRank == null)
                {
                    player.Message("Note: None of the ranks have permission to use emotes.");
                }
                else
                {
                    player.Message("Note: only {0}+&S can use emotes in chat.",
                                    reqRank.ClassyName);
                }
            }
        }

        #endregion
        #region extrainfo

        static readonly CommandDescriptor CdIPInfo = new CommandDescriptor {
            Name = "ExtraInfo",
            Aliases = new[] { "info2", "ei", "i2" },
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/ExtraInfo [PlayerName or IP [Offset]]",
            Help = "Prints the extra information about a given player",
            Handler = ExtraInfoHandler
        };

        private static void ExtraInfoHandler(Player player, CommandReader cmd) {
            if (player == null) throw new ArgumentNullException("player");
			PlayerInfo info = FindPlayerInfo(player, cmd, true);
            if (info == null) return;
            Player target = info.PlayerObject;

            player.Message("Extra Info about: {0}", info.ClassyName);
            player.Message("  Times used &6Bot&s: {0}", info.TimesUsedBot);
            player.Message("  Promoted: {0} Demoted: {1}", info.PromoCount, info.DemoCount);
            player.Message("  Reach Distance: {0} Model: {1}", info.ReachDistance, info.Mob);
            if (target != null && target.ClientName != null) {
                player.Message("  Client Name: &F{0}", target.ClientName);
            }
			player.Message(target == null ? "  Block they last held: {0}" : "  Block they are currently holding: {0}",
				info.heldBlock);
			if (target != null && target.LastMotdMessage != null) {
				player.Message("  Latest motd message: &f{0}", target.LastMotdMessage);
			}
            if (player.Can(Permission.ViewOthersInfo)) {
                player.Message("  Did they read the rules: &f{0}", info.HasRTR.ToString());
                player.Message("  Can they see IRC chat: &f{0}", info.ReadIRC.ToString());
                if (info.LastWorld != "" && info.LastWorldPos != "") {
                    player.Message("  Last block action...");
                    player.Message("    On world: &f{0}", info.LastWorld);
                    player.Message("    Player Position...");
                    player.Message("    {0}", info.LastWorldPos);
                    player.Message("    (Use &h/TPP X Y Z R L&s)");
                }
            }
            if (target != null)
                player.Message("  Ping: {0}ms Avg: {1}ms", target.PingList[9], target.PingList.Average());
        }

        #endregion
		#region GeoInfo
		static Regex nan = new Regex("[^a-zA-Z0-9,]");

        static readonly CommandDescriptor CdGeoip = new CommandDescriptor {
            Name = "geoip",
            Aliases = new[] { "geoinfo", "ipinfo", "geo", "ip" },
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/GeoInfo [PlayerName or PlayerIP [Offset]]",
            Help = "Prints the GeoIP information about a given player",
            Handler = IPInfoHandler
        };

        static void IPInfoHandler( Player player, CommandReader cmd ) {
            if (player == null)
                throw new ArgumentNullException( "player" );

			PlayerInfo info = null;
			if (!player.Can(Permission.ViewPlayerIPs)) { 
				info = player.Info;
			} else {
				info = FindPlayerInfo(player, cmd, true);
			}
            if (info == null) return;
			if (info.GeoIP != info.LastIP.ToString() || info.Accuracy == 0) {
				GetGeoip(info);
			}

            player.Message( "Geo Info about: {0}&S ({1})", info.ClassyName, info.GeoIP ?? "N/A" );
			player.Message("  Country: &f{1}&S ({0})", info.CountryCode ?? "N/A", info.CountryName ?? "N/A");
			player.Message("  Continent: &f{0}", info.Continent ?? "N/A");
			player.Message("  Subdivisions: &f{0}", info.Subdivision.JoinToString(", "));
			player.Message("  Latitude: &f{0}", info.Latitude ?? "N/A");
			player.Message("  Longitude: &f{0}", info.Longitude ?? "N/A");
			player.Message("  Time Zone: &f{0}", info.TimeZone ?? "N/A");
			player.Message("  Hostname: &f{0}", info.Hostname ?? "N/A");
			player.Message("  Accuracy: &f{0}", info.Accuracy);
			player.Message("Geoip information by: &9http://geoip.cf/");
        }

        static readonly CommandDescriptor CdGeoipNp = new CommandDescriptor {
            Name = "geoipnonplayer",
            Aliases = new[] { "geonpinfo", "ipnpinfo", "geonp", "ipnp" },
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/GeoNPInfo [IP Address]",
            Help = "Prints the geoinfo about the specified IP address",
            Handler = IPNPInfoHandler
        };

		private static void IPNPInfoHandler(Player player, CommandReader cmd) {
			string ipString = cmd.Next();
			IPAddress ip;
			if (ipString == null) {
				CdGeoipNp.PrintUsage(player);
				return;
			}
			if (!(IPAddressUtil.IsIP(ipString) && IPAddress.TryParse(ipString, out ip))) {
				player.Message("Info: Invalid IP range format. Use CIDR notation.");
				return;
			}
			JsonObject result = null;
			try {
				result = JsonObject.Parse(Server.downloadDatastring("http://geoip.cf/api/" + ip));
				if (result.Get("message") != null) {
					player.Message("No information found!");
					return;
				}
				player.Message("Geo Info about: &f{0}", result.Get("ip") ?? "N/A");
				player.Message("  Country: &f{0}&S ({1})", result.Get("country") ?? "N/A", result.Get("country_abbr") ?? "N/A");
				player.Message("  Continent: &f{0}", result.Get("continent") ?? "N/A");
				player.Message("  Subdivisions: &f{0}", nan.Replace(result.Get("subdivision"), "").Split(',').JoinToString(", "));
				player.Message("  Latitude: &f{0}", result.Get("latitude") ?? "N/A");
				player.Message("  Longitude: &f{0}", result.Get("longitude") ?? "N/A");
				player.Message("  Timezone: &f{0}", result.Get("timezone") ?? "N/A");
				byte acc;
				byte.TryParse(result.Get("accuracy"), out acc);
				player.Message("  Hostname: &f{0}", result.Get("hostname") ?? "N/A");
				player.Message("  Accuracy: &f{0}", acc);
				player.Message("Geoip information by: &9http://geoip.cf/");

			} catch (Exception ex) {
				Logger.Log(LogType.Warning, "Could not access GeoIP website (Ex: " + ex + ")");
			}
		}

		public static void GetGeoip(PlayerInfo info) {
			string ip = info.LastIP.ToString();
			if (IPAddress.Parse(ip).IsLocal() && Server.ExternalIP != null) {
				ip = Server.ExternalIP.ToString();
			}
			if (ip == info.GeoIP && info.Accuracy != 0) {
				return;
			}
			JsonObject result = null;
			try {
				result = JsonObject.Parse(Server.downloadDatastring("http://geoip.cf/api/" + ip));
				if (result.Get("message") != null) {
					return;
				}
				info.CountryName = result.Get("country") ?? "N/A";
				info.CountryCode = result.Get("country_abbr") ?? "N/A";
				info.Continent = result.Get("continent") ?? "N/A";
				info.Subdivision = nan.Replace(result.Get("subdivision"), "").Split(',');
				info.Latitude = result.Get("latitude") ?? "N/A";
				info.Longitude = result.Get("longitude") ?? "N/A";
				info.TimeZone = result.Get("timezone") ?? "N/A";
				byte.TryParse(result.Get("accuracy"), out info.Accuracy);
				info.Hostname = result.Get("hostname") ?? "N/A";
				info.GeoIP = result.Get("ip") ?? "N/A";
				return;

			} catch (Exception ex) {
				Logger.Log(LogType.Warning, "Could not access GeoIP website (Ex: " + ex + ")");
				Logger.Log(LogType.Debug, ex.ToString());
				return;
			}
		}

        #endregion
        #region API

        static readonly CommandDescriptor CdApi = new CommandDescriptor {
            Name = "classicubeapi",
            Aliases = new[] { "ccapi", "api"},
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/api [(id/i)/(player/p)] [playername/id]",
            Help = "Prints the api information about a player/id using classicube api" +
                   "Examples: /api i 106, /api p 123DontMessWitMe",
            Handler = APIPInfoHandler
        };

        private static void APIPInfoHandler(Player player, CommandReader cmd) {
            string type = cmd.Next();
            if (string.IsNullOrEmpty(type)) {
                CdApi.PrintUsage(player);
                return;
            }
            string value = cmd.Next();
            if (value == null) {
                value = "player/" + player.Name;
            }
            int id;
            switch (type.ToLower()) {
                case "id":
                case "i":
                    if (!int.TryParse(value, out id)) {
                        player.Message("ID not valid integer!");
                        return;
                    } else {
                        value = "id/" + id;
                    }
                    break;
                case "player":
                case "p":
                    value = "player/" + value;
                    break;
                default:
                    value = "player/" + player.Name;
                    break;
            }
            string data = Server.downloadDatastring("http://www.classicube.net/api/" + value);
            if (string.IsNullOrEmpty(data) || !data.Contains("username")) {
                player.Message("Player not found!");
                return;
            }
            JsonObject result = JsonObject.Parse(data);   
            string error;
            result.TryGetValue("error", out error);
            string flags1;
            result.TryGetValue("flags", out flags1);
            string flags2 = "ClassiCube User, ";
            if (flags1.Contains('b')) {
                flags2 = flags2 + "Banned from forums, ";
            }
            if (flags1.Contains('a')) {
                flags2 = flags2 + "Forum Administrator, ";
            }
            if (flags1.Contains('m')) {
                flags2 = flags2 + "Forum Moderator, ";
            }
            if (flags1.Contains('d')) {
                flags2 = flags2 + "ClassiCube Developer, ";
            }
            if (flags1.Contains('e')) {
                flags2 = flags2 + "ClassiCube Blog Editor, ";
            }
            flags2 = flags2.Remove(flags2.Length - 2, 2);
            string uid;
            result.TryGetValue("id", out uid);
            string premium;
            result.TryGetValue("premium", out premium);
            string registered1;
            result.TryGetValue("registered", out registered1);
            double registered2;
            double.TryParse(registered1, out registered2);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime registered3 = epoch.AddSeconds(registered2);
            string username;
            result.TryGetValue("username", out username);

            if (error.ToLower().Equals("user not found")) {
                player.Message("User not found!");
                return;
            }

            player.Message("API info about {0}", username);
            player.Message("  Flags: {0} {1}", flags2,
                flags1.Replace("\n", "").Replace("\r", "").Replace("\"", "").Replace(" ", ""));
            player.Message("  ID: {0}", uid);
            player.Message("  Premium*: {0}", premium);
            player.Message("  Registered: {0} at {1} UTC", registered3.ToLongDateString(),
                registered3.ToLongTimeString());
            player.Message("* = Ignore for now ");
        }

        #endregion
        #region seen

        static readonly CommandDescriptor CdSeen = new CommandDescriptor
        {
            Name = "Seen",
            Aliases = new[] { "whowas" },
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/Seen [PlayerName or IP [Offset]]",
            Help = "Prints the extra information about a given player",
            Handler = SeenHandler
        };

        static void SeenHandler(Player player, CommandReader cmd) {
			PlayerInfo info = FindPlayerInfo(player, cmd, true);
            if (info == null) return;
            Player target = info.PlayerObject;

            if (target != null)
            {
                player.Message("Player {0}&s has been &aOnline&s for {1}", target.Info.Rank.Color + target.Name, target.Info.TimeSinceLastLogin.ToMiniString());
				player.Message("They are currently on world {0}", target.World.ClassyName);
            }
            else
            {

                player.Message("Player {0}&s is &cOffline", info.ClassyName);
                player.Message("Was last seen {0} ago on world &f{1}", info.TimeSinceLastSeen.ToMiniString(), info.LastWorld);
            }
        }

        #endregion
        #region ClosestPlayer

        static readonly CommandDescriptor Cdclp = new CommandDescriptor
        {
            Name = "ClosestPlayer",
            Aliases = new[] { "clp" },
            Permissions = new[] { Permission.Chat },
            Category = CommandCategory.New | CommandCategory.Info,
            Help = "Tells you who is closest to you, and how many blocks away they are.",
            Handler = clpHandler
        };

        static void clpHandler(Player player, CommandReader cmd)
        {
            var all = player.World.Players.Where(z => z != player && !z.Info.IsHidden).OrderBy(p => player.Position.DistanceSquaredTo(p.Position));
            if (all.Count() != 0)
            {
                player.Message(all.Take(1).JoinToString((r => string.Format("Closest: {0} ({1:N0} Blocks)", r.Name, (Math.Sqrt(player.Position.DistanceSquaredTo(r.Position)) / 32) / 1))));
            }
            else
            {
                player.Message("There is no one near you.");
            }
        }

		#endregion
		#region Plugins
		static readonly CommandDescriptor CdPlugin = new CommandDescriptor {
			Name = "Plugins",
			Aliases = new[] { "plugin" },
			Category = CommandCategory.Info | CommandCategory.New,
			Permissions = new Permission[] { Permission.Chat },
			IsConsoleSafe = true,
			Usage = "/Plugins",
			Help = "Displays all plugins on the server.",
			Handler = PluginsHandler
		};

		static void PluginsHandler(Player player, CommandReader cmd) {
			List<string> plugins = new List<string>();
			player.Message("&c_Current plugins on {0}&c_", ConfigKey.ServerName.GetString());

			//Sloppy :P, PluginManager.Plugins adds ".Init", so this should split the ".Init" from the plugin name
			foreach (Plugin plugin in PluginManager.Plugins) {
				String pluginString = plugin.ToString();
				string[] splitPluginString = pluginString.Split('.');
				plugins.Add(splitPluginString[0]);
			}
			player.Message(String.Join(", ", plugins.ToArray()));
		}
		#endregion
        #region PingList

        static readonly CommandDescriptor CdPingList = new CommandDescriptor {
            Name = "PingList",
            Aliases = new[] { "Ping", "Latency" },
            Category = CommandCategory.New | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/PingList",
            Help = "Lists all players and their ping latency value",
            Handler = PingListHandler
        };

        static void PingListHandler(Player player, CommandReader cmd) {
            string offsetStr = cmd.Next();
            string value;
            int offset = 0;
            if (!int.TryParse(offsetStr, out offset)) {
                offset = 0;
            }
            Player[] visiblePlayers = Server.Players.Where(p => p.PingList.Average() != 0 && player.CanSee(p)).OrderBy(p => p.PingList.Average()).Reverse().ToArray();
            if (visiblePlayers.Count() < 1) {
                player.Message("No players online right now");
                return;
            }
            Player[] playerList = visiblePlayers.Skip(fixOffset(offset, visiblePlayers.Count())).Take(10).ToArray();
            int pad = string.Format("Ping: {0}ms Avg: {1:N0}ms", playerList[0].PingList[9], playerList[0].PingList.Average()).Length;
            player.Message("Ping/Latency List:");
            for (int i = 0; i < playerList.Count(); i++) {
                value = string.Format("Ping: {0}ms Avg: {1:N0}ms", playerList[i].PingList[9], playerList[i].PingList.Average());
                player.Message(" &7{1}&s - {0}", playerList[i].Info.ClassyName, value.PadLeft(pad, '0'));
            }
            player.Message("Showing players {0}-{1} (out of {2}).", offset + 1, offset + playerList.Length, visiblePlayers.Count());
        }
        
        static int fixOffset(int origOffset, int allPlayerCount) {
            if (origOffset >= allPlayerCount) {
                return Math.Max(0, allPlayerCount - 10);
            }
            return origOffset;
        }

        #endregion
        #region FindPlayerInfo
        public static PlayerInfo FindPlayerInfo(Player player, CommandReader cmd, bool canBeOffline) {
			string name = cmd.Next();

            if (string.IsNullOrEmpty(name)) {
                // no name given, print own info
                return player.Info;
            }

            if (name.ToLower().Equals(player.Name.ToLower())) {
                // own name given
                player.LastUsedPlayerName = player.Name;
                return player.Info;

            }

            if (!player.Can(Permission.ViewOthersInfo)) {
                // someone else's name or IP given, permission required.
                player.MessageNoAccess(Permission.ViewOthersInfo);
                return null;
            }

            // repeat last-typed name
            if (name == "-") {
                if (player.LastUsedPlayerName != null) {
                    name = player.LastUsedPlayerName;
                } else {
                    player.Message("Cannot repeat player name: you haven't used any names yet.");
                    return null;
                }
            }

            PlayerInfo[] infos;
            IPAddress ip;

            if (name.Contains("/")) {
                // IP range matching (CIDR notation)
                string ipString = name.Substring(0, name.IndexOf('/'));
                string rangeString = name.Substring(name.IndexOf('/') + 1);
                byte range;
                if (IPAddressUtil.IsIP(ipString) && IPAddress.TryParse(ipString, out ip) &&
                    Byte.TryParse(rangeString, out range) && range <= 32) {
                    player.Message("Searching {0}-{1}", ip.RangeMin(range), ip.RangeMax(range));
                    infos = PlayerDB.FindPlayersCidr(ip, range);
                } else {
                    player.Message("Info: Invalid IP range format. Use CIDR notation.");
                    return null;
                }

            } else if (IPAddressUtil.IsIP(name) && IPAddress.TryParse(name, out ip)) {
                // find players by IP
                infos = PlayerDB.FindPlayers(ip);

            } else if (name.Equals("*")) {
                infos = (PlayerInfo[])PlayerDB.PlayerInfoList.Clone();

            } else if (name.Contains("*") || name.Contains("?")) {
                // find players by regex/wildcard
                string regexString = "^" + RegexNonNameChars.Replace(name, "").Replace("*", ".*").Replace("?", ".") + "$";
                Regex regex = new Regex(regexString, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                infos = PlayerDB.FindPlayers(regex);

            } else if (name.StartsWith("@")) {
                string rankName = name.Substring(1);
                Rank rank = RankManager.FindRank(rankName);
                if (rank == null) {
                    player.MessageNoRank(rankName);
                    return null;
                }
                infos = PlayerDB.PlayerInfoList
                    .Where(info => info.Rank == rank)
                    .ToArray();
            } else if (name.StartsWith("!")) {
                // find online players by partial matches
                name = name.Substring(1);
                infos = Server.FindPlayers(player, name, SearchOptions.IncludeSelf)
                              .Select(p => p.Info)
                              .ToArray();
            } else {
                // find players by partial matching
                PlayerInfo tempInfo;
                if (!PlayerDB.FindPlayerInfo(name, out tempInfo)) {
                    infos = PlayerDB.FindPlayers(name);
                } else if (tempInfo == null) {
                    player.MessageNoPlayer(name);
                    return null;
                } else {
                    infos = new[] { tempInfo };
                }
            }

            infos = (canBeOffline ? infos : infos.Where(p => p.IsOnline).ToArray()); //Reduce to only online players is specified

            Array.Sort(infos, new PlayerInfoComparer(player));

            if (infos.Length == 1) {
                // only one match found; print it right away
                player.LastUsedPlayerName = infos[0].Name;
                return infos[0];

            }
            if (infos.Length > 1) {
                // multiple matches found
                if (infos.Length <= PlayersPerPage) {
                    // all fit to one page
                    player.MessageManyMatches("player", infos);

                } else {
                    // pagination
                    int offset;
                    if (!cmd.NextInt(out offset))
                        offset = 0;
                    if (offset >= infos.Length) {
                        offset = Math.Max(0, infos.Length - PlayersPerPage);
                    }
                    PlayerInfo[] infosPart = infos.Skip(offset).Take(PlayersPerPage).ToArray();
                    player.MessageManyMatches("player", infosPart);
                    if (offset + infosPart.Length < infos.Length) {
                        // normal page
                        player.Message("Showing {0}-{1} (out of {2}). Next: &H/Info {3} {4}",
                                        offset + 1, offset + infosPart.Length, infos.Length,
                                        name, offset + infosPart.Length);
                    } else {
                        // last page
                        player.Message("Showing matches {0}-{1} (out of {2}).",
                                        offset + 1, offset + infosPart.Length, infos.Length);
                    }
                }

            } else {
                // no matches found
                player.MessageNoPlayer(name);
            }
            return null;
        }
        #endregion 
    }
}

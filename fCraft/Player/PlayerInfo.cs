// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Object representing persistent state ("record") of a player, online or offline.
    /// There is exactly one PlayerInfo object for each known Minecraft account. All data is stored in the PlayerDB. </summary>
    public sealed partial class PlayerInfo : IClassy {
        internal const int MinFieldCount = 24;


        /// <summary> Player's Minecraft account name. </summary>
        [NotNull]
        public string Name { get; internal set; }

        public string ClassyName {
            get {
                StringBuilder sb = new StringBuilder();
                if( ConfigKey.RankColorsInChat.Enabled() ) {
                    sb.Append( Rank.Color );
                }
                if( DisplayedName != null ) {
                    sb.Append( DisplayedName );
                } else {
                    if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                        sb.Append( Rank.Prefix );
                    }
                    sb.Append( Name );
                }
                if( IsBanned ) {
                    sb.Append( Color.Red ).Append( '*' );
                }
                if( IsFrozen ) {
                    sb.Append( Color.Blue ).Append( '*' );
                }
                return sb.ToString();
            }
        }

        /// <summary> If set, replaces Name when printing name in chat. </summary>
        [CanBeNull]
        public string DisplayedName;

        [CanBeNull]
        public string Email;

        public DateTime LastServerMessageDate;
        public Player LastPlayerGreeted = Player.Console;

        public void getLeftOverTime(Double maxTime, CommandReader cmd) {
            double timeLeft = maxTime - Math.Round(TimeSinceLastServerMessage.TotalSeconds);
            if (timeLeft == 1) {
                PlayerObject.Message("&WYou can use /" + cmd.Name + "again in 1 second.");
                return;
            }
            PlayerObject.Message("&WYou can use /" + cmd.Name + " again in " + timeLeft + " seconds");
        }

        /// <summary> Player's unique numeric ID. Issued on first join. </summary>
        public int ID;

        /// <summary> First time the player ever logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime FirstLoginDate;


        /// <summary> Whether the player is ignoring IRC Messages.
        /// Boolean On or Off.</summary>
        [NotNull]
        public Boolean ReadIRC = true;

        /// <summary> Whether the player is ignoring IRC Messages.
        /// Boolean On or Off.</summary>
        [NotNull]
        public Boolean WriteIRC;

        /// <summary> Whether the player is ignoring teleport requests.</summary>
        [NotNull]
        public Boolean TPDeny;

        /// <summary> The block the player currently has in their hand.</summary>
        public Block heldBlock = Block.Stone;

        /// <summary> Most recent time the player logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastLoginDate;

        /// <summary> Last time the player has been seen online (last logout), UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastSeen;

        public int PromoCount;
        public int DemoCount;
        public int TimesUsedBot;
		public string LastMotdMessage;
        //dummy

        /// <summary> Reason for leaving the server last time. </summary>
        public LeaveReason LeaveReason;

        /// <summary> Minecraft.net account type (paid/free/unknown). </summary>
        public AccountType AccountType;

        /// <summary> Begins to asynchronously check player's account type. </summary>
        public void CheckAccountType() {
            if( AccountType != AccountType.Paid && ConfigKey.HeartbeatUrl.ToString().ToLower().Contains("minecraft.net")) {
                Scheduler.NewBackgroundTask( CheckPaidStatusCallback ).RunOnce( this, TimeSpan.Zero );
            }
        }


        static void CheckPaidStatusCallback( SchedulerTask task ) {
            PlayerInfo info = (PlayerInfo)task.UserState;
            info.AccountType = Player.CheckPaidStatus( info.Name );
        }

        /// <summary> Begins to asynchronously check player's account type. </summary>
        public void GeoipLogin() {
            String ip = LastIP.ToString();
            if (IPAddress.Parse(ip).IsLocal()) {
                ip = Server.ExternalIP.ToString();
            }
			if (ip != GeoIP || Accuracy == 0) {
                Scheduler.NewBackgroundTask(GeoipLoginCallback).RunOnce(this, TimeSpan.Zero);
            } else {
                DisplayGeoIp();
            }
        }


        public void GeoipLoginCallback( SchedulerTask task ) {
            PlayerInfo info = (PlayerInfo)task.UserState;
			InfoCommands.GetGeoip(info);
			DisplayGeoIp();
        }

        /// <summary>
        /// Displayes the GeoIP information to the server.
        /// </summary>
        public void DisplayGeoIp() {
            if (PlayerObject != null) {
                string comesFrom = string.Format("&f{0}&2 comes from {1}", PlayerObject.ListName,
					CountryName);
				string comesFromIRC = string.Format("\u212C&f{0}\u211C&2 comes from \u212C{1}", PlayerObject.ListName,
					CountryName);
                Server.Players.CanSee(PlayerObject).Message(comesFrom);
                PlayerObject.Message(comesFrom);
                if (!IsHidden) {
                    IRC.SendChannelMessage(comesFromIRC);
                }
                Logger.Log(LogType.UserActivity, comesFrom);
            }
        }
        
        /// <summary> Used when a classicube.net account is created that is the same as an account in existing player DB,
        /// where the older account has not signed in before Jan 1 2014. Minmises chance of impersonation. </summary>
        public bool ClassicubeVerified = true;


        public List<int> PingList = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


        #region Rank

        /// <summary> Player's current rank. </summary>
        [NotNull]
        public Rank Rank { get; internal set; }

        /// <summary> Player's previous rank.
        /// May be null if player has never been promoted/demoted before. </summary>
        [CanBeNull]
        public Rank PreviousRank;

        /// <summary> Date of the most recent promotion/demotion, UTC.
        /// May be DateTime.MinValue if player has never been promoted/demoted before. </summary>
        public DateTime RankChangeDate;

        /// <summary> Name of the entity that most recently promoted/demoted this player. May be empty. </summary>
        [CanBeNull]
        public string RankChangedBy;

        [NotNull]
        public string RankChangedByClassy {
            get {
                return PlayerDB.FindExactClassyName( RankChangedBy );
            }
        }

        /// <summary> Reason given for the most recent promotion/demotion. May be empty. </summary>
        [CanBeNull]
        public string RankChangeReason;

        /// <summary> Type of the most recent promotion/demotion. </summary>
        public RankChangeType RankChangeType;

        #endregion


        #region Bans

        /// <summary> Player's current BanStatus: Banned, NotBanned, or Exempt. </summary>
        public BanStatus BanStatus;

        /// <summary> Returns whether player is name-banned or not. </summary>
        public bool IsBanned {
            get { return BanStatus == BanStatus.Banned; }
        }

        /// <summary> Date of most recent ban, UTC. May be DateTime.MinValue if player was never banned. </summary>
        public DateTime BanDate;

        /// <summary> Name of the entity responsible for most recent ban. May be empty. </summary>
        [CanBeNull]
        public string BannedBy;

        [NotNull]
        public string BannedByClassy {
            get {
                return PlayerDB.FindExactClassyName( BannedBy );
            }
        }

        /// <summary> Reason given for the most recent ban. May be empty. </summary>
        [CanBeNull]
        public string BanReason;

        /// <summary> Date of most recent unban, UTC. May be DateTime.MinValue if player was never unbanned. </summary>
        public DateTime UnbanDate;

        /// <summary> Name of the entity responsible for most recent unban. May be empty. </summary>
        [CanBeNull]
        public string UnbannedBy;

        [NotNull]
        public string UnbannedByClassy {
            get {
                return PlayerDB.FindExactClassyName( UnbannedBy );
            }
        }

        /// <summary> Reason given for the most recent unban. May be empty. </summary>
        [CanBeNull]
        public string UnbanReason;

        /// <summary> Date of most recent failed attempt to log in, UTC. </summary>
        public DateTime LastFailedLoginDate;

        /// <summary> IP from which player most recently tried (and failed) to log in, UTC. </summary>
        [NotNull]
        public IPAddress LastFailedLoginIP = IPAddress.None;

        /// <summary> Date until which the player is untempbanned. If the date is in the past, player is NOT tempbanned. </summary>
        public DateTime BannedUntil;

        #endregion


        #region Stats

        /// <summary> Total amount of time the player spent on this server. </summary>
        public TimeSpan TotalTime;

        /// <summary> Total number of blocks manually built or painted by the player. </summary>
        public int BlocksBuilt;

        /// <summary> Total number of blocks manually deleted by the player. </summary>
        public int BlocksDeleted;

        /// <summary> How Many Blocks player has deleted this session. </summary>
        public int BlocksDeletedThisGame = 0;

        /// <summary> How Many Blocks player has placed this session. </summary>
        public int BlocksBuiltThisGame = 0;

        /// <summary> Total number of blocks modified using draw and copy/paste commands. </summary>
        public long BlocksDrawn;

        /// <summary> Number of sessions/logins. </summary>
        public int TimesVisited;

        /// <summary> Total number of messages written. </summary>
        public int MessagesWritten;

        /// <summary> Number of kicks issues by this player. </summary>
        public int TimesKickedOthers;

        /// <summary> Number of bans issued by this player. </summary>
        public int TimesBannedOthers;

        /// <summary> How much money the player has donated to the server. </summary>
        public double Donated { get; set; }

        /// <summary> How Many Blocks player has placed this session. </summary>
        public bool SecurityTrip { get; set; }

        #endregion


        #region Kicks

        /// <summary> Number of times that this player has been manually kicked. </summary>
        public int TimesKicked;

        /// <summary> Date of the most recent kick.
        /// May be DateTime.MinValue if the player has never been kicked. </summary>
        public DateTime LastKickDate;

        /// <summary> Name of the entity that most recently kicked this player. May be empty. </summary>
        [CanBeNull]
        public string LastKickBy;

        [NotNull]
        public string LastKickByClassy {
            get {
                return PlayerDB.FindExactClassyName( LastKickBy );
            }
        }

        /// <summary> Reason given for the most recent kick. May be empty. </summary>
        [CanBeNull]
        public string LastKickReason;

        #endregion


        #region Freeze And Mute

        /// <summary> Whether this player is currently frozen. </summary>
        public bool IsFrozen;

        /// <summary> Date of the most recent freezing.
        /// May be DateTime.MinValue of the player has never been frozen. </summary>
        public DateTime FrozenOn;

        /// <summary> Name of the entity that most recently froze this player. May be empty. </summary>
        [CanBeNull]
        public string FrozenBy;

        [NotNull]
        public string FrozenByClassy {
            get {
                return PlayerDB.FindExactClassyName( FrozenBy );
            }
        }

        /// <summary> Whether this player is currently muted. </summary>
        public bool IsMuted {
            get {
                return DateTime.UtcNow < MutedUntil;
            }
        }

        /// <summary> Date until which the player is muted. If the date is in the past, player is NOT muted. </summary>
        public DateTime MutedUntil;

        /// <summary> Name of the entity that most recently muted this player. May be empty. </summary>
        [CanBeNull]
        public string MutedBy;

        [NotNull]
        public string MutedByClassy {
            get {
                return PlayerDB.FindExactClassyName( MutedBy );
            }
        }        

        #endregion


        /// <summary> Whether the player is currently online.
        /// Another way to check online status is to check if PlayerObject is null. </summary>
        public bool IsOnline { get; private set; }

        /// <summary> If player is online, Player object associated with the session.
        /// If player is offline, null. </summary>
        [CanBeNull]
        public Player PlayerObject { get; private set; }

        /// <summary> Whether the player is currently hidden.
        /// Use Player.CanSee() method to check visibility to specific observers. </summary>
        public bool IsHidden;

        /// <summary> Whether the player is currently afk. </summary>
        public bool IsAFK;

        //public short NameID;
        //
        //public byte EntityID;

        public string oldMob = "Humanoid";
        public string Mob = "Humanoid";
        public string oldafkMob = "Humanoid";
        public string afkMob = "Humanoid";
        public string oldskinName = "";
        public string skinName = "";
        
        public short ReachDistance = 160;

        /// <summary> Control whether player can fly using HackControl packet. </summary>
        public bool AllowFlying = true;

        /// <summary> Control whether player can use noclip using HackControl packet. </summary>
        public bool AllowNoClip = true;

        /// <summary> Control whether player can speedhack using HackControl packet. </summary>
        public bool AllowSpeedhack = true;

        /// <summary> Control whether player can use "r" to respawn using HackControl packet. </summary>
        public bool AllowRespawn = true;

        /// <summary> Control whether player can use third person view using HackControl packet. </summary>
        public bool AllowThirdPerson = true;

        /// <summary> Control player jump height using HackControl packet. </summary>
        public short JumpHeight = 40;

        /// <summary> Whether when placing tnt explodes or not. </summary>
        public bool BlowTNT;

        /// <summary> Whether the player's chat is rainbow. </summary>
		public bool ChatRainbows;

		/// <summary> Whether the player's chat is bwrainbow. </summary>
		public bool ChatBWRainbows;

        /// <summary> Whether player has read the rules or not.</summary>
        public bool HasRTR;

        /// <summary> The world the player was last building on.</summary>
        public string LastWorld;

        /// <summary> The position player was last building on.</summary>
        public string LastWorldPos;

        /// <summary> Player's position in the current world. </summary>
        public Position CheckPoint = new Position(-1, -1, -1);

        /// <summary> Whether player wants to spawn on their rank world.</summary>
        public bool JoinOnRankWorld;

        /// <summary> For offline players, last IP used to succesfully log in.
        /// For online players, current IP. </summary>
        [NotNull]
        public IPAddress LastIP;

        #region Geoip
        /// <summary> Players IP address during the last geoip lookup</summary>
        public string GeoIP;
        /// <summary> Players country code based on geoip</summary>
        public string CountryCode;
        /// <summary> Players country name based on geoip</summary>
        public string CountryName;
        /// <summary> Players time zone based on geoip</summary>
        public string TimeZone;
        /// <summary> Players latitude based on geoip</summary>
        public string Latitude;
        /// <summary> Players longitude based on geoip</summary>
		public string Longitude;
		/// <summary> List of subdivisions (City, State, etc) sorting by accuracy from left to right. </summary>
		public string[] Subdivision = new string[] {"NA"};
		/// <summary> Players geoip accuracy</summary>
		public byte Accuracy = 0;
		/// <summary> Players hostname</summary>
		public string Hostname;
		/// <summary> Players continent</summary>
		public string Continent;
        #endregion


        #region Constructors and Serialization

        internal PlayerInfo( int id ) {
            ID = id;
        }

        PlayerInfo() {
            // reset everything to defaults
            LastIP = IPAddress.None;
            RankChangeDate = DateTime.MinValue;
            BanDate = DateTime.MinValue;
            UnbanDate = DateTime.MinValue;
            LastFailedLoginDate = DateTime.MinValue;
            FirstLoginDate = DateTime.MinValue;
            LastLoginDate = DateTime.MinValue;
            TotalTime = TimeSpan.Zero;
            RankChangeType = RankChangeType.Default;
            LastKickDate = DateTime.MinValue;
            LastSeen = DateTime.MinValue;
            BannedUntil = DateTime.MinValue;
            FrozenOn = DateTime.MinValue;
            MutedUntil = DateTime.MinValue;
            BandwidthUseMode = BandwidthUseMode.Default;
            LastModified = DateTime.UtcNow;
        }

        // fabricate info for an unrecognized player
        public PlayerInfo( [NotNull] string name, [NotNull] Rank rank,
                           bool setLoginDate, RankChangeType rankChangeType )
            : this() {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            Name = name;
            Rank = rank;
            if( setLoginDate ) {
                FirstLoginDate = DateTime.UtcNow;
                LastLoginDate = FirstLoginDate;
                LastSeen = FirstLoginDate;
                TimesVisited = 1;
            }
            RankChangeType = rankChangeType;
        }


        // generate blank info for a new player
        public PlayerInfo( [NotNull] string name, [NotNull] IPAddress lastIP, [NotNull] Rank startingRank )
            : this() {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            FirstLoginDate = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            LastLoginDate = DateTime.UtcNow;
            Rank = startingRank;
            Name = name;
            ID = PlayerDB.GetNextID();
            LastIP = lastIP;
        }

        #endregion


        #region Loading

        internal static PlayerInfo LoadFormat2( string[] fields ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) {
                info.LastIP = IPAddress.None;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTime( ref info.RankChangeDate );
            if( fields[4].Length > 0 ) info.RankChangedBy = fields[4];

            switch( fields[5] ) {
                case "b":
                    info.BanStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.BanStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.BanStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( fields[6].ToDateTime( ref info.BanDate ) ) {
                if( fields[7].Length > 0 ) info.BannedBy = PlayerDB.Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.BanReason = PlayerDB.Unescape( fields[10] );
            }

            // unban information
            if( fields[8].ToDateTime( ref info.UnbanDate ) ) {
                if( fields[9].Length > 0 ) info.UnbannedBy = PlayerDB.Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.UnbanReason = PlayerDB.Unescape( fields[11] );
            }

            // failed logins
            fields[12].ToDateTime( ref info.LastFailedLoginDate );

            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) { // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            // skip 14

            fields[15].ToDateTime( ref info.FirstLoginDate );

            // login/logout times
            fields[16].ToDateTime( ref info.LastLoginDate );
            fields[17].ToTimeSpan( out info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.MessagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.RankChangeReason = PlayerDB.Unescape( fields[25] );
            Int32.TryParse( fields[26], out info.TimesKicked );
            Int32.TryParse( fields[27], out info.TimesKickedOthers );
            Int32.TryParse( fields[28], out info.TimesBannedOthers );

            info.ID = Int32.Parse( fields[29] );
            if( info.ID < 256 )
                info.ID = PlayerDB.GetNextID();

            byte rankChangeTypeCode;
            if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    info.GuessRankChangeType();
                }
            } else {
                info.GuessRankChangeType();
            }

            fields[31].ToDateTime( ref info.LastKickDate );
            if( !fields[32].ToDateTime( ref info.LastSeen ) || info.LastSeen < info.LastLoginDate ) {
                info.LastSeen = info.LastLoginDate;
            }
            Int64.TryParse( fields[33], out info.BlocksDrawn );

            if( fields[34].Length > 0 ) info.LastKickBy = PlayerDB.Unescape( fields[34] );
            if( fields[35].Length > 0 ) info.LastKickReason = PlayerDB.Unescape( fields[35] );

            fields[36].ToDateTime( ref info.BannedUntil );
            info.IsFrozen = (fields[37] == "f");
            if( fields[38].Length > 0 ) info.FrozenBy = PlayerDB.Unescape( fields[38] );
            fields[39].ToDateTime( ref info.FrozenOn );
            fields[40].ToDateTime( ref info.MutedUntil );
            if( fields[41].Length > 0 ) info.MutedBy = PlayerDB.Unescape( fields[41] );
            info.Password = PlayerDB.Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            byte bandwidthUseModeCode;
            if( Byte.TryParse( fields[44], out bandwidthUseModeCode ) ) {
                info.BandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.BandwidthUseMode = BandwidthUseMode.Default;
                }
            } else {
                info.BandwidthUseMode = BandwidthUseMode.Default;
            }

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.IsHidden = false;
                } else {
                    info.IsHidden = info.Rank.Can( Permission.Hide );
                }
            }
            if( fields.Length > 46 ) {
                fields[46].ToDateTime( ref info.LastModified );
            }
            if( fields.Length > 47 && fields[47].Length > 0 ) {
                info.DisplayedName = PlayerDB.Unescape( fields[47] );
            }
            if( fields.Length > 48 ) {
                byte accountTypeCode;
                if( Byte.TryParse( fields[48], out accountTypeCode ) ) {
                    info.AccountType = (AccountType)accountTypeCode;
                    if( !Enum.IsDefined( typeof( AccountType ), accountTypeCode ) ) {
                        info.AccountType = AccountType.Unknown;
                    }
                }
            }
            if (fields.Length > 49)
            {
                double donatedammount;
                if (Double.TryParse(fields[49], out donatedammount))
                {
                    info.Donated = donatedammount;
                }
                else info.Donated = 0;
            }
            if (fields.Length > 50)
            {
                if (!Boolean.TryParse(fields[50], out info.ReadIRC))
                {
                    info.ReadIRC = true;
                }
            }
            if (fields.Length > 51)
            {
                Int32.TryParse(fields[51], out info.PromoCount);
            }
            if (fields.Length > 52)
            {
                Int32.TryParse(fields[52], out info.TimesUsedBot);
            }
            if (fields.Length > 53)
            {
                //Int32.TryParse(fields[53], out info.TimesUsedUseless);
            }
            if (fields.Length > 54)
            {
                if (!Boolean.TryParse(fields[54], out info.HasRTR))
                {
                    info.HasRTR = false;
                }
            }
            if (fields.Length > 55)
            {
                if (!Boolean.TryParse(fields[55], out info.TPDeny))
                {
                    info.TPDeny = false;
                }
            }
            if (fields.Length > 56)
            {
                if (!Boolean.TryParse(fields[56], out info.JoinOnRankWorld))
                {
                    info.JoinOnRankWorld = false;
                }
            }
            if (fields.Length > 57) info.LastWorld = fields[57];

            if (fields.Length > 58) info.LastWorldPos = fields[58];

            if (fields.Length > 59)
            {
                Int32.TryParse(fields[59], out info.DemoCount);
            }

            if (fields.Length > 60) info.Mob = fields[60];

            if (fields.Length > 61)
            {
                if (!short.TryParse(fields[61], out info.ReachDistance))
                {
                    info.ReachDistance = 160;
                }
            }
            if (fields.Length > 62 && fields[62].Length > 0)
            {
                info.Email = PlayerDB.Unescape(fields[62]);
            }
            if (fields.Length > 63)
            {
                if (!Boolean.TryParse(fields[63], out info.AllowFlying))
                {
                    info.AllowFlying = true;
                }
            }
            if (fields.Length > 64)
            {
                if (!Boolean.TryParse(fields[64], out info.AllowNoClip))
                {
                    info.AllowNoClip = true;
                }
            }
            if (fields.Length > 65)
            {
                if (!Boolean.TryParse(fields[65], out info.AllowSpeedhack))
                {
                    info.AllowSpeedhack = true;
                }
            }
            if (fields.Length > 66)
            {
                if (!Boolean.TryParse(fields[66], out info.AllowRespawn))
                {
                    info.AllowRespawn = true;
                }
            }
            if (fields.Length > 67)
            {
                if (!Boolean.TryParse(fields[67], out info.AllowThirdPerson))
                {
                    info.AllowThirdPerson = true;
                }
            }
            if (fields.Length > 68)
            {
                short.TryParse(fields[68], out info.JumpHeight);
            }

            if (fields.Length > 69)
                info.GeoIP = fields[69];
            if (fields.Length > 70)
                info.CountryCode = fields[70];
            if (fields.Length > 71)
                info.CountryName = fields[71];
            //if (fields.Length > 72) info.RegionCode = fields[72];
            //if (fields.Length > 73) info.RegionName = fields[73];
            //if (fields.Length > 74) info.City = fields[74];
            //if (fields.Length > 75) info.ZipCode = fields[75];
            if (fields.Length > 76)
                info.Latitude = fields[76];
            if (fields.Length > 77)
                info.Longitude = fields[77];
            //if (fields.Length > 78) info.MetroCode = fields[78];
            //if (fields.Length > 79) info.AreaCode = fields[79];
            if (fields.Length > 80)
                info.TimeZone = fields[80];

            if (fields.Length > 81)
				info.skinName = fields[81];

			if (fields.Length > 82)
				info.Subdivision = PlayerDB.Unescape(fields[82]).Split();
			if (fields.Length > 83)
				byte.TryParse(fields[83], out info.Accuracy);
			if (fields.Length > 84)
				info.Hostname = fields[84];
			if (fields.Length > 85)
				info.Continent = fields[85];
			if (fields.Length > 86) {
				if (!bool.TryParse(fields[86], out info.ClassicubeVerified))
					info.ClassicubeVerified = true;
			}

            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadFormat1( string[] fields ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) {
                info.LastIP = IPAddress.None;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTimeLegacy( ref info.RankChangeDate );
            if( fields[4].Length > 0 ) info.RankChangedBy = fields[4];

            switch( fields[5] ) {
                case "b":
                    info.BanStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.BanStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.BanStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( fields[6].ToDateTimeLegacy( ref info.BanDate ) ) {
                if( fields[7].Length > 0 ) info.BannedBy = PlayerDB.Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.BanReason = PlayerDB.Unescape( fields[10] );
            }

            // unban information
            if( fields[8].ToDateTimeLegacy( ref info.UnbanDate ) ) {
                if( fields[9].Length > 0 ) info.UnbannedBy = PlayerDB.Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.UnbanReason = PlayerDB.Unescape( fields[11] );
            }

            // failed logins
            fields[12].ToDateTimeLegacy( ref info.LastFailedLoginDate );

            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) { // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            // skip 14
            fields[15].ToDateTimeLegacy( ref info.FirstLoginDate );

            // login/logout times
            fields[16].ToDateTimeLegacy( ref info.LastLoginDate );
            fields[17].ToTimeSpanLegacy( ref info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.MessagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.RankChangeReason = PlayerDB.Unescape( fields[25] );
            Int32.TryParse( fields[26], out info.TimesKicked );
            Int32.TryParse( fields[27], out info.TimesKickedOthers );
            Int32.TryParse( fields[28], out info.TimesBannedOthers );

            info.ID = Int32.Parse( fields[29] );
            if( info.ID < 256 )
                info.ID = PlayerDB.GetNextID();

            byte rankChangeTypeCode;
            if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    info.GuessRankChangeType();
                }
            } else {
                info.GuessRankChangeType();
            }

            fields[31].ToDateTimeLegacy( ref info.LastKickDate );
            if( !fields[32].ToDateTimeLegacy( ref info.LastSeen ) || info.LastSeen < info.LastLoginDate ) {
                info.LastSeen = info.LastLoginDate;
            }
            Int64.TryParse( fields[33], out info.BlocksDrawn );

            if( fields[34].Length > 0 ) info.LastKickBy = PlayerDB.Unescape( fields[34] );
            if( fields[34].Length > 0 ) info.LastKickReason = PlayerDB.Unescape( fields[35] );

            fields[36].ToDateTimeLegacy( ref info.BannedUntil );
            info.IsFrozen = (fields[37] == "f");
            if( fields[38].Length > 0 ) info.FrozenBy = PlayerDB.Unescape( fields[38] );
            fields[39].ToDateTimeLegacy( ref info.FrozenOn );
            fields[40].ToDateTimeLegacy( ref info.MutedUntil );
            if( fields[41].Length > 0 ) info.MutedBy = PlayerDB.Unescape( fields[41] );
            info.Password = PlayerDB.Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            byte bandwidthUseModeCode;
            if( Byte.TryParse( fields[44], out bandwidthUseModeCode ) ) {
                info.BandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.BandwidthUseMode = BandwidthUseMode.Default;
                }
            } else {
                info.BandwidthUseMode = BandwidthUseMode.Default;
            }

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.IsHidden = false;
                } else {
                    info.IsHidden = info.Rank.Can( Permission.Hide );
                }
            }

            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadFormat0( string[] fields, bool convertDatesToUtc ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) {
                info.LastIP = IPAddress.None;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            DateTimeUtil.TryParseLocalDate( fields[3], out info.RankChangeDate );
            if( fields[4].Length > 0 ) {
                info.RankChangedBy = fields[4];
                if( info.RankChangedBy == "-" ) info.RankChangedBy = null;
            }

            switch( fields[5] ) {
                case "b":
                    info.BanStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.BanStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.BanStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( DateTimeUtil.TryParseLocalDate( fields[6], out info.BanDate ) ) {
                if( fields[7].Length > 0 ) info.BannedBy = fields[7];
                if( fields[10].Length > 0 ) {
                    info.BanReason = PlayerDB.UnescapeOldFormat( fields[10] );
                    if( info.BanReason == "-" ) info.BanReason = null;
                }
            }

            // unban information
            if( DateTimeUtil.TryParseLocalDate( fields[8], out info.UnbanDate ) ) {
                if( fields[9].Length > 0 ) info.UnbannedBy = fields[9];
                if( fields[11].Length > 0 ) {
                    info.UnbanReason = PlayerDB.UnescapeOldFormat( fields[11] );
                    if( info.UnbanReason == "-" ) info.UnbanReason = null;
                }
            }

            // failed logins
            if( fields[12].Length > 1 ) {
                DateTimeUtil.TryParseLocalDate( fields[12], out info.LastFailedLoginDate );
            }
            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) { // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            // skip 14

            // login/logout times
            DateTimeUtil.TryParseLocalDate( fields[15], out info.FirstLoginDate );
            DateTimeUtil.TryParseLocalDate( fields[16], out info.LastLoginDate );
            TimeSpan.TryParse( fields[17], out info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.MessagesWritten );
            // fields 22-23 are no longer in use

            if( fields.Length > MinFieldCount ) {
                if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
                if( fields[25].Length > 0 ) info.RankChangeReason = PlayerDB.UnescapeOldFormat( fields[25] );
                Int32.TryParse( fields[26], out info.TimesKicked );
                Int32.TryParse( fields[27], out info.TimesKickedOthers );
                Int32.TryParse( fields[28], out info.TimesBannedOthers );
                if( fields.Length > 29 ) {
                    info.ID = Int32.Parse( fields[29] );
                    if( info.ID < 256 )
                        info.ID = PlayerDB.GetNextID();
                    byte rankChangeTypeCode;
                    if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                        info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                        if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                            info.GuessRankChangeType();
                        }
                    } else {
                        info.GuessRankChangeType();
                    }
                    DateTimeUtil.TryParseLocalDate( fields[31], out info.LastKickDate );
                    if( !DateTimeUtil.TryParseLocalDate( fields[32], out info.LastSeen ) || info.LastSeen < info.LastLoginDate ) {
                        info.LastSeen = info.LastLoginDate;
                    }
                    Int64.TryParse( fields[33], out info.BlocksDrawn );

                    if( fields[34].Length > 0 ) info.LastKickBy = PlayerDB.UnescapeOldFormat( fields[34] );
                    if( fields[35].Length > 0 ) info.LastKickReason = PlayerDB.UnescapeOldFormat( fields[35] );

                } else {
                    info.ID = PlayerDB.GetNextID();
                    info.GuessRankChangeType();
                    info.LastSeen = info.LastLoginDate;
                }

                if( fields.Length > 36 ) {
                    DateTimeUtil.TryParseLocalDate( fields[36], out info.BannedUntil );
                    info.IsFrozen = (fields[37] == "f");
                    if( fields[38].Length > 0 ) info.FrozenBy = PlayerDB.UnescapeOldFormat( fields[38] );
                    DateTimeUtil.TryParseLocalDate( fields[39], out info.FrozenOn );
                    DateTimeUtil.TryParseLocalDate( fields[40], out info.MutedUntil );
                    if( fields[41].Length > 0 ) info.MutedBy = PlayerDB.UnescapeOldFormat( fields[41] );
                    info.Password = PlayerDB.UnescapeOldFormat( fields[42] );
                    // fields[43] is "online", and is ignored
                }

                if( fields.Length > 44 ) {
                    if( fields[44].Length != 0 ) {
                        info.BandwidthUseMode = (BandwidthUseMode)Int32.Parse( fields[44] );
                    }
                }
            }

            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            if( convertDatesToUtc ) {
                if( info.RankChangeDate != DateTime.MinValue ) info.RankChangeDate = info.RankChangeDate.ToUniversalTime();
                if( info.BanDate != DateTime.MinValue ) info.BanDate = info.BanDate.ToUniversalTime();
                if( info.UnbanDate != DateTime.MinValue ) info.UnbanDate = info.UnbanDate.ToUniversalTime();
                if( info.LastFailedLoginDate != DateTime.MinValue ) info.LastFailedLoginDate = info.LastFailedLoginDate.ToUniversalTime();
                if( info.FirstLoginDate != DateTime.MinValue ) info.FirstLoginDate = info.FirstLoginDate.ToUniversalTime();
                if( info.LastLoginDate != DateTime.MinValue ) info.LastLoginDate = info.LastLoginDate.ToUniversalTime();
                if( info.LastKickDate != DateTime.MinValue ) info.LastKickDate = info.LastKickDate.ToUniversalTime();
                if( info.LastSeen != DateTime.MinValue ) info.LastSeen = info.LastSeen.ToUniversalTime();
                if( info.BannedUntil != DateTime.MinValue ) info.BannedUntil = info.BannedUntil.ToUniversalTime();
                if( info.FrozenOn != DateTime.MinValue ) info.FrozenOn = info.FrozenOn.ToUniversalTime();
                if( info.MutedUntil != DateTime.MinValue ) info.MutedUntil = info.MutedUntil.ToUniversalTime();
            }

            return info;
        }


        void GuessRankChangeType() {
            if( PreviousRank != null ) {
                if( RankChangeReason == "~AutoRank" || RankChangeReason == "~AutoRankAll" || RankChangeReason == "~MassRank" ) {
                    if( PreviousRank > Rank ) {
                        RankChangeType = RankChangeType.AutoDemoted;
                    } else if( PreviousRank < Rank ) {
                        RankChangeType = RankChangeType.AutoPromoted;
                    }
                } else {
                    if( PreviousRank > Rank ) {
                        RankChangeType = RankChangeType.Demoted;
                    } else if( PreviousRank < Rank ) {
                        RankChangeType = RankChangeType.Promoted;
                    }
                }
            } else {
                RankChangeType = RankChangeType.Default;
            }
        }

        #endregion


        #region Saving

        internal void Serialize( StringBuilder sb ) {
            sb.Append( Name ).Append( ',' ); // 0
            if( !LastIP.Equals( IPAddress.None ) ) sb.Append( LastIP ); // 1
            sb.Append( ',' );

            sb.Append( Rank.FullName ).Append( ',' ); // 2
            RankChangeDate.ToUnixTimeString( sb ).Append( ',' ); // 3

            sb.AppendEscaped( RankChangedBy ).Append( ',' ); // 4

            switch( BanStatus ) {
                case BanStatus.Banned:
                    sb.Append( 'b' );
                    break;
                case BanStatus.IPBanExempt:
                    sb.Append( 'x' );
                    break;
            }
            sb.Append( ',' ); // 5

            BanDate.ToUnixTimeString( sb ).Append( ',' ); // 6
            sb.AppendEscaped( BannedBy ).Append( ',' ); // 7
            UnbanDate.ToUnixTimeString( sb ).Append( ',' ); // 8
            sb.AppendEscaped( UnbannedBy ).Append( ',' ); // 9
            sb.AppendEscaped( BanReason ).Append( ',' ); // 10
            sb.AppendEscaped( UnbanReason ).Append( ',' ); // 11

            LastFailedLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 12

            if( !LastFailedLoginIP.Equals( IPAddress.None ) ) sb.Append( LastFailedLoginIP ); // 13
            sb.Append( ',', 2 ); // skip 14

            FirstLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 15
            LastLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 16

            Player pObject = PlayerObject;
            if( pObject != null ) {
                (TotalTime.Add( TimeSinceLastLogin )).ToTickString( sb ).Append( ',' ); // 17
            } else {
                TotalTime.ToTickString( sb ).Append( ',' ); // 17
            }

            if( BlocksBuilt > 0 ) sb.Digits( BlocksBuilt ); // 18
            sb.Append( ',' );

            if( BlocksDeleted > 0 ) sb.Digits( BlocksDeleted ); // 19
            sb.Append( ',' );

            sb.Append( TimesVisited ).Append( ',' ); // 20


            if( MessagesWritten > 0 ) sb.Digits( MessagesWritten ); // 21
            sb.Append( ',', 3 ); // 22-23 no longer in use

            if( PreviousRank != null ) sb.Append( PreviousRank.FullName ); // 24
            sb.Append( ',' );

            sb.AppendEscaped( RankChangeReason ).Append( ',' ); // 25


            if( TimesKicked > 0 ) sb.Digits( TimesKicked ); // 26
            sb.Append( ',' );

            if( TimesKickedOthers > 0 ) sb.Digits( TimesKickedOthers ); // 27
            sb.Append( ',' );

            if( TimesBannedOthers > 0 ) sb.Digits( TimesBannedOthers ); // 28
            sb.Append( ',' );


            sb.Digits( ID ).Append( ',' ); // 29

            sb.Digits( (int)RankChangeType ).Append( ',' ); // 30


            LastKickDate.ToUnixTimeString( sb ).Append( ',' ); // 31

            if( IsOnline ) DateTime.UtcNow.ToUnixTimeString( sb ); // 32
            else LastSeen.ToUnixTimeString( sb );
            sb.Append( ',' );

            if( BlocksDrawn > 0 ) sb.Append( BlocksDrawn ); // 33
            sb.Append( ',' );

            sb.AppendEscaped( LastKickBy ).Append( ',' ); // 34
            sb.AppendEscaped( LastKickReason ).Append( ',' ); // 35

            BannedUntil.ToUnixTimeString(sb); // 36

            if (IsFrozen)
            {
                sb.Append(',').Append('f').Append(','); // 37
                sb.AppendEscaped(FrozenBy).Append(','); // 38
                FrozenOn.ToUnixTimeString(sb).Append(','); // 39
            }
            else
            {
                sb.Append(',', 4); // 37-39
            }

            if( MutedUntil > DateTime.UtcNow ) {
                MutedUntil.ToUnixTimeString( sb ).Append( ',' ); // 40
                sb.AppendEscaped( MutedBy ).Append( ',' ); // 41
            } else {
                sb.Append( ',', 2 ); // 40-41
            }

            sb.AppendEscaped( Password ).Append( ',' ); // 42

            if( IsOnline ) sb.Append( 'o' ); // 43
            sb.Append( ',' );

            if( BandwidthUseMode != BandwidthUseMode.Default ) sb.Append( (int)BandwidthUseMode ); // 44
            sb.Append( ',' );

            if( IsHidden ) sb.Append( 'h' ); // 45
            sb.Append( ',' );

            LastModified.ToUnixTimeString( sb ); // 46
            sb.Append( ',' );

            sb.AppendEscaped( DisplayedName ); // 47
            sb.Append( ',' );

            sb.Append( (byte)AccountType ); // 48
            sb.Append(',');

            sb.Append((double)Donated); // 49
            sb.Append(',');

            sb.Append((bool)ReadIRC); // 50            
            sb.Append(',');

            sb.Append(PromoCount); //51            
            sb.Append(',');

            sb.Append(TimesUsedBot); //52
            sb.Append(',');

            sb.Append(""); //53
            sb.Append(',');

            sb.Append(HasRTR); //54
            sb.Append(',');

            sb.Append(TPDeny); // 55
            sb.Append(',');

            sb.Append(JoinOnRankWorld); // 56
            sb.Append( ',' );

            sb.Append(LastWorld); // 57
            sb.Append(',');

            sb.Append(LastWorldPos); // 58
            sb.Append(',');

            sb.Append(DemoCount); // 59
            sb.Append(',');

            sb.Append(Mob); // 60            
            sb.Append(',');

            sb.Append(ReachDistance); // 61
            sb.Append(',');

            if (Email != null) { sb.Append( Email ); } //62
            sb.Append( ',' );

            #region HackControl //63-68
            sb.Append(AllowFlying); // 63
            sb.Append(',');

            sb.Append(AllowNoClip); // 64
            sb.Append(',');

            sb.Append(AllowSpeedhack); // 65
            sb.Append(',');

            sb.Append(AllowRespawn); // 66
            sb.Append(',');

            sb.Append(AllowThirdPerson); // 67
            sb.Append(',');

            sb.Append( JumpHeight ); // 68
            sb.Append( ',' );
            #endregion

            sb.Append( GeoIP ); // 69
            sb.Append( ',' );
            sb.Append( CountryCode ); // 70
            sb.Append( ',' );
            sb.Append( CountryName ); // 71
            sb.Append( ',' );
            //sb.Append( RegionCode ); // 72 unused
            sb.Append( ',' );
			//sb.Append( RegionName ); // 73 unused
            sb.Append( ',' );
			//sb.Append( City ); // 74 unused
            sb.Append( ',' );
			//sb.Append( ZipCode ); // 75 unused
            sb.Append( ',' );
            sb.Append( Latitude ); // 76
            sb.Append( ',' );
            sb.Append( Longitude ); // 77
            sb.Append( ',' );
			//sb.Append( MetroCode ); // 78 unused
            sb.Append( ',' );
			//sb.Append(AreaCode); // 79 unused
            sb.Append(',');
            sb.Append(TimeZone); // 80

            sb.Append(',');
            sb.Append(skinName); // 81


			sb.Append(',');
			sb.AppendEscaped(Subdivision.JoinToString() ?? "NA"); // 82
			sb.Append(',');
			sb.Append(Accuracy); // 83
			sb.Append(',');
			sb.Append(Hostname); // 84
			sb.Append(',');
			sb.Append(Continent); // 85
			sb.Append(',');
			sb.Append(ClassicubeVerified); // 86
        }

        #endregion


        #region Update Handlers

        public void ProcessMessageWritten() {
            Interlocked.Increment( ref MessagesWritten );
            LastModified = DateTime.UtcNow;
        }


        static DateTime classsicubeCutoff = new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public void ProcessLogin( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            LastIP = player.IP;
            if( LastLoginDate != DateTime.MinValue && LastLoginDate < classsicubeCutoff )
            	ClassicubeVerified = false;
            LastLoginDate = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            Interlocked.Increment( ref TimesVisited );
            IsOnline = true;
            PlayerObject = player;
            LastModified = DateTime.UtcNow;
            if (Mob == null)
                Mob = "humanoid";
        }


        public void ProcessFailedLogin( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            LastFailedLoginDate = DateTime.UtcNow;
            LastFailedLoginIP = player.IP;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessLogout( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            TotalTime += player.LastActiveTime.Subtract( player.LoginTime );
            LastSeen = DateTime.UtcNow;
            IsOnline = false;
            PlayerObject = null;
            LeaveReason = player.LeaveReason;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessRankChange( [NotNull] Rank newRank, [NotNull] string changer, [CanBeNull] string reason, RankChangeType type ) {
            if( newRank == null ) throw new ArgumentNullException( "newRank" );
            if( changer == null ) throw new ArgumentNullException( "changer" );
            PreviousRank = Rank;
            Rank = newRank;
            RankChangeDate = DateTime.UtcNow;

            RankChangedBy = changer;
            RankChangeReason = reason;
            RankChangeType = type;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessBlockPlaced( byte type ) {
            LastModified = DateTime.UtcNow;
            if (PlayerObject != null)
            {
                if (PlayerObject.World.Name == "Grief" || PlayerObject.World.Name == "Spleef")
                {
                    return;
                }
            }
            if( type == 0 ) { // air
                Interlocked.Increment( ref BlocksDeleted );
            } else {
                Interlocked.Increment( ref BlocksBuilt );
            }
            if (type == 0)
            { // air
                Interlocked.Increment(ref BlocksDeletedThisGame);
            }
            else
            {
                Interlocked.Increment(ref BlocksBuiltThisGame);
            }
        }


        public void ProcessDrawCommand( int blocksDrawn ) {
            LastModified = DateTime.UtcNow;
            if (PlayerObject != null)
            {
                if (PlayerObject.World.Name == "Grief" || PlayerObject.World.Name == "Spleef")
                {
                    return;
                }
            }
            Interlocked.Add( ref BlocksDrawn, blocksDrawn );
        }


        internal void ProcessKick( [NotNull] Player kickedBy, [CanBeNull] string reason ) {
            if( kickedBy == null ) throw new ArgumentNullException( "kickedBy" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            lock( actionLock ) {
                Interlocked.Increment( ref TimesKicked );
                Interlocked.Increment( ref kickedBy.Info.TimesKickedOthers );
                LastKickDate = DateTime.UtcNow;
                LastKickBy = kickedBy.Name;
                LastKickReason = reason;
                if( IsFrozen ) {
                    try {
                        Unfreeze( kickedBy, false, true );
                    } catch( PlayerOpException ex ) {
                        Logger.Log( LogType.Warning,
                                    "PlayerInfo.ProcessKick: {0}", ex.Message );
                    }
                }
                LastModified = DateTime.UtcNow;
            }
        }

        #endregion


        #region TimeSince_____ shortcuts

        public TimeSpan TimeSinceRankChange {
            get { return DateTime.UtcNow.Subtract( RankChangeDate ); }
        }

        public TimeSpan TimeSinceBan {
            get { return DateTime.UtcNow.Subtract( BanDate ); }
        }

        public TimeSpan TimeSinceUnban {
            get { return DateTime.UtcNow.Subtract( UnbanDate ); }
        }

        public TimeSpan TimeSinceFirstLogin {
            get { return DateTime.UtcNow.Subtract( FirstLoginDate ); }
        }

        public TimeSpan TimeSinceLastLogin {
            get { return DateTime.UtcNow.Subtract( LastLoginDate ); }
        }

        public TimeSpan TimeSinceLastKick {
            get { return DateTime.UtcNow.Subtract( LastKickDate ); }
        }

        public TimeSpan TimeSinceLastSeen {
            get { return DateTime.UtcNow.Subtract( LastSeen ); }
        }

        public TimeSpan TimeSinceFrozen {
            get { return DateTime.UtcNow.Subtract( FrozenOn ); }
        }

        public TimeSpan TimeMutedLeft {
            get { return MutedUntil.Subtract( DateTime.UtcNow ); }
        }
        
        public TimeSpan TimeSinceLastServerMessage {
            get { return (this != Player.Console.Info ? DateTime.Now.Subtract(LastServerMessageDate) : TimeSpan.MaxValue); }
        }

        #endregion


        public override string ToString() {
            return String.Format( "PlayerInfo({0},{1})", Name, Rank.Name );
        }

        public bool Can( Permission permission ) {
            return Rank.Can( permission );
        }

        public bool Can( Permission permission, Rank rank ) {
            return Rank.Can( permission, rank );
        }


        #region Unfinished / Not Implemented

        /// <summary> Not implemented (IRC/server password hash). </summary>
        public string Password = ""; // TODO

        public DateTime LastModified; // TODO

        public BandwidthUseMode BandwidthUseMode; // TODO

        #endregion
    }


    /// <summary> Sorts PlayerInfo objects by relevance.
    /// Orders players by online/offline state first, then by rank, then by time-since-last-seen.
    /// Hidden players are listed with the offline players. </summary>
    public sealed class PlayerInfoComparer : IComparer<PlayerInfo> {
        readonly Player observer;

        public PlayerInfoComparer( Player observer ) {
            this.observer = observer;
        }

        public int Compare( PlayerInfo x, PlayerInfo y ) {
            Player xPlayer = x.PlayerObject;
            Player yPlayer = y.PlayerObject;
            bool xIsOnline = xPlayer != null && observer.CanSee( xPlayer );
            bool yIsOnline = yPlayer != null && observer.CanSee( yPlayer );

            if( !xIsOnline && yIsOnline ) {
                return 1;
            } else if( xIsOnline && !yIsOnline ) {
                return -1;
            }

            if( x.Rank == y.Rank ) {
                return Math.Sign( y.LastSeen.Ticks - x.LastSeen.Ticks );
            } else {
                return x.Rank.Index - y.Rank.Index;
            }
        }
    }
}
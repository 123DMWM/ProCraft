﻿// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus>
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Net;
using System.Net.Cache;
using System.Text.RegularExpressions;
using System.Threading;
using fCraft.Drawing;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Represents a callback method for a player-made selection of one or more blocks on a map.
    /// A command may request a number of marks/blocks to select, and a specify callback
    /// to be executed when the desired number of marks/blocks is reached. </summary>
    /// <param name="player"> Player who made the selection. </param>
    /// <param name="marks"> An array of 3D marks/blocks, in terms of block coordinates. </param>
    /// <param name="tag"> An optional argument to pass to the callback,
    /// the value of player.selectionArgs </param>
    public delegate void SelectionCallback( Player player, Vector3I[] marks, object tag );

    /// <summary> Represents the method that responds to a confirmation command. </summary>
    /// <param name="player"> Player who confirmed the action. </param>
    /// <param name="tag"> Parameter that was passed to Player.Confirm() </param>
    /// <param name="fromConsole"> Whether player is console. </param>
    public delegate void ConfirmationCallback( Player player, object tag, bool fromConsole );


    /// <summary> Object representing volatile state ("session") of a connected player.
    /// For persistent state of a known player account, see PlayerInfo. </summary>
    public sealed partial class Player : IClassy {

        /// <summary> The godly pseudo-player for commands called from the server console.
        /// Console has all the permissions granted.
        /// Note that Player.Console.World is always null,
        /// and that prevents console from calling certain commands (like /TP). </summary>
        public static Player Console, AutoRank;


        #region Properties

        public bool IsSuper;

        public bool usedquit = false;

        public string quitmessage = "Used /Quit";

        /// <summary> Last sign clicked by the player</summary>
        public String LastSignClicked;

        /// <summary> Whether the player has completed the login sequence. </summary>
        public SessionState State { get; private set; }

        /// <summary> Whether the player has completed the login sequence. </summary>
        public bool HasRegistered { get; internal set; }

        /// <summary> Whether the player registered and then finished loading the world. </summary>
        public bool HasFullyConnected { get; private set; }

        /// <summary> Whether the client is currently connected. </summary>
        public bool IsOnline {
            get {
                return State == SessionState.Online;
            }
        }

        #region PlayerList

        /// <summary> ID for the playername. </summary>
        public short NameID { get; set; }
        
        #endregion

        /// <summary> Whether the player name was verified at login. </summary>
        public bool IsVerified { get; private set; }

        /// <summary> Persistent information record associated with this player. </summary>
        public PlayerInfo Info { get; private set; }

        /// <summary> Whether the player is in paint mode (deleting blocks replaces them). Used by /Paint. </summary>
        public bool IsPainting { get; set; }

        /// <summary> Whether player has blocked all incoming chat.
        /// Deaf players can't hear anything. </summary>
        public bool IsDeaf { get; set; }

        /// <summary> Whether player has blocked all incoming chat.
        /// Deaf players can't hear anything. </summary>
        public bool isSolid { get; set; }

        /// <summary> Whether player has blocked all incoming chat.
        /// Deaf players can't hear anything. </summary>
        public bool isPlayingGame { get; set; }

        /// <summary> Whether player has blocked all incoming chat.
        /// Deaf players can't hear anything. </summary>
        public Block inGameBlock = Block.Stone;
        
        /// <summary> Whether player has blocked all incoming chat.
        /// Deaf players can't hear anything. </summary>
        public Block solidPosBlock { get; set; }

        /// <summary> Whether placing dirt near grass will spread the grass onto the dirt </summary>
        public bool GrassGrowth;

        /// <summary> How Many Blocks player has deleted this session. </summary>
        public double BlocksDeletedThisSession { get; set; }

        /// <summary> How Many Blocks player has placed this session. </summary>
        public double BlocksPlacedThisSession { get; set; }
            
        /// <summary> How Many Blocks player has placed this session. </summary>
        public double BlocksPlacedDeletedMixed { get; set; }

        /// <summary> The Time that has passed since the last block change.</summary>
        public DateTime TimeLastBlockChange { get; set; }

        /// <summary> The last time the player has used /suicide.</summary>
        public DateTime LastUsedSuicide { get; set; }

        /// <summary> Has player been warned by the DEFCCON system? </summary>
        public bool Warned { get; set; }
        /// <summary> How Many Blocks player has placed this session. </summary>
        public bool SecurityTrip { get; set; }


        /// <summary> Set if Player has trace on. </summary>
        public bool HasTrace { get; set; }

        /// <summary> The world that the player is currently on. May be null.
        /// Use .JoinWorld() to make players teleport to another world. </summary>
        [CanBeNull]
        public World World { get; private set; }

        #region General purpose state storage for plugins
        private readonly ConcurrentDictionary<string, object> _publicAuxStateObjects = new ConcurrentDictionary<string, object>();
        public IDictionary<string, object> PublicAuxStateObjects { get { return _publicAuxStateObjects; } }
        #endregion

        public Font font = new Font("Times New Roman", 14, FontStyle.Regular, GraphicsUnit.Pixel);
        System.Drawing.Text.PrivateFontCollection FontC;
        public FontFamily LoadFontFamily(string fileName)
        {
            FontC = new System.Drawing.Text.PrivateFontCollection();//assing memory space to FontC
            FontC.AddFontFile(fileName);//we add the full path of the ttf file
            return FontC.Families[0];//returns the family object as usual.
        }

        /// <summary> Map from the world that the player is on.
        /// Throws PlayerOpException if player does not have a world.
        /// Loads the map if it's not loaded. Guaranteed to not return null. </summary>
        [NotNull]
        public Map WorldMap {
            get {
                World world = World;
                if( world == null ) PlayerOpException.ThrowNoWorld( this );
                return world.LoadMap();
            }
        }

        /// <summary> Player's position in the current world. </summary>
        public Position Position;

        /// <summary> Player's position in the current world. </summary>
        public Position lastSolidPos { get; set; }
        
        /// <summary> Time when the session connected. </summary>
        public DateTime LoginTime { get; private set; }

        /// <summary> Last time when the player was active (moving/messaging). UTC. </summary>
        public DateTime LastActiveTime { get; private set; }

        /// <summary> Last time when this player was patrolled by someone. </summary>
        public DateTime LastPatrolTime { get; set; }

        #region Flying

        public bool IsFlying = false;
        public ConcurrentDictionary<String, Vector3I> FlyCache;
        public readonly object FlyLock = new object();

        #endregion Flying

        /// <summary> Last command called by the player. </summary>
        [CanBeNull]
        public CommandReader LastCommand { get; private set; }


        /// <summary> Plain version of the name (no formatting). </summary>
        [NotNull]
        public string Name {
            get { return Info.Name; }
        }

        /// <summary> Name formatted for display in the player list. </summary>
        [NotNull]
        public string ListName {
            get {
                string formattedName = Name;
                if (this.Info.IsAFK)
                {
                    formattedName = Info.Rank.Color + Info.Rank.Prefix + Color.Yellow + "[AFK]" + Color.White + formattedName;
                }
                else
                {
                    formattedName = Info.Rank.Color + Info.Rank.Prefix + Color.White + formattedName;
                }
                return formattedName;
            }
        }

        /// <summary> Name formatted for display in chat. </summary>
        public string ClassyName {
            get { return Info.ClassyName; }
        }

        [CanBeNull] //Although it really shouldn't be.
        /// <summary> What team the player is currently on. Either Red or Blue./ </summary>
        public string Team;

        /// <summary> Whether or not the player is currently playing CTF </summary>
        public bool IsPlayingCTF = false;

        /// <summary> Whether or not the player is currently holding other teams flag </summary>
        public bool IsHoldingFlag = false;

        /// <summary> Whether the client supports advanced WoM client functionality. </summary>
        public bool IsUsingWoM { get; private set; }


        /// <summary> Metadata associated with the session/player. </summary>
        [NotNull]
        public MetadataCollection<object> Metadata { get; private set; }

        #endregion


        // This constructor is used to create pseudoplayers (such as Console and /dummy).
        // Such players have unlimited permissions, but no world.
        // This should be replaced by a more generic solution, like an IEntity interface.
        internal Player( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            Info = new PlayerInfo( name, RankManager.HighestRank, true, RankChangeType.AutoPromoted );
            spamBlockLog = new Queue<DateTime>( Info.Rank.AntiGriefBlocks );
            IP = IPAddress.Loopback;
            ResetAllBinds();
            State = SessionState.Offline;
            IsSuper = true;
        }


        #region Chat and Messaging

        static readonly TimeSpan ConfirmationTimeout = TimeSpan.FromSeconds( 60 );
        const string WoMAlertPrefix = "^detail.user.alert=";
        int muteWarnings;

        [CanBeNull]
        string partialMessage;


        /// <summary> Parses a message on behalf of this player. </summary>
        /// <param name="rawMessage"> Message to parse. </param>
        /// <param name="fromConsole"> Whether the message originates from console. </param>
        /// <exception cref="ArgumentNullException"> If rawMessage is null. </exception>
        public void ParseMessage( [NotNull] string rawMessage, bool fromConsole ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            // handle canceling selections and partial messages
            if( rawMessage.StartsWith( "/nvm", StringComparison.OrdinalIgnoreCase ) ||
                rawMessage.StartsWith( "/cancel", StringComparison.OrdinalIgnoreCase ) ) {
                if( partialMessage != null ) {
                    MessageNow( "Partial message cancelled." );
                    partialMessage = null;
                } else if( IsMakingSelection ) {
                    SelectionCancel();
                    MessageNow( "Selection cancelled." );
                } else {
                    MessageNow( "There is currently nothing to cancel." );
                }
                return;
            }

            if( partialMessage != null ) {
                rawMessage = partialMessage + rawMessage;
                partialMessage = null;
            }
            
            // replace %-codes with &-codes
            if (Can(Permission.UseColorCodes))
            {
                rawMessage = Chat.ReplacePercentColorCodes(rawMessage, true);
            }
            else
            {
                rawMessage = Chat.ReplacePercentColorCodes(rawMessage, false);
                rawMessage = Color.StripColors(rawMessage);
            }
            // replace emotes
            if( Can( Permission.UseEmotes ) ) {
                rawMessage = Chat.ReplaceEmoteKeywords( rawMessage );
            }
            rawMessage = Chat.UnescapeBackslashes( rawMessage );

            switch( Chat.GetRawMessageType( rawMessage ) ) {
                case RawMessageType.Chat: {
                        if( !Can( Permission.Chat ) ) return;

                        if (Info.IsMuted)
                        {
                            MessageMuted();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        // Escaped slash removed AFTER logging, to avoid confusion with real commands
                        if( rawMessage.StartsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 1 );
                        }

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        Chat.SendGlobal( this, rawMessage );
                        if (this.Info.IsAFK == true)
                        {
                            this.Info.IsAFK = false;
                            Server.UpdateTabList();
                            Server.Players.CanSee(this).Message("&S{0} is no longer AFK", this.Name);
                            this.Message("&SYou are no longer AFK");
                            this.Info.Mob = this.Info.TempMob;
                        }
                    } break;


                case RawMessageType.Command: {
                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }
                        CommandReader cmd = new CommandReader( rawMessage );
                        CommandDescriptor commandDescriptor = CommandManager.GetDescriptor( cmd.Name, true );

                    if (commandDescriptor == null) {
                        MessageNow("Unknown command \"{0}\". See &H/Commands", cmd.Name);
                        Logger.Log(LogType.UserCommand, "{0}[Not A CMD]: {1}", Name, rawMessage);
                    } else if (IsPlayingCTF && commandDescriptor.Permissions != null &&
                               (commandDescriptor.Permissions.Contains(Permission.Build) ||
                                commandDescriptor.Permissions.Contains(Permission.Draw) ||
                                commandDescriptor.Permissions.Contains(Permission.DrawAdvanced) ||
                                commandDescriptor.Permissions.Contains(Permission.CopyAndPaste) ||
                                commandDescriptor.Permissions.Contains(Permission.UndoOthersActions) ||
                                commandDescriptor.Permissions.Contains(Permission.UndoAll))) {
                        MessageNow("&WYou cannot use this command while playing CTF");
                    } else if (Info.IsFrozen && !commandDescriptor.UsableByFrozenPlayers) {
                        MessageNow("&WYou cannot use this command while frozen.");
                        Logger.Log(LogType.UserCommand, "{0}[Frozen]: {1}", Name, rawMessage);
                    } else {
                        if (!commandDescriptor.DisableLogging) {
                            Logger.Log(LogType.UserCommand, "{0}: {1}", Name, rawMessage);
                        }
                        if (commandDescriptor.RepeatableSelection) {
                            selectionRepeatCommand = cmd;
                        }
                        SendToSpectators(cmd.RawMessage);
                        if (this.Info.IsAFK == true && rawMessage.ToLower().StartsWith("/afk") == false) {
                            this.Info.IsAFK = false;
                            Server.UpdateTabList();
                            Server.Players.CanSee(this).Message("&S{0} is no longer AFK", this.Name);
                            this.Message("&SYou are no longer AFK");
                            this.Info.Mob = this.Info.TempMob;
                        }
                        CommandManager.ParseCommand(this, cmd, fromConsole);
                        if (!commandDescriptor.NotRepeatable) {
                            LastCommand = cmd;
                        }
                    }
                } break;

                case RawMessageType.RepeatCommand: {
                        if( LastCommand == null ) {
                            Message( "No command to repeat." );
                        } else {
                            if (Info.IsFrozen && !LastCommand.Descriptor.UsableByFrozenPlayers)
                            {
                                MessageNow( "&WYou cannot use this command while frozen." );
                                return;
                            }
                            LastCommand.Rewind();
                            Logger.Log( LogType.UserCommand,
                                        "{0} repeated: {1}",
                                        Name, LastCommand.RawMessage );
                            Message( "Repeat: {0}", LastCommand.RawMessage );
                            SendToSpectators( LastCommand.RawMessage );
                            CommandManager.ParseCommand( this, LastCommand, fromConsole );
                            if (this.Info.IsAFK == true && LastCommand.RawMessage.ToLower().StartsWith("/afk") == false)
                            {
                                this.Info.IsAFK = false;
                                Server.UpdateTabList();
                                Server.Players.CanSee(this).Message("&S{0} is no longer AFK", this.Name);
                                this.Message("&SYou are no longer AFK");
                                this.Info.Mob = this.Info.TempMob;
                            }
                        }
                    } break;


                case RawMessageType.PrivateChat: {
                        if( !Can( Permission.Chat ) ) return;

                        if (Info.IsMuted)
                        {
                            MessageMuted();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        string otherPlayerName, messageText;
                        if( rawMessage[1] == ' ' ) {
                            otherPlayerName = rawMessage.Substring( 2, rawMessage.IndexOf( ' ', 2 ) - 2 );
                            messageText = rawMessage.Substring( rawMessage.IndexOf( ' ', 2 ) + 1 );
                        } else {
                            otherPlayerName = rawMessage.Substring( 1, rawMessage.IndexOf( ' ' ) - 1 );
                            messageText = rawMessage.Substring( rawMessage.IndexOf( ' ' ) + 1 );
                        }

                        if( otherPlayerName == "-" ) {
                            if( LastUsedPlayerName != null ) {
                                otherPlayerName = LastUsedPlayerName;
                            } else {
                                Message( "Cannot repeat player name: you haven't used any names yet." );
                                return;
                            }
                        }

                        if (otherPlayerName.ToLower() == "irc")
                        {
                            IRC.SendChannelMessage("&P(PM)" + Name + ": " + messageText);
                            Message("&P(PM)" + this.ClassyName + " &P-> IRC&P: " + messageText);
                            return;
                        }
                        // first, find ALL players (visible and hidden)
                        Player[] allPlayers = Server.FindPlayers(otherPlayerName, SearchOptions.IncludeHidden);

                        // if there is more than 1 target player, exclude hidden players
                        if( allPlayers.Length > 1 ) {
                            allPlayers = Server.FindPlayers(otherPlayerName, SearchOptions.Default );
                        }

                        if( allPlayers.Length == 1 ) {
                            Player target = allPlayers[0];
                            if (target == this)
                            {
                                MessageNow( "Trying to talk to yourself?" );
                                return;
                            }
                            if( !target.IsIgnoring( Info ) && !target.IsDeaf ) {
                                Chat.SendPM( this, target, messageText );
                                SendToSpectators( "to {0}&F: {1}", target.ClassyName, messageText );
                            }

                            if( !CanSee( target ) ) {
                                // message was sent to a hidden player
                                MessageNoPlayer( otherPlayerName );

                            } else {
                                // message was sent normally
                                LastUsedPlayerName = target.Name;
                                if( target.IsIgnoring( Info ) ) {
                                    if( CanSee( target ) ) {
                                        MessageNow( "&WCannot PM {0}&W: you are ignored.", target.ClassyName );
                                    }
                                } else if( target.IsDeaf ) {
                                    MessageNow( "&SCannot PM {0}&S: they are currently deaf.", target.ClassyName );
                                } else {
                                    MessageNow( "&Pto {0}: {1}",
                                                target.Name, messageText );
                                }
                            }

                        } else if( allPlayers.Length == 0 ) {
                            MessageNoPlayer( otherPlayerName );

                        } else {
                            MessageManyMatches( "player", allPlayers );
                        }
                    } break;


                case RawMessageType.RankChat: {
                        if( !Can( Permission.Chat ) ) return;

                        if (Info.IsMuted)
                        {
                            MessageMuted();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        Rank rank;
                        if( rawMessage[2] == ' ' ) {
                            rank = Info.Rank;
                        } else {
                            string rankName = rawMessage.Substring( 2, rawMessage.IndexOf( ' ' ) - 2 );
                            rank = RankManager.FindRank( rankName );
                            if( rank == null ) {
                                MessageNoRank( rankName );
                                break;
                            }
                        }

                        string messageText = rawMessage.Substring( rawMessage.IndexOf( ' ' ) + 1 );

                        Player[] spectators = Server.Players.NotRanked( Info.Rank )
                                                            .Where( p => p.spectatedPlayer == this )
                                                            .ToArray();
                        if( spectators.Length > 0 ) {
                            spectators.Message( "[Spectate]: &Fto rank {0}&F: {1}", rank.ClassyName, messageText );
                        }

                        Chat.SendRank( this, rank, messageText );
                    } break;


                case RawMessageType.Confirmation:
                    {
                        if (Info.IsFrozen)
                        {
                            MessageNow("&WYou cannot use any commands while frozen.");
                            return;
                        }
                        if (ConfirmCallback != null)
                        {
                            if (DateTime.UtcNow.Subtract(ConfirmRequestTime) < ConfirmationTimeout)
                            {
                                Logger.Log(LogType.UserCommand, "{0}: /ok", Name);
                                SendToSpectators("/ok");
                                ConfirmCallback(this, ConfirmParameter, fromConsole);
                                ConfirmCancel();
                            }
                            else
                            {
                                MessageNow("Confirmation timed out. Enter the command again.");
                            }
                        }
                        else
                        {
                            MessageNow("There is no command to confirm.");
                        }
                        if (this.Info.IsAFK == true)
                        {
                            this.Info.IsAFK = false;
                            Server.UpdateTabList();
                            Server.Players.CanSee(this).Message("&S{0} is no longer AFK", this.Name);
                            this.Message("&SYou are no longer AFK");
                            this.Info.Mob = this.Info.TempMob;
                        }
                    }
                    break;


                case RawMessageType.PartialMessage:
                    partialMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                    MessageNow( "Partial: &F{0}", partialMessage );
                    break;

                case RawMessageType.Invalid:
                    MessageNow( "Could not parse message." );
                    break;
            }
        }

        /// <summary> Sends a message to all players who are spectating this player, e.g. to forward typed-in commands and PMs. </summary>
        /// <param name="message"> Message to be displayed </param>
        /// <param name="args"> Additional arguments </param>
        /// <exception cref="ArgumentNullException"> If any of the parameters are null. </exception>
        public void SendToSpectators( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            Player[] spectators = Server.Players.Where( p => p.spectatedPlayer == this ).ToArray();
            if( spectators.Length > 0 ) {
                spectators.Message( "[Spectate]: &F" + message, args );
            }
        }


        /// <summary> Sends a message as a WoM alert.
        /// Players who use World of Minecraft client will see this message on the left side of the screen.
        /// Other players will receive it as a normal message. </summary>
        /// <param name="message"> A composite format string for the message. "System color" code will be prepended. </param>
        /// <param name="args"> An object array that contains zero or more objects to format. </param>
        /// <exception cref="ArgumentNullException"> If message is null. </exception>
        /// <exception cref="FormatException"> If message format is invalid. </exception>
        [StringFormatMethod( "message" )]
        public void MessageWoMAlert( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else if( IsUsingWoM ) {
                foreach (Packet p in LineWrapper.WrapPrefixed( WoMAlertPrefix, WoMAlertPrefix + Color.Sys + message, SupportsEmoteFix )) {
                    Send( p );
                }
            } else {
                foreach (Packet p in LineWrapper.Wrap( Color.Sys + message, SupportsEmoteFix )) {
                    Send( p );
                }
            }
        }


        /// <summary> Sends a text message to this player.
        /// If the message does not fit on one line, prefix ">" is prepended to wrapped line. </summary>
        /// <param name="message"> A composite format string for the message. "System color" code will be prepended. </param>
        /// <param name="args"> An object array that contains zero or more objects to format. </param>
        /// <exception cref="ArgumentNullException"> If any of the method parameters are null. </exception>
        /// <exception cref="FormatException"> If message format is invalid. </exception>
        [StringFormatMethod( "message" )]
        public void Message( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( IsSuper ) {
                Logger.LogToConsole( message );
            } else {
                foreach (Packet p in LineWrapper.Wrap( Color.Sys + message, SupportsEmoteFix )) {
                    Send( p );
                }
            }
        }

        /// <summary> Sends a text message to this player.
        /// If the message does not fit on one line, prefix ">" is prepended to wrapped line. </summary>
        /// <param name="messageType"> A MessageType byte. </param>
        /// <param name="message"> A composite format string for the message. "System color" code will be prepended. </param>
        /// <param name="args"> An object array that contains zero or more objects to format. </param>
        /// <exception cref="ArgumentNullException"> If any of the method parameters are null. </exception>
        /// <exception cref="FormatException"> If message format is invalid. </exception>
        [StringFormatMethod("message")]
        public void Message([NotNull] byte messageType, [NotNull] string message, [NotNull] params object[] args)
        {
            if (messageType == null) throw new ArgumentNullException("messageType");
            if (message == null) throw new ArgumentNullException("message");
            if (args == null) throw new ArgumentNullException("args");
            if (args.Length > 0)
            {
                message = String.Format(message, args);
            }
            if (IsSuper)
            {
                Logger.LogToConsole("Type(" + messageType + ") Message: " + message);
            }
            else
            {
                foreach (Packet p in LineWrapper.Wrap( messageType, message, SupportsEmoteFix ))
                {
                    Send(p);
                }
            }
        }


        /// <summary> Sends a text message to this player, prefixing each line. </summary>
        /// <param name="prefix"> Prefix to prepend to each wrapped line. Not prepended to the first line. </param>
        /// <param name="message"> A composite format string for the message. "System color" code will be prepended. </param>
        /// <param name="args"> An object array that contains zero or more objects to format. </param>
        /// <exception cref="ArgumentNullException"> If any of the method parameters are null. </exception>
        /// <exception cref="FormatException"> If message format is invalid. </exception>
        [StringFormatMethod( "message" )]
        public void MessagePrefixed( [NotNull] string prefix, [NotNull] string message, [NotNull] params object[] args ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                foreach (Packet p in LineWrapper.WrapPrefixed( prefix, message, SupportsEmoteFix )) {
                    Send( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        internal void MessageNow( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( IsDeaf ) return;
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                if( Thread.CurrentThread != ioThread ) {
                    throw new InvalidOperationException( "SendNow may only be called from player's own thread." );
                }
                foreach (Packet p in LineWrapper.Wrap( Color.Sys + message, SupportsEmoteFix )) {
                    SendNow( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        internal void MessageNowPrefixed( [NotNull] string prefix, [NotNull] string message, [NotNull] params object[] args ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( IsDeaf ) return;
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                if( Thread.CurrentThread != ioThread ) {
                    throw new InvalidOperationException( "SendNow may only be called from player's own thread." );
                }
                foreach (Packet p in LineWrapper.WrapPrefixed( prefix, message, SupportsEmoteFix )) {
                    Send( p );
                }
            }
        }


        #region Macros

        /// <summary> Prints "No players found matching ___" message. </summary>
        /// <param name="playerName"> Given name, for which no players were found. </param>
        /// <exception cref="ArgumentNullException"> If playerName is null. </exception>
        public void MessageNoPlayer( [NotNull] string playerName ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Message( "No players found matching \"{0}\"", playerName );
        }


        /// <summary> Prints "No worlds found matching ___" message. </summary>
        /// <param name="worldName"> Given name, for which no worlds were found. </param>
        /// <exception cref="ArgumentNullException"> If worldName is null. </exception>
        public void MessageNoWorld( [NotNull] string worldName ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            Message( "No worlds found matching \"{0}\". See &H/Worlds", worldName );
        }


        const int MatchesToPrint = 30;

        /// <summary> Prints a comma-separated list of matches (up to 30): "More than one ___ matched: ___, ___, ..." </summary>
        /// <param name="itemType"> Type of item in the list. Should be singular (e.g. "player" or "world"). </param>
        /// <param name="items"> List of zero or more matches. ClassyName properties are used in the list. </param>
        /// <exception cref="ArgumentNullException"> If itemType or items is null. </exception>
        public void MessageManyMatches( [NotNull] string itemType, [NotNull] IEnumerable<IClassy> items ) {
            if( itemType == null ) throw new ArgumentNullException( "itemType" );
            if( items == null ) throw new ArgumentNullException( "items" );

            IClassy[] itemsEnumerated = items.ToArray();
            string nameList = itemsEnumerated.Take( MatchesToPrint ).JoinToString( ", ", p => p.ClassyName );
            int count = itemsEnumerated.Length;
            if( count > MatchesToPrint ) {
                Message( "More than {0} {1} matched: {2}",
                         count, itemType, nameList );
            } else {
                Message( "More than one {0} matched: {1}",
                         itemType, nameList );
            }
        }

        /// <summary> Prints "This command requires ___+ rank" message. </summary>
        /// <param name="permissions"> List of permissions required for the command. </param>
        /// <exception cref="ArgumentNullException"> If permissions is null. </exception>
        public void MessageNoAccess( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            if( permissions.Length == 0 ) throw new ArgumentException( "At least one permission required.", "permissions" );
            Rank reqRank = RankManager.GetMinRankWithAllPermissions( permissions );
            if (reqRank == null)
            {
                Message("None of the ranks have permissions for this command.");
            }
            else
            {
                Message("This command requires {0}+&S rank.",
                         reqRank.ClassyName);
            }
        }


        /// <summary> Prints "This command requires ___+ rank" message. </summary>
        /// <param name="cmd"> Command to check. </param>
        /// <exception cref="ArgumentNullException"> If cmd is null. </exception>
        public void MessageNoAccess( [NotNull] CommandDescriptor cmd ) {
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            Rank reqRank = cmd.MinRank;
            if (reqRank == null)
            {
                Message("This command is disabled on the server.");
            }
            else
            {
                Message("This command requires {0}+&S rank.",
                         reqRank.ClassyName);
            }
        }


        /// <summary> Prints "Unrecognized rank ___" message. </summary>
        /// <param name="rankName"> Given name, for which no rank was found. </param>
        public void MessageNoRank( [NotNull] string rankName ) {
            if( rankName == null ) throw new ArgumentNullException( "rankName" );
            Message( "Unrecognized rank \"{0}\". See &H/Ranks", rankName );
        }


        /// <summary> Prints "You cannot access files outside the map folder." message. </summary>
        public void MessageUnsafePath() {
            Message( "&WYou cannot access files outside the map folder." );
        }


        /// <summary> Prints "No zones found matching ___" message. </summary>
        /// <param name="zoneName"> Given name, for which no zones was found. </param>
        public void MessageNoZone( [NotNull] string zoneName ) {
            if( zoneName == null ) throw new ArgumentNullException( "zoneName" );
            Message( "No zones found matching \"{0}\". See &H/Zones", zoneName );
        }


        /// <summary> Prints "Unacceptable world name" message, and requirements for world names. </summary>
        /// <param name="worldName"> Given world name, deemed to be invalid. </param>
        public void MessageInvalidWorldName( [NotNull] string worldName ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            Message( "Unacceptable world name: \"{0}\"", worldName );
            Message( "World names must be 1-16 characters long, and only contain letters, numbers, and underscores." );
        }


        /// <summary> Prints "___ is not a valid player name" message. </summary>
        /// <param name="playerName"> Given player name, deemed to be invalid. </param>
        public void MessageInvalidPlayerName( [NotNull] string playerName ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Message( "\"{0}\" is not a valid player name.", playerName );
        }


        /// <summary> Prints "You are muted for ___ longer" message. </summary>
        public void MessageMuted() {
            Message( "You are muted for {0} longer.",
                     Info.TimeMutedLeft.ToMiniString() );
        }


        /// <summary> Prints "Specify a time range up to ___" message </summary>
        public void MessageMaxTimeSpan() {
            Message( "Specify a time range up to {0}", DateTimeUtil.MaxTimeSpan.ToMiniString() );
        }

        #endregion


        #region Ignore

        readonly HashSet<PlayerInfo> ignoreList = new HashSet<PlayerInfo>();
        readonly object ignoreLock = new object();


        /// <summary> Checks whether this player is currently ignoring a given PlayerInfo.</summary>
        public bool IsIgnoring( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( ignoreLock ) {
                return ignoreList.Contains( other );
            }
        }


        /// <summary> Adds a given PlayerInfo to the ignore list.
        /// Not that ignores are not persistent, and are reset when a player disconnects. </summary>
        /// <param name="other"> Player to ignore. </param>
        /// <returns> True if the player is now ignored,
        /// false is the player has already been ignored previously. </returns>
        public bool Ignore( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( ignoreLock ) {
                if( !ignoreList.Contains( other ) ) {
                    ignoreList.Add( other );
                    return true;
                } else {
                    return false;
                }
            }
        }


        /// <summary> Removes a given PlayerInfo from the ignore list. </summary>
        /// <param name="other"> PlayerInfo to unignore. </param>
        /// <returns> True if the player is no longer ignored,
        /// false if the player was already not ignored. </returns>
        public bool Unignore( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( ignoreLock ) {
                return ignoreList.Remove( other );
            }
        }


        /// <summary> Returns a list of all currently-ignored players. </summary>
        [NotNull]
        public PlayerInfo[] IgnoreList {
            get {
                lock( ignoreLock ) {
                    return ignoreList.ToArray();
                }
            }
        }

        #endregion


        #region Confirmation

        /// <summary> Callback to be called when player types in "/ok" to confirm an action.
        /// Use Player.Confirm(...) methods to set this. </summary>
        [CanBeNull]
        public ConfirmationCallback ConfirmCallback { get; private set; }


        /// <summary> Custom parameter to be passed to Player.ConfirmCallback. </summary>
        [CanBeNull]
        public object ConfirmParameter { get; private set; }


        /// <summary> Time when the confirmation was requested. UTC. </summary>
        public DateTime ConfirmRequestTime { get; private set; }


        static void ConfirmCommandCallback( [NotNull] Player player, object tag, bool fromConsole ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            CommandReader cmd = (CommandReader)tag;
            cmd.Rewind();
            cmd.IsConfirmed = true;
            CommandManager.ParseCommand( player, cmd, fromConsole );
        }


        /// <summary> Request player to confirm continuing with the command.
        /// Player is prompted to type "/ok", and when he/she does,
        /// the command is called again with IsConfirmed flag set. </summary>
        /// <param name="cmd"> Command that needs confirmation. </param>
        /// <param name="message"> Message to print before "Type /ok to continue". </param>
        /// <param name="args"> Optional String.Format() arguments, for the message. </param>
        /// <exception cref="ArgumentNullException"> If cmd, message, or args is null. </exception>
        [StringFormatMethod( "message" )]
        public void Confirm( [NotNull] CommandReader cmd, [NotNull] string message, [NotNull] params object[] args ) {
            Confirm( ConfirmCommandCallback, cmd, message, args );
        }


        /// <summary> Request player to confirm an action.
        /// Player is prompted to type "/ok", and when he/she does, custom callback will be called </summary>
        /// <param name="callback"> Method to call when player confirms. </param>
        /// <param name="callbackParameter"> Argument to pass to the callback. May be null. </param>
        /// <param name="message"> Message to print before "Type /ok to continue". </param>
        /// <param name="args"> Optional String.Format() arguments, for the message. </param>
        /// <exception cref="ArgumentNullException"> If callback, message, or args is null. </exception>
        [StringFormatMethod( "message" )]
        public void Confirm( [NotNull] ConfirmationCallback callback, [CanBeNull] object callbackParameter, [NotNull] string message, [NotNull] params object[] args ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            ConfirmCallback = callback;
            ConfirmParameter = callbackParameter;
            ConfirmRequestTime = DateTime.UtcNow;
            Message( "{0}\n" + 
                     "&sType &H/ok&S to confirm and continue.", String.Format( message, args ) );
        }


        /// <summary> Cancels any pending confirmation (/ok) prompt. </summary>
        /// <returns> True if a confirmation prompt was pending. </returns>
        public bool ConfirmCancel() {
            if( ConfirmCallback != null ) {
                ConfirmCallback = null;
                ConfirmParameter = null;
                return true;
            } else {
                return false;
            }
        }

        #endregion


        #region AntiSpam

        /// <summary> Number of messages in a AntiSpamInterval seconds required to trigger the anti-spam filter </summary>
        public static int AntispamMessageCount = 3;

        /// <summary> Interval in seconds to record number of message for anti-spam filter </summary>
        public static int AntispamInterval = 4;

        readonly Queue<DateTime> spamChatLog = new Queue<DateTime>( AntispamMessageCount );


        internal bool DetectChatSpam() {
            if( IsSuper || AntispamMessageCount < 1 || AntispamInterval < 1 ) return false;
            if( spamChatLog.Count >= AntispamMessageCount ) {
                DateTime oldestTime = spamChatLog.Dequeue();
                if( DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds < AntispamInterval ) {
                    muteWarnings++;
                    int maxMuteWarnings = ConfigKey.AntispamMaxWarnings.GetInt();
                    if( maxMuteWarnings > 0 && muteWarnings > maxMuteWarnings ) {
                        KickNow( "You were kicked for repeated spamming.", LeaveReason.MessageSpamKick );
                        Server.Message( "{0}&W was kicked for spamming.", ClassyName );
                    } else {
                        TimeSpan autoMuteDuration = TimeSpan.FromSeconds( ConfigKey.AntispamMuteDuration.GetInt() );
                        if( autoMuteDuration > TimeSpan.Zero ) {
                            Info.Mute( Console, autoMuteDuration, false, true );
                            Message( "&WYou have been muted for {0} seconds. Slow down.", autoMuteDuration );
                        } else {
                            Message( "&WYou are sending messages too quickly. Slow down." );
                        }
                    }
                    return true;
                }
            }
            spamChatLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion

        #endregion


        #region Placing Blocks

        // for grief/spam detection
        readonly Queue<DateTime> spamBlockLog = new Queue<DateTime>();

        /// <summary> Last blocktype used by the player.
        /// Make sure to use in conjunction with Player.GetBind() to ensure that bindings are properly applied. </summary>
        public Block LastUsedBlockType { get; private set; }

        /// <summary> Max distance that player may be from a block to reach it (hack detection). </summary>
        public static int MaxBlockPlacementRange { get; set; }


        /// <summary> Handles manually-placed/deleted blocks.
        /// Returns true if player's action should result in a kick. </summary>
        public bool PlaceBlock( Vector3I coord, ClickAction action, Block type ) {
            if( World == null ) PlayerOpException.ThrowNoWorld( this );
            Map map = WorldMap;
            LastUsedBlockType = type;

            Vector3I coordBelow = new Vector3I( coord.X, coord.Y, coord.Z - 1 );
            Vector3I coordAbove = new Vector3I( coord.X, coord.Y, coord.Z + 1 ); 
            // check if player is frozen or too far away to legitimately place a block
            if( Info.IsFrozen ||
                Math.Abs( coord.X * 32 - Position.X ) > MaxBlockPlacementRange ||
                Math.Abs( coord.Y * 32 - Position.Y ) > MaxBlockPlacementRange ||
                Math.Abs( coord.Z * 32 - Position.Z ) > MaxBlockPlacementRange ) {
                RevertBlockNow( coord );
                return false;
            }

            if( IsSpectating ) {
                RevertBlockNow( coord );
                Message( "You cannot build or delete while spectating." );
                return false;
            }

            if( World.IsLocked ) {
                RevertBlockNow( coord );
                Message( "This map is currently locked (read-only)." );
                return false;
            }

            if( CheckBlockSpam() ) return true;

            BlockChangeContext context = BlockChangeContext.Manual;
            if( IsPainting && action == ClickAction.Delete ) {
                context |= BlockChangeContext.Replaced;
            }

            // bindings
            bool requiresUpdate = (type != bindings[(byte)type] || IsPainting || GrassGrowth);
            if( action == ClickAction.Delete && !IsPainting ) {
                type = Block.Air;
            }
            type = bindings[(byte)type];

            // selection handling
            if( SelectionMarksExpected > 0 && !DisableClickToMark ) {
                RevertBlockNow( coord );
                SelectionAddMark( coord, true );
                return false;
            }

            CanPlaceResult canPlaceResult;
            if( type == Block.Slab && coord.Z > 0 && map.GetBlock( coordBelow ) == Block.Slab ) {
                // stair stacking
                canPlaceResult = CanPlace( map, coordBelow, Block.DoubleSlab, context );
            }
            else if (type == Block.CobbleSlab && coord.Z > 0 && map.GetBlock(coordBelow) == Block.CobbleSlab)
            {
                // stair stacking
                canPlaceResult = CanPlace(map, coordBelow, Block.Cobblestone, context);
            }
            else
            {
                // normal placement
                canPlaceResult = CanPlace( map, coord, type, context );
            }

            // if all is well, try placing it
            switch( canPlaceResult ) {
                case CanPlaceResult.Allowed:
                    Network.DEFCON DEFCON = new Network.DEFCON();
                    if (!DEFCON.Check(this, type))
                    {
                        RevertBlockNow(coord);
                        break;
                    }
                    if (type == Block.Dirt && GrassGrowth == true)
                    {
                        // Grass growing
                        type = Block.Grass;
                    }

                    if (type == Block.Snow && coord.Z > 0
                        && (map.GetBlock(coordBelow) == Block.Air
                        || map.GetBlock(coordBelow) == Block.BrownMushroom
                        || map.GetBlock(coordBelow) == Block.RedMushroom
                        || map.GetBlock(coordBelow) == Block.RedFlower
                        || map.GetBlock(coordBelow) == Block.YellowFlower
                        || map.GetBlock(coordBelow) == Block.Sapling
                        || map.GetBlock(coordBelow) == Block.Fire
                        || map.GetBlock(coordBelow) == Block.Rope))
                    {
                        // Handle snow over air
                        RevertBlockNow(coord);
                        break;
                    }
                    else if (type == Block.Snow && coord.Z > 0 && map.GetBlock(coordBelow) == Block.Snow)
                    {
                        // Handle Snow Stacking --> Ice
                        RevertBlockNow(coord);
                        coord = coordBelow;
                        type = Block.Ice;
                    }
                    BlockUpdate blockUpdate;
                    if( type == Block.Slab && coord.Z > 0 && map.GetBlock( coordBelow ) == Block.Slab ) 
                    {
                        // handle stair stacking
                        RevertBlockNow(coord);
                        coord = coordBelow;
                        type = Block.DoubleSlab;

                    }
                    if (type == Block.CobbleSlab && coord.Z > 0 && map.GetBlock(coordBelow) == Block.CobbleSlab)
                    {
                        // Handle cobble stacking
                        RevertBlockNow(coord);
                        coord = coordBelow;
                        type = Block.Cobblestone;
                    }
                    if (map.GetBlock(coordBelow) == Block.Grass
                        && action == ClickAction.Build
                        && type != Block.Sapling
                        && type != Block.Leaves
                        && type != Block.Glass
                        && type != Block.YellowFlower
                        && type != Block.RedFlower
                        && type != Block.RedMushroom
                        && type != Block.BrownMushroom
                        && type != Block.Rope
                        && type != Block.Fire
                        && type != Block.Ice)
                    {
                        // Grass -> Dirt function
                        blockUpdate = new BlockUpdate(this, coordBelow, Block.Dirt);
                        Info.ProcessBlockPlaced((byte)Block.Dirt);
                        map.QueueUpdate(blockUpdate);
                        RevertBlockNow(coordBelow);
                        RaisePlayerPlacedBlockEvent(this, World.Map, coordBelow, Block.Grass, Block.Dirt, BlockChangeContext.Replaced);
                        SendNow(Packet.MakeSetBlock(coordBelow, Block.Dirt));
                    }
                    // handle normal blocks
                    blockUpdate = new BlockUpdate(this, coord, type);
                    Info.ProcessBlockPlaced((byte)type);
                    Block old = map.GetBlock(coord);
                    map.QueueUpdate(blockUpdate);
                    RaisePlayerPlacedBlockEvent(this, World.Map, coord, old, type, context);
                    SendNow(Packet.MakeSetBlock(coord, type));
                    
                    break;
                    
                case CanPlaceResult.BlocktypeDenied:
                    Message( "&WYou are not permitted to affect this block type." );
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.RankDenied:
                    Message( "&WYour rank is not allowed to build." );
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.WorldDenied:
                    switch( World.BuildSecurity.CheckDetailed( Info ) ) {
                        case SecurityCheckResult.RankTooLow:
                            Message( "&WYour rank is not allowed to build in this world." );
                            break;
                        case SecurityCheckResult.BlackListed:
                            Message( "&WYou are not allowed to build in this world." );
                            break;
                    }
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.ZoneDenied:
                    Zone deniedZone = WorldMap.Zones.FindDenied( coord, this );
                    if( deniedZone != null ) {
                        if (deniedZone.Name.ToLower().StartsWith("sign") && deniedZone.Bounds.Height == 1 && deniedZone.Bounds.Length == 1 && deniedZone.Bounds.Width == 1)
                        {
                            if (deniedZone.Sign == null)
                            {
                                FileInfo SignInfo = new FileInfo("./signs/" + World.Name + "/" + deniedZone.Name + ".txt");
                                if (SignInfo.Exists)
                                {
                                    string[] SignList = File.ReadAllLines("./signs/" + World.Name + "/" + deniedZone.Name + ".txt");
                                    string SignMessage = "";
                                    foreach (string line in SignList)
                                    {
                                        SignMessage += line + "&n";
                                    }
                                    Message(SignMessage);
                                    Logger.Log(LogType.Debug, "[Signs] {0} clicked on sign [{1}] On map [{2}]", Name, deniedZone.Name, deniedZone.Map);
                                    LastSignClicked = deniedZone.Name;
                                }
                                //else Message("&WSignFile for this signpost not found!&n.Looking For: &e./signs/" + World.Name + "/" + deniedZone.Name + "&w.");
                                else
                                {
                                    Message("&WThis zone, {0}&W,  is marked as a signpost, but no text is added to the sign!", deniedZone.ClassyName);
                                    Logger.Log(LogType.Debug, "[Signs] {0} clicked on an empty sign [{1}] On map: [{2}]", Name, deniedZone.Name, deniedZone.Map);
                                    LastSignClicked = deniedZone.Name;
                                }
                            }
                            else
                            {
                                Message("&WThis zone, {0}&W,  is marked as a signpost, but no text is added to the sign!", deniedZone.ClassyName);
                                Logger.Log(LogType.Debug, "[Signs] {0} clicked on an empty sign [{1}] On map: [{2}]", Name, deniedZone.Name, deniedZone.Map);
                                LastSignClicked = deniedZone.Name;
                            }
                        }
                        else if (deniedZone.Name.ToLower() == "spawn")
                        {
                            Message("&WThis is the Spawn zone. To build, please exit the spawn.", deniedZone.Name);
                        }
                        else Message("&WYou are not allowed to build in zone \"{0}\".", deniedZone.Name);
                    } else {
                        Message( "&WYou are not allowed to build here." );
                    }
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.CTFDenied:
                    RevertBlockNow(coord);
                    break;

                case CanPlaceResult.PluginDenied:
                    RevertBlockNow( coord );
                    break;

                //case CanPlaceResult.PluginDeniedNoUpdate:
                //    break;
            }
            return false;
        }

        /// <summary> Sends a block change to THIS PLAYER ONLY. Does not affect the map. </summary>
        /// <param name="coords"> Coordinates of the block. </param>
        /// <param name="block"> Block type to send. </param>
        public void SendBlock( Vector3I coords, Block block ) {
            if( !WorldMap.InBounds( coords ) ) throw new ArgumentOutOfRangeException( "coords" );
            SendLowPriority( Packet.MakeSetBlock( coords, block ) );
        }


        /// <summary> Gets the block from given location in player's world,
        /// and sends it (async) to the player.
        /// Used to undo player's attempted block placement/deletion. </summary>
        public void RevertBlock( Vector3I coords ) {
            SendLowPriority( Packet.MakeSetBlock( coords, WorldMap.GetBlock( coords ) ) );
        }


        // Gets the block from given location in player's world, and sends it (sync) to the player.
        // Used to undo player's attempted block placement/deletion.
        // To avoid threading issues, only use this from this player's IoThread.
        void RevertBlockNow( Vector3I coords ) {
            SendNow(Packet.MakeSetBlock(coords, WorldMap.GetBlock(coords)));
        }


        // returns true if the player is spamming and should be kicked.
        bool CheckBlockSpam() {
            if( Info.Rank.AntiGriefBlocks == 0 || Info.Rank.AntiGriefSeconds == 0 ) return false;
            if( spamBlockLog.Count >= Info.Rank.AntiGriefBlocks ) {
                DateTime oldestTime = spamBlockLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < Info.Rank.AntiGriefSeconds ) {
                    KickNow( "You were kicked by antigrief system. Slow down.", LeaveReason.BlockSpamKick );
                    Server.Message( "{0}&W was kicked for suspected griefing.", ClassyName );
                    Logger.Log( LogType.SuspiciousActivity,
                                "{0} was kicked for block spam ({1} blocks in {2} seconds)",
                                Name, Info.Rank.AntiGriefBlocks, spamTimer );
                    return true;
                }
            }
            spamBlockLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion


        #region Binding

        readonly Block[] bindings = new Block[67];

        public void Bind( Block type, Block replacement ) {
            bindings[(byte)type] = replacement;
        }

        public void ResetBind( Block type ) {
            bindings[(byte)type] = type;
        }

        public void ResetBind( [NotNull] params Block[] types ) {
            if( types == null ) throw new ArgumentNullException( "types" );
            foreach( Block type in types ) {
                ResetBind( type );
            }
        }

        public Block GetBind( Block type ) {
            return bindings[(byte)type];
        }

        public void ResetAllBinds() {
            foreach( Block block in Enum.GetValues( typeof( Block ) ) ) {
                if( block != Block.None ) {
                    ResetBind( block );
                }
            }
        }

        #endregion


        #region Permission Checks

        /// <summary> Returns true if player has ALL of the given permissions. </summary>
        public bool Can( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            return IsSuper || permissions.All( Info.Rank.Can );
        }


        /// <summary> Returns true if player has ANY of the given permissions. </summary>
        public bool CanAny( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            return IsSuper || permissions.Any( Info.Rank.Can );
        }


        /// <summary> Returns true if player has the given permission. </summary>
        public bool Can( Permission permission ) {
            return IsSuper || Info.Rank.Can( permission );
        }


        /// <summary> Returns true if player has the given permission,
        /// and is allowed to affect players of the given rank. </summary>
        public bool Can( Permission permission, [NotNull] Rank other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return IsSuper || Info.Rank.Can( permission, other );
        }


        /// <summary> Returns true if player is allowed to run
        /// draw commands that affect a given number of blocks. </summary>
        public bool CanDraw( int volume ) {
            if( volume < 0 ) throw new ArgumentOutOfRangeException( "volume" );
            return IsSuper || (Info.Rank.DrawLimit == 0) || (volume <= Info.Rank.DrawLimit);
        }


        /// <summary> Returns true if player is allowed to join a given world. </summary>
        public bool CanJoin( [NotNull] World worldToJoin ) {
            if( worldToJoin == null ) throw new ArgumentNullException( "worldToJoin" );
            return IsSuper || worldToJoin.AccessSecurity.Check( Info );
        }


        /// <summary> Checks whether player is allowed to place a block on the current world at given coordinates.
        /// Raises the PlayerPlacingBlock event. </summary>
        public CanPlaceResult CanPlace( [NotNull] Map map, Vector3I coords, Block newBlock, BlockChangeContext context ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            CanPlaceResult result;

            // check whether coordinate is in bounds
            Block oldBlock = map.GetBlock( coords );
            if( oldBlock == Block.None ) {
                result = CanPlaceResult.OutOfBounds;
                goto eventCheck;
            }

            // check special blocktypes
            if( newBlock == Block.Admincrete && !Can( Permission.PlaceAdmincrete ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            } else if( (newBlock == Block.Water || newBlock == Block.StillWater) && !Can( Permission.PlaceWater ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            } else if( (newBlock == Block.Lava || newBlock == Block.StillLava) && !Can( Permission.PlaceLava ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }

            // check admincrete-related permissions
            if( oldBlock == Block.Admincrete && !Can( Permission.DeleteAdmincrete ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }

            // check zones & world permissions
            PermissionOverride zoneCheckResult = map.Zones.Check( coords, this );
            if( zoneCheckResult == PermissionOverride.Allow ) {
                result = CanPlaceResult.Allowed;
                goto eventCheck;
            } else if( zoneCheckResult == PermissionOverride.Deny ) {
                result = CanPlaceResult.ZoneDenied;
                goto eventCheck;
            }

            // Check world permissions
            World mapWorld = map.World;
            if( mapWorld != null ) {
                switch( mapWorld.BuildSecurity.CheckDetailed( Info ) ) {
                    case SecurityCheckResult.Allowed:
                        // Check world's rank permissions
                        if( (Can( Permission.Build ) || newBlock == Block.Air) &&
                            (Can( Permission.Delete ) || oldBlock == Block.Air) ) {
                            result = CanPlaceResult.Allowed;
                        } else {
                            result = CanPlaceResult.RankDenied;
                        }
                        break;

                    case SecurityCheckResult.WhiteListed:
                        result = CanPlaceResult.Allowed;
                        break;

                    default:
                        result = CanPlaceResult.WorldDenied;
                        break;
                }
            } else {
                result = CanPlaceResult.Allowed;
            }

        eventCheck:
            var handler = PlacingBlock;
            if( handler == null ) return result;

            var e = new PlayerPlacingBlockEventArgs( this, map, coords, oldBlock, newBlock, context, result );
            handler( null, e );
            return e.Result;
        }


        /// <summary> Whether this player can currently see another player as being online.
        /// Players can always see themselves. Super players (e.g. Console) can see all.
        /// Hidden players can only be seen by those of sufficient rank. </summary>
        public bool CanSee( [NotNull] Player other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return other == this ||
                   IsSuper ||
                   !other.Info.IsHidden ||
                   Info.Rank.CanSee( other.Info.Rank );
        }


        /// <summary> Whether this player can currently see another player moving.
        /// Behaves very similarly to CanSee method, except when spectating:
        /// Spectators and spectatee cannot see each other.
        /// Spectators can only be seen by those who'd be able to see them hidden. </summary>
        public bool CanSeeMoving( [NotNull] Player otherPlayer ) {
            if( otherPlayer == null ) throw new ArgumentNullException( "otherPlayer" );
            // Check if player can see otherPlayer while they hide/spectate, and whether otherPlayer is spectating player
            bool canSeeOther = (otherPlayer.spectatedPlayer == null && !otherPlayer.Info.IsHidden) ||
                               (otherPlayer.spectatedPlayer != this && Info.Rank.CanSee( otherPlayer.Info.Rank ));

            // Check if player is spectating otherPlayer, or if they're spectating the same target
            bool hideOther = (spectatedPlayer == otherPlayer) ||
                             (spectatedPlayer != null && spectatedPlayer == otherPlayer.spectatedPlayer);

            return otherPlayer == this || // players can see self
                   IsSuper || // superplayers have ALL permissions
                   canSeeOther && !hideOther;
        }


        /// <summary> Whether this player should see a given world on the /Worlds list by default. </summary>
        public bool CanSee( [NotNull] World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            return CanJoin( world ) && !world.IsHidden;
        }

        #endregion


        #region Undo / Redo

        readonly LinkedList<UndoState> undoStack = new LinkedList<UndoState>();
        readonly LinkedList<UndoState> redoStack = new LinkedList<UndoState>();


        [CanBeNull]
        internal UndoState RedoPop() {
            if( redoStack.Count > 0 ) {
                var lastNode = redoStack.Last;
                redoStack.RemoveLast();
                return lastNode.Value;
            } else {
                return null;
            }
        }


        [NotNull]
        internal UndoState RedoBegin( DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            undoStack.AddLast( newState );
            return newState;
        }


        [NotNull]
        internal UndoState UndoBegin( DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            redoStack.AddLast( newState );
            return newState;
        }


        [CanBeNull]
        public UndoState UndoPop() {
            if( undoStack.Count > 0 ) {
                var lastNode = undoStack.Last;
                undoStack.RemoveLast();
                return lastNode.Value;
            } else {
                return null;
            }
        }

        public UndoState DrawBegin( DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            undoStack.AddLast( newState );
            if( undoStack.Count > ConfigKey.MaxUndoStates.GetInt() ) {
                undoStack.RemoveFirst();
            }
            redoStack.Clear();
            return newState;
        }

        public void UndoClear() {
            undoStack.Clear();
        }

        public void RedoClear() {
            redoStack.Clear();
        }

        #endregion


        #region Drawing, Selection

        [NotNull]
        public IBrush Brush { get; set; }

        [CanBeNull]
        public DrawOperation LastDrawOp { get; set; }

        /// <summary> Whether clicks should be registered towards selection marks. </summary>
        public bool DisableClickToMark { get; set; }

        /// <summary> Whether player is currently making a selection. </summary>
        public bool IsMakingSelection {
            get { return SelectionMarksExpected > 0; }
        }

        /// <summary> Number of selection marks so far. </summary>
        public int SelectionMarkCount {
            get { return selectionMarks.Count; }
        }

        /// <summary> Number of marks expected to complete the selection. </summary>
        public int SelectionMarksExpected { get; private set; }

        /// <summary> Whether player is repeating a selection (/static) </summary>
        public bool IsRepeatingSelection { get; set; }

        [CanBeNull]
        CommandReader selectionRepeatCommand;

        [CanBeNull]
        SelectionCallback selectionCallback;

        readonly Queue<Vector3I> selectionMarks = new Queue<Vector3I>();

        [CanBeNull]
        object selectionArgs;

        [CanBeNull]
        Permission[] selectionPermissions;


        public void SelectionAddMark( Vector3I pos, bool executeCallbackIfNeeded ) {
            if( !IsMakingSelection ) throw new InvalidOperationException( "No selection in progress." );
            selectionMarks.Enqueue( pos );
            if( SelectionMarkCount >= SelectionMarksExpected ) {
                if( executeCallbackIfNeeded ) {
                    SelectionExecute();
                } else {
                    Message( "Last block marked at {0}. Type &H/Mark&S or click any block to continue.", pos );
                }
            } else {
                Message( "Block #{0} marked at {1}. Place mark #{2}.",
                         SelectionMarkCount, pos, SelectionMarkCount + 1 );
            }
        }


        public void SelectionExecute() {
            if( !IsMakingSelection || selectionCallback == null ) {
                throw new InvalidOperationException( "No selection in progress." );
            }
            SelectionMarksExpected = 0;
            // check if player still has the permissions required to complete the selection.
            if( selectionPermissions == null || Can( selectionPermissions )) {
                selectionCallback( this, selectionMarks.ToArray(), selectionArgs );
                if( IsRepeatingSelection && selectionRepeatCommand != null ) {
                    selectionRepeatCommand.Rewind();
                    CommandManager.ParseCommand( this, selectionRepeatCommand, this == Console );
                }
                selectionMarks.Clear();
            } else {
                // More complex permission checks can be done in the callback function itself.
                Message( "&WYou are no longer allowed to complete this action." );
                MessageNoAccess( selectionPermissions );
            }
        }


        public void SelectionStart( int marksExpected,
                                    [NotNull] SelectionCallback callback,
                                    [CanBeNull] object args,
                                    [CanBeNull] params Permission[] requiredPermissions ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            selectionArgs = args;
            SelectionMarksExpected = marksExpected;
            selectionMarks.Clear();
            selectionCallback = callback;
            selectionPermissions = requiredPermissions;
            if( DisableClickToMark ) {
                Message( "&8Reminder: Click-to-mark is disabled." );
            }
        }


        public void SelectionResetMarks() {
            selectionMarks.Clear();
        }


        public void SelectionCancel() {
            selectionMarks.Clear();
            SelectionMarksExpected = 0;
            selectionCallback = null;
            selectionArgs = null;
            selectionPermissions = null;
        }

        #endregion


        #region Copy/Paste

        CopyState[] copyInformation;
        public CopyState[] CopyInformation {
            get { return copyInformation; }
        }

        int copySlot;
        public int CopySlot {
            get { return copySlot; }
            set {
                if( value < 0 || value > Info.Rank.CopySlots ) {
                    throw new ArgumentOutOfRangeException( "value" );
                }
                copySlot = value;
            }
        }

        internal void InitCopySlots() {
            Array.Resize( ref copyInformation, Info.Rank.CopySlots );
            CopySlot = Math.Min( CopySlot, Info.Rank.CopySlots - 1 );
        }

        [CanBeNull]
        public CopyState GetCopyInformation() {
            return CopyInformation[copySlot];
        }

        public void SetCopyInformation( [CanBeNull] CopyState info ) {
            if( info != null ) info.Slot = copySlot;
            CopyInformation[copySlot] = info;
        }

        #endregion

        [CanBeNull]
        Player possessionPlayer;

        #region Spectating

        [CanBeNull]
        Player spectatedPlayer;

        /// <summary> Player currently being spectated. Use Spectate/StopSpectate methods to set. </summary>
        [CanBeNull]
        public Player SpectatedPlayer {
            get { return spectatedPlayer; }
        }

        /// <summary> While spectating, currently-specated player.
        /// When not spectating, most-recently-spectated player. </summary>
        [CanBeNull]
        public PlayerInfo LastSpectatedPlayer { get; private set; }

        readonly object spectateLock = new object();

        /// <summary> Whether this player is currently spectating someone. </summary>
        public bool IsSpectating {
            get { return (spectatedPlayer != null); }
        }


        /// <summary> Starts spectating the given player. </summary>
        /// <param name="target"> Player to spectate. </param>
        /// <returns> True if this player is now spectating the target.
        /// False if this player has already been spectating the target. </returns>
        /// <exception cref="ArgumentNullException"> If target is null. </exception>
        /// <exception cref="PlayerOpException"> If this player does not have sufficient permissions,
        /// or if trying to spectate self. </exception>
        public bool Spectate( [NotNull] Player target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            lock( spectateLock ) {
                if( spectatedPlayer == target ) return false;

                if( target == this ) {
                    PlayerOpException.ThrowCannotTargetSelf( this, Info, "spectate" );
                }

                if( !Can( Permission.Spectate, target.Info.Rank ) ) {
                    PlayerOpException.ThrowPermissionLimit( this, target.Info, "spectate", Permission.Spectate );
                }

                spectatedPlayer = target;
                LastSpectatedPlayer = target.Info;
                Message( "Now spectating {0}&S. Type &H/unspec&S to stop.", target.ClassyName );
                return true;
            }
        }

        //used for impersonation (skin changing)
        //if null, default skin is used
        public string iName = null;
        public bool entityChanged = false;

        /// <summary> Stops spectating. </summary>
        /// <returns> True if this player was spectating someone (and now stopped).
        /// False if this player was not spectating anyone. </returns>
        public bool StopSpectating()
        {
            lock (spectateLock)
            {
                if (spectatedPlayer == null) return false;
                Message("Stopped spectating {0}", spectatedPlayer.ClassyName);
                spectatedPlayer = null;
                return true;
            }
        }
                public bool Possess([NotNull] Player target)
        {
            if (target == null) throw new ArgumentNullException("target");
            lock (spectateLock)
            {
                if (target == this)
                {
                    PlayerOpException.ThrowCannotTargetSelf(this, Info, "possess");
                }

                if (!Can(Permission.Import, target.Info.Rank))
                {
                    PlayerOpException.ThrowPermissionLimit(this, target.Info, "possess", Permission.Import);
                }

                if (target.possessionPlayer == this) return false;

                target.possessionPlayer = this;
                Message("Now Possessing {0}&S. Type &H/unpossess&S to stop.", target.ClassyName);
                return true;
            }
        }
        public bool StopPossessing([NotNull]Player target)
        {
            lock (spectateLock)
            {
                if (target.possessionPlayer == null) return false;
                Message("Stopped possessing {0}", target.ClassyName);
                target.possessionPlayer = null;
                return true;
            }
        }

        #endregion


        #region Static Utilities

        static readonly Uri PaidCheckUri = new Uri( "http://www.minecraft.net/haspaid.jsp?user=" );
        const int PaidCheckTimeout = 5000;


        /// <summary> Checks whether a given player has a paid minecraft.net account. </summary>
        /// <returns> True if the account is paid. False if it is not paid, or if information is unavailable. </returns>
        public static AccountType CheckPaidStatus( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( PaidCheckUri + Uri.EscapeDataString( name ) );
            request.ServicePoint.BindIPEndPointDelegate = Server.BindIPEndPointCallback;
            request.Timeout = PaidCheckTimeout;
            request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );

            try {
                using( WebResponse response = request.GetResponse() ) {
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        string paidStatusString = responseReader.ReadToEnd();
                        bool isPaid;
                        if( Boolean.TryParse( paidStatusString, out isPaid ) ) {
                            if( isPaid ) {
                                return AccountType.Paid;
                            } else {
                                return AccountType.Free;
                            }
                        } else {
                            return AccountType.Unknown;
                        }
                    }
                }
            } catch( WebException ex ) {
                Logger.Log( LogType.Warning,
                            "Could not check paid status of player {0}: {1}",
                            name, ex.Message );
                return AccountType.Unknown;
            }
        }

        static readonly Regex
            EmailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,6}$", RegexOptions.Compiled),
            AccountRegex = new Regex(@"^[a-zA-Z0-9._]{2,16}$", RegexOptions.Compiled),
            PlayerNameRegex = new Regex(@"^([a-zA-Z0-9._]{2,16}|[a-zA-Z0-9._]{1,15}@\d*)$", RegexOptions.Compiled);


        /// <summary> Checks if given string could be an email address.
        /// Matches 99.9% of emails. We don't care about the last 0.1% (and neither does Mojang).
        /// Regex courtesy of http://www.regular-expressions.info/email.html </summary>
        public static bool IsValidEmail([NotNull] string email)
        {
            if (email == null) throw new ArgumentNullException("email");
            return EmailRegex.IsMatch(email);
        }


        /// <summary> Ensures that a player name has the correct length and character set for a Minecraft account.
        /// Does not permit email addresses. </summary>
        public static bool IsValidAccountName([NotNull] string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            return AccountRegex.IsMatch(name);
        }

        /// <summary> Ensures that a player name has the correct length and character set. </summary>
        public static bool IsValidPlayerName([NotNull] string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            return PlayerNameRegex.IsMatch(name);
        }
        
        /// <summary> Ensures that a player name has the correct length and character set. </summary>
        public static bool ContainsValidCharacters( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            for( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if( (ch < '0' && ch != '.') || (ch > '9' && ch < '@') || (ch > 'Z' && ch < '_') || 
                    (ch > '_' && ch < 'a') || ch > 'z' ) {
                    return false;
                }
            }
            return true;
        }

        #endregion


        /// <summary> Teleports player to a given coordinate within this map. </summary>
        public void TeleportTo( Position pos ) {
            StopSpectating();
            Send( Packet.MakeSelfTeleport( pos ) );
            Position = pos;
        }


        /// <summary> Time since the player was last active (moved, talked, or clicked). </summary>
        public TimeSpan IdBotTime {
            get {
                return DateTime.UtcNow.Subtract( LastActiveTime );
            }
        }


        /// <summary> Resets the IdBotTimer to 0. </summary>
        public void ResetIdBotTimer()
        {
            //if (this.isSolid) // && this.isPlayingGame)
            //{
            //    this.Message("&7You moved and are no longer a solid block.");
            //    this.WorldMap.SetBlock(this.lastSolidPos, this.solidPosBlock);
            //    BlockUpdate blockUpdate = new BlockUpdate(null, this.lastSolidPos, this.solidPosBlock);
            //    this.World.Map.QueueUpdate(blockUpdate);
            //    this.isSolid = false;
            //    this.Info.IsHidden = false;
            //    RaisePlayerHideChangedEvent(this, false, true);
            //}
            LastActiveTime = DateTime.UtcNow;
        }


        const string CustomBlocksExtName = "CustomBlocks";
        const int CustomBlocksExtVersion = 1;
        const byte CustomBlocksLevel = 1;
        const string BlockPermissionsExtName = "BlockPermissions";
        const int BlockPermissionsExtVersion = 1;    
        const string ClickDistanceExtName = "ClickDistance";
        const int ClickDistanceExtVersion = 1;
        const string EnvColorsExtName = "EnvColors";
        const int EnvColorsExtVersion = 1;
        const string ChangeModelExtName = "ChangeModel";
        const int ChangeModelExtVersion = 1;
        const string EnvMapAppearanceExtName = "EnvMapAppearance";
        const int EnvMapAppearanceExtVersion = 1;
        const string EnvWeatherTypeExtName = "EnvWeatherType";
        const int EnvWeatherTypeExtVersion = 1;
        const string HeldBlockExtName = "HeldBlock";
        const int HeldBlockExtVersion = 1;
        const string ExtPlayerListExtName = "ExtPlayerList";
        const int ExtPlayerListExtVersion = 1;
        const string SelectionCuboidExtName = "SelectionCuboid";
        const int SelectionCuboidExtVersion = 1;
        const string MessageTypesExtName = "MessageTypes";
        const int MessageTypesExtVersion = 1;
        const string HackControlExtName = "HackControl";
        const int HackControlExtVersion = 1;
        const string EmoteFixExtName = "EmoteFix";
        const int EmoteFixExtVersion = 1;

        // Note: if more levels are added, change UsesCustomBlocks from bool to int
        public bool UsesCustomBlocks { get; set; }
        public bool SupportsBlockPermissions { get; set; }
        public bool SupportsClickDistance { get; set; }
        public bool SupportsEnvColors { get; set; }
        public bool SupportsChangeModel { get; set; }
        public bool SupportsEnvMapAppearance { get; set; }
        public bool SupportsEnvWeatherType { get; set; }
        public bool SupportsHeldBlock { get; set; }
        public bool SupportsExtPlayerList { get; set; }
        public bool SupportsSelectionCuboid { get; set; }
        public bool SupportsMessageTypes { get; set; }
        public bool SupportsHackControl { get; set; }
        public bool SupportsEmoteFix { get; set; }
        string ClientName { get; set; }

        bool NegotiateProtocolExtension()
        {
            // write our ExtInfo and ExtEntry packets
            writer.Write(Packet.MakeExtInfo("ProCraft", 13).Bytes);
            writer.Write(Packet.MakeExtEntry(CustomBlocksExtName, CustomBlocksExtVersion).Bytes);
            writer.Write(Packet.MakeExtEntry(BlockPermissionsExtName, BlockPermissionsExtVersion).Bytes);
            writer.Write(Packet.MakeExtEntry(ClickDistanceExtName, ClickDistanceExtVersion).Bytes);
            writer.Write(Packet.MakeExtEntry(EnvColorsExtName, EnvColorsExtVersion).Bytes);
            writer.Write(Packet.MakeExtEntry(ChangeModelExtName, ChangeModelExtVersion).Bytes);
            writer.Write(Packet.MakeExtEntry(EnvMapAppearanceExtName, EnvMapAppearanceExtVersion).Bytes);
            writer.Write(Packet.MakeExtEntry(EnvWeatherTypeExtName, EnvWeatherTypeExtVersion).Bytes);
            writer.Write(Packet.MakeExtEntry(HeldBlockExtName, HeldBlockExtVersion).Bytes);
            writer.Write(Packet.MakeExtEntry(ExtPlayerListExtName, ExtPlayerListExtVersion).Bytes);
            writer.Write(Packet.MakeExtEntry(SelectionCuboidExtName, SelectionCuboidExtVersion).Bytes);
            writer.Write(Packet.MakeExtEntry(MessageTypesExtName, MessageTypesExtVersion).Bytes);
            writer.Write( Packet.MakeExtEntry( HackControlExtName, HackControlExtVersion ).Bytes );
            writer.Write( Packet.MakeExtEntry( EmoteFixExtName, EmoteFixExtVersion ).Bytes );

            // Expect ExtInfo reply from the client
            OpCode extInfoReply = reader.ReadOpCode();
            //Logger.Log(LogType.Debug, "Expected: {0} / Received: {1}", OpCode.ExtInfo, extInfoReply );
            if (extInfoReply != OpCode.ExtInfo)
            {
                Logger.Log(LogType.Debug, "Player {0}: Unexpected ExtInfo reply ({1})", Info.Name, extInfoReply);
                return false;
            }
            ClientName = reader.ReadString();
            int expectedEntries = reader.ReadInt16();

            // wait for client to send its ExtEntries
            bool sendCustomBlockPacket = false;
            List<string> clientExts = new List<string>();
            for (int i = 0; i < expectedEntries; i++) {
                // Expect ExtEntry replies (0 or more)
                OpCode extEntryReply = reader.ReadOpCode();
                if (extEntryReply != OpCode.ExtEntry) {
                    Logger.Log(LogType.Warning, "Player {0} from {1}: Unexpected ExtEntry reply ({2})", Name, IP,
                        extInfoReply);
                    return false;
                }
                string extName = reader.ReadString();
                int extVersion = reader.ReadInt32();
                //Logger.Log(LogType.Debug, "Expected: {0} / Received: {1} {2} {3}", OpCode.ExtEntry, extEntryReply, extName, extVersion);
                if (extName == CustomBlocksExtName && extVersion == CustomBlocksExtVersion) {
                    // Hooray, client supports custom blocks! We still need to check support level.
                    UsesCustomBlocks = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == BlockPermissionsExtName && extVersion == BlockPermissionsExtVersion) {
                    this.SupportsBlockPermissions = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == ClickDistanceExtName && extVersion == ClickDistanceExtVersion) {
                    this.SupportsClickDistance = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == EnvColorsExtName && extVersion == EnvColorsExtVersion) {
                    this.SupportsEnvColors = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == ChangeModelExtName && extVersion == ChangeModelExtVersion) {
                    this.SupportsChangeModel = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == EnvMapAppearanceExtName && extVersion == EnvMapAppearanceExtVersion) {
                    this.SupportsEnvMapAppearance = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == EnvWeatherTypeExtName && extVersion == EnvWeatherTypeExtVersion) {
                    this.SupportsEnvWeatherType = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == HeldBlockExtName && extVersion == HeldBlockExtVersion) {
                    this.SupportsHeldBlock = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == ExtPlayerListExtName && extVersion == ExtPlayerListExtVersion) {
                    this.SupportsExtPlayerList = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == SelectionCuboidExtName && extVersion == SelectionCuboidExtVersion) {
                    this.SupportsSelectionCuboid = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == MessageTypesExtName && extVersion == MessageTypesExtVersion) {
                    this.SupportsMessageTypes = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == HackControlExtName && extVersion == HackControlExtVersion) {
                    SupportsHackControl = true;
                    clientExts.Add(extName + " " + extVersion);
                }
                if (extName == EmoteFixExtName && extVersion == EmoteFixExtVersion) {
                    SupportsEmoteFix = true;
                    clientExts.Add(extName + " " + extVersion);
                }
            }

            // log client's capabilities
            if (clientExts.Count > 0)
            {
                Logger.Log(LogType.Debug, "Player {0} is using \"{1}\", supporting: {2}",
                            Info.Name,
                            ClientName,
                            clientExts.JoinToString(", "));
            }
            if (clientExts.Count == 0)
            {
                Kick("Please use the ClassiCube.net client", LeaveReason.InvalidOpcodeKick);
            }

            // if client also supports CustomBlockSupportLevel, figure out what level to use

            // Send CustomBlockSupportLevel
            writer.Write(Packet.MakeCustomBlockSupportLevel(CustomBlocksLevel).Bytes);

            // Expect CustomBlockSupportLevel reply
            OpCode customBlockSupportLevelReply = reader.ReadOpCode();
            //Logger.Log( LogType.Debug, "Expected: {0} / Received: {1}", OpCode.CustomBlockSupportLevel, customBlockSupportLevelReply );
            if (customBlockSupportLevelReply != OpCode.CustomBlockSupportLevel)
            {
                Logger.Log(LogType.Warning, "Player {0} from {1}: Unexpected CustomBlockSupportLevel reply ({2})",
                                   Info.Name,
                                   IP,
                                   customBlockSupportLevelReply);
                return false;
            }
            byte clientLevel = reader.ReadByte();
            UsesCustomBlocks = (clientLevel >= CustomBlocksLevel);
            return true;
        }

        // For non-extended players, use appropriate substitution
        public Packet ProcessOutgoingSetBlock(Packet packet) {
            if (packet.Bytes[7] > (byte) Map.MaxLegalBlockType && !this.UsesCustomBlocks) {
                packet.Bytes[7] = (byte) Map.GetFallbackBlock((Block) packet.Bytes[7]);
            }
            return packet;
        }

        #region Kick

        /// <summary> Advanced kick command. </summary>
        /// <param name="player"> Player who is kicking. </param>
        /// <param name="reason"> Reason for kicking. May be null or blank if allowed by server configuration. </param>
        /// <param name="context"> Classification of kick context. </param>
        /// <param name="announce"> Whether the kick should be announced publicly on the server and IRC. </param>
        /// <param name="raiseEvents"> Whether Player.BeingKicked and Player.Kicked events should be raised. </param>
        /// <param name="recordToPlayerDB"> Whether the kick should be counted towards player's record.</param>
        public void Kick( [NotNull] Player player, [CanBeNull] string reason, LeaveReason context,
                          bool announce, bool raiseEvents, bool recordToPlayerDB ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( !Enum.IsDefined( typeof( LeaveReason ), context ) ) {
                throw new ArgumentOutOfRangeException( "context" );
            }
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            // Check if player can ban/unban in general
            if( !player.Can( Permission.Kick ) ) {
                PlayerOpException.ThrowPermissionMissing( player, Info, "kick", Permission.Kick );
            }

            // Check if player is trying to ban/unban self
            if (player == this)
            {
                PlayerOpException.ThrowCannotTargetSelf( player, Info, "kick" );
            }

            // Check if player has sufficiently high permission limit
            if( !player.Can( Permission.Kick, Info.Rank ) ) {
                PlayerOpException.ThrowPermissionLimit( player, Info, "kick", Permission.Kick );
            }

            // check if kick reason is missing but required
            PlayerOpException.CheckKickReason( reason, player, Info );

            // raise Player.BeingKicked event
            if( raiseEvents ) {
                var e = new PlayerBeingKickedEventArgs( this, player, reason, announce, recordToPlayerDB, context );
                RaisePlayerBeingKickedEvent( e );
                if( e.Cancel ) PlayerOpException.ThrowCancelled( player, Info );
                recordToPlayerDB = e.RecordToPlayerDB;
            }

            // actually kick
            string kickReason;
            if( reason != null ) {
                kickReason = String.Format( "&eKicked by {0}&e: {1}", player.Name, reason );
            } else {
                kickReason = String.Format( "&eKicked by {0}", player.Name );
            }
            Kick( kickReason, context );

            // log and record kick to PlayerDB
            Logger.Log( LogType.UserActivity,
                        "{0} kicked {1}. Reason: {2}",
                        player.Name, Name, reason ?? "" );
            if( recordToPlayerDB ) {
                Info.ProcessKick( player, reason );
            }

            // announce kick
            if( announce ) {
                if( reason != null && ConfigKey.AnnounceKickAndBanReasons.Enabled() ) {
                    Server.Message( "{0}&W was kicked by {1}&W: {2}",
                                    ClassyName, player.ClassyName, reason );
                } else {
                    Server.Message( "{0}&W was kicked by {1}",
                                    ClassyName, player.ClassyName );
                }
            }

            // raise Player.Kicked event
            if( raiseEvents ) {
                var e = new PlayerKickedEventArgs( this, player, reason, announce, recordToPlayerDB, context );
                RaisePlayerKickedEvent( e );
            }
        }

        #endregion


        [CanBeNull]
        public string LastUsedPlayerName { get; set; }

        [CanBeNull]
        public string LastUsedWorldName { get; set; }


        /// <summary> Name formatted for the debugger. </summary>
        public override string ToString() {
            if( Info != null ) {
                return String.Format( "Player({0})", Info.Name );
            } else {
                return String.Format( "Player({0})", IP );
            }
        }
    }


    sealed class PlayerListSorter : IComparer<Player> {
        public static readonly PlayerListSorter Instance = new PlayerListSorter();

        public int Compare( Player x, Player y ) {
            if( x.Info.Rank == y.Info.Rank ) {
                return StringComparer.OrdinalIgnoreCase.Compare( x.Name, y.Name );
            } else {
                return x.Info.Rank.Index - y.Info.Rank.Index;
            }
        }
    }
}
﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Net;
using System.Collections.Generic;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    partial class Server {

        /// <summary> Occurs when the server is about to be initialized. </summary>
        public static event EventHandler Initializing;

        /// <summary> Occurs when the server has been initialized. </summary>
        public static event EventHandler Initialized;

        /// <summary> Occurs when the server is about to start. </summary>
        public static event EventHandler Starting;

        /// <summary> Occurs when the server has just started. </summary>
        public static event EventHandler Started;

        /// <summary> Occurs when the server is about to start shutting down. </summary>
        public static event EventHandler<ShutdownEventArgs> ShutdownBegan;

        /// <summary> Occurs when the server finished shutting down. </summary>
        public static event EventHandler<ShutdownEventArgs> ShutdownEnded;

        /// <summary> Occurs when the player list has just changed (any time players connected or disconnected). </summary>
        public static event EventHandler PlayerListChanged;


        /// <summary> Occurs when a player is searching for players (with autocompletion).
        /// The list of players in the search results may be replaced. </summary>
        public static event EventHandler<SearchingForPlayerEventArgs> SearchingForPlayer;


        static void RaiseEvent( EventHandler h ) {
            if( h != null ) h( null, EventArgs.Empty );
        }

        static void RaiseShutdownBeganEvent( ShutdownParams shutdownParams ) {
            var h = ShutdownBegan;
            if( h != null ) h( null, new ShutdownEventArgs( shutdownParams ) );
        }

        static void RaiseShutdownEndedEvent( ShutdownParams shutdownParams ) {
            var h = ShutdownEnded;
            if( h != null ) h( null, new ShutdownEventArgs( shutdownParams ) );
        }

        internal static void RaisePlayerListChangedEvent() {
            RaiseEvent( PlayerListChanged );
        }


        #region Session-related

        /// <summary> Occurs any time the server receives an incoming connection (cancelable). </summary>
        public static event EventHandler<SessionConnectingEventArgs> SessionConnecting;


        /// <summary> Occurs any time a new session has connected, but before any communication is done. </summary>
        public static event EventHandler<PlayerEventArgs> SessionConnected;


        /// <summary> Occurs when a connection is closed or lost. </summary>
        public static event EventHandler<SessionDisconnectedEventArgs> SessionDisconnected;



        internal static bool RaiseSessionConnectingEvent( [NotNull] IPAddress ip ) {
            if( ip == null ) throw new ArgumentNullException( "ip" );
            var h = SessionConnecting;
            if( h == null ) return false;
            var e = new SessionConnectingEventArgs( ip );
            h( null, e );
            return e.Cancel;
        }


        internal static void RaiseSessionConnectedEvent( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            var h = SessionConnected;
            if( h != null ) h( null, new PlayerEventArgs( player ) );
        }


        internal static void RaiseSessionDisconnectedEvent( [NotNull] Player player, LeaveReason leaveReason ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            var h = SessionDisconnected;
            if( h != null ) h( null, new SessionDisconnectedEventArgs( player, leaveReason ) );
        }

        #endregion

    }
}


namespace fCraft.Events {

    public sealed class ShutdownEventArgs : EventArgs {
        internal ShutdownEventArgs( [NotNull] ShutdownParams shutdownParams ) {
            if( shutdownParams == null ) throw new ArgumentNullException( "shutdownParams" );
            ShutdownParams = shutdownParams;
        }

        [NotNull]
        public ShutdownParams ShutdownParams { get; private set; }
    }


    public sealed class SearchingForPlayerEventArgs : EventArgs, IPlayerEvent
    {
        internal SearchingForPlayerEventArgs([CanBeNull] Player player, [NotNull] string searchTerm,
                                             List<Player> matches, SearchOptions options)
        {
            if (searchTerm == null) throw new ArgumentNullException("searchTerm");
            Player = player;
            SearchTerm = searchTerm;
            Matches = matches;
            Options = options;
        }

        [CanBeNull]
        public Player Player { get; private set; }

        public string SearchTerm { get; private set; }
        public SearchOptions Options { get; private set; }

        public List<Player> Matches { get; set; }
    }
}
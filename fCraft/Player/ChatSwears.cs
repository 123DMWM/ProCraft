// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | Copyright 2014 123DMWM <shmo1joe2@gmail.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides a way to create and manage publicly-announced countdowns.
    /// Long swears announce once an hour (e.g. "7h left").
    /// During the last hour, swear announces more often: every 10 minutes, then every minute,
    /// then every 10 seconds, and finally every second - until the swear is up. </summary>
    public sealed class ChatSwears {
        /// <summary> Swear's unique numeric ID. </summary>
        public readonly int ID;

        /// <summary> Whether this swear has been aborted. </summary>
        public bool Aborted { get; private set; }

        /// <summary> Whether or not the timer is currently running. </summary>
        public bool IsRunning { get; private set; }

        /// <summary> Word marked as a swear. </summary>
        public string Swear { get; private set; }

        /// <summary> Word to replace the swear word in chat. </summary>
        public string Replacement { get; private set; }


        readonly SchedulerTask task;
       
        ChatSwears( string swear, string replacement ) {
            ID = Interlocked.Increment( ref swearCounter );
            Swear = swear.ToLower();
            Replacement = replacement;
            if (Replacement != null) {
                Replacement = Replacement.ToLower();
            }
            IsRunning = true;
            AddSwearToList( this );
            try
            {
                if (!(Directory.Exists("./Filters"))) Directory.CreateDirectory("./Filters");
                string[] output = { "Replace: " + Swear, "With: " + Replacement };
                File.WriteAllLines("./Filters/" + ID + "_" + Swear + ".txt", output);
            }
            catch (Exception ex)
            {
                Player.Console.Message("Filter Writer Has Crashed: {0}", ex);
            }
        }
                
        
        /// <summary> Stops this swear, and removes it from the list of swears. </summary>
        public void Abort()
        {
            Stop(true);
        }


        void Stop(bool aborted)
        {
            try
            {
                if (!(Directory.Exists("./Filters"))) Directory.CreateDirectory("./Filters");
                if (File.Exists("./Filters/" + ID + "_" + Swear + ".txt"))
                {
                    File.Delete("./Filters/" + ID + "_" + Swear + ".txt");
                }
            }
            catch (Exception ex)
            {
                Player.Console.Message("Filter Remover Has Crashed: {0}", ex);
            }
            IsRunning = false;
            Aborted = aborted; 
            RemoveSwearFromList( this );
            RaiseStoppedEvent( this );
        }


        #region Static
                

        /// <summary> Starts this swear with the specified duration, and end message. </summary>
        /// <param name="duration"> Amount of time the swear should run before completion. </param>
        /// <param name="message"> Message to display when swear reaches zero. May be null. </param>
        /// <param name="startedBy"> Name of player who started swear. May not be null. </param>
        /// <returns> Newly-created, and already-started swear. </returns>
        /// <exception cref="ArgumentNullException"> If startedBy is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If duration is less than one second. </exception>
        public static ChatSwears Start( string swear, string replacement ) {
            ChatSwears newSwear = new ChatSwears( swear, replacement );
            RaiseStartedEvent( newSwear );
            return newSwear;
        }

        static int swearCounter;
        static readonly object SwearListLock = new object();
        static readonly Dictionary<int, ChatSwears> Swears = new Dictionary<int, ChatSwears>();


        static void AddSwearToList( [NotNull] ChatSwears swear ) {
            if( swear == null ) throw new ArgumentNullException( "filter" );
            lock( SwearListLock ) {
                Swears.Add( swear.ID, swear );
            }
        }


        static void RemoveSwearFromList( [NotNull] ChatSwears swear ) {
            if( swear == null ) throw new ArgumentNullException( "filter" );
            lock( SwearListLock ) {
                Swears.Remove( swear.ID );
            }
        }


        /// <summary> Returns a list of all active swears. </summary>
        public static ChatSwears[] SwearList {
            get {
                lock( SwearListLock ) {
                    return Swears.Values.ToArray();
                }
            }
        }


        /// <summary> Searches for a swear by its numeric ID. </summary>
        /// <param name="id"> ID to search for. </param>
        /// <returns> ChatSwears object if found; null if not found. </returns>
        [CanBeNull]
        public static ChatSwears FindSwearById( int id ) {
            lock( SwearListLock ) {
                ChatSwears result;
                if( Swears.TryGetValue( id, out result ) ) {
                    return result;
                } else {
                    return null;
                }
            }
        }

        #endregion


        #region Events

        /// <summary> Occurs after a ChatSwears was added. </summary>
        public static event EventHandler<ChatSwearsEventArgs> Started;


        /// <summary> Occurs after a ChatSwears has expired or was aborted. </summary>
        public static event EventHandler<ChatSwearsEventArgs> Stopped;


        static void RaiseStartedEvent( ChatSwears swear ) {
            var h = Started;
            if( h != null ) h( null, new ChatSwearsEventArgs( swear ) );
        }


        static void RaiseStoppedEvent( ChatSwears swear ) {
            var h = Stopped;
            if( h != null ) h( null, new ChatSwearsEventArgs( swear ) );
        }

        #endregion
    }


    /// <summary> Provides data for ChatSwears.Started and ChatSwears.Stopped events. Immutable. </summary>
    public sealed class ChatSwearsEventArgs : EventArgs {
        public ChatSwearsEventArgs( ChatSwears swear ) {
            Swear = swear;
        }

        public ChatSwears Swear { get; private set; }
    }
}
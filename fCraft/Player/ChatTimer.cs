﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides a way to create and manage publicly-announced countdowns.
    /// Long timers announce once an hour (e.g. "7h left").
    /// During the last hour, timer announces more often: every 10 minutes, then every minute,
    /// then every 10 seconds, and finally every second - until the timer is up. </summary>
    public sealed class ChatTimer {
        /// <summary> Timer's unique numeric ID. </summary>
        public readonly int ID;

        /// <summary> Whether or not the timer is currently running. </summary>
        public bool IsRunning { get; private set; }

        /// <summary> Whether this timer has been aborted. </summary>
        public bool Aborted { get; private set; }

        /// <summary> Message to be displayed once the timer reaches zero. </summary>
        [CanBeNull]
        public string Message { get; private set; }

        /// <summary> Date/Time (UTC) at which this timer was started. </summary>
        public DateTime StartTime { get; private set; }

        /// <summary> Date/Time (UTC) at which this timer will end. </summary>
        public DateTime EndTime { get; private set; }

        /// <summary> The amount of time between when this timer was started and when it will end. </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary> The amount of time remaining in this timer. </summary>
        public TimeSpan TimeLeft {
            get { return EndTime.Subtract( DateTime.UtcNow ); }
        }

        /// <summary> Name of the player (or entity) who started this timer </summary>
        [NotNull]
        public string StartedBy { get; private set; }


        readonly SchedulerTask task;
        int announceIntervalIndex, lastHourAnnounced;


        ChatTimer( TimeSpan duration, [CanBeNull] string message, [NotNull] string startedBy ) {
            if( startedBy == null ) throw new ArgumentNullException( "startedBy" );
            StartedBy = startedBy;
            Message = message;
            StartTime = DateTime.UtcNow;
            EndTime = StartTime.Add( duration );
            Duration = duration;
            int oneSecondRepeats = (int)duration.TotalSeconds + 1;
            if( duration > Hour ) {
                announceIntervalIndex = AnnounceIntervals.Length - 1;
                lastHourAnnounced = (int)duration.TotalHours;
            } else {
                for( int i = 0; i < AnnounceIntervals.Length; i++ ) {
                    if( duration <= AnnounceIntervals[i] ) {
                        announceIntervalIndex = i - 1;
                        break;
                    }
                }
            }
            task = Scheduler.NewTask( TimerCallback, this );
            ID = Interlocked.Increment( ref timerCounter );
            AddTimerToList( this );
            IsRunning = true;
            task.RunRepeating( TimeSpan.Zero,
                               TimeSpan.FromSeconds( 1 ),
                               oneSecondRepeats );
            try
            {
                if (!(Directory.Exists("./Timers"))) Directory.CreateDirectory("./Timers");
                string[] output = { "StartDate: " + StartTime.ToString(), "EndDate: " + EndTime.ToString(), "CreatedBy: " + StartedBy, "Message: " + Message };
                File.WriteAllLines("./Timers/" + EndTime.Year.ToString() + "_" + EndTime.Month.ToString() + "_" + EndTime.Day.ToString() + "_" + EndTime.Hour.ToString() + "_" + EndTime.Minute.ToString() + "_" + EndTime.Second.ToString() + ".txt", output);
            }
            catch (Exception ex)
            {
                Player.Console.Message("Timer Writer Has Crashed: {0}", ex);
            }
        }

        static void TimerCallback( [NotNull] SchedulerTask task ) {
            if( task == null ) throw new ArgumentNullException( "task" );
            ChatTimer timer = (ChatTimer)task.UserState;
            if( task.MaxRepeats == 1 ) {
                if( String.IsNullOrEmpty( timer.Message ) ) {
                    Server.Players.Message("&2[&7CountDown Finished&2]");
                    IRC.SendChannelMessage("\u212C&S[&7CountDown Finished&S]");
                } else {
                    Server.Players.Message("&2[&7Timer Finished&2] &7" + timer.Message);
                    IRC.SendChannelMessage("\u212C&S[&7Timer Finished&S]\u211C " + timer.Message);
                }
                //Timer Ends Here.
                timer.Stop( false );

            } else if( timer.announceIntervalIndex >= 0 ) {
                if( timer.lastHourAnnounced != (int)timer.TimeLeft.TotalHours ) {
                    timer.lastHourAnnounced = (int)timer.TimeLeft.TotalHours;
                    timer.Announce( TimeSpan.FromHours( Math.Ceiling( timer.TimeLeft.TotalHours ) ) );
                }
                if( timer.TimeLeft <= AnnounceIntervals[timer.announceIntervalIndex] ) {
                    timer.Announce( AnnounceIntervals[timer.announceIntervalIndex] );
                    timer.announceIntervalIndex--;
                }
            }
        }


        void Announce( TimeSpan timeLeft ) {
            if( String.IsNullOrEmpty( Message ) ) {
                Server.Players.Message("&2[&7CountDown&2][&7{0}&2]", timeLeft.ToMiniString());
                IRC.SendChannelMessage("\u212C&S[&7CountDown&S][&7{0}&S]", timeLeft.ToMiniString());
            } else {
                Server.Players.Message("&2[&7Timer&2][&7{0}&2] &7{1} &2-&7{2}", timeLeft.ToMiniString(), Message, StartedBy);
                IRC.SendChannelMessage("\u212C&S[&7Timer&S][&7{2}&S][&7{0}&S]\u211C {1}", timeLeft.ToMiniString(), Message, StartedBy);
            }
        }


        /// <summary> Stops this timer, and removes it from the list of timers. </summary>
        public void Abort() {
            Stop( true );
        }


        void Stop( bool aborted ) {
            try
            {
                if (!(Directory.Exists("./Timers"))) Directory.CreateDirectory("./Timers");
                if (File.Exists("./Timers/" + EndTime.Year.ToString() + "_" + EndTime.Month.ToString() + "_" + EndTime.Day.ToString() + "_" + EndTime.Hour.ToString() + "_" + EndTime.Minute.ToString() + "_" + EndTime.Second.ToString() + ".txt"))
                {
                    File.Delete("./Timers/" + EndTime.Year.ToString() + "_" + EndTime.Month.ToString() + "_" + EndTime.Day.ToString() + "_" + EndTime.Hour.ToString() + "_" + EndTime.Minute.ToString() + "_" + EndTime.Second.ToString() + ".txt");
                }
            }
            catch (Exception ex)
            {
                Player.Console.Message("Timer Deleter Has Crashed: {0}", ex);
            }
            Aborted = aborted;
            IsRunning = false;
            task.Stop();
            RemoveTimerFromList( this );
            RaiseStoppedEvent( this );
        }


        #region Static

        /// <summary> Minimum allowed timer duration (one second). </summary>
        public static readonly TimeSpan MinDuration = TimeSpan.FromSeconds( 1 );

        static readonly TimeSpan Hour = TimeSpan.FromHours( 1 );


        /// <summary> Starts this timer with the specified duration, and end message. </summary>
        /// <param name="duration"> Amount of time the timer should run before completion. </param>
        /// <param name="message"> Message to display when timer reaches zero. May be null. </param>
        /// <param name="startedBy"> Name of player who started timer. May not be null. </param>
        /// <returns> Newly-created, and already-started timer. </returns>
        /// <exception cref="ArgumentNullException"> If startedBy is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If duration is less than one second. </exception>
        public static ChatTimer Start( TimeSpan duration, [CanBeNull] string message, [NotNull] string startedBy ) {
            if( startedBy == null ) throw new ArgumentNullException( "startedBy" );
            if( duration < MinDuration ) {
                throw new ArgumentException( "Timer duration should be at least 1s", "duration" );
            }
            ChatTimer newTimer = new ChatTimer( duration, message, startedBy );
            RaiseStartedEvent( newTimer );
            return newTimer;
        }


        static readonly TimeSpan[] AnnounceIntervals = new[] {
            TimeSpan.FromSeconds( 1 ),
            TimeSpan.FromSeconds( 2 ),
            TimeSpan.FromSeconds( 3 ),
            TimeSpan.FromSeconds( 4 ),
            TimeSpan.FromSeconds( 5 ),
            TimeSpan.FromSeconds( 10 ),
            TimeSpan.FromSeconds( 20 ),
            TimeSpan.FromSeconds( 30 ),
            TimeSpan.FromSeconds( 40 ),
            TimeSpan.FromSeconds( 50 ),
            TimeSpan.FromMinutes( 1 ),
            TimeSpan.FromMinutes( 2 ),
            TimeSpan.FromMinutes( 3 ),
            TimeSpan.FromMinutes( 4 ),
            TimeSpan.FromMinutes( 5 ),
            TimeSpan.FromMinutes( 10 ),
            TimeSpan.FromMinutes( 20 ),
            TimeSpan.FromMinutes( 30 ),
            TimeSpan.FromMinutes( 40 ),
            TimeSpan.FromMinutes( 50 )
        };

        static int timerCounter;
        static readonly object TimerListLock = new object();
        static readonly Dictionary<int, ChatTimer> Timers = new Dictionary<int, ChatTimer>();


        static void AddTimerToList( [NotNull] ChatTimer timer ) {
            if( timer == null ) throw new ArgumentNullException( "timer" );
            lock( TimerListLock ) {
                Timers.Add( timer.ID, timer );
            }
        }


        static void RemoveTimerFromList( [NotNull] ChatTimer timer ) {
            if( timer == null ) throw new ArgumentNullException( "timer" );
            lock( TimerListLock ) {
                Timers.Remove( timer.ID );
            }
        }


        /// <summary> Returns a list of all active timers. </summary>
        public static ChatTimer[] TimerList {
            get {
                lock( TimerListLock ) {
                    return Timers.Values.ToArray();
                }
            }
        }


        /// <summary> Searches for a timer by its numeric ID. </summary>
        /// <param name="id"> ID to search for. </param>
        /// <returns> ChatTimer object if found; null if not found. </returns>
        [CanBeNull]
        public static ChatTimer FindTimerById( int id ) {
            lock( TimerListLock ) {
                ChatTimer result;
                if( Timers.TryGetValue( id, out result ) ) {
                    return result;
                } else {
                    return null;
                }
            }
        }

        #endregion


        #region Events

        /// <summary> Occurs after a ChatTimer was added. </summary>
        public static event EventHandler<ChatTimerEventArgs> Started;


        /// <summary> Occurs after a ChatTimer has expired or was aborted. </summary>
        public static event EventHandler<ChatTimerEventArgs> Stopped;


        static void RaiseStartedEvent( ChatTimer timer ) {
            var h = Started;
            if( h != null ) h( null, new ChatTimerEventArgs( timer ) );
        }


        static void RaiseStoppedEvent( ChatTimer timer ) {
            var h = Stopped;
            if( h != null ) h( null, new ChatTimerEventArgs( timer ) );
        }

        #endregion
    }


    /// <summary> Provides data for ChatTimer.Started and ChatTimer.Stopped events. Immutable. </summary>
    public sealed class ChatTimerEventArgs : EventArgs {
        public ChatTimerEventArgs( ChatTimer timer ) {
            Timer = timer;
        }

        public ChatTimer Timer { get; private set; }
    }
}
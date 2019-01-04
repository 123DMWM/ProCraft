﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> A general-purpose task scheduler. </summary>
    public static class Scheduler {
        static readonly HashSet<SchedulerTask> Tasks = new HashSet<SchedulerTask>();
        static SchedulerTask[] taskCache;
        static readonly Queue<SchedulerTask> BackgroundTasks = new Queue<SchedulerTask>();
        static readonly object TaskListLock = new object(),
                               BackgroundTaskQueueLock = new object();

        static Thread schedulerThread,
                      backgroundThread;

#if DEBUG_SCHEDULER
        public static event EventHandler<SchedulerTaskEventArgs> TaskAdded;
        public static event EventHandler<SchedulerTaskEventArgs> TaskRemoved;
        public static event EventHandler<SchedulerTaskEventArgs> TaskExecuted;
        public static event EventHandler<SchedulerTaskEventArgs> TaskExecuting;
#endif
        public static int CriticalTaskCount {
            get {
                lock( BackgroundTaskQueueLock ) {
                    return BackgroundTasks.Count( t => t.IsCritical );
                }
            }
        }


        internal static void Start() {
#if DEBUG_SCHEDULER
            Logger.Log( LogType.Debug, "Scheduler: Starting..." );
#endif
            schedulerThread = new Thread( MainLoop ) {
                Name = "fCraft.Main",
                CurrentCulture = new CultureInfo( "en-US" )
            };
            schedulerThread.Start();
            backgroundThread = new Thread( BackgroundLoop ) {
                Name = "fCraft.Background",
                CurrentCulture = new CultureInfo( "en-US" )
            };
            backgroundThread.Start();
        }


        static void MainLoop() {
            while( !Server.IsShuttingDown ) {
                DateTime ticksNow = DateTime.UtcNow;

                SchedulerTask[] taskListCache = taskCache;

                for( int i = 0; i < taskListCache.Length && !Server.IsShuttingDown; i++ ) {
                    SchedulerTask task = taskListCache[i];
                    if( task.IsStopped || task.NextTime > ticksNow ) continue;
                    if( task.IsRecurring && task.AdjustForExecutionTime ) {
                        task.NextTime += task.Interval;
                    }

                    if( task.IsBackground ) {
                        lock( BackgroundTaskQueueLock ) {
                            BackgroundTasks.Enqueue( task );
                        }
                    } else {
                        task.IsExecuting = true;
                        task.ExecuteStart = DateTime.UtcNow;
#if DEBUG_SCHEDULER
                        FireEvent( TaskExecuting, task );
#endif

#if DEBUG
                        task.Callback( task );
                        task.IsExecuting = false;
#else
                        try {
                            task.Callback( task );
                        } catch( Exception ex ) {
                            Logger.LogAndReportCrash( "Exception thrown by ScheduledTask callback", "ProCraft", ex, false );
                        } finally {
                            task.IsExecuting = false;
                        }
#endif

                        task.ExecuteEnd = DateTime.UtcNow;
#if DEBUG_SCHEDULER
                        FireEvent( TaskExecuted, task );
#endif
                    }

                    if( !task.IsRecurring || task.MaxRepeats == 1 ) {
                        task.Stop();
                        continue;
                    }
                    task.MaxRepeats--;

                    ticksNow = DateTime.UtcNow;
                    if( !task.AdjustForExecutionTime ) {
                        task.NextTime = ticksNow.Add( task.Interval );
                    }
                }

                Thread.Sleep( 10 );
            }
        }


        static void BackgroundLoop() {
            while( !Server.IsShuttingDown ) {
                if( BackgroundTasks.Count > 0 ) {
                    SchedulerTask task;
                    lock( BackgroundTaskQueueLock ) {
                        task = BackgroundTasks.Dequeue();
                    }
                    ExecuteBackgroundTask( task );
                }
                Thread.Sleep( 10 );
            }

            while( BackgroundTasks.Count > 0 ) {
                SchedulerTask task;
                lock( BackgroundTaskQueueLock ) {
                    task = BackgroundTasks.Dequeue();
                }
                if( task.IsCritical ) {
                    ExecuteBackgroundTask( task );
                }
            }
        }


        static void ExecuteBackgroundTask( SchedulerTask task ) {
            task.IsExecuting = true;
#if DEBUG_SCHEDULER
                    task.ExecuteStart = DateTime.UtcNow;
                    FireEvent( TaskExecuting, task );
#endif

#if DEBUG
            task.Callback( task );
#else
                    try {
                        task.Callback( task );
                    } catch( Exception ex ) {
                        Logger.LogAndReportCrash( "Exception thrown by ScheduledTask callback", "ProCraft", ex, false );
                    } finally {
                        task.IsExecuting = false;
                    }
#endif

#if DEBUG_SCHEDULER
					task.ExecuteEnd = DateTime.UtcNow;
                    FireEvent( TaskExecuted, task );
#endif
        }


        /// <summary> Schedules a given task for execution. </summary>
        /// <param name="task"> Task to schedule. </param>
        internal static void AddTask( [NotNull] SchedulerTask task ) {
            if( task == null ) throw new ArgumentNullException( "task" );
            lock( TaskListLock ) {
                if( Server.IsShuttingDown ) return;
                task.IsStopped = false;
#if DEBUG_SCHEDULER
                FireEvent( TaskAdded, task );
                if( Tasks.Add( task ) ) {
                    UpdateCache();
                    Logger.Log( LogType.Debug, "Scheduler.AddTask: Added {0}", task );
                } else {
                    Logger.Log( LogType.Debug, "Scheduler.AddTask: Added duplicate {0}", task );
                }
#else
                if( Tasks.Add( task ) ) {
                    UpdateCache();
                }
#endif
            }
        }


        /// <summary> Creates a new SchedulerTask object to run in the main thread.
        /// Use this if your task is time-sensitive or frequent, and your callback won't take too long to execute. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewTask( [NotNull] SchedulerCallback callback ) {
            return new SchedulerTask( callback, false );
        }


        /// <summary> Creates a new SchedulerTask object to run in the background thread.
        /// Use this if your task is not very time-sensitive or frequent, or if your callback is resource-intensive. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewBackgroundTask( [NotNull] SchedulerCallback callback ) {
            return new SchedulerTask( callback, true );
        }


        /// <summary> Creates a new SchedulerTask object to run in the main thread.
        /// Use this if your task is time-sensitive or frequent, and your callback won't take too long to execute. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <param name="userState"> Parameter to pass to the method. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewTask( [NotNull] SchedulerCallback callback, [CanBeNull] object userState ) {
            return new SchedulerTask( callback, false, userState );
        }


        /// <summary> Creates a new SchedulerTask object to run in the background thread.
        /// Use this if your task is not very time-sensitive or frequent, or if your callback is resource-intensive. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <param name="userState"> Parameter to pass to the method. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewBackgroundTask( [NotNull] SchedulerCallback callback, [CanBeNull] object userState ) {
            return new SchedulerTask( callback, true, userState );
        }


        // Removes stopped tasks from the list
        static void UpdateCache() {
            List<SchedulerTask> newList = new List<SchedulerTask>();
            List<SchedulerTask> deletionList = new List<SchedulerTask>();
            lock( TaskListLock ) {
                foreach( SchedulerTask task in Tasks ) {
                    if( task.IsStopped ) {
                        deletionList.Add( task );
                    } else {
                        newList.Add( task );
                    }
                }
                for( int i = 0; i < deletionList.Count; i++ ) {
                    Tasks.Remove( deletionList[i] );
#if DEBUG_SCHEDULER
                    FireEvent( TaskRemoved, deletionList[i] );
                    Logger.Log( LogType.Debug,
                                "Scheduler.UpdateCache: Removed {0}", deletionList[i] );
#endif
                }
            }
            taskCache = newList.ToArray();
        }


        // Clears the task list
        internal static void BeginShutdown() {
#if DEBUG_SCHEDULER
            Logger.Log( LogType.Debug, "Scheduler: BeginShutdown..." );
#endif
            lock( TaskListLock ) {
                foreach( SchedulerTask task in Tasks ) {
                    task.Stop();
                }
                Tasks.Clear();
                taskCache = new SchedulerTask[0];
            }
        }


        // Makes sure that both scheduler threads finish and quit.
        internal static void EndShutdown() {
#if DEBUG_SCHEDULER
            Logger.Log( LogType.Debug, "Scheduler: EndShutdown..." );
#endif
            try {
                if( schedulerThread != null && schedulerThread.IsAlive ) {
                    schedulerThread.Join();
                }
                schedulerThread = null;
            } catch( ThreadStateException ) { }
            try {
                if( backgroundThread != null && backgroundThread.IsAlive ) {
                    backgroundThread.Join();
                }
                backgroundThread = null;
            } catch( ThreadStateException ) { }
        }


        #region PrintTasks

        public static void PrintTasks( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( TaskListLock ) {
                foreach( SchedulerTask task in Tasks ) {
                    player.Message( task.ToString() );
                }
            }
        }


        static void FireEvent( EventHandler<SchedulerTaskEventArgs> eventToFire, SchedulerTask task ) {
            var h = eventToFire;
            if( h != null ) h( null, new SchedulerTaskEventArgs( task ) );
        }
        #endregion
    }
}
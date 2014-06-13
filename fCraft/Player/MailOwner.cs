// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | Copyright 2014 123DMWM <shmo1joe2@gmail.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using JetBrains.Annotations;

namespace fCraft
{
    /// <summary> Provides a way to create and manage publicly-announced countdowns.
    /// Long Mailers announce once an hour (e.g. "7h left").
    /// During the last hour, Mailer announces more often: every 10 minutes, then every minute,
    /// then every 10 seconds, and finally every second - until the Mailer is up. </summary>
    public sealed class ChatMailer
    {
        /// <summary> Mailer's unique numeric ID. </summary>
        public readonly int ID;

        /// <summary> Whether or not the Mailer is currently running. </summary>
        public bool IsRunning { get; private set; }

        /// <summary> Whether this Mailer has been aborted. </summary>
        public bool Aborted { get; private set; }

        /// <summary> Message to be displayed once the Mailer reaches zero. </summary>
        [CanBeNull]
        public string Message { get; private set; }

        /// <summary> Date/Time (UTC) at which this Mailer was started. </summary>
        public DateTime StartTime { get; private set; }

        /// <summary> Date/Time (UTC) at which this Mailer will end. </summary>
        public DateTime EndTime { get; private set; }

        /// <summary> The amount of time between when this Mailer was started and when it will end. </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary> The amount of time remaining in this Mailer. </summary>
        public TimeSpan TimeLeft
        {
            get { return EndTime.Subtract(DateTime.UtcNow); }
        }

        /// <summary> Name of the player (or entity) who started this Mailer </summary>
        [NotNull]
        public string StartedBy { get; private set; }


        readonly SchedulerTask task;
        int announceIntervalIndex, lastHourAnnounced;


        ChatMailer([CanBeNull] string message, [NotNull] string startedBy)
        {
            if (startedBy == null) throw new ArgumentNullException("startedBy");
            StartedBy = startedBy;
            Message = message;
            StartTime = DateTime.UtcNow;
            ID = Interlocked.Increment(ref MailerCounter);
            AddMailerToList(this);
            IsRunning = true;
            try
            {
                if (!(Directory.Exists("./Mail"))) Directory.CreateDirectory("./Mail");
                string[] output = { "CreatedBy: " + StartedBy, "Date: " + StartTime.ToString(), "Message: " + Message };
                File.WriteAllLines("./Mail/" + ID + "_" + StartedBy + ".txt", output);
            }
            catch (Exception ex)
            {
                Player.Console.Message("Mail Writer Has Crashed: {0}", ex);
            }
        }


        
        /// <summary> Stops this Mailer, and removes it from the list of Mailers. </summary>
        public void Abort(int ID)
        {
            try
            {
                if (!(Directory.Exists("./Mail"))) Directory.CreateDirectory("./Mail");
                if (File.Exists("./Mail/" + ID + "_" + StartedBy + ".txt"))
                {
                    File.Delete("./Mail/" + ID + "_" + StartedBy + ".txt");
                }
            }
            catch (Exception ex)
            {
                Player.Console.Message("Mail Deleter Has Crashed: {0}", ex);
            }
            Aborted = true;
            IsRunning = false;
            RemoveMailerFromList(this);
            RaiseStoppedEvent(this);
        }


        #region Static

        /// <summary> Minimum allowed Mailer duration (one second). </summary>
        public static readonly TimeSpan MinDuration = TimeSpan.FromSeconds(1);

        static readonly TimeSpan Hour = TimeSpan.FromHours(1);


        /// <summary> Starts this Mailer with the specified duration, and end message. </summary>
        /// <param name="duration"> Amount of time the Mailer should run before completion. </param>
        /// <param name="message"> Message to display when Mailer reaches zero. May be null. </param>
        /// <param name="startedBy"> Name of player who started Mailer. May not be null. </param>
        /// <returns> Newly-created, and already-started Mailer. </returns>
        /// <exception cref="ArgumentNullException"> If startedBy is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If duration is less than one second. </exception>
        public static ChatMailer Start([CanBeNull] string message, [NotNull] string startedBy)
        {
            if (startedBy == null) throw new ArgumentNullException("startedBy");
            ChatMailer newMailer = new ChatMailer(message, startedBy);
            RaiseStartedEvent(newMailer);
            return newMailer;
        }


        static int MailerCounter;
        static readonly object MailerListLock = new object();
        static readonly Dictionary<int, ChatMailer> Mailers = new Dictionary<int, ChatMailer>();


        static void AddMailerToList([NotNull] ChatMailer Mailer)
        {
            if (Mailer == null) throw new ArgumentNullException("Mail");
            lock (MailerListLock)
            {
                Mailers.Add(Mailer.ID, Mailer);
            }
        }


        static void RemoveMailerFromList([NotNull] ChatMailer Mailer)
        {
            if (Mailer == null) throw new ArgumentNullException("Mail");
            lock (MailerListLock)
            {
                Mailers.Remove(Mailer.ID);
            }
        }


        /// <summary> Returns a list of all active Mailers. </summary>
        public static ChatMailer[] MailerList
        {
            get
            {
                lock (MailerListLock)
                {
                    return Mailers.Values.ToArray();
                }
            }
        }


        /// <summary> Searches for a Mailer by its numeric ID. </summary>
        /// <param name="id"> ID to search for. </param>
        /// <returns> ChatMailer object if found; null if not found. </returns>
        [CanBeNull]
        public static ChatMailer FindMailerById(int id)
        {
            lock (MailerListLock)
            {
                ChatMailer result;
                if (Mailers.TryGetValue(id, out result))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion


        #region Events

        /// <summary> Occurs after a ChatMailer was added. </summary>
        public static event EventHandler<ChatMailerEventArgs> Started;


        /// <summary> Occurs after a ChatMailer has expired or was aborted. </summary>
        public static event EventHandler<ChatMailerEventArgs> Stopped;


        static void RaiseStartedEvent(ChatMailer Mailer)
        {
            var h = Started;
            if (h != null) h(null, new ChatMailerEventArgs(Mailer));
        }


        static void RaiseStoppedEvent(ChatMailer Mailer)
        {
            var h = Stopped;
            if (h != null) h(null, new ChatMailerEventArgs(Mailer));
        }

        #endregion
    }


    /// <summary> Provides data for ChatMailer.Started and ChatMailer.Stopped events. Immutable. </summary>
    public sealed class ChatMailerEventArgs : EventArgs
    {
        public ChatMailerEventArgs(ChatMailer mailer)
        {
            Mailer = mailer;
        }

        public ChatMailer Mailer { get; private set; }
    }
}
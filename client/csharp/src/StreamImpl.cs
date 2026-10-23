using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using KRPC.Client.Services.KRPC;

namespace KRPC.Client
{
    /// <summary>
    /// The state shared by every handle of one stream. It is changed under the stream's
    /// condition, and then the update lock.
    /// </summary>
    sealed class StreamImpl
    {
        readonly Connection connection;
        object _value;
        readonly object updateLock;
        readonly object condition = new object ();
        readonly IDictionary<int, Callback> callbacks = new Dictionary<int, Callback> ();
        int nextCallbackKey;
        readonly IDictionary<int, Action<Exception>> endCallbacks = new Dictionary<int, Action<Exception>> ();
        int nextEndCallbackKey;
        volatile Exception ending;
        // The ending as a thread holding the condition reads it. An ending from the server
        // reaches the holder when its waiters are woken, as a value does.
        volatile Exception announcedEnding;
        // Whether a remove or a close ended the stream, which discards an unseen firing
        volatile bool endedByClient;
        volatile bool updated;
        float rate = 0;

        public StreamImpl (Connection connection, ulong id,
                           TypeSpec returnType, object updateLock)
        {
            this.connection = connection;
            Id = id;
            ReturnType = returnType;
            this.updateLock = updateLock;
        }

        public ulong Id { get; private set; }

        /// <summary>
        /// The type of the stream's values. Null for a stream the reader registered before a
        /// handle gave its type.
        /// </summary>
        public TypeSpec ReturnType { get; internal set; }

        /// <summary>
        /// The latest result of a stream with no return type yet, kept undecoded for its
        /// first handle. Changed under the update lock.
        /// </summary>
        internal Schema.KRPC.ProcedureResult Raw { get; set; }

        public float Rate {
            get { return rate; }
            set {
                rate = value;
                // The server has no stream for the id once it ends
                if (!Ended)
                    connection.KRPC ().SetStreamRate (Id, value);
            }
        }

        public bool Ended {
            get { return ending != null; }
        }

        /// <summary>
        /// Whether a remove or a close ended the stream.
        /// </summary>
        public bool EndedByClient {
            get { return endedByClient; }
        }

        /// <summary>
        /// Whether the ending has been announced to a thread holding the condition.
        /// </summary>
        public bool EndingAnnounced {
            get { return announcedEnding != null; }
        }

        /// <summary>
        /// Announce the ending to a thread holding the condition, with it held.
        /// </summary>
        public void AnnounceEnding ()
        {
            announcedEnding = ending;
        }

        /// <summary>
        /// Throw the exception that ended the stream, if it has ended. A thread holding the
        /// condition reads the ending once it is announced.
        /// </summary>
        public void ThrowIfEnded ()
        {
            var exn = ending;
            if (exn == null)
                return;
            if (exn != announcedEnding && !connection.StreamManager.Closed && Monitor.IsEntered (condition))
                return;
            throw exn;
        }

        /// <summary>
        /// The latest value, or the last one announced to waiters for a thread holding the
        /// condition, which then sees the value change only when its waiters are woken.
        /// </summary>
        public object Value {
            get {
                ThrowIfEnded ();
                if (Monitor.IsEntered (condition)) {
                    if (!announced)
                        throw new System.InvalidOperationException ("Stream has no value");
                    seen.Value = AnnouncedVersion;
                    return announcedValue;
                }
                lock (updateLock) {
                    if (!updated)
                        throw new System.InvalidOperationException ("Stream has no value");
                    seen.Value = Version;
                    return _value;
                }
            }
        }

        // The version of the value each thread last read, which a wait on that thread waits past
        readonly ThreadLocal<long> seen = new ThreadLocal<long> ();

        /// <summary>
        /// The version of the value this thread last read.
        /// </summary>
        public long SeenVersion {
            get { return seen.Value; }
        }

        /// <summary>
        /// Whether there is a value, announced to waiters for a thread holding the condition.
        /// </summary>
        public bool Updated {
            get { return Monitor.IsEntered (condition) ? announced : updated; }
        }

        // The last value announced to waiters, which Wait and a thread holding the condition
        // read. Changed with the condition held, so the holder sees it frozen.
        object announcedValue;
        volatile bool announced;

        /// <summary>
        /// The version of the last value announced to waiters.
        /// </summary>
        public long AnnouncedVersion { get; private set; }

        /// <summary>
        /// Announce a stored value to waiters, with the condition held.
        /// </summary>
        public void Announce (object value, long version)
        {
            // Stored before the flag, as a reader checks the flag with no lock held
            announcedValue = value;
            AnnouncedVersion = version;
            announced = true;
        }

        /// <summary>
        /// The number of values received.
        /// </summary>
        public long Version { get; private set; }

        /// <summary>
        /// The number of true values received, which for an event is its firings.
        /// </summary>
        public long Fired { get; private set; }

        /// <summary>
        /// The highest firing count passed to an event callback. Changed under the update lock.
        /// </summary>
        public long CallbacksFired { get; private set; }

        // The firing a replay in flight passes to a new event callback. Changed under the
        // update lock.
        long replaying;

        public void SetValue (object value)
        {
            lock (updateLock) {
                _value = value;
                updated = true;
                Version++;
                if (value is bool && (bool)value)
                    Fired++;
            }
        }

        /// <summary>
        /// End the stream, without waking its waiters. An ending that is not announced reaches a
        /// thread holding the condition once AnnounceEnding is called. Returns the end callbacks
        /// to call, with no lock held, or none if the stream had already ended.
        /// </summary>
        public IList<Action<Exception>> SetEnding (Exception exn, bool announce = true, bool byClient = false)
        {
            lock (updateLock) {
                if (ending != null)
                    return new Action<Exception> [0];
                ending = exn;
                endedByClient = byClient;
                if (announce)
                    announcedEnding = exn;
                var toCall = new List<Action<Exception>> (endCallbacks.Values);
                endCallbacks.Clear ();
                return toCall;
            }
        }

        /// <summary>
        /// End the stream and wake its waiters. Returns the end callbacks to call, with no lock
        /// held, or none if the stream had already ended.
        /// </summary>
        public IList<Action<Exception>> End (Exception exn)
        {
            lock (condition) {
                var toCall = SetEnding (exn, true, true);
                Monitor.PulseAll (condition);
                return toCall;
            }
        }

        public object Condition {
            get { return condition; }
        }

        /// <summary>
        /// Wait on the condition, which the caller holds, until done returns true. Returns false
        /// when the timeout in seconds passes first, and throws the ending if the stream ends
        /// first. An infinite timeout waits indefinitely.
        /// </summary>
        public bool Wait (Func<bool> done, double timeout)
        {
            CheckTimeout (timeout);
            var timer = double.IsPositiveInfinity (timeout) ? null : Stopwatch.StartNew ();
            while (!done ()) {
                ThrowIfEnded ();
                if (timer == null) {
                    Monitor.Wait (condition);
                    continue;
                }
                // The timeout is a deadline, so a wake with nothing new waits for the remainder
                var remaining = timeout * 1000.0 - timer.Elapsed.TotalMilliseconds;
                if (remaining <= 0)
                    return false;
                Monitor.Wait (condition, (int)Math.Min (Math.Ceiling (remaining), int.MaxValue));
            }
            return true;
        }

        /// <summary>
        /// Throw ArgumentOutOfRangeException for a negative or NaN timeout.
        /// </summary>
        public static void CheckTimeout (double timeout)
        {
            if (double.IsNaN (timeout) || timeout < 0)
                throw new System.ArgumentOutOfRangeException (
                    nameof (timeout), timeout, "The timeout must not be negative or NaN");
        }

        /// <summary>
        /// A value callback, or an event callback that is passed only firings. The version is
        /// that of the last value passed to a value callback, or the firing count last passed to
        /// an event callback. The callback runs with the entry locked, so it runs on one thread
        /// at a time.
        /// </summary>
        /// <remarks>
        /// While the adding thread makes the first call, the dispatcher thread queues its values in
        /// Missed for the adding thread to pass on, and leaves a server ending in EndCallbacks
        /// and Ending for the adding thread to call the end callbacks with after them.
        /// </remarks>
        public sealed class Callback
        {
            public Action<object> Fn;
            public bool Event;
            public long Version;
            public bool Adding;
            // Whether the adding thread is passing on the values queued during the first call
            public bool Draining;
            public Queue<KeyValuePair<long, object>> Missed = new Queue<KeyValuePair<long, object>> ();
            public IList<Action<Exception>> EndCallbacks;
            public Exception Ending;
        }

        public IDictionary<int, Callback> Callbacks {
            get { return callbacks; }
        }

        public int AddCallback (Action<object> callback) {
            int key;
            Callback entry;
            bool hasValue;
            object value;
            long version;
            lock (updateLock) {
                entry = Register (callback, false, out key);
                hasValue = updated && ending == null;
                value = _value;
                version = Version;
            }
            // Called with the value the stream already has, which no update passes to it
            if (hasValue)
                Call (entry, version, value, true);
            return key;
        }

        /// <summary>
        /// Add an event callback. It is called straight away for a firing passed neither to a
        /// wait that has seen the given number of firings, nor to the event's callbacks.
        /// </summary>
        public int AddEventCallback (Action callback, long seen) {
            int key;
            long replay;
            var entry = RegisterEventCallback (callback, seen, out key, out replay);
            if (replay > 0)
                Replay (entry, replay);
            return key;
        }

        /// <summary>
        /// Register an event callback, and reserve the latest firing for it if that firing is to
        /// be replayed. The reserved firing is 0 when there is none.
        /// </summary>
        internal Callback RegisterEventCallback (Action callback, long seen, out int key, out long replay) {
            lock (updateLock) {
                var entry = Register (x => callback (), true, out key);
                var fired = Fired;
                replay = 0;
                // The reservation stops a concurrent add replaying the same firing
                if (!endedByClient && fired > Math.Max (seen, Math.Max (CallbacksFired, replaying))) {
                    replaying = fired;
                    replay = fired;
                }
                return entry;
            }
        }

        /// <summary>
        /// Pass a reserved firing to a new event callback. A firing the call does not pass, as
        /// the callback has been removed or the stream has ended, is left for a later callback.
        /// </summary>
        internal void Replay (Callback entry, long fired) {
            try {
                Call (entry, fired, true, true);
            } finally {
                lock (updateLock) {
                    if (replaying == fired)
                        replaying = 0;
                }
            }
        }

        Callback Register (Action<object> callback, bool evnt, out int key) {
            key = nextCallbackKey;
            nextCallbackKey++;
            var entry = new Callback { Fn = callback, Event = evnt };
            callbacks[key] = entry;
            return entry;
        }

        /// <summary>
        /// Call a callback with a value, unless it has been passed a newer one, has been
        /// removed, or the stream has ended. The version is the firing count for an event
        /// callback, which is passed only firings. Called with no lock held.
        /// </summary>
        /// <remarks>
        /// The first call, from the adding thread, skips the value if the dispatcher thread is
        /// calling the callback, as that thread has a newer value. The dispatcher thread queues its
        /// values for the adding thread while the first call runs.
        /// </remarks>
        public void Call (Callback callback, long version, object value, bool added = false) {
            if (callback.Event && !(value is bool && (bool)value))
                return;
            if (added) {
                if (!Monitor.TryEnter (callback))
                    return;
            } else if (!EnterOnDispatcherThread (callback, version, value))
                return;
            try {
                lock (updateLock) {
                    // A late event callback is passed an unseen firing after a server ending
                    var ended = ending != null && (!added || endedByClient);
                    if (ended || !NotePassed (callback, version))
                        return;
                    callback.Adding = added;
                }
                var drained = false;
                try {
                    StreamManager.InvokeCallback (callback.Fn, value);
                    if (added) {
                        CallMissed (callback);
                        drained = true;
                    }
                } finally {
                    // An exception leaves the first call with values still queued
                    if (added && !drained) {
                        lock (updateLock) {
                            callback.Adding = false;
                            callback.Draining = false;
                            callback.Missed.Clear ();
                        }
                        CallDeferredEnding (callback);
                    }
                }
            } finally {
                Monitor.Exit (callback);
            }
        }

        /// <summary>
        /// Take a callback's entry on the dispatcher thread. Returns false, without the entry, once
        /// the value is queued for the adding thread or the connection is closing.
        /// </summary>
        bool EnterOnDispatcherThread (Callback callback, long version, object value) {
            while (!Monitor.TryEnter (callback, 10)) {
                if (connection.StreamManager.CloseRequested)
                    return false;
                lock (updateLock) {
                    if (callback.Adding) {
                        callback.Missed.Enqueue (new KeyValuePair<long, object> (version, value));
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Call a callback, on the adding thread, with the values the dispatcher thread queued
        /// during its first call, then the end callbacks of a server ending that arrives
        /// meanwhile. A value queued before the server ended the stream is still passed on, and a
        /// remove or a close drops the queue. Adding is cleared in the hold that takes the queue,
        /// so a value that arrives later waits for the entry on the dispatcher thread. Called
        /// with the entry held.
        /// </summary>
        void CallMissed (Callback callback) {
            Queue<KeyValuePair<long, object>> missed;
            lock (updateLock) {
                missed = callback.Missed;
                callback.Missed = new Queue<KeyValuePair<long, object>> ();
                callback.Adding = false;
                callback.Draining = true;
            }
            try {
                foreach (var entry in missed) {
                    lock (updateLock) {
                        if (endedByClient)
                            break;
                        if (!NotePassed (callback, entry.Key))
                            continue;
                    }
                    StreamManager.InvokeCallback (callback.Fn, entry.Value);
                }
            } finally {
                lock (updateLock) {
                    callback.Draining = false;
                }
            }
            CallDeferredEnding (callback);
        }

        /// <summary>
        /// Call the end callbacks of a server ending left to a callback's adding thread.
        /// </summary>
        void CallDeferredEnding (Callback callback) {
            IList<Action<Exception>> endCallbacks;
            Exception exn;
            lock (updateLock) {
                endCallbacks = callback.EndCallbacks;
                exn = callback.Ending;
                callback.EndCallbacks = null;
                callback.Ending = null;
            }
            if (endCallbacks == null)
                return;
            foreach (var endCallback in endCallbacks)
                StreamManager.InvokeEndCallback (endCallback, exn);
        }

        /// <summary>
        /// Leave a server ending to a callback's adding thread, to call the end callbacks once
        /// the values queued during the first call are passed. Returns false if no first call
        /// is running.
        /// </summary>
        public bool DeferEnding (IList<Action<Exception>> endCallbacks, Exception exn) {
            lock (updateLock) {
                foreach (var callback in callbacks.Values) {
                    if (callback.Adding || callback.Draining) {
                        callback.EndCallbacks = endCallbacks;
                        callback.Ending = exn;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Note that a callback is passed a version. Returns false, noting nothing, if the
        /// callback has been removed or passed that version or a newer one. Called with the
        /// update lock held.
        /// </summary>
        bool NotePassed (Callback callback, long version) {
            if (version <= callback.Version || !callbacks.Values.Contains (callback))
                return false;
            callback.Version = version;
            if (callback.Event)
                CallbacksFired = Math.Max (CallbacksFired, version);
            return true;
        }

        public void RemoveCallback (int key) {
            lock (updateLock) {
                callbacks.Remove (key);
            }
        }

        public int AddEndCallback (Action<Exception> callback) {
            Exception exn;
            int key;
            lock (updateLock) {
                key = nextEndCallbackKey;
                nextEndCallbackKey++;
                exn = ending;
                if (exn == null)
                    endCallbacks[key] = callback;
            }
            // A stream that has already ended calls the callback here, as no ending is to come
            if (exn != null)
                StreamManager.InvokeEndCallback (callback, exn);
            return key;
        }

        public void RemoveEndCallback (int key) {
            lock (updateLock) {
                endCallbacks.Remove (key);
            }
        }

        public void Remove () {
            connection.StreamManager.RemoveStream (this);
        }
    }
}

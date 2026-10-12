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

        public TypeSpec ReturnType { get; private set; }

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
        /// Throw the exception that ended the stream, if it has ended.
        /// </summary>
        public void ThrowIfEnded ()
        {
            var exn = ending;
            if (exn != null)
                throw exn;
        }

        public object Value {
            get {
                ThrowIfEnded ();
                if (!Updated)
                    throw new System.InvalidOperationException ("Stream has no value");
                return _value;
            }
        }

        public bool Updated {
            get { return updated; }
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
        /// End the stream and wake its waiters. Returns the end callbacks to call, with no lock
        /// held. Returns none if the stream had already ended.
        /// </summary>
        public IList<Action<Exception>> End (Exception exn)
        {
            IList<Action<Exception>> toCall;
            lock (condition) {
                lock (updateLock) {
                    if (ending != null)
                        return new Action<Exception> [0];
                    ending = exn;
                    toCall = new List<Action<Exception>> (endCallbacks.Values);
                    endCallbacks.Clear ();
                }
                Monitor.PulseAll (condition);
            }
            return toCall;
        }

        public object Condition {
            get { return condition; }
        }

        /// <summary>
        /// Wait on the condition, which the caller holds, until done returns true. Returns false
        /// when the timeout in seconds passes first, and throws the ending if the stream ends
        /// first. A negative timeout waits indefinitely.
        /// </summary>
        public bool Wait (Func<bool> done, double timeout)
        {
            var timer = timeout >= 0 ? Stopwatch.StartNew () : null;
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
        /// A value callback, or an event callback that is passed only firings. The version is
        /// that of the last value passed to a value callback, or the firing count last passed to
        /// an event callback. The callback runs with the entry locked, so it runs on one thread
        /// at a time.
        /// </summary>
        /// <remarks>
        /// While the adding thread makes the first call, the update thread queues its values in
        /// Missed for the adding thread to pass on.
        /// </remarks>
        public sealed class Callback
        {
            public Action<object> Fn;
            public bool Event;
            public long Version;
            public bool Adding;
            public readonly Queue<KeyValuePair<long, object>> Missed = new Queue<KeyValuePair<long, object>> ();
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
                if (ending == null && fired > Math.Max (seen, Math.Max (CallbacksFired, replaying))) {
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
        /// The first call, from the adding thread, skips the value if the update thread is
        /// calling the callback, as that thread has a newer value. The update thread queues its
        /// values for the adding thread while the first call runs.
        /// </remarks>
        public void Call (Callback callback, long version, object value, bool added = false) {
            if (callback.Event && !(value is bool && (bool)value))
                return;
            if (added) {
                if (!Monitor.TryEnter (callback))
                    return;
            } else if (!EnterOnUpdateThread (callback, version, value))
                return;
            try {
                lock (updateLock) {
                    if (ending != null || !NotePassed (callback, version))
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
                            callback.Missed.Clear ();
                        }
                    }
                }
            } finally {
                Monitor.Exit (callback);
            }
        }

        /// <summary>
        /// Take a callback's entry on the update thread. Returns false, without the entry, once
        /// the value is queued for the adding thread or the connection is closing.
        /// </summary>
        bool EnterOnUpdateThread (Callback callback, long version, object value) {
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
        /// Call a callback, on the adding thread, with the values the update thread queued
        /// during its first call. A value queued before the stream ended is still passed on.
        /// Called with the entry held.
        /// </summary>
        void CallMissed (Callback callback) {
            while (true) {
                object value;
                lock (updateLock) {
                    // Cleared with the queue found empty, so no value is queued after the drain
                    if (callback.Missed.Count == 0) {
                        callback.Adding = false;
                        return;
                    }
                    var missed = callback.Missed.Dequeue ();
                    if (!NotePassed (callback, missed.Key))
                        continue;
                    value = missed.Value;
                }
                StreamManager.InvokeCallback (callback.Fn, value);
            }
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

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
        IDictionary<int, Action<object>> callbacks = new Dictionary<int, Action<object>> ();
        int nextCallbackKey;
        readonly IDictionary<int, Action<Exception>> endCallbacks = new Dictionary<int, Action<Exception>> ();
        int nextEndCallbackKey;
        volatile Exception ending;
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

        public void Start () {
            if (!Started) {
                connection.KRPC ().StartStream (Id);
                Started = true;
            }
        }

        public float Rate {
            get { return rate; }
            set {
                rate = value;
                // The server has no stream for the id once it ends
                if (!Ended)
                    connection.KRPC ().SetStreamRate (Id, value);
            }
        }

        public bool Started { get; private set; }

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

        public bool Updated { get; private set; }

        /// <summary>
        /// The number of values received.
        /// </summary>
        public long Version { get; private set; }

        /// <summary>
        /// The number of true values received, which for an event is its firings.
        /// </summary>
        public long Fired { get; private set; }

        public void SetValue (object value)
        {
            lock (updateLock) {
                _value = value;
                Updated = true;
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

        public IDictionary<int, Action<object>> Callbacks {
            get { return callbacks; }
        }

        public int AddCallback (Action<object> callback) {
            lock (updateLock) {
                int key = nextCallbackKey;
                nextCallbackKey++;
                callbacks[key] = callback;
                return key;
            }
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

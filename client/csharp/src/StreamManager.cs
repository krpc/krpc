using System;
using System.Collections.Generic;
using System.Threading;
using KRPC.Client.Services.KRPC;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    /// <summary>
    /// The streams of a connection. The thread reading the socket registers streams and stores
    /// their values, and the dispatcher thread wakes their waiters and calls their callbacks.
    /// </summary>
    sealed class StreamManager
    {
        readonly Connection connection;
        readonly Action<Action> dispatch;
        readonly object updateLock = new object ();
        readonly IDictionary<ulong, StreamImpl> streams = new Dictionary<ulong, StreamImpl> ();
        bool closed;
        volatile bool closeRequested;
        // Every registered stream, to wake its waiters when the connection closes
        readonly List<WeakReference<StreamImpl>> registered = new List<WeakReference<StreamImpl>> ();
        int pruneRegisteredAt = 16;

        public StreamManager (Connection serverConnection, Action<Action> dispatchWork)
        {
            connection = serverConnection;
            dispatch = dispatchWork;
        }

        /// <summary>
        /// Whether the connection has closed and every stream has ended.
        /// </summary>
        public bool Closed {
            get { return closed; }
        }

        /// <summary>
        /// Whether a thread is disposing the connection, after which the dispatcher thread stops
        /// trying to take a stream's condition.
        /// </summary>
        public bool CloseRequested {
            get { return closeRequested; }
        }

        /// <summary>
        /// Whether any stream is open. Read without the update lock, by the session deciding
        /// whether to read between calls.
        /// </summary>
        public bool HasStreams {
            get { return streams.Count > 0; }
        }

        /// <summary>
        /// Whether this thread holds the condition of a stream.
        /// </summary>
        public bool HoldsACondition ()
        {
            lock (updateLock) {
                foreach (var reference in registered) {
                    StreamImpl stream;
                    if (reference.TryGetTarget (out stream) && Monitor.IsEntered (stream.Condition))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Add a stream to the registered streams. Called with the update lock held.
        /// </summary>
        void NoteRegistered (StreamImpl stream)
        {
            // Collected streams are pruned when the list doubles
            if (registered.Count >= pruneRegisteredAt) {
                registered.RemoveAll (x => {
                    StreamImpl target;
                    return !x.TryGetTarget (out target);
                });
                pruneRegisteredAt = Math.Max (16, 2 * registered.Count);
            }
            registered.Add (new WeakReference<StreamImpl> (stream));
        }

        public StreamImpl AddStream (TypeSpec returnType, ProcedureCall call)
        {
            var id = connection.KRPC ().AddStream (call).Id;
            return GetStream (returnType, id);
        }

        /// <summary>
        /// Register a stream the server has added for this client. Called by the reader, before
        /// it reads the stream's first update.
        /// </summary>
        public void Register (ulong id)
        {
            Session.Enter (updateLock);
            try {
                if (closed || streams.ContainsKey (id))
                    return;
                var stream = new StreamImpl (connection, id, null, updateLock);
                streams [id] = stream;
                NoteRegistered (stream);
            } finally {
                Monitor.Exit (updateLock);
            }
        }

        /// <summary>
        /// Whether a stream is registered for the id.
        /// </summary>
        internal bool Registered (ulong id)
        {
            lock (updateLock) {
                return streams.ContainsKey (id);
            }
        }

        public StreamImpl GetStream (TypeSpec returnType, ulong id)
        {
            var added = false;
            StreamImpl stream;
            lock (updateLock) {
                if (streams.TryGetValue (id, out stream)) {
                    if (stream.ReturnType != null)
                        return stream;
                    stream.ReturnType = returnType;
                    var raw = stream.Raw;
                    stream.Raw = null;
                    if (raw != null) {
                        // No handle has been given out, so there is no waiter to wake or
                        // callback to call
                        bool ended;
                        bool undecodable;
                        var value = Decode (stream, raw, out ended, out undecodable);
                        if (undecodable) {
                        } else if (ended) {
                            stream.SetEnding ((System.Exception)value);
                        } else {
                            stream.SetValue (value);
                            stream.Announce (value, stream.Version);
                        }
                    }
                } else {
                    stream = new StreamImpl (connection, id, returnType, updateLock);
                    if (closed) {
                        stream.SetEnding (ClosedError (null));
                        return stream;
                    }
                    streams [id] = stream;
                    NoteRegistered (stream);
                    added = true;
                }
            }
            if (added)
                connection.Session.StreamAdded ();
            return stream;
        }

        /// <summary>
        /// Remove a stream from the server and end it, unless it has already ended.
        /// </summary>
        public void RemoveStream (StreamImpl stream)
        {
            lock (updateLock) {
                StreamImpl current;
                if (!streams.TryGetValue (stream.Id, out current) || current != stream)
                    // A stream the server or the connection has already ended
                    return;
                streams.Remove (stream.Id);
            }
            var ending = new StreamRemovedException ("Stream has been removed");
            // Ended first, so a value that arrives during the call is dropped
            var endCallbacks = stream.End (ending);
            foreach (var callback in endCallbacks)
                InvokeEndCallback (callback, ending);
            connection.KRPC ().RemoveStream (stream.Id);
        }

        /// <summary>
        /// Note that the connection is closing. The dispatcher thread then stops trying to take a
        /// stream's condition, and the closing thread wakes the stream's waiters.
        /// </summary>
        public void RequestClose ()
        {
            closeRequested = true;
        }

        /// <summary>
        /// End every stream, once the connection is closing and no further update is applied.
        /// Called by the closing thread, and by the dispatcher thread when the connection fails.
        /// </summary>
        /// <remarks>
        /// The waiters of a stream whose condition another thread holds are woken by a thread of
        /// their own, once the holder lets go of it, such as a thread holding one stream's
        /// condition while it waits on another.
        /// </remarks>
        public void NotifyClosed (System.Exception cause)
        {
            var endings = new List<KeyValuePair<IList<Action<System.Exception>>, System.Exception>> ();
            var toWake = new List<StreamImpl> ();
            lock (updateLock) {
                closed = true;
                var ending = ClosedError (cause);
                foreach (var stream in streams.Values)
                    endings.Add (new KeyValuePair<IList<Action<System.Exception>>, System.Exception> (
                        stream.SetEnding (ending, true, true), ending));
                streams.Clear ();
                foreach (var reference in registered) {
                    StreamImpl stream;
                    if (reference.TryGetTarget (out stream))
                        toWake.Add (stream);
                }
            }
            toWake.RemoveAll (x => TryWake (x, 0));
            if (toWake.Count > 0) {
                var waker = new Thread (() => {
                    while (toWake.Count > 0)
                        toWake.RemoveAll (x => TryWake (x, 10));
                });
                waker.IsBackground = true;
                waker.Name = "krpc-waker";
                waker.Start ();
            }
            foreach (var entry in endings) {
                foreach (var callback in entry.Key)
                    InvokeEndCallback (callback, entry.Value);
            }
        }

        static bool TryWake (StreamImpl stream, int timeout)
        {
            if (!Monitor.TryEnter (stream.Condition, timeout))
                return false;
            try {
                Monitor.PulseAll (stream.Condition);
            } finally {
                Monitor.Exit (stream.Condition);
            }
            return true;
        }

        /// <summary>
        /// Take a stream's condition on the dispatcher thread. Returns false, without the
        /// condition, once the connection is closing, as the disposing thread can hold the
        /// condition while it joins the dispatcher thread.
        /// </summary>
        bool EnterCondition (object condition)
        {
            while (!Monitor.TryEnter (condition, 50)) {
                if (closeRequested)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// The error that ends a stream once the connection closes. Its inner exception is the
        /// error that closed the connection, if it failed, and it carries that error's
        /// disconnect reason.
        /// </summary>
        static System.Exception ClosedError (System.Exception cause)
        {
            if (cause == null)
                return new ConnectionException ("Connection closed");
            var connectionCause = cause as ConnectionException;
            return new ConnectionException ("Connection closed", cause) {
                Reason = connectionCause != null ? connectionCause.Reason : null
            };
        }

        internal static void InvokeEndCallback (Action<System.Exception> callback, System.Exception exn)
        {
            try {
                callback (exn);
            } catch (System.Exception callbackExn) {
                Console.Error.WriteLine ("Exception in kRPC stream end callback: " + callbackExn);
            }
        }

        /// <summary>
        /// Decode a result, which is final if it is an error. A final result removes the stream
        /// from the registry, as the server has already removed it. A value that fails to decode
        /// is dropped. Called with the update lock held.
        /// </summary>
        object Decode (StreamImpl stream, ProcedureResult result, out bool ended, out bool undecodable)
        {
            ended = true;
            undecodable = false;
            object value;
            try {
                if (result.Error != null)
                    value = connection.GetException (result.Error);
                else {
                    value = result.IsNull ? null : Encoder.Decode (result.Value, stream.ReturnType, connection);
                    ended = false;
                }
            } catch (System.Exception) {
                undecodable = true;
                ended = false;
                value = null;
            }
            StreamImpl current;
            if (ended && streams.TryGetValue (stream.Id, out current) && current == stream)
                streams.Remove (stream.Id);
            return value;
        }

        // A stream's notification for the dispatcher: for a value the callbacks, version, firing
        // count and value to pass, or for an ending the end callbacks and ending
        sealed class Change
        {
            public StreamImpl Stream;
            public IList<StreamImpl.Callback> Callbacks;
            public long Version;
            public long Fired;
            public object Value;
            public IList<Action<System.Exception>> EndCallbacks;
            public System.Exception Ending;
        }

        /// <summary>
        /// Store the values and endings of a stream update, and queue the work of waking waiters
        /// and calling callbacks with the dispatcher. Called by the reader. A result for a stream
        /// not in the registry is dropped, as the client has removed it.
        /// </summary>
        internal void Update (IList<StreamResult> results)
        {
            var changes = new List<Change> ();
            Session.Enter (updateLock);
            try {
                foreach (var result in results) {
                    StreamImpl stream;
                    if (!streams.TryGetValue (result.Id, out stream))
                        continue;
                    if (stream.ReturnType == null) {
                        // Kept for the first handle, unless an ending is already kept
                        if (stream.Raw == null || stream.Raw.Error == null)
                            stream.Raw = result.Result;
                        continue;
                    }
                    bool ended;
                    bool undecodable;
                    var value = Decode (stream, result.Result, out ended, out undecodable);
                    if (undecodable)
                        continue;
                    if (ended) {
                        var ending = (System.Exception)value;
                        changes.Add (new Change {
                            Stream = stream, EndCallbacks = stream.SetEnding (ending, false), Ending = ending
                        });
                        continue;
                    }
                    stream.SetValue (value);
                    // Copied with the value stored, so a callback added after this reads the
                    // value itself. The callbacks run with no lock held.
                    changes.Add (new Change {
                        Stream = stream,
                        Callbacks = new List<StreamImpl.Callback> (stream.Callbacks.Values),
                        Version = stream.Version,
                        Fired = stream.Fired,
                        Value = value
                    });
                }
            } finally {
                Monitor.Exit (updateLock);
            }
            if (changes.Count > 0)
                dispatch (() => Apply (changes));
        }

        /// <summary>
        /// Store one result and wait for the dispatcher thread to apply it. For the tests, which
        /// stand in for the reader.
        /// </summary>
        internal void Update (ulong id, ProcedureResult result)
        {
            Update (new [] { new StreamResult { Id = id, Result = result } });
            var applied = new ManualResetEvent (false);
            dispatch (() => applied.Set ());
            applied.WaitOne (10000);
        }

        /// <summary>
        /// Wake the waiters of streams with a new value or an ending, and call their callbacks.
        /// Runs on the dispatcher thread, with no lock held.
        /// </summary>
        void Apply (IList<Change> changes)
        {
            foreach (var change in changes) {
                var stream = change.Stream;
                var entered = EnterCondition (stream.Condition);
                if (entered) {
                    try {
                        if (change.EndCallbacks == null)
                            stream.Announce (change.Value, change.Version);
                        else
                            stream.AnnounceEnding ();
                        Monitor.PulseAll (stream.Condition);
                    } finally {
                        Monitor.Exit (stream.Condition);
                    }
                }
                // The ending is already stored, so the end callbacks run even once closing
                if (change.EndCallbacks != null) {
                    if (!stream.DeferEnding (change.EndCallbacks, change.Ending)) {
                        foreach (var callback in change.EndCallbacks)
                            InvokeEndCallback (callback, change.Ending);
                    }
                } else if (entered) {
                    foreach (var callback in change.Callbacks)
                        stream.Call (callback, callback.Event ? change.Fired : change.Version, change.Value);
                }
            }
        }

        internal static void InvokeCallback (Action<object> callback, object value)
        {
            try {
                callback (value);
            } catch (System.Exception exn) {
                // A callback that throws must not stop the remaining callbacks running, nor
                // escape and stop the dispatcher thread. There is no caller to propagate it to,
                // so report it and carry on.
                Console.Error.WriteLine ("Exception in kRPC stream callback: " + exn);
            }
        }
    }
}

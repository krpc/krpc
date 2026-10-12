using System;
using System.Collections.Generic;
using System.Threading;
using KRPC.Client.Services.KRPC;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    sealed class StreamManager : IDisposable
    {
        readonly Connection connection;
        readonly object updateLock = new object ();
        readonly IDictionary<ulong, StreamImpl> streams = new Dictionary<ulong, StreamImpl> ();
        bool closed;
        // Streams out of the registry and not yet ended, with the ending for each. The
        // closing thread ends any whose condition the update thread stops trying to take.
        readonly IDictionary<StreamImpl, System.Exception> toEnd =
            new Dictionary<StreamImpl, System.Exception> ();
        // Every registered stream, to find one whose condition the closing thread holds
        readonly List<WeakReference<StreamImpl>> registered = new List<WeakReference<StreamImpl>> ();
        int pruneRegisteredAt = 16;
        // The highest stream id the server has given this client, and those of its ids not
        // yet registered. A result for one of these, or for a higher id, can arrive before
        // its stream is registered, and is kept until it is.
        long issued = -1;
        readonly HashSet<ulong> unregistered = new HashSet<ulong> ();
        readonly IDictionary<ulong, Pending> pending = new Dictionary<ulong, Pending> ();
        readonly UpdateThread updateThreadObject;
        readonly Thread updateThread;

        public StreamManager (Connection serverConnection, MessageReader reader, Action closeConnection)
        {
            connection = serverConnection;
            updateThreadObject = new UpdateThread (this, reader, closeConnection);
            updateThread = new Thread (new ThreadStart (updateThreadObject.Main));
            updateThread.Start ();
        }

        bool disposed;

        /// <summary>
        /// Whether a thread is disposing the connection, after which the update thread stops.
        /// </summary>
        public bool CloseRequested {
            get { return updateThreadObject.Stopped; }
        }

        /// <summary>
        /// Whether this thread is the update thread.
        /// </summary>
        public bool OnUpdateThread {
            get { return Thread.CurrentThread == updateThread; }
        }

        ~StreamManager ()
        {
            Dispose (false);
        }

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        void Dispose (bool disposing)
        {
            if (!disposed) {
                if (disposing) {
                    updateThreadObject.Stop ();
                    // A callback on the update thread can wait for a condition this thread holds,
                    // so we end the streams here, and join only when this thread holds none
                    var holding = HoldsACondition ();
                    EndAll (false);
                    // A callback runs on the update thread, and can dispose the connection
                    if (Thread.CurrentThread != updateThread && !holding)
                        updateThread.Join ();
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Whether this thread holds the condition of a stream.
        /// </summary>
        bool HoldsACondition ()
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

        void CheckDisposed ()
        {
            if (disposed)
                throw new ObjectDisposedException (GetType ().Name);
        }

        public StreamImpl AddStream (TypeSpec returnType, ProcedureCall call)
        {
            CheckDisposed ();
            var id = connection.KRPC ().AddStream (call).Id;
            return GetStream (returnType, id);
        }

        /// <summary>
        /// Note a stream id the server has given this client. Called in the order the
        /// server's replies arrive.
        /// </summary>
        public void NoteIssued (ulong id)
        {
            lock (updateLock) {
                issued = Math.Max (issued, (long)id);
                if (!streams.ContainsKey (id))
                    unregistered.Add (id);
            }
        }

        /// <summary>
        /// Stop keeping results for a stream id the client has removed from the server.
        /// </summary>
        public void NoteRemoved (ulong id)
        {
            lock (updateLock) {
                issued = Math.Max (issued, (long)id);
                unregistered.Remove (id);
                pending.Remove (id);
            }
        }

        /// <summary>
        /// Whether the id is noted and not registered, or has a kept result.
        /// </summary>
        internal bool Keeps (ulong id)
        {
            lock (updateLock) {
                return unregistered.Contains (id) || pending.ContainsKey (id);
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
            CheckDisposed ();
            StreamImpl stream;
            lock (updateLock) {
                if (streams.TryGetValue (id, out stream))
                    return stream;
            }
            var created = new StreamImpl (connection, id, returnType, updateLock);
            System.Exception ending = null;
            bool undecodable = false;
            IList<Action<System.Exception>> endCallbacks;
            // The condition is taken first, as everywhere else. The stream is new, so it has no
            // waiters or callbacks for its pending results.
            lock (created.Condition) {
                lock (updateLock) {
                    if (streams.TryGetValue (id, out stream))
                        return stream;
                    stream = created;
                    unregistered.Remove (id);
                    Pending kept;
                    if (pending.TryGetValue (id, out kept))
                        pending.Remove (id);
                    if (closed)
                        ending = ClosedError ();
                    else {
                        NoteRegistered (stream);
                        if (kept != null) {
                            bool ended = false;
                            if (kept.Value != null) {
                                var value = Decode (stream, kept.Value, out ended, out undecodable);
                                if (ended)
                                    ending = (System.Exception)value;
                                else
                                    stream.SetValue (value);
                            }
                            if (!ended && kept.Ending != null)
                                ending = (System.Exception)Decode (stream, kept.Ending, out ended, out undecodable);
                        }
                    }
                    if (ending == null) {
                        streams [id] = stream;
                        return stream;
                    }
                }
                endCallbacks = stream.End (ending);
            }
            foreach (var callback in endCallbacks)
                InvokeEndCallback (callback, ending);
            if (undecodable) {
                // Decoding a value can get a stream, for an event, with the update lock held
                if (Monitor.IsEntered (updateLock) || OnUpdateThread)
                    RemoveFromServerLater (id);
                else
                    RemoveFromServer (id);
            }
            return stream;
        }

        /// <summary>
        /// Remove a stream from the server and end it, unless it has already ended or a stream
        /// added since has taken its id.
        /// </summary>
        public void RemoveStream (StreamImpl stream)
        {
            bool onServer;
            System.Exception ending;
            lock (updateLock) {
                StreamImpl current;
                onServer = streams.TryGetValue (stream.Id, out current) && current == stream;
                if (onServer) {
                    streams.Remove (stream.Id);
                    ending = new System.InvalidOperationException ("Stream has been removed");
                } else {
                    // A stream with a decoded final result, or one the client is closing, is
                    // ended with that ending. The server has no stream for it to remove.
                    if (!toEnd.TryGetValue (stream, out ending))
                        return;
                }
            }
            // Ended first, so a value that arrives during the call is dropped
            var endCallbacks = stream.End (ending);
            lock (updateLock) {
                toEnd.Remove (stream);
            }
            foreach (var callback in endCallbacks)
                InvokeEndCallback (callback, ending);
            if (onServer)
                connection.KRPC ().RemoveStream (stream.Id);
        }

        /// <summary>
        /// End every stream, after the connection has closed and no further update can arrive.
        /// </summary>
        void NotifyClosed ()
        {
            EndAll (true);
        }

        /// <summary>
        /// End every stream, once the connection is closing and no further update is applied.
        /// Called by the disposing thread and by the update thread.
        /// </summary>
        /// <remarks>
        /// Each pass ends the streams whose condition it can take, and the passes repeat until
        /// every stream has ended. A thread can hold one stream's condition while it waits on
        /// another, and ending the other releases the first. The update thread leaves the
        /// remaining streams to the disposing thread.
        /// </remarks>
        void EndAll (bool onUpdateThread)
        {
            lock (updateLock) {
                closed = true;
                var ending = ClosedError ();
                foreach (var stream in streams.Values)
                    toEnd [stream] = ending;
                streams.Clear ();
                pending.Clear ();
                unregistered.Clear ();
            }
            while (true) {
                IList<KeyValuePair<StreamImpl, System.Exception>> remaining;
                lock (updateLock) {
                    foreach (var stream in new List<StreamImpl> (toEnd.Keys)) {
                        if (stream.Ended)
                            toEnd.Remove (stream);
                    }
                    if (toEnd.Count == 0)
                        return;
                    if (onUpdateThread && CloseRequested)
                        return;
                    remaining = new List<KeyValuePair<StreamImpl, System.Exception>> (toEnd);
                }
                foreach (var entry in remaining)
                    TryEnd (entry.Key, entry.Value);
            }
        }

        void TryEnd (StreamImpl stream, System.Exception ending)
        {
            if (Monitor.TryEnter (stream.Condition, 10))
                End (stream, ending);
        }

        /// <summary>
        /// End a stream out of the registry, and release its condition.
        /// </summary>
        void End (StreamImpl stream, System.Exception ending)
        {
            IList<Action<System.Exception>> endCallbacks;
            try {
                endCallbacks = stream.End (ending);
                lock (updateLock) {
                    toEnd.Remove (stream);
                }
            } finally {
                Monitor.Exit (stream.Condition);
            }
            foreach (var callback in endCallbacks)
                InvokeEndCallback (callback, ending);
        }

        /// <summary>
        /// Take a stream's condition on the update thread. Returns false, without the condition,
        /// once the connection is closing, as the disposing thread can hold the condition while
        /// it joins the update thread.
        /// </summary>
        bool EnterCondition (object condition)
        {
            while (!Monitor.TryEnter (condition, 50)) {
                if (CloseRequested)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// End a stream the server has ended. When the update thread stops trying to take its
        /// condition, the disposing thread ends it.
        /// </summary>
        void EndOnUpdateThread (StreamImpl stream, System.Exception ending)
        {
            if (EnterCondition (stream.Condition))
                End (stream, ending);
        }

        /// <summary>
        /// Remove a stream whose value failed to decode from the server. Best effort, as the
        /// stream has already ended.
        /// </summary>
        void RemoveFromServer (ulong id)
        {
            if (CloseRequested)
                return;
            try {
                connection.KRPC ().RemoveStream (id);
            } catch (System.Exception) {
                // The connection can be closing, which also removes the stream
            }
        }

        /// <summary>
        /// Remove a stream from the server on another thread, as a call can wait behind a long
        /// call on another thread.
        /// </summary>
        void RemoveFromServerLater (ulong id)
        {
            var thread = new Thread (() => RemoveFromServer (id));
            thread.IsBackground = true;
            thread.Start ();
        }

        static System.Exception ClosedError ()
        {
            return new ConnectionException ("Connection closed");
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
        /// Decode a result, which is final if it is an error or fails to decode. Called with
        /// the update lock held.
        /// </summary>
        object Decode (StreamImpl stream, ProcedureResult result, out bool ended, out bool undecodable)
        {
            ended = true;
            undecodable = false;
            try {
                if (result.Error != null)
                    return connection.GetException (result.Error);
                var value = result.IsNull ? null : Encoder.Decode (result.Value, stream.ReturnType, connection);
                ended = false;
                return value;
            } catch (System.Exception exn) {
                // A value that fails to decode ends only its stream
                undecodable = true;
                return exn;
            }
        }

        /// <summary>
        /// Keep a result for a stream not registered yet. Only the latest value and an ending
        /// are observable once the stream is registered, so only those are kept.
        /// </summary>
        void Keep (ulong id, ProcedureResult result)
        {
            Pending kept;
            if (!pending.TryGetValue (id, out kept)) {
                kept = new Pending ();
                pending [id] = kept;
            }
            if (kept.Ending != null)
                return;
            if (result.Error != null)
                kept.Ending = result;
            else
                kept.Value = result;
        }

        internal void Update (ulong id, ProcedureResult result)
        {
            StreamImpl stream;
            object value;
            bool ended;
            bool undecodable;
            lock (updateLock) {
                if (!streams.TryGetValue (id, out stream)) {
                    // A result for a stream not registered yet is kept. One for an id below
                    // those is for a stream that has ended.
                    if (closed)
                        return;
                    if ((long)id > issued || unregistered.Contains (id))
                        Keep (id, result);
                    return;
                }
                value = Decode (stream, result, out ended, out undecodable);
                if (ended) {
                    // A final result moves the stream to the streams to end, as the server has
                    // already removed it, unless the value failed to decode
                    streams.Remove (id);
                    toEnd [stream] = (System.Exception)value;
                }
            }
            if (ended) {
                EndOnUpdateThread (stream, (System.Exception)value);
                if (undecodable)
                    RemoveFromServerLater (id);
                return;
            }
            // The stream's condition is always taken before the update lock
            var condition = stream.Condition;
            if (!EnterCondition (condition))
                return;
            IList<StreamImpl.Callback> callbacks;
            long version;
            long fired;
            try {
                lock (updateLock) {
                    // The stream can be removed while its new value is being decoded. The ending
                    // it stored is kept.
                    if (stream.Ended)
                        return;
                    stream.SetValue (value);
                    // Copied with the value stored, so a callback added after this reads the
                    // value itself. The callbacks run with no lock held.
                    callbacks = new List<StreamImpl.Callback> (stream.Callbacks.Values);
                    version = stream.Version;
                    fired = stream.Fired;
                }
                Monitor.PulseAll (condition);
            } finally {
                Monitor.Exit (condition);
            }
            foreach (var callback in callbacks)
                stream.Call (callback, callback.Event ? fired : version, value);
        }

        internal static void InvokeCallback (Action<object> callback, object value)
        {
            try {
                callback (value);
            } catch (System.Exception exn) {
                // A callback that throws must not stop the remaining callbacks running,
                // nor escape and end the update thread, which would silently stop every
                // stream on the connection. There is no caller to propagate it to, so
                // report it and carry on.
                Console.Error.WriteLine ("Exception in kRPC stream callback: " + exn);
            }
        }

        sealed class Pending
        {
            public ProcedureResult Value;
            public ProcedureResult Ending;
        }

        sealed class UpdateThread
        {
            readonly StreamManager manager;
            readonly MessageReader reader;
            readonly Action closeConnection;
            volatile bool stop;
            readonly EventWaitHandle stopEvent = new EventWaitHandle (false, EventResetMode.ManualReset);

            public UpdateThread (StreamManager streamManager, MessageReader streamReader, Action close)
            {
                manager = streamManager;
                reader = streamReader;
                closeConnection = close;
            }

            public bool Stopped {
                get { return stop; }
            }

            public void Stop ()
            {
                stop = true;
                stopEvent.Set ();
            }

            public void Main ()
            {
                try {
                    while (!stop) {
                        if (!reader.Read (stopEvent) || stop)
                            break;
                        var update = StreamUpdate.Parser.ParseFrom (
                            reader.Buffer, reader.Offset, reader.Size);
                        if (stop)
                            break;
                        foreach (var result in update.Results) {
                            manager.Update (result.Id, result.Result);
                            if (stop)
                                break;
                        }
                    }
                } catch (System.Exception) {
                    // The connection closed, or a message that cannot be read left it unusable
                    if (!stop)
                        closeConnection ();
                } finally {
                    manager.NotifyClosed ();
                }
            }
        }
    }
}

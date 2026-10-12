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
                    // A callback runs on the update thread, and can dispose the connection
                    if (Thread.CurrentThread != updateThread)
                        updateThread.Join ();
                }
                disposed = true;
            }
        }

        void CheckDisposed ()
        {
            if (disposed)
                throw new ObjectDisposedException (GetType ().Name);
        }

        public StreamImpl AddStream (TypeSpec returnType, ProcedureCall call)
        {
            CheckDisposed ();
            var id = connection.KRPC ().AddStream (call, false).Id;
            return GetStream (returnType, id);
        }

        public StreamImpl GetStream (TypeSpec returnType, ulong id)
        {
            CheckDisposed ();
            StreamImpl stream;
            lock (updateLock) {
                if (streams.TryGetValue (id, out stream))
                    return stream;
                stream = new StreamImpl (connection, id, returnType, updateLock);
                if (!closed) {
                    streams [id] = stream;
                    return stream;
                }
            }
            // No update arrives after the connection closes
            EndStream (stream, ClosedError ());
            return stream;
        }

        /// <summary>
        /// Remove a stream from the server and end it, unless it has already ended or a stream
        /// added since has taken its id.
        /// </summary>
        public void RemoveStream (StreamImpl stream)
        {
            lock (updateLock) {
                StreamImpl registered;
                if (!streams.TryGetValue (stream.Id, out registered) || registered != stream)
                    return;
                streams.Remove (stream.Id);
            }
            EndStream (stream, new System.InvalidOperationException ("Stream has been removed"));
            connection.KRPC ().RemoveStream (stream.Id);
        }

        /// <summary>
        /// End every stream, after the connection has closed and no further update can arrive.
        /// </summary>
        void NotifyClosed ()
        {
            IList<StreamImpl> ended;
            lock (updateLock) {
                closed = true;
                ended = new List<StreamImpl> (streams.Values);
                streams.Clear ();
            }
            foreach (var stream in ended)
                EndStream (stream, ClosedError ());
        }

        static System.Exception ClosedError ()
        {
            return new ConnectionException ("Connection closed");
        }

        static void EndStream (StreamImpl stream, System.Exception exn)
        {
            foreach (var callback in stream.End (exn))
                InvokeEndCallback (callback, exn);
        }

        internal static void InvokeEndCallback (Action<System.Exception> callback, System.Exception exn)
        {
            try {
                callback (exn);
            } catch (System.Exception callbackExn) {
                Console.Error.WriteLine ("Exception in kRPC stream end callback: " + callbackExn);
            }
        }

        internal void Update (ulong id, ProcedureResult result)
        {
            StreamImpl stream;
            object value;
            bool ended = true;
            IList<Action<object>> callbacks;
            // The stream's condition is always taken before the update lock, so we release the
            // update lock before taking the condition. The callbacks are copied to run with no
            // lock held.
            lock (updateLock) {
                if (!streams.TryGetValue (id, out stream))
                    return;
                try {
                    if (result.Error != null) {
                        value = connection.GetException (result.Error);
                    } else {
                        value = result.IsNull ? null : Encoder.Decode (result.Value, stream.ReturnType, connection);
                        ended = false;
                    }
                } catch (System.Exception exn) {
                    // A value that fails to decode ends only its stream
                    value = exn;
                }
                // A final result ends the stream, and the server has already removed it
                if (ended)
                    streams.Remove (id);
                callbacks = new List<Action<object>> (stream.Callbacks.Values);
            }
            if (ended) {
                EndStream (stream, (System.Exception)value);
                return;
            }
            var condition = stream.Condition;
            lock (condition) {
                lock (updateLock) {
                    // The stream can be removed while its new value is being decoded. The ending
                    // it stored is kept.
                    if (stream.Ended)
                        return;
                    stream.SetValue (value);
                }
                Monitor.PulseAll (condition);
            }
            foreach (var callback in callbacks) {
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

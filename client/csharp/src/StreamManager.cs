using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using KRPC.Client.Services.KRPC;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    sealed class StreamManager : IDisposable
    {
        readonly Connection connection;
        readonly object updateLock = new object ();
        readonly IDictionary<ulong, StreamImpl> streams = new Dictionary<ulong, StreamImpl> ();
        readonly UpdateThread updateThreadObject;
        readonly Thread updateThread;

        public StreamManager (Connection serverConnection, TcpClient streamClient)
        {
            connection = serverConnection;
            updateThreadObject = new UpdateThread (this, streamClient);
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

        public StreamImpl AddStream (System.Type returnType, ProcedureCall call)
        {
            CheckDisposed ();
            var id = connection.KRPC ().AddStream (call, false).Id;
            lock (updateLock) {
                if (!streams.ContainsKey (id))
                    streams [id] = new StreamImpl(connection, id, returnType, updateLock);
                return streams [id];
            }
        }

        public StreamImpl GetStream (System.Type returnType, ulong id)
        {
            CheckDisposed ();
            lock (updateLock) {
                if (!streams.ContainsKey (id))
                    streams [id] = new StreamImpl(connection, id, returnType, updateLock);
                return streams [id];
            }
        }

        public void RemoveStream (ulong id)
        {
            CheckDisposed ();
            lock (updateLock) {
                if (streams.ContainsKey (id)) {
                    connection.KRPC ().RemoveStream (id);
                    streams.Remove (id);
                }
            }
        }

        void Update (ulong id, ProcedureResult result)
        {
            StreamImpl stream;
            object value;
            IList<Action<object>> callbacks;
            // The update lock is held only to find the stream and decode its new value, and is
            // released before the stream's condition is taken. A thread waiting for an update
            // holds the condition and then needs the update lock -- Event.Wait resets the
            // stream value while holding it, as its documented use requires -- so taking the
            // two in the opposite order here deadlocks. The callbacks are copied for the same
            // reason: they run below without the lock held.
            lock (updateLock) {
                if (!streams.ContainsKey (id))
                    return;
                stream = streams [id];
                if (result.Error == null)
                    value = Encoder.Decode (result.Value, stream.ReturnType, connection);
                else
                    value = connection.GetException (result.Error);
                callbacks = new List<Action<object>> (stream.Callbacks.Values);
            }
            var condition = stream.Condition;
            lock (condition) {
                lock (updateLock) {
                    // The stream can be removed while its new value is being decoded, in which
                    // case Remove has already stored the error saying so and this value must
                    // not overwrite it. The stream is gone from the registry, so nothing would
                    // ever replace it and it would be returned as though still live.
                    if (!streams.ContainsKey (id))
                        return;
                    stream.Value = value;
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
            readonly NetworkStream stream;
            volatile bool stop;
            readonly EventWaitHandle stopEvent = new EventWaitHandle (false, EventResetMode.ManualReset);
            byte [] buffer = new byte [Connection.BUFFER_INITIAL_SIZE];

            public UpdateThread (StreamManager streamManager, TcpClient streamClient)
            {
                manager = streamManager;
                stream = streamClient.GetStream ();
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
                        var size = Connection.ReadMessageData (stream, ref buffer, stopEvent);
                        if (size == 0 || stop)
                            break;
                        var update = StreamUpdate.Parser.ParseFrom (new CodedInputStream (buffer, 0, size));
                        if (stop)
                            break;
                        foreach (var result in update.Results) {
                            manager.Update (result.Id, result.Result);
                            if (stop)
                                break;
                        }
                    }
                } catch (ObjectDisposedException) {
                    // The connection was closed (e.g. on disconnect); end the update thread.
                } catch (IOException) {
                    // The connection was closed (e.g. on disconnect); end the update thread.
                }
            }
        }
    }
}

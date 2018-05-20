using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using KRPC.Client.Services.KRPC;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    [SuppressMessage ("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule")]
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

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public StreamImpl GetStream (System.Type returnType, ulong id)
        {
            CheckDisposed ();
            lock (updateLock) {
                if (!streams.ContainsKey (id))
                    streams [id] = new StreamImpl(connection, id, returnType, updateLock);
                return streams [id];
            }
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
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

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        void Update (ulong id, ProcedureResult result)
        {
            lock (updateLock) {
                if (!streams.ContainsKey (id))
                    return;
                var stream = streams [id];
                object value;
                if (result.Error == null)
                    value = Encoder.Decode (result.Value, stream.ReturnType, connection);
                else
                    value = connection.GetException (result.Error);
                var condition = stream.Condition;
                lock (condition) {
                    stream.Value = value;
                    Monitor.PulseAll (condition);
                }
                foreach (var callback in stream.Callbacks.Values)
                    callback (value);
            }
        }

        [SuppressMessage ("Gendarme.Rules.Correctness", "TypesWithDisposableFieldsShouldBeDisposableRule")]
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
                    // Connection closed, so exit
                    // FIXME: is there a better way to handle this?
                } catch (IOException) {
                    // Connection closed, so exit
                    // FIXME: is there a better way to handle this?
                }
            }
        }
    }
}

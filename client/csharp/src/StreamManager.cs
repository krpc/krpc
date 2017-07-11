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
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    sealed class StreamManager : IDisposable
    {
        readonly Connection connection;
        readonly object accessLock = new object ();
        readonly IDictionary<ulong, System.Type> streamTypes = new Dictionary<ulong, System.Type> ();
        readonly IDictionary<ulong, object> streamValues = new Dictionary<ulong, object> ();
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

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public ulong AddStream (ProcedureCall call, System.Type type)
        {
            CheckDisposed ();
            var id = connection.KRPC ().AddStream (call).Id;
            lock (accessLock) {
                if (!streamTypes.ContainsKey (id)) {
                    streamTypes [id] = type;
                    ByteString result = null;
                    object error = null;
                    try {
                        result = connection.Invoke (call);
                    } catch (System.Exception exn) {
                        error = exn;
                    }
                    streamValues [id] = (error == null ? Encoder.Decode (result, type, connection) : error);
                }
            }
            return id;
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public void RemoveStream (ulong id)
        {
            CheckDisposed ();
            connection.KRPC ().RemoveStream (id);
            lock (accessLock) {
                streamTypes.Remove (id);
                streamValues.Remove (id);
            }
        }

        public object GetValue (ulong id)
        {
            CheckDisposed ();
            object result;
            lock (accessLock) {
                if (!streamTypes.ContainsKey (id))
                    throw new System.InvalidOperationException ("Stream does not exist or has been closed");
                result = streamValues [id];
                var exn = result as System.Exception;
                if (exn != null)
                    throw exn;
            }
            return result;
        }

        void Update (ulong id, ProcedureResult result)
        {
            lock (accessLock) {
                object value;
                if (result.Error == null)
                    value = Encoder.Decode (result.Value, streamTypes [id], connection);
                else
                    value = connection.GetException (result.Error);
                streamValues [id] = value;
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

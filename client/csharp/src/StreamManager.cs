using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
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
        readonly Object accessLock = new Object ();
        readonly IDictionary<UInt32, System.Type> streamTypes = new Dictionary<UInt32, System.Type> ();
        readonly IDictionary<UInt32, ByteString> streamData = new Dictionary<UInt32, ByteString> ();
        readonly IDictionary<UInt32, Object> streamValues = new Dictionary<UInt32, Object> ();
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
        public UInt32 AddStream (Request request, System.Type type)
        {
            CheckDisposed ();
            var id = connection.KRPC ().AddStream (request);
            lock (accessLock) {
                if (!streamTypes.ContainsKey (id)) {
                    streamTypes [id] = type;
                    streamData [id] = connection.Invoke (request);
                }
            }
            return id;
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public void RemoveStream (UInt32 id)
        {
            CheckDisposed ();
            connection.KRPC ().RemoveStream (id);
            lock (accessLock) {
                streamTypes.Remove (id);
                streamData.Remove (id);
                streamValues.Remove (id);
            }
        }

        public Object GetValue (UInt32 id)
        {
            CheckDisposed ();
            Object result;
            lock (accessLock) {
                if (!streamTypes.ContainsKey (id))
                    throw new InvalidOperationException ("Stream does not exist or has been closed");
                if (streamValues.ContainsKey (id))
                    return streamValues [id];
                streamValues [id] = Encoder.Decode (streamData [id], streamTypes [id], connection);
                result = streamValues [id];
            }
            return result;
        }

        void Update (UInt32 id, Response response)
        {
            lock (accessLock) {
                if (!streamData.ContainsKey (id))
                    return;
                if (response.Error.Length > 0)
                    return; //TODO: do something with the error
                var data = response.ReturnValue;
                streamData [id] = data;
                streamValues.Remove (id);
            }
        }

        [SuppressMessage ("Gendarme.Rules.Correctness", "TypesWithDisposableFieldsShouldBeDisposableRule")]
        sealed class UpdateThread
        {
            readonly StreamManager manager;
            readonly NetworkStream stream;
            volatile bool stop;
            EventWaitHandle stopEvent = new EventWaitHandle (false, EventResetMode.ManualReset);
            byte[] buffer = new byte [Connection.BUFFER_INITIAL_SIZE];

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
                        var message = StreamMessage.Parser.ParseFrom (new CodedInputStream (buffer, 0, size));
                        //TODO: handle errors
                        if (stop)
                            break;
                        foreach (var response in message.Responses) {
                            manager.Update (response.Id, response.Response);
                            if (stop)
                                break;
                        }
                    }
                } catch (ObjectDisposedException) {
                    // Connection closed, so exit
                    //FIXME: is there a better way to handle this?
                } catch (IOException) {
                    // Connection closed, so exit
                    //FIXME: is there a better way to handle this?
                }
            }
        }
    }
}

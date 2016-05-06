using Google.Protobuf;
using KRPC.Client.Services.KRPC;
using KRPC.Schema.KRPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace KRPC.Client
{
    internal class StreamManager
    {
        Connection connection;
        Object accessLock = new Object ();
        IDictionary<UInt32, Type> streamTypes = new Dictionary<UInt32, Type> ();
        IDictionary<UInt32, ByteString> streamData = new Dictionary<UInt32, ByteString> ();
        IDictionary<UInt32, Object> streamValues = new Dictionary<UInt32, Object> ();
        Thread updateThread;

        internal StreamManager (Connection connection, IPAddress address, int port, byte[] clientIdentifier)
        {
            this.connection = connection;
            updateThread = new Thread (new ThreadStart (new UpdateThread (this, address, port, clientIdentifier).Main));
            updateThread.Start ();
        }

        internal UInt32 AddStream (Request request, Type type)
        {
            var id = this.connection.KRPC ().AddStream (request);
            lock (accessLock) {
                if (!streamTypes.ContainsKey (id)) {
                    streamTypes [id] = type;
                    streamData [id] = this.connection.Invoke (request);
                }
            }
            return id;
        }

        internal void RemoveStream (UInt32 id)
        {
            this.connection.KRPC ().RemoveStream (id);
            lock (accessLock) {
                streamTypes.Remove (id);
                streamData.Remove (id);
                streamValues.Remove (id);
            }
        }

        internal Object GetValue (UInt32 id)
        {
            Object result;
            lock (accessLock) {
                if (!streamTypes.ContainsKey (id))
                    throw new InvalidOperationException ("Stream does not exist or has been closed");
                if (streamValues.ContainsKey (id))
                    return streamValues [id];
                streamValues [id] = Encoder.Decode (streamData [id], streamTypes [id], this.connection);
                result = streamValues [id];
            }
            return result;
        }

        void Update (UInt32 id, KRPC.Schema.KRPC.Response response)
        {
            lock (accessLock) {
                if (!streamData.ContainsKey (id))
                    throw new ArgumentException ("Stream does not exist");
                if (response.Error.Length > 0)
                    return; //TODO: do something with the error
                var data = response.ReturnValue;
                streamData [id] = data;
                streamValues.Remove (id);
            }
        }

        class UpdateThread
        {
            StreamManager manager;
            IPAddress address;
            int port;
            byte[] clientIdentifier;

            TcpClient client;
            Stream stream;
            CodedInputStream codedStream;

            internal UpdateThread (StreamManager manager, IPAddress address, int port, byte[] clientIdentifier)
            {
                this.manager = manager;
                this.address = address;
                this.port = port;
                this.clientIdentifier = clientIdentifier;
            }

            internal void Main ()
            {
                client = new TcpClient ();
                client.Connect (address, port);
                stream = client.GetStream ();
                stream.Write (Encoder.streamHelloMessage, 0, Encoder.streamHelloMessage.Length);
                stream.Write (clientIdentifier, 0, clientIdentifier.Length);
                var recvOkMessage = new byte [Encoder.okMessage.Length];
                stream.Read (recvOkMessage, 0, Encoder.okMessage.Length);
                if (recvOkMessage.Equals (Encoder.okMessage))
                    throw new Exception ("Invalid hello message received from stream server. " +
                    "Got " + Encoder.ToHexString (recvOkMessage));
                this.codedStream = new CodedInputStream (stream);

                try {
                    while (true) {
                        var message = new StreamMessage ();
                        codedStream.ReadMessage (message);
                        foreach (var response in message.Responses)
                            manager.Update (response.Id, response.Response);
                    }
                } catch (IOException) {
                    // Exit when the connection closes
                }
            }
        }
    }
}

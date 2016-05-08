using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using KRPC.Client.Services.KRPC;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    class StreamManager
    {
        readonly Connection connection;
        readonly Object accessLock = new Object ();
        readonly IDictionary<UInt32, Type> streamTypes = new Dictionary<UInt32, Type> ();
        readonly IDictionary<UInt32, ByteString> streamData = new Dictionary<UInt32, ByteString> ();
        readonly IDictionary<UInt32, Object> streamValues = new Dictionary<UInt32, Object> ();
        readonly Thread updateThread;

        internal StreamManager (Connection connection, IPAddress address, int port, byte[] clientIdentifier)
        {
            this.connection = connection;
            updateThread = new Thread (new ThreadStart (new UpdateThread (this, address, port, clientIdentifier).Main));
            updateThread.Start ();
        }

        internal UInt32 AddStream (Request request, Type type)
        {
            var id = connection.KRPC ().AddStream (request);
            lock (accessLock) {
                if (!streamTypes.ContainsKey (id)) {
                    streamTypes [id] = type;
                    streamData [id] = connection.Invoke (request);
                }
            }
            return id;
        }

        internal void RemoveStream (UInt32 id)
        {
            connection.KRPC ().RemoveStream (id);
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
                streamValues [id] = Encoder.Decode (streamData [id], streamTypes [id], connection);
                result = streamValues [id];
            }
            return result;
        }

        void Update (UInt32 id, Response response)
        {
            lock (accessLock) {
                if (!streamData.ContainsKey (id))
                    throw new InvalidOperationException ("Stream does not exist or has been closed");
                if (response.HasError)
                    return; //TODO: do something with the error
                var data = response.ReturnValue;
                streamData [id] = data;
                streamValues.Remove (id);
            }
        }

        class UpdateThread
        {
            readonly StreamManager manager;
            readonly IPAddress address;
            readonly int port;
            readonly byte[] clientIdentifier;

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
                stream.Write (Encoder.StreamHelloMessage, 0, Encoder.StreamHelloMessage.Length);
                stream.Write (clientIdentifier, 0, clientIdentifier.Length);
                var recvOkMessage = new byte [Encoder.OkMessage.Length];
                stream.Read (recvOkMessage, 0, Encoder.OkMessage.Length);
                if (recvOkMessage.Equals (Encoder.OkMessage))
                    throw new Exception ("Invalid hello message received from stream server. " +
                    "Got " + Encoder.ToHexString (recvOkMessage));
                codedStream = new CodedInputStream (stream);

                try {
                    while (true) {
                        var message = new StreamMessage ();
                        codedStream.ReadMessage (message);
                        foreach (var response in message.Responses)
                            manager.Update (response.Id, response.Response);
                    }
                } catch (IOException) {
                    // Exit when the connection closes
                } catch (InvalidOperationException) {
                    // Exit when a stream update fails - connection has been closed
                }
            }
        }
    }
}

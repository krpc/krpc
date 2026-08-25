using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using KRPC.Schema.KRPC;

namespace KRPC.Client.Test
{
    /// <summary>
    /// A stand-in for a kRPC server, which answers the connection handshake and then echoes
    /// back a response for every request. These tests are about how a transport carries the
    /// messages, so nothing the server would do with them has to be understood to answer them.
    /// The transport it listens on is supplied by whichever test case is running.
    /// </summary>
    public sealed class StandInServer : IDisposable
    {
        readonly Socket listener;
        readonly List<Socket> accepted = new List<Socket> ();
        readonly Thread thread;
        volatile bool stopping;

        /// <summary>
        /// The status the handshake is answered with, so that a test can have a connection
        /// refused as the server refuses one made to the wrong endpoint.
        /// </summary>
        public ConnectionResponse.Types.Status Status { get; set; }

        /// <summary>
        /// The message accompanying a refused connection.
        /// </summary>
        public string Message { get; set; }

        public StandInServer (Socket listeningSocket)
        {
            listener = listeningSocket;
            listener.Listen (2);
            Status = ConnectionResponse.Types.Status.Ok;
            Message = string.Empty;
            thread = new Thread (Run);
            thread.IsBackground = true;
            thread.Start ();
        }

        public EndPoint EndPoint {
            get { return listener.LocalEndPoint; }
        }

        public void Dispose ()
        {
            stopping = true;
            listener.Close ();
            thread.Join (5000);
            foreach (var socket in accepted)
                socket.Close ();
        }

        void Run ()
        {
            while (!stopping) {
                Socket socket;
                try {
                    socket = listener.Accept ();
                } catch (SocketException) {
                    return;
                } catch (ObjectDisposedException) {
                    return;
                }
                lock (accepted)
                    accepted.Add (socket);
                var serve = new Thread (() => Serve (socket));
                serve.IsBackground = true;
                serve.Start ();
            }
        }

        void Serve (Socket socket)
        {
            try {
                using (var stream = new NetworkStream (socket)) {
                    var reader = new MessageReader (stream);
                    reader.Read ();
                    ConnectionRequest.Parser.ParseFrom (reader.Buffer, reader.Offset, reader.Size);
                    var response = new ConnectionResponse ();
                    response.Status = Status;
                    response.Message = Message;
                    response.ClientIdentifier = ByteString.CopyFrom (new byte [16]);
                    Write (stream, response);
                    if (Status != ConnectionResponse.Types.Status.Ok)
                        return;
                    // Every request is answered with an empty result, the same as a call
                    // returning nothing gets back from a real server
                    while (!stopping) {
                        reader.Read ();
                        Request.Parser.ParseFrom (reader.Buffer, reader.Offset, reader.Size);
                        var reply = new Response ();
                        reply.Results.Add (new ProcedureResult ());
                        Write (stream, reply);
                    }
                }
            } catch (IOException) {
                // The client has gone, which is how a connection ends here. A malformed
                // message raises the same exception.
            } catch (SocketException) {
            } catch (ObjectDisposedException) {
            }
        }

        static void Write (System.IO.Stream stream, IMessage message)
        {
            var output = new CodedOutputStream (stream, true);
            output.WriteLength (message.CalculateSize ());
            message.WriteTo (output);
            output.Flush ();
        }
    }
}

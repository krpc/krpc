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
        /// refused.
        /// </summary>
        public ConnectionResponse.Types.Status Status { get; set; }

        /// <summary>
        /// The message accompanying a refused connection.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Whether the handshake is answered with a malformed length prefix.
        /// </summary>
        public bool MalformedHandshake { get; set; }

        /// <summary>
        /// The reason given in a disconnect sent in answer to a request, or null to answer
        /// requests.
        /// </summary>
        public Disconnect.Types.Reason? DisconnectReason { get; set; }

        /// <summary>
        /// Whether a request is answered with the start of a message that never finishes.
        /// </summary>
        public bool StallResponses { get; set; }

        /// <summary>
        /// Whether a request is answered only once the client's disconnect arrives, after which
        /// the connection is closed.
        /// </summary>
        public bool AnswerOnDisconnect {
            get { return answerOnDisconnect; }
            set { answerOnDisconnect = value; }
        }

        volatile bool answerOnDisconnect;

        /// <summary>
        /// Set when a request held by <see cref="AnswerOnDisconnect"/> arrives.
        /// </summary>
        public ManualResetEvent RequestHeld {
            get { return requestHeld; }
        }

        readonly ManualResetEvent requestHeld = new ManualResetEvent (false);

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
                    var request = Schema.KRPC.Message.Parser.ParseFrom (reader.Buffer, reader.Offset, reader.Size);
                    if (request.ConnectionRequest == null)
                        return;
                    if (MalformedHandshake) {
                        // A varint that does not end within five bytes
                        stream.Write (new byte [] { 0xff, 0xff, 0xff, 0xff, 0xff, 0x01 }, 0, 6);
                        return;
                    }
                    var response = new ConnectionResponse ();
                    response.Status = Status;
                    response.Message = Message;
                    response.ProtocolVersion = 2;
                    response.ClientIdentifier = ByteString.CopyFrom (new byte [16]);
                    Write (stream, new Schema.KRPC.Message { ConnectionResponse = response });
                    if (Status != ConnectionResponse.Types.Status.Ok)
                        return;
                    // Every request is answered with an empty result, the same as a call
                    // returning nothing gets back from a real server
                    Response held = null;
                    while (!stopping) {
                        reader.Read ();
                        var message = Schema.KRPC.Message.Parser.ParseFrom (reader.Buffer, reader.Offset, reader.Size);
                        if (message.Disconnect != null) {
                            if (held != null) {
                                Thread.Sleep (100);
                                Write (stream, new Schema.KRPC.Message { Response = held });
                                socket.Shutdown (SocketShutdown.Both);
                            }
                            return;
                        }
                        if (DisconnectReason.HasValue) {
                            Write (stream, new Schema.KRPC.Message {
                                Disconnect = new Disconnect { Reason = DisconnectReason.Value, Message = "Bye" }
                            });
                            return;
                        }
                        if (StallResponses) {
                            // A length prefix of 10, and 2 of the bytes it promises
                            stream.Write (new byte [] { 0x0a, 0x12, 0x00 }, 0, 3);
                            continue;
                        }
                        var reply = new Response { Id = message.Request.Id };
                        reply.Results.Add (new ProcedureResult ());
                        if (answerOnDisconnect) {
                            held = reply;
                            requestHeld.Set ();
                            continue;
                        }
                        Write (stream, new Schema.KRPC.Message { Response = reply });
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

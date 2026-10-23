using System;
using System.Diagnostics;
using Google.Protobuf;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;
using KRPC.Utils;
using IMessage = Google.Protobuf.IMessage;
using ContentCase = KRPC.Schema.KRPC.Message.ContentOneofCase;
using ConnectionResponse = KRPC.Schema.KRPC.ConnectionResponse;
using Disconnect = KRPC.Schema.KRPC.Disconnect;
using Reason = KRPC.Schema.KRPC.Disconnect.Types.Reason;
using SchemaMessage = KRPC.Schema.KRPC.Message;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;

namespace KRPC.Server.Message
{
    /// <summary>
    /// A connection from a client, carrying the messages of one session at a time. It reads the
    /// handshake, waits for the connection to be approved, then carries the session's requests,
    /// responses and stream updates. A connection request during a session starts a new one.
    /// Subclasses provide the framing of messages on the transport.
    /// </summary>
    abstract class Connection
    {
        enum State
        {
            Handshake,
            Pending,
            Session,
            Closing,
            Closed
        }

        const int BUFFER_INITIAL_SIZE = 64 * 1024;
        const int BUFFER_INCREASE_SIZE = 1024 * 1024;
        const double HANDSHAKE_TIMEOUT = 3;
        const double CLOSING_TIMEOUT = 1;

        readonly Server server;
        State state;
        byte[] buffer = new byte [BUFFER_INITIAL_SIZE];
        int bufferSize;
        readonly Stopwatch handshakeTimer = Stopwatch.StartNew ();
        readonly Stopwatch stallTimer = new Stopwatch ();
        readonly Stopwatch closingTimer = new Stopwatch ();
        readonly MessageEnvelope envelope = new MessageEnvelope ();
        readonly StreamUpdateMessage streamUpdate = new StreamUpdateMessage ();
        Client session;
        bool sessionStarted;
        Request bufferedRequest;

        protected Connection (Server messageServer, IClient<byte,byte> client)
        {
            server = messageServer;
            Client = client;
        }

        /// <summary>
        /// The byte client the connection is carried on.
        /// </summary>
        public IClient<byte,byte> Client { get; private set; }

        /// <summary>
        /// The client in log lines: the session's client once a connection request has named
        /// one, and the byte client before.
        /// </summary>
        protected string Label {
            get { return session != null ? session.Label () : Client.Label (); }
        }

        protected IStream<byte,byte> Stream {
            get { return Client.Stream; }
        }

        /// <summary>
        /// The client of the current session, or null if there is none.
        /// </summary>
        public Client Session {
            get { return state == State.Session ? session : null; }
        }

        /// <summary>
        /// Whether the client's session is the current one, pending approval or started.
        /// </summary>
        public bool IsCurrent (Client client)
        {
            return client == session;
        }

        /// <summary>
        /// Decode a message from <paramref name="length"/> bytes of <paramref name="data"/>,
        /// starting at <paramref name="offset"/>. Returns the number of bytes consumed, and sets
        /// <paramref name="message"/> if they held a complete message. Returns zero if the data
        /// holds only part of a message. Throws a MalformedRequestException if the data is not
        /// a message, and a ClientDisconnectedException to end the connection with no reply.
        /// </summary>
        protected abstract int Read (ref SchemaMessage message, byte[] data, int offset, int length);

        /// <summary>
        /// Write a message.
        /// </summary>
        protected abstract void Write (IMessage message);

        /// <summary>
        /// Close the write side of the connection.
        /// </summary>
        protected abstract void HalfClose ();

        /// <summary>
        /// Discard bytes received while the connection drains. Returns the number of bytes
        /// consumed, and sets <paramref name="closed"/> if they complete the close, such as a
        /// WebSockets close frame.
        /// </summary>
        protected virtual int Discard (byte[] data, int length, out bool closed)
        {
            closed = false;
            return length;
        }

        /// <summary>
        /// Whether the transport has closed the write side while reading, such as with a
        /// WebSockets close frame. The connection then drains before it closes.
        /// </summary>
        protected virtual bool WriteClosed {
            get { return false; }
        }

        /// <summary>
        /// Whether part of a message has been consumed from the buffer, such as a fragment.
        /// </summary>
        protected virtual bool PartialMessage {
            get { return false; }
        }

        /// <summary>
        /// Seconds a partial message may go without a new byte before the connection ends.
        /// </summary>
        protected virtual double StallTimeout {
            get { return server.StallTimeout; }
        }

        /// <summary>
        /// Whether the handshake must complete within 3 seconds of the connection opening.
        /// </summary>
        protected virtual bool TimesHandshake {
            get { return true; }
        }

        /// <summary>
        /// Whether ending a session closes the connection. On a serial port the connection
        /// stays, and waits for a new connection request.
        /// </summary>
        protected virtual bool EndsByClosing {
            get { return true; }
        }

        /// <summary>
        /// Read the handshake, ask for the connection to be approved, read ahead during a
        /// session, and drain a connection that is closing. Called on every server update.
        /// </summary>
        public void Update ()
        {
            if (state == State.Handshake)
                ReceiveHandshake ();
            if (state == State.Pending || state == State.Session)
                ReadAhead ();
            if (state == State.Pending)
                RequestApproval ();
            if (state == State.Closing)
                Drain ();
        }

        /// <summary>
        /// Whether the connection is draining before it closes.
        /// </summary>
        public bool Draining {
            get { return state == State.Closing; }
        }

        /// <summary>
        /// Close the connection at once if it is still draining.
        /// </summary>
        public void EndDrain ()
        {
            if (state == State.Closing)
                CloseNow ();
        }

        /// <summary>
        /// The byte client disconnected. A disconnect message already in the buffer still
        /// ends the session as sent.
        /// </summary>
        public void Disconnected ()
        {
            if (state == State.Session) {
                var disconnect = BufferedDisconnect ();
                if (disconnect != null)
                    LogDisconnect (disconnect);
                else
                    Logger.WriteLine ("Client " + session.Label () + " disconnected with no disconnect message", Logger.Severity.Warning);
            }
            EndSession ();
            state = State.Closed;
        }

        /// <summary>
        /// Write a disconnect message as the server stops, then drain until the byte server closes
        /// the connection.
        /// </summary>
        public void Stop ()
        {
            if (state == State.Closing || state == State.Closed)
                return;
            EndWith (DisconnectMessage (Reason.ServerStopped, "The server stopped"));
            if (!EndsByClosing)
                state = State.Closed;
        }

        /// <summary>
        /// Whether a request from the session's client is waiting to be read. Reads messages
        /// until one is a request, handling the others: a disconnect or a connection request
        /// ends the session, and a message the client should not send ends the connection.
        /// </summary>
        public bool Poll (Client client)
        {
            if (state != State.Session || client != session)
                return false;
            if (bufferedRequest != null)
                return true;
            while (true) {
                SchemaMessage message;
                bool stalled;
                try {
                    message = Receive (out stalled);
                } catch (MalformedRequestException e) {
                    End (Reason.MalformedMessage, "Failed to parse a message: " + e.Message);
                    return false;
                } catch (ServerException e) {
                    Logger.WriteLine ("Client " + client.Label () + " disconnected: " + e.Message, Logger.Severity.Warning);
                    if (WriteClosed)
                        EndWith (null);
                    else
                        CloseNow ();
                    return false;
                }
                if (message == null) {
                    if (stalled)
                        End (Reason.Timeout, "A message stalled for more than " + StallTimeout + " seconds");
                    return false;
                }
                switch (message.ContentCase) {
                case ContentCase.Request:
                    var request = message.Request;
                    try {
                        bufferedRequest = request.ToMessage ();
                    } catch (System.Exception e) {
                        throw new RequestDecodeException (request.Id, e);
                    }
                    return true;
                case ContentCase.Disconnect:
                    ReceivedDisconnect (message.Disconnect);
                    return false;
                case ContentCase.ConnectionRequest:
                    Logger.WriteLine ("Client " + client.Label () + " reconnected");
                    EndSession ();
                    HandleConnectionMessage (message);
                    return false;
                case ContentCase.None:
                    Logger.WriteLine ("Skipped a message of an unknown kind from client " + client.Label (), Logger.Severity.Debug);
                    continue;
                default:
                    End (Reason.ProtocolError, "Unexpected " + message.ContentCase + " message from the client");
                    return false;
                }
            }
        }

        /// <summary>
        /// Take the request found by Poll.
        /// </summary>
        public Request TakeRequest (Client client)
        {
            if (!Poll (client))
                throw new NoRequestException ();
            var request = bufferedRequest;
            bufferedRequest = null;
            return request;
        }

        /// <summary>
        /// Write a response, if the client's session is still current.
        /// </summary>
        public void Write (Client client, Response response)
        {
            if (state != State.Session || client != session)
                return;
            TryWrite (envelope.Set (SchemaMessage.ResponseFieldNumber, response.ToProtobufMessage ()));
        }

        /// <summary>
        /// Write a stream update, if the client's session is still current.
        /// </summary>
        public void Write (Client client, StreamUpdate update)
        {
            if (state != State.Session || client != session)
                return;
            streamUpdate.Set (update);
            TryWrite (envelope.Set (SchemaMessage.StreamUpdateFieldNumber, streamUpdate));
        }

        /// <summary>
        /// Free the state kept for a stream that has been removed.
        /// </summary>
        public void StreamRemoved (ulong id)
        {
            streamUpdate.Removed (id);
        }

        /// <summary>
        /// End the client's session with a disconnect message.
        /// </summary>
        public void Disconnect (Client client, DisconnectReason reason, string message)
        {
            if (state != State.Session || client != session)
                return;
            End ((Reason)reason, message);
        }

        /// <summary>
        /// Close the connection at once, with no message.
        /// </summary>
        public void Close (Client client)
        {
            if (client != session)
                return;
            CloseNow ();
        }

        void ReceiveHandshake ()
        {
            SchemaMessage message;
            bool stalled;
            try {
                message = Receive (out stalled);
            } catch (MalformedRequestException e) {
                Refuse (Status.MalformedMessage, "Failed to parse the connection request: " + e.Message);
                return;
            } catch (ClientDisconnectedException) {
                // The transport has already written its reply, such as to a failed WebSockets
                // upgrade
                EndWith (null);
                return;
            } catch (ServerException e) {
                Logger.WriteLine ("Connection from " + Label + " failed: " + e.Message, Logger.Severity.Warning);
                CloseNow ();
                return;
            }
            if (message != null)
                HandleConnectionMessage (message);
            else if (TimesHandshake && handshakeTimer.ElapsedSeconds () > HANDSHAKE_TIMEOUT)
                Refuse (Status.Timeout, "No connection request received within " + HANDSHAKE_TIMEOUT + " seconds");
            else if (stalled)
                Refuse (Status.Timeout, "The connection request stalled for more than " + StallTimeout + " seconds");
        }

        void HandleConnectionMessage (SchemaMessage message)
        {
            if (message.ContentCase == ContentCase.Disconnect) {
                Logger.WriteLine ("Connection from " + Label + " ended before its handshake", Logger.Severity.Debug);
                if (EndsByClosing)
                    CloseNow ();
                return;
            }
            if (message.ContentCase != ContentCase.ConnectionRequest) {
                Refuse (Status.MalformedMessage, "Expected a connection request");
                return;
            }
            var request = message.ConnectionRequest;
            if (!ProtocolVersion.IsSupported (request.ProtocolVersion)) {
                Refuse (Status.WrongProtocolVersion, ProtocolVersion.UnsupportedMessage (request.ProtocolVersion));
                return;
            }
            session = new Client (request.ClientName, request.ProtocolVersion, this);
            state = State.Pending;
            if (Logger.ShouldLog (Logger.Severity.Debug))
                Logger.WriteLine (
                    "Connection from " + Client.Label () + " requested a session, as client " +
                    session.Label () + " ('" + session.Name + "')", Logger.Severity.Debug);
        }

        /// <summary>
        /// Read the bytes that have arrived, and keep them for the session. The byte client
        /// reports a closed peer only once the bytes before the close have been read, which
        /// the core does not poll for while a call is pending or yielding.
        /// </summary>
        void ReadAhead ()
        {
            try {
                ReadAvailable ();
            } catch (MalformedRequestException e) {
                if (state == State.Session)
                    End (Reason.MalformedMessage, "Failed to parse a message: " + e.Message);
                else
                    Refuse (Status.MalformedMessage, "Failed to parse a message: " + e.Message);
            } catch (ServerException e) {
                Logger.WriteLine ("Connection from " + Label + " failed: " + e.Message, Logger.Severity.Warning);
                CloseNow ();
            }
        }

        void RequestApproval ()
        {
            var args = new ClientRequestingConnectionEventArgs<Request,Response> (session);
            server.RequestingConnection (args);
            if (args.Request.ShouldAllow) {
                var response = new ConnectionResponse ();
                response.Status = Status.Ok;
                response.ProtocolVersion = ProtocolVersion.Current;
                response.ClientIdentifier = ByteString.CopyFrom (session.Guid.ToByteArray ());
                if (!TryWrite (envelope.Set (SchemaMessage.ConnectionResponseFieldNumber, response)))
                    return;
                state = State.Session;
                sessionStarted = true;
                Logger.WriteLine ("Client " + session.Label () + " connected ('" + session.Name + "')");
                server.SessionStarted (session);
            } else if (args.Request.ShouldDeny) {
                Refuse (Status.Denied, "The connection was denied");
            }
        }

        void Refuse (Status status, string message)
        {
            Logger.WriteLine ("Refused a connection from " + Label + ": " + message, Logger.Severity.Warning);
            var response = new ConnectionResponse ();
            response.Status = status;
            response.Message = message;
            if (!EndsByClosing && status == Status.Timeout)
                bufferSize = 0;
            EndWith (envelope.Set (SchemaMessage.ConnectionResponseFieldNumber, response));
        }

        void End (Reason reason, string message)
        {
            var severity = reason == Reason.ServerStopped || reason == Reason.Removed ? Logger.Severity.Info : Logger.Severity.Warning;
            Logger.WriteLine ("Ending the session of client " + session.Label () + ": " + message, severity);
            // A stalled partial message on a serial line is dropped, so that the line reads
            // what follows as a new message
            if (!EndsByClosing && reason == Reason.Timeout)
                bufferSize = 0;
            EndWith (DisconnectMessage (reason, message));
        }

        IMessage DisconnectMessage (Reason reason, string message)
        {
            var disconnect = new Disconnect ();
            disconnect.Reason = reason;
            disconnect.Message = message;
            return envelope.Set (SchemaMessage.DisconnectFieldNumber, disconnect);
        }

        /// <summary>
        /// End the session, write the last message, then close the write side and drain the
        /// connection, so that the message is not lost to a reset.
        /// </summary>
        void EndWith (IMessage lastMessage)
        {
            EndSession ();
            if (lastMessage != null && !TryWrite (lastMessage))
                return;
            if (!EndsByClosing) {
                state = State.Handshake;
                return;
            }
            TryHalfClose ();
            state = State.Closing;
            closingTimer.Reset ();
            closingTimer.Start ();
        }

        void ReceivedDisconnect (Disconnect disconnect)
        {
            LogDisconnect (disconnect);
            EndWith (null);
        }

        void LogDisconnect (Disconnect disconnect)
        {
            var reason = disconnect.Reason;
            var text = "Client " + session.Label () + " disconnected (" + reason + ")";
            if (!string.IsNullOrEmpty (disconnect.Message))
                text += ": " + disconnect.Message;
            Logger.WriteLine (text, reason == Reason.Closed ? Logger.Severity.Info : Logger.Severity.Warning);
        }

        /// <summary>
        /// The first disconnect message among the complete messages left in the buffer, or
        /// null if there is none. The messages before it are dropped.
        /// </summary>
        Disconnect BufferedDisconnect ()
        {
            try {
                SchemaMessage message;
                while ((message = Decode ()) != null) {
                    if (message.ContentCase == ContentCase.Disconnect)
                        return message.Disconnect;
                }
            } catch (ServerException) {
            }
            return null;
        }

        void EndSession ()
        {
            if (session != null && sessionStarted)
                server.SessionEnded (session);
            session = null;
            sessionStarted = false;
            bufferedRequest = null;
            streamUpdate.Clear ();
        }

        void CloseNow ()
        {
            EndSession ();
            Client.Close ();
            state = State.Closed;
        }

        void Drain ()
        {
            bool closed;
            try {
                while (true) {
                    Consume (Discard (buffer, bufferSize, out closed));
                    if (closed || !Stream.DataAvailable || closingTimer.ElapsedSeconds () > CLOSING_TIMEOUT)
                        break;
                    var read = Stream.Read (buffer, bufferSize, buffer.Length - bufferSize);
                    if (read == 0)
                        break;
                    bufferSize += read;
                }
            } catch (ServerException) {
                CloseNow ();
                return;
            }
            if (closed || !Client.Connected || closingTimer.ElapsedSeconds () > CLOSING_TIMEOUT)
                CloseNow ();
        }

        bool TryWrite (IMessage message)
        {
            try {
                Write (message);
                return true;
            } catch (ServerException e) {
                Logger.WriteLine ("Failed to write to " + Label + ": " + e.Message, Logger.Severity.Warning);
                CloseNow ();
                return false;
            }
        }

        void TryHalfClose ()
        {
            try {
                HalfClose ();
            } catch (ServerException) {
            }
        }

        /// <summary>
        /// Read the bytes that have arrived, and decode the next message from them. Returns null
        /// if no complete message has arrived, and sets <paramref name="stalled"/> if a partial
        /// message has received no new byte within the stall timeout. The clock is checked only
        /// after a read finds no new bytes, so a game freeze never ends a message whose bytes
        /// are already waiting.
        /// </summary>
        SchemaMessage Receive (out bool stalled)
        {
            stalled = false;
            var received = ReadAvailable ();
            var message = Decode ();
            if (message != null)
                return message;
            if (!received && (bufferSize > 0 || PartialMessage) && stallTimer.ElapsedSeconds () > StallTimeout) {
                stalled = true;
                stallTimer.Reset ();
            }
            return null;
        }

        /// <summary>
        /// Read the bytes that have arrived into the buffer, restarting the stall clock if there
        /// were any. Returns whether any arrived.
        /// </summary>
        bool ReadAvailable ()
        {
            var received = false;
            while (Stream.DataAvailable) {
                if (bufferSize == buffer.Length) {
                    if (buffer.Length > int.MaxValue - BUFFER_INCREASE_SIZE) {
                        bufferSize = 0;
                        throw new MalformedRequestException ("The message is too large");
                    }
                    var newBuffer = new byte [buffer.Length + BUFFER_INCREASE_SIZE];
                    Array.Copy (buffer, newBuffer, bufferSize);
                    buffer = newBuffer;
                }
                var read = Stream.Read (buffer, bufferSize, buffer.Length - bufferSize);
                if (read == 0)
                    break;
                bufferSize += read;
                received = true;
            }
            if (received) {
                stallTimer.Reset ();
                stallTimer.Start ();
            }
            return received;
        }

        /// <summary>
        /// Decode the next complete message in the buffer, or return null if there is none.
        /// </summary>
        SchemaMessage Decode ()
        {
            while (bufferSize > 0) {
                SchemaMessage message = null;
                int read;
                try {
                    read = Read (ref message, buffer, 0, bufferSize);
                } catch (MalformedRequestException e) {
                    // Malformed bytes are always dropped, so that a serial line reads on
                    Consume (e.BytesRead > 0 ? e.BytesRead : bufferSize);
                    throw;
                }
                if (read > bufferSize)
                    throw new InvalidOperationException ("Read too many bytes");
                Consume (read);
                if (message != null)
                    return message;
                if (read == 0)
                    break;
            }
            return null;
        }

        void Consume (int read)
        {
            if (read >= bufferSize)
                bufferSize = 0;
            else if (read > 0) {
                Array.Copy (buffer, read, buffer, 0, bufferSize - read);
                bufferSize -= read;
            }
        }
    }
}

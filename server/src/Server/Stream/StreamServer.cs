using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.Stream
{
    sealed class StreamServer : IServer<byte,StreamMessage>
    {
        const double defaultTimeout = 0.1;
        byte[] expectedHeader = { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x2D, 0x53, 0x54, 0x52, 0x45, 0x41, 0x4D };
        byte[] okMessage = { 0x4F, 0x4B };
        const int identifierLength = 16;

        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler<ClientRequestingConnectionArgs<byte,StreamMessage>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedArgs<byte,StreamMessage>> OnClientConnected;
        /// <summary>
        /// Does not trigger this event, unless the underlying server does.
        /// </summary>
        public event EventHandler<ClientActivityArgs<byte,StreamMessage>> OnClientActivity;
        public event EventHandler<ClientDisconnectedArgs<byte,StreamMessage>> OnClientDisconnected;

        IServer<byte,byte> server;
        Dictionary<IClient<byte,byte>,StreamClient> clients = new Dictionary<IClient<byte,byte>, StreamClient> ();
        Dictionary<IClient<byte,byte>,StreamClient> pendingClients = new Dictionary<IClient<byte,byte>, StreamClient> ();

        public StreamServer (IServer<byte,byte> server)
        {
            this.server = server;
            server.OnStarted += (s, e) => {
                if (OnStarted != null)
                    OnStarted (this, EventArgs.Empty);
            };
            server.OnStopped += (s, e) => {
                if (OnStopped != null)
                    OnStopped (this, EventArgs.Empty);
            };
            server.OnClientRequestingConnection += HandleClientRequestingConnection;
            server.OnClientConnected += HandleClientConnected;
            server.OnClientConnected += HandleClientActivity;
            server.OnClientDisconnected += HandleClientDisconnected;
        }

        public IServer<byte,byte> Server {
            get { return server; }
        }

        public void Start ()
        {
            server.Start ();
        }

        public void Stop ()
        {
            server.Stop ();
        }

        public void Update ()
        {
            server.Update ();
        }

        public bool Running {
            get { return server.Running; }
        }

        public IEnumerable<IClient<byte,StreamMessage>> Clients {
            get {
                foreach (var client in clients.Values) {
                    yield return client;
                }
            }
        }

        public ulong BytesRead {
            get { return server.BytesRead; }
        }

        public ulong BytesWritten {
            get { return server.BytesWritten; }
        }

        public void ClearStats ()
        {
            server.ClearStats ();
        }

        void HandleClientConnected (object sender, IClientEventArgs<byte,byte> args)
        {
            // Note: pendingClients and clients dictionaries are updated from HandleClientRequestingConnection
            if (OnClientConnected != null) {
                var client = clients [args.Client];
                OnClientConnected (this, new ClientConnectedArgs<byte,StreamMessage> (client));
            }
        }

        void HandleClientActivity (object sender, IClientEventArgs<byte,byte> args)
        {
            if (OnClientActivity != null) {
                var client = clients [args.Client];
                OnClientActivity (this, new ClientActivityArgs<byte,StreamMessage> (client));
            }
        }

        void HandleClientDisconnected (object sender, IClientEventArgs<byte,byte> args)
        {
            var client = clients [args.Client];
            clients.Remove (args.Client);
            if (OnClientDisconnected != null) {
                OnClientDisconnected (this, new ClientDisconnectedArgs<byte,StreamMessage> (client));
            }
        }

        /// <summary>
        /// When a client requests a connection, check the hello message,
        /// then trigger RPCServer.OnClientRequestingConnection to get response of delegates
        /// </summary>
        public void HandleClientRequestingConnection (object sender, ClientRequestingConnectionArgs<byte,byte> args)
        {
            if (!pendingClients.ContainsKey (args.Client)) {
                // A new client connection attempt. Verify the hello message.
                var guid = CheckHelloMessage (args.Client);
                if (guid != Guid.Empty) {
                    // Hello message OK, add it to the pending clients
                    var client = new StreamClient (args.Client, guid);
                    pendingClients [args.Client] = client;
                } else {
                    // Deny the connection, don't add it to pending clients
                    args.Request.Deny ();
                    return;
                }
            }

            // Client is in pending clients and passed hello message verification.
            // Invoke connection request events.
            if (OnClientRequestingConnection != null) {
                var client = pendingClients [args.Client];
                var subArgs = new ClientRequestingConnectionArgs<byte,StreamMessage> (client);
                OnClientRequestingConnection (this, subArgs);
                if (subArgs.Request.ShouldAllow) {
                    args.Request.Allow ();
                    clients [args.Client] = client;
                    Logger.WriteLine ("StreamServer: Client connection allowed.");
                    args.Client.Stream.Write (okMessage);
                }
                if (subArgs.Request.ShouldDeny) {
                    args.Request.Deny ();
                    Logger.WriteLine ("StreamServer: Client connection denied.", Logger.Severity.Warning);
                }
                if (!subArgs.Request.StillPending) {
                    pendingClients.Remove (args.Client);
                }
            } else {
                args.Request.Allow ();
                clients [args.Client] = pendingClients [args.Client];
                Logger.WriteLine ("StreamServer: Client connection allowed.");
                args.Client.Stream.Write (okMessage);
                pendingClients.Remove (args.Client);
            }
        }

        /// <summary>
        /// Read hello message from client, and client identifier and check they are correct.
        /// This is triggered whenever a client connects to the server.
        /// Returns the guid of the client, or Guid.Empty if the hello message and client identifier are valid.
        /// </summary>
        Guid CheckHelloMessage (IClient<byte,byte> client)
        {
            Logger.WriteLine ("StreamServer: Waiting for hello message from client...", Logger.Severity.Debug);
            var buffer = new byte[expectedHeader.Length + identifierLength];
            int read = ReadHelloMessage (client.Stream, buffer);

            // Failed to read enough bytes in sufficient time, so kill the connection
            if (read != buffer.Length) {
                Logger.WriteLine ("StreamServer: Client connection abandoned. Timed out waiting for hello message.", Logger.Severity.Warning);
                return Guid.Empty;
            }

            // Extract bytes for header and identifier
            var header = new byte[expectedHeader.Length];
            var identifier = new byte[identifierLength];
            Array.Copy (buffer, header, header.Length);
            Array.Copy (buffer, header.Length, identifier, 0, identifier.Length);

            // Validate header
            if (!CheckHelloMessageHeader (header)) {
                string hex = ("0x" + BitConverter.ToString (header)).Replace ("-", " 0x");
                Logger.WriteLine ("StreamServer: Client connection abandoned. Invalid hello message received (" + hex + ")", Logger.Severity.Warning);
                return Guid.Empty;
            }

            // Validate and decode the identifier
            Guid identifierGuid;
            try {
                identifierGuid = DecodeClientIdentifier (identifier);
            } catch (ArgumentException) {
                string hex = ("0x" + BitConverter.ToString (identifier)).Replace ("-", " 0x");
                Logger.WriteLine ("StreamServer: Client connection abandoned. Failed to decode client identifier (" + hex + ")", Logger.Severity.Warning);
                return Guid.Empty;
            }

            // Valid header and identifier received
            Logger.WriteLine ("StreamServer: Correct hello message received from client '" + identifierGuid + "'", Logger.Severity.Debug);
            return identifierGuid;
        }

        /// <summary>
        /// Read a fixed length hello message and identifier from the client
        /// </summary>
        int ReadHelloMessage (IStream<byte,byte> stream, byte[] buffer)
        {
            // FIXME: Add better support for delayed receipt of hello message
            int offset = 0;
            for (int i = 0; i < 5; i++) {
                if (stream.DataAvailable) {
                    offset += stream.Read (buffer, offset);
                    if (offset == expectedHeader.Length + identifierLength)
                        break;
                }
                System.Threading.Thread.Sleep (50);
            }
            return offset;
        }

        bool CheckHelloMessageHeader (IEnumerable<byte> receivedHeader)
        {
            return receivedHeader.SequenceEqual (expectedHeader);
        }

        /// <summary>
        /// Validate a fixed-length 32-bit identifier, and return it as an int.
        /// </summary>
        /// <returns>The clients GUID.</returns>
        /// <param name="receivedIdentifier">Received identifier.</param>
        static Guid DecodeClientIdentifier (byte[] receivedIdentifier)
        {
            return new Guid (receivedIdentifier);
        }
    }
}

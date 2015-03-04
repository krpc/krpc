using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KRPC.Schema.KRPC;
using KRPC.Utils;

namespace KRPC.Server.Stream
{
    sealed class StreamServer : IServer<byte,byte>
    {
        const double defaultTimeout = 0.1;
        byte[] expectedHeader = { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0xBA, 0xDA, 0x55 };
        byte[] okMessage = { 0x4F, 0x4B };
        const int identifierLength = 16;

        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler<ClientRequestingConnectionArgs<byte,byte>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedArgs<byte,byte>> OnClientConnected;
        /// <summary>
        /// Does not trigger this event, unless the underlying server does.
        /// </summary>
        public event EventHandler<ClientActivityArgs<byte,byte>> OnClientActivity;
        public event EventHandler<ClientDisconnectedArgs<byte,byte>> OnClientDisconnected;

        IServer<byte,byte> server;
        Dictionary<IClient<byte,byte>,StreamClient> clients = new Dictionary<IClient<byte, byte>, StreamClient> ();
        Dictionary<IClient<byte,byte>,StreamClient> pendingClients = new Dictionary<IClient<byte, byte>, StreamClient> ();

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

        public IEnumerable<IClient<byte,byte>> Clients {
            get {
                foreach (var client in clients.Values) {
                    yield return client;
                }
            }
        }

        void HandleClientConnected (object sender, IClientEventArgs<byte,byte> args)
        {
            // Note: pendingClients and clients dictionaries are updated from HandleClientRequestingConnection
            if (OnClientConnected != null) {
                var client = clients [args.Client];
                OnClientConnected (this, new ClientConnectedArgs<byte,byte> (client));
            }
        }

        void HandleClientActivity (object sender, IClientEventArgs<byte,byte> args)
        {
            if (OnClientActivity != null) {
                var client = clients [args.Client];
                OnClientActivity (this, new ClientActivityArgs<byte,byte> (client));
            }
        }

        void HandleClientDisconnected (object sender, IClientEventArgs<byte,byte> args)
        {
            var client = clients [args.Client];
            clients.Remove (args.Client);
            if (OnClientDisconnected != null) {
                OnClientDisconnected (this, new ClientDisconnectedArgs<byte,byte> (client));
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
                if (CheckHelloMessage (args.Client)) {
                    // Hello message OK, add it to the pending clients
                    var client = new StreamClient (args.Client);
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
                var subArgs = new ClientRequestingConnectionArgs<byte,byte> (client);
                OnClientRequestingConnection (this, subArgs);
                if (subArgs.Request.ShouldAllow) {
                    args.Request.Allow ();
                    clients [args.Client] = client;
                    Logger.WriteLine ("StreamServer: Client connection allowed.");
                    args.Client.Stream.Write(okMessage);
                }
                if (subArgs.Request.ShouldDeny) {
                    args.Request.Deny ();
                    Logger.WriteLine ("StreamServer: Client connection denied.");
                }
                if (!subArgs.Request.StillPending) {
                    pendingClients.Remove (args.Client);
                }
            } else {
                args.Request.Allow ();
                clients [args.Client] = pendingClients [args.Client];
                Logger.WriteLine ("StreamServer: Client connection allowed.");
                args.Client.Stream.Write(okMessage);
                pendingClients.Remove (args.Client);
            }
        }

        /// <summary>
        /// Read hello message from client, and client identifier and check they are correct.
        /// This is triggered whenever a client connects to the server.
        /// Returns true if the hello message and client identifier are valid.
        /// </summary>
        bool CheckHelloMessage (IClient<byte,byte> client)
        {
            Logger.WriteLine ("StreamServer: Waiting for hello message from client...");
            var buffer = new byte[expectedHeader.Length + identifierLength];
            int read = ReadHelloMessage (client.Stream, buffer);

            // Failed to read enough bytes in sufficient time, so kill the connection
            if (read != buffer.Length) {
                Logger.WriteLine ("StreamServer: Client connection abandoned. Timed out waiting for hello message.");
                return false;
            }

            // Extract bytes for header and identifier
            var header = new byte[expectedHeader.Length];
            var identifier = new byte[identifierLength];
            Array.Copy (buffer, header, header.Length);
            Array.Copy (buffer, header.Length, identifier, 0, identifier.Length);

            // Validate header
            if (!CheckHelloMessageHeader (header)) {
                string hex = ("0x" + BitConverter.ToString (header)).Replace ("-", " 0x");
                Logger.WriteLine ("StreamServer: Client connection abandoned. Invalid hello message received (" + hex + ")");
                return false;
            }

            // Validate and decode the identifier
            Guid identifierGuid;
            try {
                identifierGuid = CheckAndDecodeClientIdentifier (identifier);
                //TODO: add validation
            } catch (ArgumentException) {
                string hex = ("0x" + BitConverter.ToString (identifier)).Replace ("-", " 0x");
                Logger.WriteLine ("StreamServer: Client connection abandoned. Failed to decode client identifier (" + hex + ")");
                return false;
            }

            // TODO: check the client identifier

            // Valid header and identifier received
            Logger.WriteLine ("StreamServer: Correct hello message received from client '" + identifierGuid.ToString() + "'");
            return true;
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

        bool CheckHelloMessageHeader (byte[] receivedHeader)
        {
            return receivedHeader.SequenceEqual (expectedHeader);
        }

        /// <summary>
        /// Validate a fixed-length 32-bit identifier, and return it as an int.
        /// </summary>
        /// <returns>The clients GUID.</returns>
        /// <param name="receivedIdentifier">Received identifier.</param>
        Guid CheckAndDecodeClientIdentifier (byte[] receivedIdentifier)
        {
            return new Guid(receivedIdentifier);
        }
    }
}

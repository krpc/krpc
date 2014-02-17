using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using Google.ProtocolBuffers;
using KRPC.Schema.KRPC;
using KRPC.Utils;

namespace KRPC.Server.RPC
{
    sealed class RPCServer : IServer<Request,Response>
    {
        private const double defaultTimeout = 0.1;

        private byte[] expectedHeader = { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0xBA, 0xDA, 0x55 };
        private const int identifierLength = 32;

        public event EventHandler<ClientRequestingConnectionArgs<Request,Response>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedArgs<Request,Response>> OnClientConnected;
        public event EventHandler<ClientDisconnectedArgs<Request,Response>> OnClientDisconnected;

        private IServer<byte,byte> server;
        private Dictionary<IClient<byte,byte>,RPCClient> clients = new Dictionary<IClient<byte, byte>, RPCClient> ();
        private Dictionary<IClient<byte,byte>,RPCClient> pendingClients = new Dictionary<IClient<byte, byte>, RPCClient> ();

        public RPCServer (IServer<byte,byte> server)
        {
            this.server = server;
            server.OnClientRequestingConnection += HandleClientRequestingConnection;
            server.OnClientConnected += HandleClientConnected;
            server.OnClientDisconnected += HandleClientDisconnected;
        }

        public IServer<byte,byte> Server {
            get { return server; }
        }

        public void Start()
        {
            server.Start();
        }

        public void Stop()
        {
            server.Stop();
        }

        public void Update()
        {
            server.Update ();
        }

        public bool Running {
            get { return server.Running; }
        }

        public IEnumerable<IClient<Request,Response>> Clients {
            get {
                foreach (var client in clients.Values) {
                    yield return client;
                }
            }
        }

        private void HandleClientConnected(object sender, IClientEventArgs<byte,byte> args) {
            // Note: pendingClients and clients dictionaries are updated from HandleClientRequestingConnection
            if (OnClientConnected != null) {
                var client = clients [args.Client];
                OnClientConnected(this, new ClientConnectedArgs<Request,Response> (client));
            }
        }

        private void HandleClientDisconnected(object sender, IClientEventArgs<byte,byte> args) {
            var client = clients [args.Client];
            clients.Remove (args.Client);
            if (OnClientDisconnected != null) {
                OnClientDisconnected(this, new ClientDisconnectedArgs<Request,Response> (client));
            }
        }

        /// <summary>
        /// When a client requests a connection, check and parse the hello message,
        /// then trigger RPCServer.OnClientRequestingConnection to get response of delegates
        /// </summary>
        public void HandleClientRequestingConnection(object sender, ClientRequestingConnectionArgs<byte,byte> args) {
            if (!pendingClients.ContainsKey (args.Client)) {
                // A new client connection attempt. Verify the hello message.
                string name = CheckHelloMessage (args.Client);
                if (name != null) {
                    // Hello message OK, add it to the pending clients
                    var client = new RPCClient (name, args.Client);
                    pendingClients [args.Client] = client;
                } else {
                    // Deny the connection, don't add it to pending clients
                    args.Deny ();
                    return;
                }
            }

            // Client is in pending clients and passed hello message verification.
            // Invoke connection request events.
            if (OnClientRequestingConnection != null) {
                var client = pendingClients [args.Client];
                var attempt = new ClientRequestingConnectionArgs<Request,Response> (client);
                OnClientRequestingConnection (this, attempt);
                if (attempt.ShouldAllow) {
                    args.Allow ();
                    clients [args.Client] = client;
                }
                if (attempt.ShouldDeny) {
                    args.Deny ();
                }
                if (!attempt.StillPending) {
                    pendingClients.Remove (args.Client);
                }
            }
        }

        /// <summary>
        /// Read hello message and string identifier from client and check that they are correct.
        /// This is triggered whenever a client connects to the server. Returns the string identifier,
        /// or null if the message is not valid.
        /// </summary>
        public string CheckHelloMessage(IClient<byte,byte> client) {
            Logger.WriteLine("RPCServer: Waiting for hello message from client...");
            byte[] buffer = new byte[expectedHeader.Length + identifierLength];
            int read = ReadHelloMessage (client.Stream, buffer);

            // Failed to read enough bytes in sufficient time, so kill the connection
            if (read != buffer.Length) {
                Logger.WriteLine("RPCServer: Client connection abandoned. Timed out waiting for hello message.");
                return null;
            }

            // Extract bytes for header and identifier
            byte[] header = new byte[expectedHeader.Length];
            byte[] identifier = new byte[identifierLength];
            Array.Copy (buffer, header, header.Length);
            Array.Copy (buffer, header.Length, identifier, 0, identifier.Length);

            // Validate header
            if (!CheckHelloMessageHeader (header)) {
                string hex = ("0x" + BitConverter.ToString (header)).Replace ("-", " 0x");
                Logger.WriteLine ("RPCServer: Client connection abandoned. Invalid hello message received (" + hex + ")");
                return null;
            }

            // Validate and decode the identifier
            string identifierString = CheckAndDecodeHelloMessageIdentifier (identifier);
            if (identifierString == null) {
                string hex = ("0x" + BitConverter.ToString (identifier)).Replace ("-", " 0x");
                Logger.WriteLine ("RPCServer: Client connection abandoned. Failed to decode UTF-8 client identifier (" + hex + ")");
                return null;
            }

            // Valid header and identifier received
            Logger.WriteLine("RPCServer: Correct hello message received from client '" + identifierString + "'");
            return identifierString;
        }

        /// <summary>
        /// Read a fixed length 40-byte message from the client with the given timeout
        /// </summary>
        private int ReadHelloMessage(IStream<byte,byte> stream, byte[] buffer) {
            // FIXME: Add better support for delayed receipt of hello message
            int offset = 0;
            for (int i = 0; i < 5; i++) {
                if (stream.DataAvailable) {
                    offset += stream.Read (buffer, offset);
                    if (offset == expectedHeader.Length + identifierLength)
                        break;
                }
                System.Threading.Thread.Sleep(50);
            }
            return offset;
        }

        private bool CheckHelloMessageHeader(byte[] receivedHeader) {
            return receivedHeader.SequenceEqual (expectedHeader);
        }

        /// <summary>
        /// Validate a fixed-length 32-byte array as a UTF8 string, and return it as a string object.
        /// Return null if it's not valid.
        /// </summary>
        /// <returns>The and decode hello message identifier.</returns>
        /// <param name="receivedIdentifier">Received identifier.</param>
        private string CheckAndDecodeHelloMessageIdentifier(byte[] receivedIdentifier) {
            string identifierString = "";

            // Strip null bytes from the end
            int length = 0;
            bool foundEnd = false;
            foreach (byte x in receivedIdentifier) {
                if (!foundEnd) {
                    if (x == 0x00)
                        foundEnd = true;
                    else
                        length++;
                } else {
                    if (x != 0x00) {
                        // Found non-null bytes after end of string
                        return null;
                    }
                }
            }
                
            if (length > 0) {
                // Got valid sequence of non-zero bytes, try to decode them
                byte[] strippedIdentifier = new byte[length];
                Array.Copy (receivedIdentifier, strippedIdentifier, length);
                var encoder = new UTF8Encoding (encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                try {
                    identifierString = encoder.GetString (strippedIdentifier);
                } catch (ArgumentException) {
                    return null;
                }
            }
            return identifierString;
        }
    }
}

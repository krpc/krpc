using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class RPCServer : IServer<Request,Response>
    {
        const double defaultTimeout = 0.1;
        byte[] expectedHeader = { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x2D, 0x52, 0x50, 0x43, 0x00, 0x00, 0x00 };
        const int clientNameLength = 32;

        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler<ClientRequestingConnectionArgs<Request,Response>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedArgs<Request,Response>> OnClientConnected;
        /// <summary>
        /// Does not trigger this event, unless the underlying server does.
        /// </summary>
        public event EventHandler<ClientActivityArgs<Request,Response>> OnClientActivity;
        public event EventHandler<ClientDisconnectedArgs<Request,Response>> OnClientDisconnected;

        IServer<byte,byte> server;
        Dictionary<IClient<byte,byte>,RPCClient> clients = new Dictionary<IClient<byte, byte>, RPCClient> ();
        Dictionary<IClient<byte,byte>,RPCClient> pendingClients = new Dictionary<IClient<byte, byte>, RPCClient> ();
        ulong closedClientsBytesRead;
        ulong closedClientsBytesWritten;

        public RPCServer (IServer<byte,byte> server)
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

        public IEnumerable<IClient<Request,Response>> Clients {
            get {
                foreach (var client in clients.Values) {
                    yield return client;
                }
            }
        }

        public ulong BytesRead {
            get { return closedClientsBytesRead + clients.Values.Select (c => c.Stream.BytesRead).SumUnsignedLong (); }
        }

        public ulong BytesWritten {
            get { return closedClientsBytesWritten + clients.Values.Select (c => c.Stream.BytesWritten).SumUnsignedLong (); }
        }

        public void ClearStats ()
        {
            closedClientsBytesRead = 0;
            closedClientsBytesWritten = 0;
            foreach (var client in clients.Values)
                client.Stream.ClearStats ();
        }

        void HandleClientConnected (object sender, IClientEventArgs<byte,byte> args)
        {
            // Note: pendingClients and clients dictionaries are updated from HandleClientRequestingConnection
            if (OnClientConnected != null) {
                var client = clients [args.Client];
                OnClientConnected (this, new ClientConnectedArgs<Request,Response> (client));
            }
        }

        void HandleClientActivity (object sender, IClientEventArgs<byte,byte> args)
        {
            if (OnClientActivity != null) {
                var client = clients [args.Client];
                OnClientActivity (this, new ClientActivityArgs<Request,Response> (client));
            }
        }

        void HandleClientDisconnected (object sender, IClientEventArgs<byte,byte> args)
        {
            var client = clients [args.Client];
            closedClientsBytesRead += client.Stream.BytesRead;
            closedClientsBytesWritten += client.Stream.BytesWritten;
            clients.Remove (args.Client);
            if (OnClientDisconnected != null) {
                OnClientDisconnected (this, new ClientDisconnectedArgs<Request,Response> (client));
            }
        }

        /// <summary>
        /// When a client requests a connection, check and parse the hello message (which should
        /// consist of a header and a client name), then trigger RPCServer.OnClientRequestingConnection
        /// to get response of delegates
        /// </summary>
        public void HandleClientRequestingConnection (object sender, ClientRequestingConnectionArgs<byte,byte> args)
        {
            if (!pendingClients.ContainsKey (args.Client)) {
                // A new client connection attempt. Verify the hello message.
                string clientName = CheckHelloMessage (args.Client);
                if (clientName != null) {
                    // Hello message OK, add it to the pending clients
                    var client = new RPCClient (clientName, args.Client);
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
                var subArgs = new ClientRequestingConnectionArgs<Request,Response> (client);
                OnClientRequestingConnection (this, subArgs);
                if (subArgs.Request.ShouldAllow) {
                    args.Request.Allow ();
                    clients [args.Client] = client;
                    args.Client.Stream.Write (client.Guid.ToByteArray ());
                    Logger.WriteLine ("RPCServer: client connection allowed");
                }
                if (subArgs.Request.ShouldDeny) {
                    args.Request.Deny ();
                    Logger.WriteLine ("RPCServer: client connection denied", Logger.Severity.Warning);
                }
                if (!subArgs.Request.StillPending) {
                    pendingClients.Remove (args.Client);
                }
            } else {
                // No events configured, so allow the connection
                args.Request.Allow ();
                clients [args.Client] = pendingClients [args.Client];
                args.Client.Stream.Write (args.Client.Guid.ToByteArray ());
                Logger.WriteLine ("RPCServer: client connection allowed");
                pendingClients.Remove (args.Client);
            }
        }

        /// <summary>
        /// Read the hello message (header and client name) and check that they are correct.
        /// This is triggered whenever a client connects to the server. Returns the client name as a string,
        /// or null if the hello message is not valid.
        /// </summary>
        string CheckHelloMessage (IClient<byte,byte> client)
        {
            Logger.WriteLine ("RPCServer: waiting for hello message from client...", Logger.Severity.Debug);
            var buffer = new byte[expectedHeader.Length + clientNameLength];
            int read = ReadHelloMessage (client.Stream, buffer);

            // Failed to read enough bytes in sufficient time, so kill the connection
            if (read != buffer.Length) {
                Logger.WriteLine ("RPCServer: client connection abandoned; timed out waiting for hello message", Logger.Severity.Warning);
                return null;
            }

            // Extract bytes for header and name
            var header = new byte[expectedHeader.Length];
            var clientName = new byte[clientNameLength];
            Array.Copy (buffer, header, header.Length);
            Array.Copy (buffer, header.Length, clientName, 0, clientName.Length);

            // Validate header
            if (!CheckHelloMessageHeader (header)) {
                string hex = ("0x" + BitConverter.ToString (header)).Replace ("-", " 0x");
                Logger.WriteLine ("RPCServer: client connection abandoned; invalid hello message received (" + hex + ")", Logger.Severity.Warning);
                return null;
            }

            // Validate and decode the client name
            string clientNameString = CheckAndDecodeClientName (clientName);
            if (clientNameString == null) {
                string hex = ("0x" + BitConverter.ToString (clientName)).Replace ("-", " 0x");
                Logger.WriteLine ("RPCServer: client connection abandoned; failed to decode UTF-8 client name (" + hex + ")", Logger.Severity.Warning);
                return null;
            }

            // Valid header and client name received
            Logger.WriteLine ("RPCServer: correct hello message received from client '" + client.Guid + "' (" + clientNameString + ")", Logger.Severity.Debug);
            return clientNameString;
        }

        /// <summary>
        /// Read a fixed length 40-byte message from the client with the given timeout
        /// </summary>
        int ReadHelloMessage (IStream<byte,byte> stream, byte[] buffer)
        {
            // FIXME: Add better support for delayed receipt of hello message
            int offset = 0;
            for (int i = 0; i < 5; i++) {
                if (stream.DataAvailable) {
                    offset += stream.Read (buffer, offset);
                    if (offset == expectedHeader.Length + clientNameLength)
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
        /// Validate a fixed-length 32-byte array as a UTF8 string, and return it as a string object.
        /// </summary>
        /// <returns>The decoded client name, or null if not valid.</returns>
        static string CheckAndDecodeClientName (byte[] receivedClientName)
        {
            string clientNameString = "";

            // Strip null bytes from the end
            int length = 0;
            bool foundEnd = false;
            foreach (byte x in receivedClientName) {
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
                var strippedClientName = new byte[length];
                Array.Copy (receivedClientName, strippedClientName, length);
                var encoder = new UTF8Encoding (false, true);
                try {
                    clientNameString = encoder.GetString (strippedClientName);
                } catch (ArgumentException) {
                    return null;
                }
            }
            return clientNameString;
        }
    }
}

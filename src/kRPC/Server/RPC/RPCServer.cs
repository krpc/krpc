using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using Google.ProtocolBuffers;
using KRPC.Schema.RPC;
using KRPC.Utils;

namespace KRPC.Server.RPC
{
    sealed class RPCServer : IServer<Request,Response>
    {
        private const double defaultTimeout = 15;

        private byte[] header = { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0xBA, 0xDA, 0x55 };
        private const int headerLength = 8;
        private const int identifierLength = 32;

        public event EventHandler<ClientRequestingConnectionArgs<Request,Response>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedArgs<Request,Response>> OnClientConnected;
        public event EventHandler<ClientDisconnectedArgs<Request,Response>> OnClientDisconnected;

        private IServer<byte,byte> server;
        private double timeout;

        public RPCServer (IServer<byte,byte> server, double timeout = defaultTimeout)
        {
            this.server = server;
            this.timeout = timeout;
            server.OnClientRequestingConnection += CheckHelloMessage;
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
                foreach (var client in server.Clients) {
                    yield return new RPCClient (client);
                }
            }
        }

        /// <summary>
        /// Read hello message and string identifier from client and check that they are correct.
        /// This is triggered whenever a client connects to the server.
        /// </summary>
        public void CheckHelloMessage(object sender, ClientRequestingConnectionArgs<byte,byte> args) {

            Logger.WriteLine("RPCServer: Waiting for hello message from client...");
            byte[] buffer = new byte[headerLength + identifierLength];
            int read = ReadHelloMessage (args.Client.Stream, buffer);

            // Failed to read enough bytes in sufficient time, so kill the connection
            if (read != headerLength + identifierLength) {
                Logger.WriteLine("RPCServer: Client connection abandoned. Timed out waiting for hello message.");
                args.Deny ();
                return;
            }

            // Extract bytes for header and identifier
            byte[] header = new byte[headerLength];
            byte[] identifier = new byte[identifierLength];
            Array.Copy (buffer, header, header.Length);
            Array.Copy (buffer, header.Length, identifier, 0, identifier.Length);

            // Validate header
            if (!CheckHelloMessageHeader (header)) {
                string hex = ("0x" + BitConverter.ToString (header)).Replace ("-", " 0x");
                Logger.WriteLine ("RPCServer: Client connection abandoned. Invalid hello message received (" + hex + ")");
                args.Deny ();
                return;
            }

            // Validate and decode the identifier
            string identifierString = CheckAndDecodeHelloMessageIdentifier (identifier);
            if (identifierString == null) {
                string hex = ("0x" + BitConverter.ToString (identifier)).Replace ("-", " 0x");
                Logger.WriteLine ("RPCServer: Client connection abandoned. Failed to decode UTF-8 client identifier (" + hex + ")");
                args.Deny ();
            }

            // Valid header and identifier received
            Logger.WriteLine("RPCServer: Correct hello message received from client '" + identifierString + "'");
            args.Allow ();
        }

        /// <summary>
        /// Read a fixed length 40-byte message from the client with the given timeout
        /// </summary>
        private int ReadHelloMessage(IStream<byte,byte> stream, byte[] buffer) {
            int offset = 0;
            for (int i = 0; i < (int)(timeout * 1000) / 50; i++) {
                if (stream.DataAvailable) {
                    offset += stream.Read (buffer, offset);
                    if (offset == headerLength + identifierLength)
                        break;
                }
                System.Threading.Thread.Sleep(50);
            }
            return offset;
        }

        private bool CheckHelloMessageHeader(byte[] receivedHeader) {
            return receivedHeader.SequenceEqual (header);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class RPCServer : Message.RPCServer
    {
        byte[] expectedHeader = { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x2D, 0x52, 0x50, 0x43, 0x00, 0x00, 0x00 };
        const int clientNameLength = 32;

        public RPCServer (IServer<byte,byte> server) : base (server)
        {
        }

        /// <summary>
        /// When a client requests a connection, check and parse the hello message
        /// (which should consist of a header and a client name)
        /// </summary>
        protected override IClient<Request,Response> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            string clientName = CheckHelloMessage (args.Client);
            if (clientName != null)
                return new RPCClient (clientName, args.Client);
            else
                args.Request.Deny ();
            return null;
        }

        /// <summary>
        /// When a client requests a connection, and is successful, send the clients guid
        /// </summary>
        public override void HandleClientRequestingConnection (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            base.HandleClientRequestingConnection (sender, args);
            if (args.Request.ShouldAllow)
                args.Client.Stream.Write (args.Client.Guid.ToByteArray ());
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
            var clientNameString = string.Empty;

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

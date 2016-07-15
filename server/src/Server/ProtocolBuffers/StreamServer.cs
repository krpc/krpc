using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class StreamServer : Message.StreamServer
    {
        byte[] expectedHeader = { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x2D, 0x53, 0x54, 0x52, 0x45, 0x41, 0x4D };
        byte[] okMessage = { 0x4F, 0x4B };
        const int identifierLength = 16;

        public StreamServer (IServer<byte,byte> server) : base (server)
        {
        }

        /// <summary>
        /// When a client requests a connection, check the hello message
        /// </summary>
        protected override IClient<NoMessage,StreamMessage> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            var guid = CheckHelloMessage (args.Client);
            if (guid != Guid.Empty)
                return new StreamClient (guid, args.Client);
            else
                args.Request.Deny ();
            return null;

        }

        /// <summary>
        /// When a client requests a connection, and is successful, send the ok message
        /// </summary>
        public override void HandleClientRequestingConnection (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            base.HandleClientRequestingConnection (sender, args);
            if (args.Request.ShouldAllow)
                args.Client.Stream.Write (okMessage);
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

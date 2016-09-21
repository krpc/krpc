using System;
using Google.Protobuf;
using KRPC.Schema.KRPC;
using NUnit.Framework;
using System.IO;

using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;

namespace KRPC.Test.Server.ProtocolBuffers
{
    public static class TestingTools
    {
        public static byte[] CreateRPCConnectionRequest (byte[] clientIdentifier = null)
        {
            return CreateConnectionRequest (new byte[]{ 0x4b, 0x52, 0x50, 0x43, 0x2d, 0x52, 0x50, 0x43 }, clientIdentifier);
        }

        public static byte[] CreateStreamConnectionRequest (byte[] clientIdentifier = null)
        {
            return CreateConnectionRequest (new byte[]{ 0x4b, 0x52, 0x50, 0x43, 0x2d, 0x53, 0x54, 0x52 }, clientIdentifier);
        }

        static byte[] CreateConnectionRequest (byte[] header, byte[] clientIdentifier = null)
        {
            byte[] requestBytes;
            var request = new ConnectionRequest ();
            request.ClientName = "Jebediah Kerman!!!";
            if (clientIdentifier != null)
                request.ClientIdentifier = ByteString.CopyFrom (clientIdentifier);
            using (var buffer = new MemoryStream ()) {
                var stream = new CodedOutputStream (buffer, true);
                stream.WriteMessage (request);
                stream.Flush ();
                requestBytes = buffer.ToArray ();
            }

            var connectionMessage = new byte[header.Length + requestBytes.Length];
            Array.Copy (header, 0, connectionMessage, 0, header.Length);
            Array.Copy (requestBytes, 0, connectionMessage, header.Length, requestBytes.Length);
            return connectionMessage;
        }

        public static void CheckConnectionResponse (byte[] responseBytes, int expectedLength, Status expectedStatus, string expectedMessage, int expectedIdLength)
        {
            Assert.AreEqual (expectedLength, responseBytes.Length);
            var response = new ConnectionResponse ();
            var codedResponseStream = new CodedInputStream (responseBytes);
            codedResponseStream.ReadMessage (response);
            Assert.AreEqual (expectedStatus, response.Status);
            Assert.AreEqual (expectedMessage, response.Message);
            Assert.AreEqual (expectedIdLength, response.ClientIdentifier.Length);
        }
    }
}

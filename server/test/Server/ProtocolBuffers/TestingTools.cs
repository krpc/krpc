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
        public static byte[] CreateConnectionRequest (byte[] clientIdentifier = null)
        {
            var request = new ConnectionRequest ();
            request.ClientName = "Jebediah Kerman!!!";
            if (clientIdentifier != null)
                request.ClientIdentifier = ByteString.CopyFrom (clientIdentifier);
            using (var buffer = new MemoryStream ()) {
                var stream = new CodedOutputStream (buffer, true);
                stream.WriteMessage (request);
                stream.Flush ();
                return buffer.ToArray ();
            }
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

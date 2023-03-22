using System.IO;
using Google.Protobuf;
using KRPC.Schema.KRPC;
using NUnit.Framework;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;
using Type = KRPC.Schema.KRPC.ConnectionRequest.Types.Type;

namespace KRPC.Test.Server.SerialIO
{
    public static class TestingTools
    {
        public static byte[] CreateConnectionRequest (Type type, byte[] clientIdentifier = null)
        {
            var request = new MultiplexedRequest ();
            request.ConnectionRequest = new ConnectionRequest ();
            request.ConnectionRequest.Type = type;
            request.ConnectionRequest.ClientName = "Jebediah Kerman!!!";
            if (clientIdentifier != null)
                request.ConnectionRequest.ClientIdentifier = ByteString.CopyFrom (clientIdentifier);
            using (var buffer = new MemoryStream ()) {
                var stream = new CodedOutputStream (buffer, true);
                stream.WriteMessage (request);
                stream.Flush ();
                return buffer.ToArray ();
            }
        }

        public static void CheckConnectionResponse (byte[] responseBytes, int expectedLength, Status expectedStatus, string expectedMessage, int expectedIdLength)
        {
            KRPC.Test.Server.ProtocolBuffers.TestingTools.CheckConnectionResponse (responseBytes, expectedLength, expectedStatus, expectedMessage, expectedIdLength);
        }
    }
}

using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Google.Protobuf;
using KRPC.Schema.KRPC;
using System.Collections.Generic;

namespace KRPC.Client
{
    public class Connection
    {
        private TcpClient rpcClient;
        private TcpClient streamClient;

        /**
         * Connect to a kRPC server on the specified IP address and port numbers. If
         * streamPort is 0, does not connect to the stream server.
         * Passes an optional name to the server to identify the client (up to 32 bytes of UTF-8 encoded text).
         */
        public Connection (string name = "", IPAddress address = null, int rpcPort = 50000, int streamPort = 50001)
        {
            if (address == null)
                address = IPAddress.Loopback;

            rpcClient = new TcpClient ();
            rpcClient.Connect (address, rpcPort);
            Stream rpcStream = rpcClient.GetStream ();
            rpcStream.Write (Encoder.rpcHelloMessage, 0, Encoder.rpcHelloMessage.Length);
            var clientName = Encoder.EncodeClientName (name);
            rpcStream.Write (clientName, 0, clientName.Length);
            var clientIdentifier = new byte[Encoder.clientIdentifierLength];
            rpcStream.Read (clientIdentifier, 0, Encoder.clientIdentifierLength);

            if (streamPort == 0)
                streamClient = null;
            else {
                streamClient = new TcpClient ();
                streamClient.Connect (address, streamPort);
                var streamStream = streamClient.GetStream ();
                streamStream.Write (Encoder.streamHelloMessage, 0, Encoder.streamHelloMessage.Length);
                streamStream.Write (clientIdentifier, 0, clientIdentifier.Length);
                var recvOkMessage = new byte [Encoder.okMessage.Length];
                streamStream.Read (recvOkMessage, 0, Encoder.okMessage.Length);
                if (recvOkMessage.Equals (Encoder.okMessage))
                    throw new Exception ("Invalid hello message received from stream server. " +
                    "Got " + Encoder.ToHexString (recvOkMessage));
            }
        }

        public ByteString Invoke (string service, string procedure, IList<ByteString> arguments = null)
        {
            var outStream = new CodedOutputStream (rpcClient.GetStream ());
            var request = new Request ();
            request.Service = service;
            request.Procedure = procedure;
            if (arguments != null) {
                uint position = 0;
                foreach (var value in arguments) {
                    var argument = new Argument ();
                    argument.Position = position;
                    argument.Value = value;
                    request.Arguments.Add (argument);
                    position++;
                }
            }
            outStream.WriteLength (request.CalculateSize ());
            request.WriteTo (outStream);
            outStream.Flush ();

            var inStream = new CodedInputStream (rpcClient.GetStream ());
            var response = new Response ();
            inStream.ReadMessage (response);
            if (response.HasError)
                throw new RPCException (response.Error);
            return response.HasReturnValue ? response.ReturnValue : null;
        }
    }
}

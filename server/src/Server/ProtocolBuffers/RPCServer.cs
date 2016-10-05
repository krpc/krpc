using System;
using System.Linq;
using Google.Protobuf;
using KRPC.Service.Messages;
using KRPC.Utils;
using ConnectionRequest = KRPC.Schema.KRPC.ConnectionRequest;
using ConnectionResponse = KRPC.Schema.KRPC.ConnectionResponse;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class RPCServer : Message.RPCServer
    {
        byte[] expectedHeader = { 0x4b, 0x52, 0x50, 0x43, 0x2d, 0x52, 0x50, 0x43 };

        public RPCServer (IServer<byte,byte> server) : base (server)
        {
        }

        /// <summary>
        /// Handle the initiation of a client connection request
        /// </summary>
        protected override IClient<Request,Response> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            var stream = args.Client.Stream;
            var address = args.Client.Address;
            try {
                Logger.WriteLine ("ProtocolBuffers: client requesting connection (" + address + ")", Logger.Severity.Debug);
                var request = ReadConnectionRequest (stream);
                return new RPCClient (request.ClientName, args.Client);
            } catch (InvalidProtocolBufferException e) {
                WriteErrorConnectionResponse (Status.MalformedMessage, e.Message, stream);
            } catch (TimeoutException e) {
                WriteErrorConnectionResponse (Status.Timeout, e.Message, stream);
            } catch (ConnectionException e) {
                WriteErrorConnectionResponse (e.Status, e.Message, stream);
            }
            args.Request.Deny ();
            Logger.WriteLine ("ProtocolBuffers: client connection denied (" + address + ")", Logger.Severity.Error);
            return null;
        }

        /// <summary>
        /// Handle a client connection request
        /// </summary>
        public override void HandleClientRequestingConnection (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            base.HandleClientRequestingConnection (sender, args);
            if (args.Request.ShouldAllow) {
                var client = args.Client;
                var response = new ConnectionResponse ();
                response.Status = Status.Ok;
                response.ClientIdentifier = ByteString.CopyFrom (client.Guid.ToByteArray ());
                Utils.WriteMessage (client.Stream, response);
                Logger.WriteLine ("ProtocolBuffers: client connection accepted (" + args.Client.Address + ")");
            } else if (args.Request.ShouldDeny)
                args.Client.Stream.Close ();
        }

        ConnectionRequest ReadConnectionRequest (IStream<byte, byte> stream)
        {
            var receivedHeader = new byte[expectedHeader.Length];
            Utils.Read (stream, receivedHeader);
            if (!receivedHeader.SequenceEqual (expectedHeader))
                throw new ConnectionException (Status.MalformedHeader, String.Empty);
            return Utils.ReadMessage<ConnectionRequest> (stream);
        }

        static void WriteErrorConnectionResponse (Status status, string message, IStream<byte,byte> stream)
        {
            if (status == Status.Ok)
                throw new ArgumentException ("Error response must have a non-OK status code");
            var response = new ConnectionResponse ();
            response.Status = status;
            response.Message = message;
            Utils.WriteMessage (stream, response);
            Logger.WriteLine ("ProtocolBuffers: client connection denied: " + status + " " + message, Logger.Severity.Error);
        }
    }
}

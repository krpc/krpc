using System;
using Google.Protobuf;
using KRPC.Service.Messages;
using ConnectionRequest = KRPC.Schema.KRPC.ConnectionRequest;
using ConnectionResponse = KRPC.Schema.KRPC.ConnectionResponse;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class StreamServer : Message.StreamServer
    {
        public StreamServer (IServer<byte,byte> server) : base (server)
        {
        }

        /// <summary>
        /// Handle the initiation of a client connection request
        /// </summary>
        protected override IClient<NoMessage,StreamUpdate> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            var stream = args.Client.Stream;
            try {
                var request = Utils.ReadMessage<ConnectionRequest> (stream);
                if (request.ClientIdentifier.Length != 16) {
                    WriteErrorConnectionResponse (Status.MalformedMessage, "Client identifier must be 16 bytes.", stream);
                } else {
                    var guid = new Guid (request.ClientIdentifier.ToByteArray ());
                    return new StreamClient (guid, args.Client);
                }
            } catch (InvalidProtocolBufferException e) {
                WriteErrorConnectionResponse (Status.MalformedMessage, e.Message, stream);
            } catch (TimeoutException e) {
                WriteErrorConnectionResponse (Status.Timeout, e.Message, stream);
            } catch (ConnectionException e) {
                WriteErrorConnectionResponse (e.Status, e.Message, stream);
            }
            args.Request.Deny ();
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
            } else if (args.Request.ShouldDeny)
                args.Client.Stream.Close ();
        }

        static void WriteErrorConnectionResponse (Status status, string message, IStream<byte,byte> stream)
        {
            if (status == Status.Ok)
                throw new ArgumentException ("Error response must have a non-OK status code");
            var response = new ConnectionResponse ();
            response.Status = status;
            response.Message = message;
            Utils.WriteMessage (stream, response);
        }
    }
}

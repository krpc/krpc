using System;
using Google.Protobuf;
using KRPC.Service.Messages;
using ConnectionRequest = KRPC.Schema.KRPC.ConnectionRequest;
using ConnectionResponse = KRPC.Schema.KRPC.ConnectionResponse;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;
using Type = KRPC.Schema.KRPC.ConnectionRequest.Types.Type;

namespace KRPC.Server.ProtocolBuffers
{
    public sealed class StreamServer : Message.StreamServer
    {
        public StreamServer (IServer<byte,byte> server) : base (server)
        {
        }

        /// <summary>
        /// Handle the initiation of a client connection request
        /// </summary>
        protected override IClient<NoMessage,StreamUpdate> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            var client = args.Client;
            var stream = client.Stream;
            try {
                bool timeout;
                var request = Utils.ReadMessage<ConnectionRequest> (client, out timeout);
                if (timeout) {
                    WriteErrorConnectionResponse (Status.Timeout, "Connection request message not received after waiting 3 seconds", stream);
                    args.Request.Deny ();
                    return null;
                }
                if (request == null)
                    return null;
                if (request.Type != Type.Stream) {
                    var name = request.Type.ToString ().ToLower ();
                    WriteErrorConnectionResponse (Status.WrongType,
                        "Connection request was for the " + name + " server, but this is the stream server. " +
                        "Did you connect to the wrong port number?", stream);
                } else if (request.ClientIdentifier.Length != 16) {
                    WriteErrorConnectionResponse (Status.MalformedMessage, "Client identifier must be 16 bytes.", stream);
                } else {
                    var guid = new Guid (request.ClientIdentifier.ToByteArray ());
                    return new StreamClient (guid, args.Client);
                }
            } catch (InvalidProtocolBufferException e) {
                WriteErrorConnectionResponse (Status.MalformedMessage, e.Message, stream);
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

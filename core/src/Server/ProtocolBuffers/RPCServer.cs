using Google.Protobuf;
using KRPC.Service.Messages;
using KRPC.Utils;
using ConnectionRequest = KRPC.Schema.KRPC.ConnectionRequest;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;
using Type = KRPC.Schema.KRPC.ConnectionRequest.Types.Type;

namespace KRPC.Server.ProtocolBuffers
{
    public sealed class RPCServer : Message.RPCServer
    {
        public RPCServer (IServer<byte,byte> server) : base (server)
        {
        }

        /// <summary>
        /// Handle the initiation of a client connection request
        /// </summary>
        protected override IClient<Request,Response> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            var client = args.Client;
            var address = client.Address;
            try {
                Logger.WriteLine ("ProtocolBuffers: client requesting connection (" + address + ")", Logger.Severity.Debug);
                bool timeout;
                var request = Utils.ReadMessage<ConnectionRequest> (client, out timeout);
                if (timeout) {
                    WriteErrorConnectionResponse (client, Status.Timeout, "Connection request message not received after waiting 3 seconds");
                    args.Request.Deny ();
                    Logger.WriteLine ("ProtocolBuffers: client connection timed out (" + address + ")", Logger.Severity.Error);
                    return null;
                }
                if (request == null)
                    return null;
                if (request.Type != Type.Rpc) {
                    var name = request.Type.ToString ().ToLower ();
                    WriteErrorConnectionResponse (client, Status.WrongType,
                        "Connection request was for the " + name + " server, but this is the rpc server. " +
                        "Did you connect to the wrong port number?");
                } else {
                    return new RPCClient (request.ClientName, args.Client);
                }
            } catch (InvalidProtocolBufferException e) {
                WriteErrorConnectionResponse (client, Status.MalformedMessage, e.Message);
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
            var client = args.Client;
            if (args.Request.ShouldAllow) {
                Utils.WriteConnectionResponse (client);
                Logger.WriteLine ("ProtocolBuffers: client connection accepted (" + args.Client.Address + ")");
            } else if (args.Request.ShouldDeny) {
                client.Stream.Close ();
            }
        }

        static void WriteErrorConnectionResponse (IClient<byte,byte> client, Status status, string message)
        {
            Utils.WriteConnectionResponse (client, status, message);
            Logger.WriteLine ("ProtocolBuffers: client connection denied: " + status + " " + message, Logger.Severity.Error);
        }
    }
}

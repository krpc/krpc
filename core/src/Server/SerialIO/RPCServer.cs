using System;
using Google.Protobuf;
using KRPC.Service.Messages;
using KRPC.Utils;
using MultiplexedRequest = KRPC.Schema.KRPC.MultiplexedRequest;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;
using Type = KRPC.Schema.KRPC.ConnectionRequest.Types.Type;

namespace KRPC.Server.SerialIO
{
    /// <summary>
    /// RPC server for receiving requests and sending responses over an underlying message server.
    /// </summary>
    public sealed class RPCServer : Message.RPCServer
    {
        /// <summary>
        /// Construct an RPC server from a byte server
        /// </summary>
        public RPCServer (IServer<byte,byte> innerServer) : base(innerServer)
        {
        }

        /// <summary>
        /// When a client requests a connection, process the connection request
        /// </summary>
        protected override IClient<Request,Response> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args) {
            var client = args.Client;
            var address = client.Address;
            try {
                Logger.WriteLine (
                    "SerialIO: client requesting connection on " + address, Logger.Severity.Debug);
                bool timeout;
                var request = ProtocolBuffers.Utils.ReadMessage<MultiplexedRequest> (client, out timeout);
                if (timeout) {
                    WriteErrorConnectionResponse (client, Status.Timeout, "Connection request message not received after waiting 3 seconds");
                    args.Request.Deny ();
                    Logger.WriteLine ("SerialIO: client connection timed out on " + address, Logger.Severity.Error);
                    return null;
                }
                if (request == null)
                    return null;
                var connectionRequest = request.ConnectionRequest;
                if (connectionRequest == null) {
                    WriteErrorConnectionResponse (
                        client, Status.MalformedMessage, "Expected a ConnectionRequest message");
                }
                if (connectionRequest.Type != Type.Rpc) {
                    var name = connectionRequest.Type.ToString().ToLower ();
                    WriteErrorConnectionResponse(
                        client, Status.WrongType,
                        "Connection request was for a " + name + " server, " +
                        "but this is an rpc server.");
                } else {
                    return new RPCClient(connectionRequest.ClientName, client, Server);
                }
            } catch (InvalidProtocolBufferException e) {
                WriteErrorConnectionResponse(client, Status.MalformedMessage, e.Message);
            } catch (TimeoutException e) {
                WriteErrorConnectionResponse(client, Status.Timeout, e.Message);
            }
            args.Request.Deny();
            Logger.WriteLine(
                "SerialIO: client connection denied on " + address, Logger.Severity.Error);
            return null;
        }

        /// <summary>
        /// Send an upgrade response to the client on successful connection.
        /// </summary>
        public override void HandleClientRequestingConnection(
            object sender, ClientRequestingConnectionEventArgs<byte,byte> args) {
            base.HandleClientRequestingConnection (sender, args);
            var client = args.Client;
            if (args.Request.ShouldAllow) {
                ProtocolBuffers.Utils.WriteConnectionResponse (client);
                Logger.WriteLine("SerialIO: client connection accepted on " + client.Address);
            } else if (args.Request.ShouldDeny) {
                client.Stream.Close();
            }
        }

        static void WriteErrorConnectionResponse (IClient<byte,byte> client, Status status, string message)
        {
            ProtocolBuffers.Utils.WriteConnectionResponse (client, status, message);
            Logger.WriteLine("SerialIO: client connection denied on " + client.Address + ": " +
                             status + " " + message, Logger.Severity.Error);
        }
    }
}

using System;
using KRPC.Service.Messages;

namespace KRPC.Server.SerialIO
{
    /// <summary>
    /// Stream server for receiving requests and sending responses over an underlying message server.
    /// </summary>
    public sealed class StreamServer : Message.StreamServer
    {
        /// <summary>
        /// Construct a stream server from a byte server
        /// </summary>
        public StreamServer () : base (new NullServer ("None", string.Empty))
        {
        }

        /// <summary>
        /// Deny requests to create a client as the stream server is not supported over SerialIO
        /// </summary>
        protected override IClient<NoMessage,StreamUpdate> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            throw new InvalidOperationException ();
        }
    }
}

using System;
using KRPC.Service.Messages;

namespace KRPC.Server.SerialIO
{
    public sealed class StreamServer : Message.StreamServer
    {
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

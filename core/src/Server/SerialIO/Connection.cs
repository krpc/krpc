namespace KRPC.Server.SerialIO
{
    /// <summary>
    /// A connection over a serial port. A serial line has no connect or disconnect, so a
    /// session that ends leaves the connection waiting for a new connection request. A stalled
    /// message is dropped after 3 seconds, so that a client that resets can get back in.
    /// </summary>
    sealed class Connection : ProtocolBuffers.Connection
    {
        public Connection (Message.Server server, IClient<byte,byte> client) : base (server, client)
        {
        }

        protected override double StallTimeout {
            get { return 3; }
        }

        protected override bool TimesHandshake {
            get { return false; }
        }

        protected override bool EndsByClosing {
            get { return false; }
        }

        protected override void HalfClose ()
        {
        }
    }
}

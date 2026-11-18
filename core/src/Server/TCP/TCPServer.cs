using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using KRPC.Utils;

namespace KRPC.Server.TCP
{
    /// <summary>
    /// A raw TCP server.
    /// </summary>
    public sealed class TCPServer : SocketServer
    {
        /// <summary>
        /// The actual local port number of the server. Will be identical to
        /// port, unless port was set to 0.
        /// </summary>
        ushort actualPort;

        /// <summary>
        /// Create a TCP server. After Start() is called, the server will listen for
        /// connections to the specified local address and port number.
        /// </summary>
        public TCPServer (IPAddress address, ushort port) : base ("TCPServer")
        {
            ListenAddress = address;
            ListenPort = port;
        }

        /// <summary>
        /// Local address that the server listens on.
        /// </summary>
        public IPAddress ListenAddress { get; set; }

        /// <summary>
        /// Port number that the server listens on.
        /// If set to 0, an available port number is automatically chosen
        /// when the server starts. See <see cref="ActualPort"/>.
        /// </summary>
        public ushort ListenPort { get; set; }

        /// <summary>
        /// Port number that the server is currently listening on.
        /// </summary>
        public ushort ActualPort {
            get {
                if (!Running)
                    throw new InvalidOperationException ("Server is not running");
                return actualPort;
            }
        }

        /// <summary>
        /// Open the listening socket.
        /// </summary>
        protected override Socket Listen ()
        {
            var listener = new TcpListener (ListenAddress, ListenPort);
            try {
                listener.Start ();
            } catch (SocketException exn) {
                string socketError = "socket error '" + exn.SocketErrorCode + "': " + exn.Message;
                Logger.WriteLine ("TCPServer: failed to start server; " + socketError, Logger.Severity.Error);
                throw new ServerException (socketError);
            }
            actualPort = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
            return listener.Server;
        }

        internal override SocketClient Accepted (Socket socket)
        {
            socket.NoDelay = true;
            var address = string.Empty;
            try {
                var remoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
                if (remoteEndPoint != null)
                    address = remoteEndPoint.Address.ToString ();
            } catch (SocketException) {
            } catch (ObjectDisposedException) {
            }
            return new SocketClient (socket, address);
        }

        /// <summary>
        /// Address the server is listening on. Format depends on the server protocol.
        /// </summary>
        public override string Address {
            get { return ListenAddress + ":" + (Running ? ActualPort : ListenPort); }
        }

        const string localClientOnlyText = "Local clients only";
        const string anyClientText = "Any client";
        const string subnetAllowedText = "Subnet {0}";
        const string unknownClientsAllowedText = "Unknown visibility";

        /// <summary>
        /// Information about the server.
        /// </summary>
        public override string Info {
            get {
                if (IPAddress.IsLoopback (ListenAddress))
                    return localClientOnlyText;
                if (ListenAddress == IPAddress.Any)
                    return anyClientText;
                try {
                    var subnet = NetworkInformation.GetSubnetMask (ListenAddress);
                    return string.Format (CultureInfo.InvariantCulture, subnetAllowedText, subnet);
                } catch (NotImplementedException) {
                } catch (ArgumentException) {
                } catch (DllNotFoundException) {
                }
                return unknownClientsAllowedText;
            }
        }
    }
}

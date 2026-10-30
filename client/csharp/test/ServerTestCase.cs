using System;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    public class ServerTestCase
    {
        protected Connection Connection { get; private set; }

        [SetUp]
        public virtual void SetUp ()
        {
            Connection = Connect ("CSharpClientTest");
        }

        /// <summary>
        /// Connect over whichever transport the harness started the server with, which it
        /// tells us about by port or by socket path.
        /// </summary>
        public static Connection Connect (string name)
        {
            if (LocalSocketPath != null)
                return Connection.ConnectLocal (name, LocalSocketPath);
            return new Connection (name, port: TcpipPort);
        }

        public static string LocalSocketPath {
            get { return Environment.GetEnvironmentVariable ("KRPC_TEST_LOCALSOCKET_PATH"); }
        }

        [TearDown]
        public virtual void TearDown ()
        {
            Connection.Dispose ();
        }

        /// <summary>
        /// How long a connection to a port nothing is listening on is waited for. It is normally
        /// refused at once; where the system drops the attempt instead, this bounds the wait
        /// rather than leaving the test to hang.
        /// </summary>
        public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds (10);

        /// <summary>
        /// A port nothing is listening on, for the tests that connect to the wrong one. Binding
        /// a port and giving it straight back leaves one that a connection is refused on, and
        /// leaves it in the range the system hands out. A port derived from the server's own can
        /// land anywhere, including on a low one, and a connection to those is dropped rather
        /// than refused on Windows, which leaves the client waiting.
        /// </summary>
        public static ushort UnusedPort ()
        {
            var listener = new TcpListener (IPAddress.Loopback, 0);
            listener.Start ();
            var port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop ();
            return port;
        }

        public static ushort TcpipPort {
            get {
                ushort port = 50000;
                var envPort = Environment.GetEnvironmentVariable ("KRPC_TEST_TCPIP_PORT");
                if (envPort != null)
                    ushort.TryParse (envPort, out port);
                return port;
            }
        }
    }
}

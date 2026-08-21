using System;
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
        /// tells us about by port or by socket path. The rpc and stream arguments name which
        /// of the server's two endpoints each connection should go to, so a test can
        /// deliberately connect them the wrong way round.
        /// </summary>
        public static Connection Connect (string name, string rpc = "rpc", string stream = "stream")
        {
            if (RPCPath != null) {
                return Connection.ConnectLocal (
                    name, rpc == "rpc" ? RPCPath : StreamPath,
                    stream == null ? null : (stream == "rpc" ? RPCPath : StreamPath));
            }
            return new Connection (
                name, rpcPort: rpc == "rpc" ? RPCPort : StreamPort,
                streamPort: stream == null ? 0 : (stream == "rpc" ? RPCPort : StreamPort));
        }

        public static string RPCPath {
            get { return Environment.GetEnvironmentVariable ("RPC_PATH"); }
        }

        public static string StreamPath {
            get { return Environment.GetEnvironmentVariable ("STREAM_PATH"); }
        }

        [TearDown]
        public virtual void TearDown ()
        {
            Connection.Dispose ();
        }

        public static ushort RPCPort {
            get {
                ushort port = 50000;
                var envPort = Environment.GetEnvironmentVariable ("RPC_PORT");
                if (envPort != null)
                    ushort.TryParse (envPort, out port);
                return port;
            }
        }

        public static ushort StreamPort {
            get {
                ushort port = 50001;
                var envPort = Environment.GetEnvironmentVariable ("STREAM_PORT");
                if (envPort != null)
                    ushort.TryParse (envPort, out port);
                return port;
            }
        }
    }
}

using System;
using KRPC.Client;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    public class ServerTestCase
    {
        protected Connection Connection { get; private set; }

        [SetUp]
        public virtual void SetUp ()
        {
            Connection = new Connection ("CSharpClientTest", rpcPort: RPCPort, streamPort: StreamPort);
        }

        [TearDown]
        public virtual void TearDown ()
        {
            // TODO: This shouldn't be necessary, but avoids an assertion in Mono 4.4.0.182
            //       when the stream update thread is still running when the process exits.
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

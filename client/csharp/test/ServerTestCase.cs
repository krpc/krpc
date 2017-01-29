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
            ushort rpcPort = 50000;
            ushort streamPort = 50001;
            var envRpcPort = Environment.GetEnvironmentVariable ("RPC_PORT");
            var envStreamPort = Environment.GetEnvironmentVariable ("STREAM_PORT");
            if (envRpcPort != null)
                ushort.TryParse (envRpcPort, out rpcPort);
            if (envStreamPort != null)
                ushort.TryParse (envStreamPort, out streamPort);
            Connection = new Connection ("CSharpClientTest", rpcPort: rpcPort, streamPort: streamPort);
        }

        [TearDown]
        public virtual void TearDown ()
        {
            // TODO: This shouldn't be necessary, but avoids an assertion in Mono 4.4.0.182
            // when the stream update thread is still running when the process exits.
            Connection.Dispose ();
        }
    }
}

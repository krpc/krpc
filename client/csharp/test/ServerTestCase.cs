using System;
using KRPC.Client;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    public class ServerTestCase
    {
        protected Connection connection;

        [SetUp]
        public virtual void SetUp ()
        {
            var envRpcPort = Environment.GetEnvironmentVariable ("RPC_PORT");
            var envStreamPort = Environment.GetEnvironmentVariable ("STREAM_PORT");
            ushort rpcPort = envRpcPort == null ? (ushort)50000 : ushort.Parse (envRpcPort);
            ushort streamPort = envStreamPort == null ? (ushort)50001 : ushort.Parse (envStreamPort);
            connection = new Connection (name: "CSharpClientTest", rpcPort: rpcPort, streamPort: streamPort);
        }
    }
}

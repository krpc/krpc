using System;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    public class ServerTestCase
    {
        protected Connection Connection { get; private set; }

        /// <summary>
        /// Connect to the server the harness started, on the port it passes.
        /// </summary>
        [SetUp]
        public void SetUp ()
        {
            Connection = new Connection ("CSharpClientTest", port: TcpipPort);
        }

        [TearDown]
        public void TearDown ()
        {
            Connection.Dispose ();
        }

        static ushort TcpipPort {
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

using System;
using System.Net.Sockets;
using KRPC.Schema.KRPC;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    /// <summary>
    /// What a connection to a server does regardless of what carries it: making the
    /// handshake, carrying a call, and reporting a server that turns the connection down.
    /// Only opening the socket differs between the transports, so each of them supplies
    /// that and shares these.
    /// </summary>
    public abstract class ConnectionTestCase
    {
        StandInServer server;

        /// <summary>
        /// A socket listening on the transport under test, bound to somewhere nothing else
        /// is using.
        /// </summary>
        protected abstract Socket Listen ();

        /// <summary>
        /// Connect to the running server over the transport under test.
        /// </summary>
        protected abstract Connection Connect (string name);

        /// <summary>
        /// Where the running server is listening, for a test that opens its own socket.
        /// </summary>
        protected StandInServer Server {
            get { return server; }
        }

        [SetUp]
        public void SetUp ()
        {
            server = new StandInServer (Listen ());
        }

        [TearDown]
        public void TearDown ()
        {
            server.Dispose ();
        }

        [Test]
        public void ConnectAndDisconnect ()
        {
            using (var connection = Connect ("CSharpConnectionTest")) {
                Assert.IsNotNull (connection);
            }
        }

        [Test]
        public void CarriesACall ()
        {
            // The stand-in answers every request, so this measures the request reaching it and
            // the response coming back, and not what the call means
            using (var connection = Connect ("CSharpConnectionTest")) {
                Assert.IsNotNull (connection.Invoke ("TestService", "TestProcedure"));
            }
        }

        [Test]
        public void CarriesManyCalls ()
        {
            // A response is taken out of a buffer that reads fill a block at a time, so a run
            // of calls covers the buffer being refilled and reused, and not only its first use
            using (var connection = Connect ("CSharpConnectionTest")) {
                for (int i = 0; i < 100; i++)
                    Assert.IsNotNull (connection.Invoke ("TestService", "TestProcedure"));
            }
        }

        [Test]
        public void ConnectionRefusedByTheServer ()
        {
            server.Status = ConnectionResponse.Types.Status.WrongType;
            server.Message = "Connection request was for the wrong server";
            var exn = Assert.Throws<ConnectionException> (
                          () => Connect ("CSharpConnectionTestRefused"));
            Assert.AreEqual ("Connection request was for the wrong server", exn.Message);
        }
    }
}

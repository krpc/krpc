using System;
using System.Net.Sockets;
using System.Threading;
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
        /// Connect to the running server over the transport under test, with the default stall
        /// timeout when none is given.
        /// </summary>
        protected abstract Connection Connect (string name, TimeSpan? stallTimeout = null);

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
        public void CloseWithACallInFlight ()
        {
            var connection = Connect ("CSharpConnectionTestClose");
            server.AnswerOnDisconnect = true;
            ConnectionException error = null;
            var thread = new Thread (() => {
                try {
                    connection.Invoke ("TestService", "TestProcedure", null, null);
                } catch (ConnectionException exn) {
                    error = exn;
                }
            });
            thread.IsBackground = true;
            thread.Start ();
            Assert.IsTrue (server.RequestHeld.WaitOne (5000));
            // The call holds the reader role, and its response arrives 100 ms after the
            // disconnect, followed by the server's close. The response does not reach the call.
            var stopwatch = System.Diagnostics.Stopwatch.StartNew ();
            connection.Dispose ();
            Assert.Less (stopwatch.ElapsedMilliseconds, 600);
            Assert.IsTrue (thread.Join (5000));
            Assert.IsInstanceOf<ConnectionException> (error);
        }

        [Test]
        public void ConnectionRefusedByTheServer ()
        {
            server.Status = ConnectionResponse.Types.Status.Denied;
            server.Message = "The connection was denied";
            var exn = Assert.Throws<ConnectionException> (
                          () => Connect ("CSharpConnectionTestRefused"));
            Assert.AreEqual ("The server refused the connection (Denied): The connection was denied", exn.Message);
            Assert.AreEqual (ConnectionResponse.Types.Status.Denied, exn.Status);
        }

        [Test]
        public void MalformedHandshakeResponse ()
        {
            server.MalformedHandshake = true;
            var exn = Assert.Throws<ConnectionException> (
                          () => Connect ("CSharpConnectionTestMalformed"));
            Assert.IsInstanceOf<System.IO.InvalidDataException> (exn.InnerException);
        }

        [Test]
        public void StallTimeout ()
        {
            server.StallResponses = true;
            using (var connection = Connect ("CSharpConnectionTestStall", TimeSpan.FromMilliseconds (200))) {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew ();
                var exn = Assert.Throws<ConnectionException> (
                              () => connection.Invoke ("TestService", "TestProcedure"));
                Assert.Less (stopwatch.ElapsedMilliseconds, 5000);
                StringAssert.Contains ("stalled", exn.Message);
            }
        }

        [Test]
        public void ConnectionEndedByTheServer ()
        {
            server.DisconnectReason = Disconnect.Types.Reason.Removed;
            using (var connection = Connect ("CSharpConnectionTestEnded")) {
                var exn = Assert.Throws<ConnectionException> (
                              () => connection.Invoke ("TestService", "TestProcedure"));
                Assert.AreEqual (Disconnect.Types.Reason.Removed, exn.Reason);
                Assert.AreEqual ("The server ended the connection (Removed): Bye", exn.Message);
                // A later call fails at once with the same error
                Assert.AreSame (exn, Assert.Throws<ConnectionException> (
                    () => connection.Invoke ("TestService", "TestProcedure")));
            }
        }
    }
}

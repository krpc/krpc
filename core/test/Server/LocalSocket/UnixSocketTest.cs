using System;
using System.IO;
using System.Net.Sockets;
using KRPC.Server.LocalSocket;
using NUnit.Framework;

namespace KRPC.Test.Server.LocalSocket
{
    /// <summary>
    /// Making a socket ready by calling winsock, which is how the runtime the game uses is
    /// given a unix domain socket it can accept connections on. The runtime running these
    /// tests binds one for itself and is never sent this way, but the calls are the same, so
    /// running them here covers what winsock is handed and what it makes of it.
    /// </summary>
    [TestFixture]
    [Platform ("Win", Reason = "there is only winsock to call on Windows")]
    public class UnixSocketTest
    {
        string path;

        [SetUp]
        public void SetUp ()
        {
            path = Path.Combine (
                TestingTools.SocketDirectory (),
                "krpc-test-" + Guid.NewGuid ().ToString ("N").Substring (0, 8));
        }

        [TearDown]
        public void TearDown ()
        {
            File.Delete (path);
        }

        static Socket NewSocket ()
        {
            return new Socket (
                AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        }

        [Test]
        public void ListensOnThePathItWasGiven ()
        {
            using (var listener = NewSocket ()) {
                UnixSocket.BindAndListenThroughWinsock (listener, path, 1);
                // Reaching it is what shows the socket is bound to that path and listening on
                // it, and takes nothing from the runtime this test is running on, which cannot
                // accept on a socket it did not bind itself
                using (var client = NewSocket ()) {
                    client.Connect (new UnixEndPoint (path));
                    Assert.IsTrue (client.Connected);
                }
            }
        }

        [Test]
        public void ReportsAPathTooLongToFitInAnAddress ()
        {
            using (var listener = NewSocket ()) {
                Assert.Throws<ArgumentException> (
                    () => UnixSocket.BindAndListenThroughWinsock (
                        listener, new string ('x', 200), 1));
            }
        }
    }
}

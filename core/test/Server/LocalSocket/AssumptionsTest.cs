using System;
using System.IO;
using System.Net.Sockets;
using KRPC.Server.LocalSocket;
using NUnit.Framework;

namespace KRPC.Test.Server.LocalSocket
{
    /// <summary>
    /// The local socket transport reuses TCPStream and the connectedness check from
    /// TCPClient, both of which rely on how a socket reports that data is waiting and
    /// that the peer has gone. These tests pin that behavior down for a unix domain
    /// socket, so a runtime where it differs fails here rather than somewhere obscure.
    /// </summary>
    [TestFixture]
    public class AssumptionsTest
    {
        string path;
        Socket listener;
        Socket client;
        Socket server;

        [SetUp]
        public void SetUp ()
        {
            path = Path.Combine (TestingTools.SocketDirectory (), "krpc-test-" + Guid.NewGuid ().ToString ("N").Substring (0, 8));
            listener = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            listener.Bind (new UnixEndPoint (path));
            listener.Listen (1);
            client = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            client.Connect (new UnixEndPoint (path));
            server = listener.Accept ();
        }

        [TearDown]
        public void TearDown ()
        {
            if (server != null)
                server.Close ();
            if (client != null)
                client.Close ();
            if (listener != null)
                listener.Close ();
            File.Delete (path);
        }

        static void WaitUntil (Func<bool> predicate)
        {
            for (int i = 0; i < 100 && !predicate (); i++)
                System.Threading.Thread.Sleep (10);
        }

        [Test]
        public void DataAvailableReportsWaitingData ()
        {
            using (var stream = new NetworkStream (server)) {
                Assert.IsFalse (stream.DataAvailable);
                client.Send (new byte [] { 42 });
                WaitUntil (() => stream.DataAvailable);
                Assert.IsTrue (stream.DataAvailable);
                Assert.AreEqual (42, stream.ReadByte ());
                Assert.IsFalse (stream.DataAvailable);
            }
        }

        [Test]
        public void PollReportsWaitingData ()
        {
            Assert.IsFalse (server.Poll (0, SelectMode.SelectRead));
            client.Send (new byte [] { 42 });
            WaitUntil (() => server.Poll (0, SelectMode.SelectRead));
            Assert.IsTrue (server.Poll (0, SelectMode.SelectRead));
        }

        [Test]
        public void PeekDoesNotConsume ()
        {
            client.Send (new byte [] { 42 });
            WaitUntil (() => server.Available > 0);
            var buffer = new byte [1];
            Assert.AreEqual (1, server.Receive (buffer, 0, 1, SocketFlags.Peek));
            Assert.AreEqual (42, buffer [0]);
            Assert.AreEqual (1, server.Receive (buffer, 0, 1, SocketFlags.None));
            Assert.AreEqual (42, buffer [0]);
        }

        [Test]
        public void ClosedPeerPollsReadableAndReadsNothing ()
        {
            // This is what tells a disconnected client apart from an idle one
            client.Shutdown (SocketShutdown.Both);
            client.Close ();
            client = null;
            WaitUntil (() => server.Poll (0, SelectMode.SelectRead));
            Assert.IsTrue (server.Poll (0, SelectMode.SelectRead));
            Assert.AreEqual (0, server.Receive (new byte [1], SocketFlags.Peek));
        }

        [Test]
        public void AvailableCountsWaitingBytes ()
        {
            Assert.AreEqual (0, server.Available);
            client.Send (new byte [] { 1, 2, 3 });
            WaitUntil (() => server.Available == 3);
            Assert.AreEqual (3, server.Available);
        }
    }
}

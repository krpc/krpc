using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using KRPC.Server;
using NUnit.Framework;

namespace KRPC.Test.Server
{
    [TestFixture]
    public class SocketClientTest
    {
        TcpListener listener;
        TcpClient peer;

        [SetUp]
        public void SetUp ()
        {
            listener = new TcpListener (IPAddress.Loopback, 0);
            listener.Start ();
            peer = new TcpClient ();
            peer.Connect (IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
        }

        [TearDown]
        public void TearDown ()
        {
            peer.Close ();
            listener.Stop ();
        }

        SocketClient Accept ()
        {
            return new SocketClient (listener.AcceptSocket (), "address");
        }

        [Test]
        public void Equality ()
        {
            var clientA = Accept ();
            using (var peerB = new TcpClient ()) {
                peerB.Connect (IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                var clientB = Accept ();
                Assert.IsTrue (clientA.Equals (clientA));
                Assert.IsFalse (clientA.Equals (clientB));
                Assert.AreEqual (clientA.Guid.ToString (), clientA.Name);
                Assert.AreEqual ("address", clientA.Address);
                clientB.Close ();
            }
            clientA.Close ();
        }

        [Test]
        public void NotConnectedAfterClose ()
        {
            var client = Accept ();
            Assert.IsTrue (client.Connected);
            client.Close ();
            Assert.IsFalse (client.Connected);
        }

        [Test]
        public void ConnectedUntilThePeerClosesAfterAHalfClose ()
        {
            var client = Accept ();
            ((SocketStream)client.Stream).HalfClose ();
            Assert.IsTrue (client.Connected);
            peer.Close ();
            var timer = Stopwatch.StartNew ();
            while (client.Connected && timer.ElapsedMilliseconds < 5000)
                Thread.Sleep (10);
            Assert.IsFalse (client.Connected);
            client.Close ();
        }
    }
}

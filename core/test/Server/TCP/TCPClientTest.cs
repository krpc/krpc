using KRPC.Server.TCP;
using NUnit.Framework;

namespace KRPC.Test.Server.TCP
{
    [TestFixture]
    public class ClientTest
    {
        [Test]
        public void Equality ()
        {
            var clientA = new TCPClient (new System.Net.Sockets.TcpClient ());
            TCPClient clientA2 = clientA;
            var clientB = new TCPClient (new System.Net.Sockets.TcpClient ());
            Assert.IsTrue (clientA.Equals (clientA));
            Assert.IsFalse (clientA.Equals (clientB));
            Assert.IsTrue (clientA == clientA2);
            Assert.IsFalse (clientA == clientB);
            Assert.IsFalse (clientA != clientA2);
            Assert.IsTrue (clientA != clientB);
        }

        [Test]
        public void NotConnected ()
        {
            var client = new TCPClient (new System.Net.Sockets.TcpClient ());
            Assert.IsFalse (client.Connected);
            Assert.AreEqual (client.Guid.ToString (), client.Name);
            Assert.AreEqual (string.Empty, client.Address);
            Assert.DoesNotThrow (client.Close);
        }

        [Test]
        public void ConnectedUntilThePeerClosesAfterAHalfClose ()
        {
            var listener = new System.Net.Sockets.TcpListener (System.Net.IPAddress.Loopback, 0);
            listener.Start ();
            try {
                var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
                using (var peer = new System.Net.Sockets.TcpClient ()) {
                    peer.Connect (System.Net.IPAddress.Loopback, port);
                    var client = new TCPClient (listener.AcceptTcpClient ());
                    ((TCPStream)client.Stream).HalfClose ();
                    Assert.IsTrue (client.Connected);
                    peer.Close ();
                    var timer = System.Diagnostics.Stopwatch.StartNew ();
                    while (client.Connected && timer.ElapsedMilliseconds < 5000)
                        System.Threading.Thread.Sleep (10);
                    Assert.IsFalse (client.Connected);
                    client.Close ();
                }
            } finally {
                listener.Stop ();
            }
        }
    }
}

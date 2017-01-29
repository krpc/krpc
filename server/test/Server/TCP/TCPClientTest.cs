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
    }
}

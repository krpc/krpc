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
            Assert.IsFalse (new TCPClient (null).Equals (new TCPClient (null)));
            Assert.IsFalse (new TCPClient (null) == new TCPClient (null));
            Assert.IsTrue (new TCPClient (null) != new TCPClient (null));

            var clientA = new TCPClient (null);
            TCPClient clientA2 = clientA;
            var clientB = new TCPClient (null);
            Assert.IsTrue (clientA.Equals (clientA));
            Assert.IsFalse (clientA.Equals (clientB));
            Assert.IsTrue (clientA == clientA2);
            Assert.IsFalse (clientA == clientB);
            Assert.IsFalse (clientA != clientA2);
            Assert.IsTrue (clientA != clientB);

            Assert.IsFalse (clientA.Equals (null));
            Assert.IsFalse (clientA == null);
            Assert.IsTrue (clientA != null);
        }

        [Test]
        public void NotConnected ()
        {
            var client = new TCPClient (new System.Net.Sockets.TcpClient ());
            Assert.IsFalse (client.Connected);
            Assert.AreEqual (client.Guid.ToString (), client.Name);
            Assert.AreEqual ("", client.Address);
            Assert.DoesNotThrow (client.Close);
        }
    }
}


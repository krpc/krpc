using NUnit.Framework;
using System;
using KRPC.Server;
using KRPC.Server.Net;

namespace KRPCTest.Server.Net
{
    [TestFixture]
    public class ClientTest
    {
        [Test]
        public void Equality ()
        {
            Assert.IsTrue (new TCPClient (0, null).Equals(new TCPClient (0, null)));
            Assert.IsFalse (new TCPClient (0, null).Equals(new TCPClient (1, null)));
            Assert.IsTrue (new TCPClient (0, null) == new TCPClient (0, null));
            Assert.IsFalse (new TCPClient (0, null) == new TCPClient (1, null));
            Assert.IsFalse (new TCPClient (0, null) != new TCPClient (0, null));
            Assert.IsTrue (new TCPClient (0, null) != new TCPClient (1, null));

            TCPClient clientA = new TCPClient (0, null);
            TCPClient clientA2 = clientA;
            TCPClient clientB = new TCPClient (1, null);
            Assert.IsTrue (clientA.Equals(clientA));
            Assert.IsFalse (clientA.Equals(clientB));
            Assert.IsTrue (clientA == clientA2);
            Assert.IsFalse (clientA == clientB);
            Assert.IsFalse (clientA != clientA2);
            Assert.IsTrue (clientA != clientB);

            Assert.IsFalse (clientA.Equals(null));
            Assert.IsFalse (clientA == null);
            Assert.IsTrue (clientA != null);
        }

        [Test]
        public void HashCode () {
            Assert.IsTrue (new TCPClient (0, null).GetHashCode() == new TCPClient (0, null).GetHashCode());
        }
    }
}


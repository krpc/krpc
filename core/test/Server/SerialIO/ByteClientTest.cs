using KRPC.Server.SerialIO;
using KRPC.IO.Ports;
using NUnit.Framework;

namespace KRPC.Test.Server.SerialIO
{
    [TestFixture]
    public class ClientTest
    {
        [Test]
        public void Equality ()
        {
            var clientA = new ByteClient (new SerialPort ());
            ByteClient clientA2 = clientA;
            var clientB = new ByteClient (new SerialPort ());
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
            var client = new ByteClient (new SerialPort ());
            Assert.IsFalse (client.Connected);
            Assert.AreEqual (client.Guid.ToString (), client.Name);
            Assert.AreEqual (new SerialPort ().PortName, client.Address);
            Assert.DoesNotThrow (client.Close);
        }
    }
}

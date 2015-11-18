using NUnit.Framework;
using System.Net;
using KRPC;

namespace KRPCTest
{
    [TestFixture]
    public class KRPCConfigurationTest
    {
        [Test]
        public void DefaultConfig ()
        {
            var config = new KRPCConfiguration ("settings.cfg");
            Assert.AreEqual (IPAddress.Loopback, config.Address);
            Assert.AreEqual (50000, config.RPCPort);
            Assert.AreEqual (50001, config.StreamPort);
            Assert.AreEqual (true, config.MainWindowVisible);
            Assert.AreEqual (false, config.AutoAcceptConnections);
        }
    }
}


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
            Assert.AreEqual (50000, config.Port);
            Assert.AreEqual (true, config.MainWindowVisible);
        }
    }
}


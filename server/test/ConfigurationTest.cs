using System.Net;
using NUnit.Framework;

namespace KRPC.Test
{
    [TestFixture]
    public class ConfigurationTest
    {
        [Test]
        public void DefaultConfig ()
        {
            var config = new Configuration ("settings.cfg");
            Assert.AreEqual (IPAddress.Loopback, config.Address);
            Assert.AreEqual (50000, config.RPCPort);
            Assert.AreEqual (50001, config.StreamPort);
            Assert.AreEqual (true, config.MainWindowVisible);
            Assert.AreEqual (false, config.AutoAcceptConnections);
        }
    }
}

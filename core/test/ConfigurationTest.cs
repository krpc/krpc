using KRPC.Server;
using NUnit.Framework;

namespace KRPC.Test
{
    [TestFixture]
    public class ConfigurationTest
    {
        [Test]
        public void DefaultConfig ()
        {
            var config = new Configuration ();
            Assert.AreEqual (true, config.MainWindowVisible);
            Assert.AreEqual (true, config.AutoAcceptConnections);
        }

        [Test]
        public void DefaultServers ()
        {
            var servers = Configuration.DefaultServers ();
            Assert.AreEqual (2, servers.Count);
            Assert.AreEqual (Protocol.ProtocolBuffersOverTCP, servers [0].Protocol);
            Assert.AreEqual ("127.0.0.1", servers [0].Settings ["tcpip_address"]);
            Assert.AreEqual ("50000", servers [0].Settings ["tcpip_port"]);
            Assert.AreEqual (Protocol.ProtocolBuffersOverWebsockets, servers [1].Protocol);
            Assert.AreEqual ("127.0.0.1", servers [1].Settings ["ws_address"]);
            Assert.AreEqual ("50001", servers [1].Settings ["ws_port"]);
            Assert.AreNotEqual (servers [0].Id, servers [1].Id);
        }

        [Test]
        public void DefaultPort ()
        {
            Assert.AreEqual (50000, Configuration.DefaultPort (Protocol.ProtocolBuffersOverTCP));
            Assert.AreEqual (50001, Configuration.DefaultPort (Protocol.ProtocolBuffersOverWebsockets));
            Assert.AreEqual (0, Configuration.DefaultPort (Protocol.ProtocolBuffersOverLocalSocket));
        }

        [Test]
        public void ServerWithoutPortSetting ()
        {
            // A server whose settings have no port listens on its protocol's default port
            var config = new Configuration.Server (Protocol.ProtocolBuffersOverWebsockets);
            config.Settings.Remove ("ws_port");
            Assert.AreEqual ("ws://127.0.0.1:50001", config.Create ().Address);
        }
    }
}

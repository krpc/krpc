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
        public void DefaultsAreTheBalancedPreset ()
        {
            var config = new Configuration ();
            Assert.AreEqual ("Balanced", config.CurrentPreset.Name);
        }

        [Test]
        public void ApplyAPreset ()
        {
            var config = new Configuration ();
            config.TargetFrameRate = 30;
            var callsFirst = Configuration.Presets [2];
            callsFirst.Apply (config);
            Assert.AreSame (callsFirst, config.CurrentPreset);
            Assert.IsFalse (config.AdaptiveRateControl);
            Assert.AreEqual (20000, config.MaxTimePerFrame);
            Assert.AreEqual (20000, config.MaxCallWait);
            // Without adaptive rate control, the target is left as it is
            Assert.AreEqual (30, config.TargetFrameRate);
        }

        [Test]
        public void EditingASettingLeavesThePreset ()
        {
            var config = new Configuration ();
            config.MaxCallWait = 2000;
            Assert.IsNull (config.CurrentPreset);
            config.MaxCallWait = 5000;
            config.TargetFrameRate = 30;
            Assert.IsNull (config.CurrentPreset);
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
        public void NewServer ()
        {
            var config = new Configuration ();
            foreach (var server in Configuration.DefaultServers ())
                config.Servers.Add (server);
            Assert.AreEqual ("Server", config.NewServer (Protocol.ProtocolBuffersOverTCP).Name);
            config.Servers.Add (config.NewServer (Protocol.ProtocolBuffersOverTCP));
            Assert.AreEqual ("Server 2", config.NewServer (Protocol.ProtocolBuffersOverWebsockets).Name);
            config.Servers.Add (config.NewServer (Protocol.ProtocolBuffersOverTCP));
            Assert.AreEqual ("Server 3", config.NewServer (Protocol.ProtocolBuffersOverTCP).Name);
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

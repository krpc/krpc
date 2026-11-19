using System;
using System.Collections.Generic;
using KRPC.Server;
using KRPC.Server.SerialIO;
using NUnit.Framework;
#if NET
using SerialPorts = System.IO.Ports;
#else
using SerialPorts = KRPC.IO.Ports;
#endif

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
            Assert.AreEqual ("TCP/IP Server 2", config.NewServer (Protocol.ProtocolBuffersOverTCP).Name);
            config.Servers.Add (config.NewServer (Protocol.ProtocolBuffersOverTCP));
            Assert.AreEqual ("TCP/IP Server 3", config.NewServer (Protocol.ProtocolBuffersOverTCP).Name);
            config.Servers.Clear ();
            Assert.AreEqual ("WebSockets Server", config.NewServer (Protocol.ProtocolBuffersOverWebsockets).Name);
        }

        [Test]
        public void ServerWithoutPortSetting ()
        {
            // A server whose settings have no port listens on its protocol's default port
            var config = new Configuration.Server (Protocol.ProtocolBuffersOverWebsockets);
            config.Settings.Remove ("ws_port");
            Assert.AreEqual ("ws://127.0.0.1:50001", config.Create ().Address);
        }

        static ByteServer SerialServer (string flowControl)
        {
            var settings = new Dictionary<string, string> { { "serialio_port", "COM1" } };
            if (flowControl != null)
                settings ["serialio_flow_control"] = flowControl;
            var config = new Configuration.Server {
                Id = Guid.NewGuid (),
                Name = "Serial",
                Protocol = Protocol.ProtocolBuffersOverSerialIO,
                Settings = settings
            };
            var server = config.Create ();
            return (ByteServer)((KRPC.Server.SerialIO.Server)server.InnerServer).InnerServer;
        }

        [Test]
        public void SerialFlowControl ()
        {
            Assert.AreEqual (SerialPorts.Handshake.None, SerialServer (null).FlowControl);
            Assert.AreEqual (SerialPorts.Handshake.None, SerialServer ("None").FlowControl);
            Assert.AreEqual (SerialPorts.Handshake.RequestToSend, SerialServer ("RequestToSend").FlowControl);
        }

        [Test]
        public void SerialFlowControlUnsupported ()
        {
            // XON/XOFF bytes would collide with the binary messages
            Assert.AreEqual (SerialPorts.Handshake.None, SerialServer ("XOnXOff").FlowControl);
            Assert.AreEqual (SerialPorts.Handshake.None, SerialServer ("RequestToSendXOnXOff").FlowControl);
            Assert.AreEqual (SerialPorts.Handshake.None, SerialServer ("Unknown").FlowControl);
            // The number of RequestToSend, which the server window would show and save as None
            Assert.AreEqual (SerialPorts.Handshake.None, SerialServer ("2").FlowControl);
        }

        [Test]
        public void SerialServerRejectsXOnXOff ()
        {
            Assert.Throws<ArgumentException> (() => new ByteServer (
                "COM1", 9600, 8, SerialPorts.Parity.None, SerialPorts.StopBits.One, SerialPorts.Handshake.XOnXOff));
        }
    }
}

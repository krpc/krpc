using System;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Utils;
using Moq;
using NUnit.Framework;

namespace KRPC.Test
{
    [TestFixture]
    public class CoreTest
    {
        // A server whose running state is driven by the test, so that stopping it
        // can be simulated without any network activity.
        sealed class TestServer
        {
            readonly Mock<IServer<Request,Response>> messageServer = new Mock<IServer<Request,Response>> ();
            bool running;

            public TestServer ()
            {
                messageServer.SetupGet (x => x.Running).Returns (() => running);
                messageServer.Setup (x => x.Start ()).Callback (() => {
                    running = true;
                    messageServer.Raise (x => x.OnStarted += null, EventArgs.Empty);
                });
                messageServer.Setup (x => x.Stop ()).Callback (() => {
                    running = false;
                    messageServer.Raise (x => x.OnStopped += null, EventArgs.Empty);
                });
                Server = new KRPC.Server.Server (
                    Guid.NewGuid (), Protocol.ProtocolBuffersOverTCP, "TestServer",
                    messageServer.Object);
            }

            public KRPC.Server.Server Server { get; private set; }
        }

        [Test]
        public void TheObjectStoreOutlivesAServerThatStops ()
        {
            var core = Core.Instance;
            var first = new TestServer ().Server;
            var second = new TestServer ().Server;
            core.Add (first);
            core.Add (second);
            try {
                first.Start ();
                second.Start ();
                var obj = new object ();
                var id = ObjectStore.Instance.AddInstance (obj);

                first.Stop ();
                Assert.AreSame (obj, ObjectStore.Instance.GetInstance (id));

                second.Stop ();
                Assert.Throws<ArgumentException> (() => ObjectStore.Instance.GetInstance (id));
            } finally {
                core.Remove (first.Id);
                core.Remove (second.Id);
            }
        }

        [Test]
        public void AnUnknownProcedureLoggedAtDebugGetsAnError ()
        {
            var core = Core.Instance;
            var config = Configuration.Instance;
            var blockingRecv = config.BlockingRecv;
            var enabled = Logger.Enabled;
            var level = Logger.Level;
            var writer = Logger.Writer;
            config.BlockingRecv = false;
            Logger.Enabled = true;
            Logger.Level = Logger.Severity.Debug;
            Logger.Writer = (message, severity) => { };
            var updateTime = new PinnedUpdateTime ();
            var client = new ScriptedClient (0, 0, "NoSuchProcedure");
            core.ClientConnected (client);
            try {
                core.Update ();
                Assert.IsNull (client.DisconnectedWith);
                Assert.AreEqual (1, client.Written.Count);
                Assert.IsTrue (client.Written [0].Results [0].HasError);
            } finally {
                core.ClientDisconnected (client);
                updateTime.Restore ();
                config.BlockingRecv = blockingRecv;
                Logger.Enabled = enabled;
                Logger.Level = level;
                Logger.Writer = writer;
            }
        }

        [Test]
        public void AdaptiveRateControlKeepsTheConfiguredMaxTimePerUpdate ()
        {
            var core = Core.Instance;
            var config = Configuration.Instance;
            var blockingRecv = config.BlockingRecv;
            var maxTimePerUpdate = config.MaxTimePerUpdate;
            var adaptiveRateControl = config.AdaptiveRateControl;
            config.BlockingRecv = false;
            config.AdaptiveRateControl = true;
            config.MaxTimePerUpdate = 5000;
            try {
                Assert.AreEqual (5000, core.MaxTimePerUpdate);
                // An idle update moves the working limit off the configured one, in either direction
                core.Update ();
                Assert.AreEqual (5000, config.MaxTimePerUpdate);
                Assert.AreNotEqual (5000, core.MaxTimePerUpdate);

                config.MaxTimePerUpdate = 2000;
                Assert.AreEqual (2000, core.MaxTimePerUpdate);

                core.Update ();
                Assert.AreNotEqual (2000, core.MaxTimePerUpdate);
                config.AdaptiveRateControl = false;
                Assert.AreEqual (2000, core.MaxTimePerUpdate);
                core.Update ();
                Assert.AreEqual (2000, core.MaxTimePerUpdate);
            } finally {
                config.BlockingRecv = blockingRecv;
                config.MaxTimePerUpdate = maxTimePerUpdate;
                config.AdaptiveRateControl = adaptiveRateControl;
            }
        }
    }
}

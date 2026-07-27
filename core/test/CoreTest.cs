using System;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Messages;
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
            readonly Mock<IServer<Request,Response>> rpcServer = new Mock<IServer<Request,Response>> ();
            readonly Mock<IServer<NoMessage,StreamUpdate>> streamServer = new Mock<IServer<NoMessage,StreamUpdate>> ();
            bool running;

            public TestServer ()
            {
                rpcServer.SetupGet (x => x.Running).Returns (() => running);
                streamServer.SetupGet (x => x.Running).Returns (() => running);
                rpcServer.Setup (x => x.Start ()).Callback (() => {
                    running = true;
                    rpcServer.Raise (x => x.OnStarted += null, EventArgs.Empty);
                });
                rpcServer.Setup (x => x.Stop ()).Callback (() => {
                    running = false;
                    rpcServer.Raise (x => x.OnStopped += null, EventArgs.Empty);
                });
                Server = new KRPC.Server.Server (
                    Guid.NewGuid (), Protocol.ProtocolBuffersOverTCP, "TestServer",
                    rpcServer.Object, streamServer.Object);
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
    }
}

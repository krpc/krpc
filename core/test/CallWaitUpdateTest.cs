using System.Collections.Generic;
using System.Diagnostics;
using KRPC.Service;
using NUnit.Framework;

namespace KRPC.Test
{
    [TestFixture]
    public class CallWaitUpdateTest
    {
        Core core;
        uint maxCallWait;
        PinnedUpdateTime updateTime;
        readonly List<ScriptedClient> clients = new List<ScriptedClient> ();

        [SetUp]
        public void SetUp ()
        {
            core = Core.Instance;
            CallContext.GameScene = GameScene.Flight;
            maxCallWait = Configuration.Instance.MaxCallWait;
            Configuration.Instance.MaxCallWait = 5000;
            updateTime = new PinnedUpdateTime ();
        }

        [TearDown]
        public void TearDown ()
        {
            foreach (var client in clients)
                core.ClientDisconnected (client);
            clients.Clear ();
            Configuration.Instance.MaxCallWait = maxCallWait;
            updateTime.Restore ();
        }

        ScriptedClient Connect (int pollsBeforeReady, params string[] procedures)
        {
            var client = new ScriptedClient (pollsBeforeReady, 0, procedures);
            core.ClientConnected (client);
            clients.Add (client);
            return client;
        }

        [Test]
        public void ASecondCallWithinTheWaitRunsInTheSameUpdate ()
        {
            // Each request is ready on the third poll. The first two updates find it not
            // ready, and the third runs it, then waits for the second
            var client = Connect (2, "GetClientName", "GetClientName");
            core.Update ();
            core.Update ();
            core.Update ();
            Assert.AreEqual (2, client.Written.Count);
        }

        [Test]
        public void WithNoCallWaitASecondCallRunsInALaterUpdate ()
        {
            Configuration.Instance.MaxCallWait = 0;
            var client = Connect (2, "GetClientName", "GetClientName");
            core.Update ();
            core.Update ();
            core.Update ();
            Assert.AreEqual (1, client.Written.Count);
        }

        [Test]
        public void TheUpdateWaitsForANewClient ()
        {
            // A client with no turnarounds measured is waited for about a millisecond
            var client = Connect (0, "GetClientName");
            var timer = Stopwatch.StartNew ();
            core.Update ();
            timer.Stop ();
            Assert.AreEqual (1, client.Written.Count);
            Assert.GreaterOrEqual (timer.Elapsed.TotalMilliseconds, 1);
            Assert.Less (timer.ElapsedMilliseconds, 1000);
        }

        [Test]
        public void AnIdleClientDoesNotMarkFramesBound ()
        {
            // A budget of 0.9 ms is too little for the millisecond wait of a client with no
            // turnarounds measured, so the update that runs its call is bound
            Configuration.Instance.MaxTimePerFrame = 900;
            Connect (0, "GetClientName");
            core.Update ();
            core.ClearStats ();
            for (int i = 0; i < 5; i++)
                core.Update ();
            Assert.AreEqual (0, core.BudgetBoundShare);
        }

        [Test]
        public void AMaxTimePerFrameOfZeroStillRunsCalls ()
        {
            Configuration.Instance.MaxTimePerFrame = 0;
            var client = Connect (0, "GetClientName");
            // The least time per frame is short, so a slow machine may leave the call to a
            // later update
            for (int i = 0; i < 10 && client.Written.Count == 0; i++)
                core.Update ();
            Assert.AreEqual (1, client.Written.Count);
        }
    }
}

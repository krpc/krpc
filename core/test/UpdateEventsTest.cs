using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace KRPC.Test
{
    [TestFixture]
    public class UpdateEventsTest
    {
        Core core;
        bool blockingRecv;
        uint tickHoldTimeout;
        readonly List<ScriptedClient> clients = new List<ScriptedClient> ();
        int beforeCalls;
        int afterCalls;

        [SetUp]
        public void SetUp ()
        {
            core = Core.Instance;
            var config = Configuration.Instance;
            blockingRecv = config.BlockingRecv;
            tickHoldTimeout = config.TickHoldTimeout;
            config.BlockingRecv = false;
            beforeCalls = 0;
            afterCalls = 0;
            core.OnBeforeCalls += OnBeforeCalls;
            core.OnAfterCalls += OnAfterCalls;
        }

        [TearDown]
        public void TearDown ()
        {
            core.OnBeforeCalls -= OnBeforeCalls;
            core.OnAfterCalls -= OnAfterCalls;
            foreach (var client in clients)
                core.RPCClientDisconnected (client);
            clients.Clear ();
            var config = Configuration.Instance;
            config.BlockingRecv = blockingRecv;
            config.TickHoldTimeout = tickHoldTimeout;
        }

        void OnBeforeCalls (object sender, EventArgs args)
        {
            beforeCalls++;
        }

        void OnAfterCalls (object sender, EventArgs args)
        {
            afterCalls++;
        }

        ScriptedClient Connect (int pollsBeforeReady, params string[] procedures)
        {
            var client = new ScriptedClient (pollsBeforeReady, 0, procedures);
            core.RPCClientConnected (client);
            clients.Add (client);
            return client;
        }

        [Test]
        public void AnIdleUpdateRaisesBothEvents ()
        {
            core.Update ();
            Assert.AreEqual (1, beforeCalls);
            Assert.AreEqual (1, afterCalls);
        }

        [Test]
        public void EachUpdateRaisesBothEventsOnce ()
        {
            Connect (1, "GetClientName", "GetClientName");
            core.Update ();
            core.Update ();
            core.Update ();
            Assert.AreEqual (3, beforeCalls);
            Assert.AreEqual (3, afterCalls);
        }

        [Test]
        public void AnUpdateThatHeldTheTickRaisesBothEventsOnce ()
        {
            // A held update runs its poll loop many times over and executes the whole script
            // inside itself, so it is the case in which a hook raised from the wrong place
            // would fire repeatedly.
            var client = Connect (1, "HoldTick", "GetClientName", "GetClientName", "ReleaseTick");
            core.Update ();
            core.Update ();
            Assert.AreEqual (4, client.Written.Count);
            Assert.AreEqual (2, beforeCalls);
            Assert.AreEqual (2, afterCalls);
        }

        [Test]
        public void TheEventsBracketTheCallsThatTheUpdateExecutes ()
        {
            // The number of the script's calls answered each time an event was raised. The
            // update that runs the script must raise the first before any of them and the
            // second after all of them
            var answeredAtBefore = new List<int> ();
            var answeredAtAfter = new List<int> ();
            ScriptedClient client = null;
            EventHandler before = (s, e) => answeredAtBefore.Add (client.Written.Count);
            EventHandler after = (s, e) => answeredAtAfter.Add (client.Written.Count);
            core.OnBeforeCalls += before;
            core.OnAfterCalls += after;
            try {
                client = Connect (1, "HoldTick", "GetClientName", "ReleaseTick");
                core.Update ();
                core.Update ();
                Assert.AreEqual (3, client.Written.Count);
                Assert.AreEqual (new [] { 0, 0 }, answeredAtBefore);
                Assert.AreEqual (new [] { 0, 3 }, answeredAtAfter);
            } finally {
                core.OnBeforeCalls -= before;
                core.OnAfterCalls -= after;
            }
        }
    }
}

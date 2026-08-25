using System;
using System.Collections.Generic;
using System.Diagnostics;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Messages;
using NUnit.Framework;

namespace KRPC.Test
{
    [TestFixture]
    public class TickHoldTest
    {
        Core core;
        bool blockingRecv;
        uint tickHoldTimeout;
        readonly List<ScriptedClient> clients = new List<ScriptedClient> ();

        [SetUp]
        public void SetUp ()
        {
            core = Core.Instance;
            // Procedures are gated on the game scene, so the calls these tests script only run
            // in one. Set here rather than relied on from whichever fixture ran last.
            CallContext.GameScene = GameScene.Flight;
            var config = Configuration.Instance;
            blockingRecv = config.BlockingRecv;
            tickHoldTimeout = config.TickHoldTimeout;
            // Without blocking receives the server polls each client once per pass, so a client
            // that is not ready costs it one poll rather than a wait, and the number of updates
            // a script takes is decided by the script rather than by a clock.
            config.BlockingRecv = false;
        }

        [TearDown]
        public void TearDown ()
        {
            foreach (var client in clients)
                core.RPCClientDisconnected (client);
            clients.Clear ();
            var config = Configuration.Instance;
            config.BlockingRecv = blockingRecv;
            config.TickHoldTimeout = tickHoldTimeout;
        }

        ScriptedClient Connect (int pollsBeforeReady, params string[] procedures)
        {
            return Connect (pollsBeforeReady, 0, procedures);
        }

        ScriptedClient Connect (int pollsBeforeReady, int pollsBeforeGone,
                                params string[] procedures)
        {
            var client = new ScriptedClient (pollsBeforeReady, pollsBeforeGone, procedures);
            core.RPCClientConnected (client);
            clients.Add (client);
            return client;
        }

        ScriptedClient Batched (params string[][] requests)
        {
            var client = ScriptedClient.Batched (0, 0, requests);
            core.RPCClientConnected (client);
            clients.Add (client);
            return client;
        }

        static void AssertNoErrors (IEnumerable<Response> responses)
        {
            foreach (var response in responses) {
                Assert.IsFalse (response.HasError, response.Error == null ? string.Empty : response.Error.Description);
                // A call that failed is reported in its own result, so checking the response
                // alone would pass however the calls went.
                foreach (var result in response.Results)
                    Assert.IsFalse (result.HasError, result.Error == null ? string.Empty : result.Error.Description);
            }
        }

        [Test]
        public void HoldingTheTickExecutesEveryCallInOneUpdate ()
        {
            var client = Connect (1, "HoldTick", "GetClientName", "GetClientName", "ReleaseTick");
            // The first update spends its one poll finding the client not ready, and the second
            // takes the hold and then runs the rest of the script inside itself.
            core.Update ();
            core.Update ();
            Assert.AreEqual (4, client.Written.Count);
            AssertNoErrors (client.Written);
        }

        [Test]
        public void WithoutAHoldOneCallIsExecutedPerUpdate ()
        {
            var client = Connect (1, "GetClientName", "GetClientName", "GetClientName", "GetClientName");
            core.Update ();
            core.Update ();
            Assert.AreEqual (1, client.Written.Count);
            AssertNoErrors (client.Written);
        }

        [Test]
        public void AHoldRunsOutOfTime ()
        {
            Configuration.Instance.TickHoldTimeout = 20000;
            // Nothing follows the hold, so the update has nothing to wait for but the timeout.
            var client = Connect (1, "HoldTick");
            core.Update ();
            var timer = Stopwatch.StartNew ();
            core.Update ();
            timer.Stop ();
            Assert.AreEqual (1, client.Written.Count);
            AssertNoErrors (client.Written);
            Assert.GreaterOrEqual (timer.ElapsedMilliseconds, 15);
            Assert.Less (timer.ElapsedMilliseconds, 2000);
        }

        [Test]
        public void ADisconnectedClientDoesNotKeepHoldingTheTick ()
        {
            Configuration.Instance.TickHoldTimeout = 10000000;
            // The client stops answering, and then goes away, while the tick is held.
            var client = Connect (1, 50, "HoldTick");
            core.Update ();
            // Taken on this update, and the client goes away while it is held. The update has
            // to notice, rather than waiting out a timeout that is far longer than the test.
            var timer = Stopwatch.StartNew ();
            core.Update ();
            timer.Stop ();
            Assert.AreEqual (1, client.Written.Count);
            Assert.Less (timer.ElapsedMilliseconds, 2000);
        }

        [Test]
        public void AHoldTakenAfterAReleaseWaitsForTheNextTick ()
        {
            // Sent as separate requests, so the release is the last call of its update.
            var client = Connect (0, "HoldTick", "ReleaseTick", "HoldTick", "ReleaseTick");
            core.Update ();
            // The update ends when the hold does, so the second hold is not taken in it.
            Assert.AreEqual (2, client.Written.Count);
            core.Update ();
            Assert.AreEqual (4, client.Written.Count);
            AssertNoErrors (client.Written);
        }

        [Test]
        public void AHoldSentWithTheReleaseBeforeItWaitsForTheNextTick ()
        {
            // Sent as one request, so both calls run in one continuation and the release cannot
            // end the update before the hold behind it is executed. The hold has to turn itself
            // down, or a client could defer the tick for as long as it kept asking.
            var client = Batched (
                new [] { "HoldTick" },
                new [] { "ReleaseTick", "HoldTick" },
                new [] { "ReleaseTick" });
            core.Update ();
            // Only the first hold has been answered: the request that released and asked again
            // is waiting for the next update, and the one behind it has not been read.
            Assert.AreEqual (1, client.Written.Count);
            core.Update ();
            Assert.AreEqual (3, client.Written.Count);
            AssertNoErrors (client.Written);
        }

        [Test]
        public void AReleaseAndAHoldInSeparateRequestsCanMissATick ()
        {
            // The hold is a call the server has to be waiting for when it looks, and a client
            // that is not ready costs it a poll. Nothing holds the tick after the release, so
            // the update it would have held ends without it.
            var client = Connect (1, "HoldTick", "ReleaseTick", "HoldTick", "ReleaseTick");
            core.Update ();
            core.Update ();
            Assert.AreEqual (2, client.Written.Count);
            core.Update ();
            // The tick this update ran is the one that was missed: the second hold is still
            // waiting to be read.
            Assert.AreEqual (2, client.Written.Count);
            core.Update ();
            Assert.AreEqual (4, client.Written.Count);
            AssertNoErrors (client.Written);
        }

        [Test]
        public void NextTickTakesTheFollowingTickWithoutBeingPolledFor ()
        {
            // The same script as above with one call in place of the two, and the same client
            // that is never ready on the first poll.
            var client = Connect (1, "HoldTick", "NextTick", "ReleaseTick");
            core.Update ();
            core.Update ();
            // The hold has been answered and the tick released again, but the call that asked
            // for the next tick is waiting in the server rather than on the wire.
            Assert.AreEqual (1, client.Written.Count);
            core.Update ();
            // Carried on at the start of this update, ahead of any polling, so the tick after
            // the one that was released is held rather than missed.
            Assert.AreEqual (3, client.Written.Count);
            AssertNoErrors (client.Written);
        }

        [Test]
        public void ALoopOfNextTicksTakesOneTickPerIteration ()
        {
            var client = Connect (1, "HoldTick", "NextTick", "NextTick", "NextTick",
                                  "ReleaseTick");
            core.Update ();
            // One call answered per update from here on, each in the tick after the last.
            for (int answered = 1; answered <= 4; answered++) {
                core.Update ();
                Assert.AreEqual (answered == 4 ? 5 : answered, client.Written.Count);
            }
            AssertNoErrors (client.Written);
        }

        [Test]
        public void TheTickCannotBeHeldOutsideACall ()
        {
            var client = Connect (1);
            // Holding the tick from a stream or an event would leave a hold in force with no
            // call waiting to release it, and take another before that one had ended.
            Assert.Throws<InvalidOperationException> (() => core.HoldTick (client));
        }

        [Test]
        public void ReleasingATickThatIsNotHeldDoesNothing ()
        {
            var client = Connect (1);
            Assert.DoesNotThrow (() => core.ReleaseTick (client));
        }
    }
}

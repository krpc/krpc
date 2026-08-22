using System;
using System.Collections.Generic;
using System.Diagnostics;
using KRPC.Server;
using KRPC.Service.Messages;
using NUnit.Framework;

namespace KRPC.Test
{
    [TestFixture]
    public class TickHoldTest
    {
        // A client whose requests are handed over one at a time, and only once the server has
        // polled for them often enough. Polling stands in for the time a real client spends
        // deciding what to call next, so that what is measured is how many updates the calls
        // are spread over rather than how long anything took.
        sealed class ScriptedClient : IClient<Request,Response>
        {
            sealed class ScriptedStream : IStream<Request,Response>
            {
                readonly Queue<Request> pending;
                readonly int pollsBeforeReady;
                readonly Action onPollsBeforeGone;
                readonly int pollsBeforeGone;
                int polls;

                public ScriptedStream (IEnumerable<Request> requests, int pollsBeforeReady,
                                       int pollsBeforeGone, Action onPollsBeforeGone)
                {
                    pending = new Queue<Request> (requests);
                    this.pollsBeforeReady = pollsBeforeReady;
                    this.pollsBeforeGone = pollsBeforeGone;
                    this.onPollsBeforeGone = onPollsBeforeGone;
                }

                public IList<Response> Written { get; } = new List<Response> ();

                public bool DataAvailable {
                    get {
                        // Counted here, since polling for a request is what the server does
                        // while it waits for one.
                        polls++;
                        if (pollsBeforeGone > 0 && polls > pollsBeforeGone)
                            onPollsBeforeGone ();
                        if (pending.Count == 0)
                            return false;
                        return polls > pollsBeforeReady;
                    }
                }

                public Request Read ()
                {
                    polls = 0;
                    return pending.Dequeue ();
                }

                public void Write (Response value)
                {
                    Written.Add (value);
                }

                public ulong BytesRead { get { return 0; } }

                public ulong BytesWritten { get { return 0; } }

                public void ClearStats ()
                {
                }

                public void Close ()
                {
                }

                public int Read (Request[] buffer, int offset)
                {
                    throw new NotSupportedException ();
                }

                public int Read (Request[] buffer, int offset, int size)
                {
                    throw new NotSupportedException ();
                }

                public void Write (Response[] buffer)
                {
                    throw new NotSupportedException ();
                }

                public void Write (Response[] buffer, int offset, int size)
                {
                    throw new NotSupportedException ();
                }
            }

            readonly ScriptedStream stream;

            public ScriptedClient (int pollsBeforeReady, int pollsBeforeGone,
                                   params string[] procedures)
            {
                var requests = new List<Request> ();
                foreach (var procedure in procedures) {
                    var request = new Request ();
                    request.Calls.Add (new ProcedureCall ("KRPC", procedure));
                    requests.Add (request);
                }
                stream = new ScriptedStream (requests, pollsBeforeReady, pollsBeforeGone,
                                             () => Connected = false);
                Guid = Guid.NewGuid ();
                Connected = true;
            }

            public IList<Response> Written { get { return stream.Written; } }

            public string Name { get { return "ScriptedClient"; } }

            public Guid Guid { get; private set; }

            public string Address { get { return "scripted"; } }

            public bool Connected { get; set; }

            public IStream<Request,Response> Stream { get { return stream; } }

            public void Close ()
            {
                Connected = false;
            }

            // The round robin scheduler and the set of clients already being served both
            // compare clients, so identity has to come from the identifier.
            public bool Equals (IClient<Request,Response> other)
            {
                return other != null && Guid == other.Guid;
            }

            public override bool Equals (object obj)
            {
                return Equals (obj as IClient<Request,Response>);
            }

            public override int GetHashCode ()
            {
                return Guid.GetHashCode ();
            }
        }

        Core core;
        bool blockingRecv;
        uint tickHoldTimeout;
        readonly List<ScriptedClient> clients = new List<ScriptedClient> ();

        [SetUp]
        public void SetUp ()
        {
            core = Core.Instance;
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

        static void AssertNoErrors (IEnumerable<Response> responses)
        {
            foreach (var response in responses)
                Assert.IsFalse (response.HasError, response.Error == null ? string.Empty : response.Error.Description);
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

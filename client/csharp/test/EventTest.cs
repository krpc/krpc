using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using KRPC.Client.Services.TestService;
using KRPC.Client.Services.KRPC;
using NUnit.Framework;

using Expr = KRPC.Client.Services.KRPC.Expression;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class EventTest : ServerTestCase
    {
        [Test]
        public void TestEvent () {
            var e = Connection.TestService ().OnTimer (200);
            lock (e.Condition) {
                var timer = new Stopwatch ();
                timer.Start ();
                Assert.True (e.Wait ());
                var time = timer.ElapsedMilliseconds;
                // Lower bound is a correctness check: the event must not fire before its
                // timer. The upper bound is a generous hang detector, kept loose so the test
                // does not flake under parallel load.
                Assert.Greater (time, 150);
                Assert.Less (time, 2000);
            }
        }

        [Test]
        public void TestEventTimeoutShort () {
            var e = Connection.TestService ().OnTimer (200);
            lock (e.Condition) {
                var timer = new Stopwatch ();
                timer.Start ();
                Assert.False (e.Wait (0.1));
                var time = timer.ElapsedMilliseconds;
                // Wait must return on its timeout, before the 200ms event fires.
                Assert.Greater (time, 50);
                Assert.Less (time, 200);
                Assert.True (e.Wait ());
            }
        }

        [Test]
        public void TestEventTimeoutLong () {
            var e = Connection.TestService ().OnTimer (200);
            lock (e.Condition) {
                var timer = new Stopwatch ();
                timer.Start ();
                Assert.True (e.Wait (1));
                var time = timer.ElapsedMilliseconds;
                Assert.Greater (time, 150);
                Assert.Less (time, 2000);
            }
        }

        [Test]
        public void TestEventCallback () {
            var e = Connection.TestService ().OnTimer (200);
            var called = new ManualResetEvent (false);
            e.AddCallback (() => called.Set ());
            var timer = new Stopwatch ();
            timer.Start ();
            called.WaitOne (1000);
            var time = timer.ElapsedMilliseconds;
            Assert.Greater (time, 150);
            Assert.Less (time, 2000);
        }

        [Test]
        public void TestEventCallbackTimeout () {
            var e = Connection.TestService ().OnTimer (1000);
            var called = new ManualResetEvent (false);
            e.AddCallback (() => called.Set ());
            var timer = new Stopwatch ();
            timer.Start ();
            called.WaitOne (100);
            var time = timer.ElapsedMilliseconds;
            // WaitOne returns on its 100ms timeout, before the 1000ms event fires.
            Assert.Greater (time, 50);
            Assert.Less (time, 1000);
        }

        [Test]
        public void TestEventCallbackLoop () {
            var e = Connection.TestService ().OnTimer (200, 5);
            int count = 0;
            e.AddCallback (() => { count++; });
            var timer = new Stopwatch ();
            timer.Start ();
            while (count < 5) {
            }
            var time = timer.ElapsedMilliseconds;
            Assert.Greater (time, 950);
            Assert.Less (time, 3000);
            Assert.AreEqual (count, 5);
        }

        [Test]
        public void TestEventRemoveCallback () {
            var e = Connection.TestService ().OnTimer (200);
            var stop = new ManualResetEvent (false);
            bool called1 = false;
            bool called2 = false;
            e.AddCallback (() => {
                called1 = true;
                stop. Set();
            });
            int callback2Tag = e.AddCallback (() => {
                called2 = true;
            });
            e.RemoveCallback (callback2Tag);
            var timer = new Stopwatch ();
            timer.Start ();
            stop.WaitOne ();
            var time = timer.ElapsedMilliseconds;
            Assert.Greater (time, 150);
            Assert.Less (time, 2000);
            Assert.IsTrue (called1);
            Assert.IsFalse (called2);
        }

        [Test]
        public void TestCustomEvent () {
            var counter = Expr.Call(Connection,
                Connection.GetCall (
                    () => Connection.TestService ().Counter("TestEvent.TestCustomEvent", 1)));
            var expr = Expr.Equal(Connection,
                Expr.Multiply(Connection,
                    Expr.ConstantInt(Connection, 2),
                    Expr.ConstantInt(Connection, 10)),
                counter);
            var evnt = Connection.KRPC ().AddEvent(expr);
            lock (evnt.Condition) {
                Assert.True (evnt.Wait ());
                // The event fires when the server-side counter reaches 20. The counter
                // increments on every expression evaluation, so the value read back is at
                // least 21: 20 at the trigger, plus this read. The exact figure depends on how
                // many more times the server evaluated the expression, so assert the lower
                // bound to avoid flaking under load.
                Assert.GreaterOrEqual(Connection.TestService ().Counter("TestEvent.TestCustomEvent", 1), 21);
            }
        }

        [Test]
        public void TestEquality () {
            var e0 = Connection.TestService ().OnTimer (100);
            var e1 = e0;
            var e2 = Connection.TestService ().OnTimer (100);

            Assert.True (e0.Equals (e0));

            Assert.True (e0.Equals (e1));
            Assert.True (e0 == e1);
            Assert.False (e0 != e1);

            Assert.False (e0.Equals (e2));
            Assert.False (e0 == e2);
            Assert.True (e0 != e2);

            Assert.False (e0.Equals (null));
            Assert.False (e0 == null);
            Assert.True (e0 != null);
            Assert.False (null == e0);
            Assert.True (null != e0);

            Assert.AreEqual (e0.GetHashCode (), e1.GetHashCode ());
            Assert.AreNotEqual (e0.GetHashCode (), e2.GetHashCode ());
        }

        [Test]
        public void ServerRemovedEvent () {
            var e = Connection.TestService ().OnTimer (100);
            var ended = new List<Exception> ();
            var called = new ManualResetEvent (false);
            e.AddEndCallback (exn => {
                lock (ended)
                    ended.Add (exn);
                called.Set ();
            });
            lock (e.Condition) {
                Assert.True (e.Wait ());
                Assert.Throws<StreamRemovedException> (() => e.Wait ());
                Assert.Throws<StreamRemovedException> (() => e.Wait (0.1));
            }
            // The end callbacks run after the waiters wake
            Assert.True (called.WaitOne (10000));
            lock (ended) {
                Assert.AreEqual (1, ended.Count);
                Assert.IsInstanceOf<StreamRemovedException> (ended [0]);
            }
        }

        [Test]
        public void FiringBeforeWait () {
            var e = Connection.TestService ().OnTimer (100);
            Thread.Sleep (500);
            lock (e.Condition) {
                var timer = Stopwatch.StartNew ();
                Assert.True (e.Wait (1));
                Assert.Less (timer.ElapsedMilliseconds, 100);
            }
        }

        [Test]
        public void FiringsCoalesce () {
            var e = Connection.TestService ().OnTimer (50, 3);
            Thread.Sleep (500);
            lock (e.Condition) {
                Assert.True (e.Wait ());
                Assert.Throws<StreamRemovedException> (() => e.Wait ());
            }
        }

        [Test]
        public void TwoThreadsWaitOnOneEvent () {
            var e = Connection.TestService ().OnTimer (1000);
            var woke = 0;
            var threads = new List<Thread> ();
            for (var i = 0; i < 2; i++) {
                var thread = new Thread (() => {
                    lock (e.Condition) {
                        if (e.Wait (5))
                            Interlocked.Increment (ref woke);
                    }
                });
                thread.IsBackground = true;
                thread.Start ();
                threads.Add (thread);
            }
            foreach (var thread in threads)
                Assert.True (thread.Join (10000));
            Assert.AreEqual (2, woke);
        }

        [Test]
        public void CallbackAfterAWaitIsNotCalled () {
            var e = Connection.TestService ().OnTimer (500, 2);
            lock (e.Condition)
                Assert.True (e.Wait ());
            var count = 0;
            e.AddCallback (() => Interlocked.Increment (ref count));
            Assert.AreEqual (0, count);
        }

        [Test]
        public void CallbackAfterAnUnseenFiringIsCalledOnce () {
            var e = Connection.TestService ().OnTimer (500, 2);
            // Between the two firings
            Thread.Sleep (750);
            var count = 0;
            e.AddCallback (() => Interlocked.Increment (ref count));
            Assert.AreEqual (1, count);
        }

        [Test]
        public void CallbackAfterAFiringPassedToACallbackIsNotCalled () {
            var e = Connection.TestService ().OnTimer (500, 2);
            var called = new ManualResetEvent (false);
            e.AddCallback (() => called.Set ());
            Assert.True (called.WaitOne (10000));
            var count = 0;
            e.AddCallback (() => Interlocked.Increment (ref count));
            Assert.AreEqual (0, count);
        }

        /// <summary>
        /// An event over an id the server has not issued, so no update arrives for it.
        /// </summary>
        Event UnissuedEvent (ulong id)
        {
            return new Event (Connection, new KRPC.Schema.KRPC.Event {
                Stream = new KRPC.Schema.KRPC.Stream { Id = id }
            });
        }

        void Fire (ulong id)
        {
            Connection.StreamManager.Update (id, new KRPC.Schema.KRPC.ProcedureResult {
                Value = Google.Protobuf.ByteString.CopyFrom (0x01)
            });
        }

        [Test]
        public void AFiringPassedToAValueCallbackIsReplayed () {
            var e = UnissuedEvent (1000021);
            var values = new List<bool> ();
            e.Stream.AddCallback (values.Add);
            Fire (1000021);
            CollectionAssert.AreEqual (new [] { true }, values);
            var count = 0;
            e.AddCallback (() => count++);
            Assert.AreEqual (1, count);
        }

        [Test]
        public void ACallbackRemovedBeforeItsReplayTakesNoFiring () {
            var stream = UnissuedEvent (1000022).Stream.stream;
            Fire (1000022);
            var calls = new List<int> ();
            int key;
            long replay;
            var callback = stream.RegisterEventCallback (() => calls.Add (1), 0, out key, out replay);
            Assert.AreEqual (1, replay);
            // Stands in for a removal between the reservation and the replay
            stream.RemoveCallback (key);
            stream.Replay (callback, replay);
            stream.AddEventCallback (() => calls.Add (2), 0);
            CollectionAssert.AreEqual (new [] { 2 }, calls);
        }

        [Test]
        public void TwoLateCallbacksDoNotBothReplayAFiring () {
            var stream = UnissuedEvent (1000023).Stream.stream;
            Fire (1000023);
            var calls = new List<int> ();
            int key;
            long replay;
            var callback = stream.RegisterEventCallback (() => calls.Add (1), 0, out key, out replay);
            // Stands in for a second add while the first replay is in flight
            stream.AddEventCallback (() => calls.Add (2), 0);
            stream.Replay (callback, replay);
            CollectionAssert.AreEqual (new [] { 1 }, calls);
        }

        [Test]
        public void ARemovedCallbackDoesNotTakeAFiring () {
            var manager = Connection.StreamManager;
            var stream = manager.GetStream (TypeSpec.For (typeof (bool)), 1000020);
            var removed = 0;
            var key = stream.AddEventCallback (() => removed++, 0);
            var callback = stream.Callbacks [key];
            stream.RemoveCallback (key);
            manager.Update (1000020, new KRPC.Schema.KRPC.ProcedureResult {
                Value = Google.Protobuf.ByteString.CopyFrom (0x01)
            });
            // Stands in for an update thread that copied the callback before its removal
            stream.Call (callback, 1, true);
            var called = 0;
            stream.AddEventCallback (() => called++, 0);
            Assert.AreEqual (0, removed);
            Assert.AreEqual (1, called);
        }

        [Test]
        public void ErrorEndsTheEvent () {
            var expr = Expr.Equal (Connection,
                Expr.Call (Connection,
                    Connection.GetCall (() => Connection.TestService ().ThrowInvalidOperationException ())),
                Expr.ConstantInt (Connection, 0));
            var e = Connection.KRPC ().AddEvent (expr);
            lock (e.Condition) {
                var exn = Assert.Throws<System.InvalidOperationException> (() => e.Wait ());
                Assert.AreSame (exn, Assert.Catch (() => e.Wait ()));
            }
        }

        [Test]
        public void RemoveWakesAnEventWait () {
            var e = Connection.TestService ().OnTimer (10000);
            var raised = new ManualResetEvent (false);
            var thread = new Thread (() => {
                lock (e.Condition) {
                    try {
                        e.Wait ();
                    } catch (System.InvalidOperationException) {
                        raised.Set ();
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start ();
            Thread.Sleep (200);
            e.Remove ();
            Assert.True (raised.WaitOne (10000));
        }

        [Test]
        public void CloseWakesAnEventWait () {
            var connection = Connect ("CSharpClientTest");
            var e = connection.TestService ().OnTimer (10000);
            var raised = new ManualResetEvent (false);
            var thread = new Thread (() => {
                lock (e.Condition) {
                    try {
                        e.Wait ();
                    } catch (ConnectionException) {
                        raised.Set ();
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start ();
            Thread.Sleep (200);
            connection.Dispose ();
            Assert.True (raised.WaitOne (10000));
        }

        [Test]
        public void ServerDisconnectWakesAnEventWait () {
            using (var connection = Connect ("CSharpClientTest")) {
                var e = connection.TestService ().OnTimer (10000);
                var raised = new ManualResetEvent (false);
                var thread = new Thread (() => {
                    lock (e.Condition) {
                        try {
                            e.Wait ();
                        } catch (ConnectionException) {
                            raised.Set ();
                        }
                    }
                });
                thread.IsBackground = true;
                thread.Start ();
                Thread.Sleep (200);
                // Stands in for the server closing the connection
                var field = typeof (Connection).GetField (
                    "streamSocket", BindingFlags.NonPublic | BindingFlags.Instance);
                ((Socket)field.GetValue (connection)).Shutdown (SocketShutdown.Both);
                Assert.True (raised.WaitOne (10000));
            }
        }

        [Test]
        public void AnInvokeWithoutAReturnTypeNotesAnEventId ()
        {
            using (var connection = Connect ("CSharpClientTest")) {
                var result = connection.Invoke ("TestService", "OnTimer", new [] {
                    Encoder.Encode (100000u, TypeSpec.For (typeof (uint))),
                    Encoder.Encode (1u, TypeSpec.For (typeof (uint)))
                });
                var id = KRPC.Schema.KRPC.Event.Parser.ParseFrom (result.Value).Stream.Id;
                Assert.IsTrue (connection.StreamManager.Keeps (id));
                connection.KRPC ().RemoveStream (id);
            }
        }

        [Test]
        public void TheListedReturnTypesAreStreamsAndEvents ()
        {
            var types = Connection.ListStreamReturnTypes ();
            Assert.AreEqual (typeof (Event), types ["TestService.OnTimer"]);
            Assert.AreEqual (typeof (KRPC.Schema.KRPC.Stream), types ["KRPC.AddStream"]);
            Assert.IsFalse (types.ContainsKey ("TestService.FloatToString"));
        }
    }
}

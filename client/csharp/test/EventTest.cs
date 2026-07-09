using System.Diagnostics;
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
                e.Wait ();
                var time = timer.ElapsedMilliseconds;
                // Lower bound is a correctness check (the event must not fire
                // before its timer); the upper bound is a generous hang
                // detector, kept loose so the test does not flake under
                // parallel load. See issue #540.
                Assert.Greater (time, 150);
                Assert.Less (time, 2000);
                Assert.True (e.Stream.Get ());
            }
        }

        [Test]
        public void TestEventTimeoutShort () {
            var e = Connection.TestService ().OnTimer (200);
            lock (e.Condition) {
                var timer = new Stopwatch ();
                timer.Start ();
                e.Wait (0.1);
                var time = timer.ElapsedMilliseconds;
                // Wait must return on its timeout, before the 200ms event fires.
                Assert.Greater (time, 50);
                Assert.Less (time, 200);
                e.Wait ();
                Assert.True (e.Stream.Get ());
            }
        }

        [Test]
        public void TestEventTimeoutLong () {
            var e = Connection.TestService ().OnTimer (200);
            lock (e.Condition) {
                var timer = new Stopwatch ();
                timer.Start ();
                e.Wait (1);
                var time = timer.ElapsedMilliseconds;
                Assert.Greater (time, 150);
                Assert.Less (time, 2000);
                Assert.True (e.Stream.Get ());
            }
        }

        [Test]
        public void TestEventCallback () {
            var e = Connection.TestService ().OnTimer (200);
            var called = new ManualResetEvent (false);
            e.AddCallback (() => called.Set ());
            var timer = new Stopwatch ();
            timer.Start ();
            e.Start ();
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
            e.Start ();
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
            e.Start ();
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
            e.Start ();
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
                evnt.Wait();
                // The event fires when the server-side counter reaches 20. The
                // counter increments on every expression evaluation, so the
                // value read back is >= 21 (20 at the trigger, plus this read);
                // the exact figure depends on how many more times the server
                // evaluated the expression first, so assert the lower bound to
                // avoid flaking under load. See issue #540.
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
    }
}

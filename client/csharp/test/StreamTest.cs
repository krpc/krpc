using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using KRPC.Client.Services.TestService;
using Google.Protobuf;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class StreamTest : ServerTestCase
    {
        static void Wait ()
        {
            Thread.Sleep (50);
        }

        [Test]
        public void Method ()
        {
            var x = Connection.AddStream (() => Connection.TestService ().FloatToString (3.14159f));
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual ("3.14159", x.Get ());
                Wait ();
            }
        }

        [Test]
        public void Property ()
        {
            Connection.TestService ().StringProperty = "foo";
            var x = Connection.AddStream (() => Connection.TestService ().StringProperty);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual ("foo", x.Get ());
                Wait ();
            }
        }

        [Test]
        public void ClassMethod ()
        {
            var obj = Connection.TestService ().CreateTestObject ("bob");
            var x = Connection.AddStream (() => obj.FloatToString (3.14159f));
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual ("bob3.14159", x.Get ());
                Wait ();
            }
        }

        [Test]
        public void ClassStaticMethod ()
        {
            // Note: have to specify optional parameter "" in expression trees
            var x = Connection.AddStream (() => TestClass.StaticMethod (Connection, "foo", string.Empty));
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual ("jebfoo", x.Get ());
                Wait ();
            }
        }

        [Test]
        public void ClassProperty ()
        {
            var obj = Connection.TestService ().CreateTestObject ("jeb");
            obj.IntProperty = 42;
            var x = Connection.AddStream (() => obj.IntProperty);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual (42, x.Get ());
                Wait ();
            }
        }

        [Test]
        public void MultiplyByOne ()
        {
            var obj = Connection.TestService ().CreateTestObject ("jeb");
            obj.IntProperty = 42;
            var x = Connection.AddStream (() => obj.IntProperty * 1);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual (42, x.Get ());
                Wait ();
            }
            Assert.AreEqual (
                Connection.AddStream (() => obj.IntProperty), x);
            var call = Connection.GetCall (() => obj.IntProperty * 1);
            Assert.AreEqual (
                Connection.GetCall (() => obj.IntProperty).Procedure, call.Procedure);
            Assert.Throws<ArgumentException> (
                () => Connection.GetCall (() => obj.IntProperty * 2));
        }

        [Test]
        public void MultiplyByConstant ()
        {
            var obj = Connection.TestService ().CreateTestObject ("jeb");
            obj.IntProperty = 42;
            var x = Connection.AddStream (() => obj.IntProperty * 2);
            var y = Connection.AddStream (() => 3 * obj.IntProperty);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual (84, x.Get ());
                Assert.AreEqual (126, y.Get ());
                Wait ();
            }
            Assert.AreNotEqual (
                Connection.AddStream (() => obj.IntProperty), x);
            Assert.AreEqual (
                Connection.AddStream (() => obj.IntProperty * 2), x);
            Assert.AreEqual (
                Connection.AddStream (() => 2 * obj.IntProperty), x);
        }

        [Test]
        public void MultiplyPromotesType ()
        {
            var obj = Connection.TestService ().CreateTestObject ("jeb");
            obj.IntProperty = 42;
            var x = Connection.AddStream (() => obj.IntProperty * 1.0);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual (42.0, x.Get ());
                Wait ();
            }
        }

        [Test]
        public void MultiplyMethodCall ()
        {
            var x = Connection.AddStream (
                () => Connection.TestService ().StringToInt32 ("42") * 1);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual (42, x.Get ());
                Wait ();
            }
        }

        [Test]
        public void MultiplyTwoRemoteCalls ()
        {
            var obj = Connection.TestService ().CreateTestObject ("jeb");
            obj.IntProperty = 42;
            var exn = Assert.Throws<ArgumentException> (
                          () => Connection.AddStream (() => obj.IntProperty * obj.IntProperty));
            Assert.That (exn.Message, Does.Contain ("Cannot multiply two remote calls"));
        }

        [Test]
        public void MultiplyByVariable ()
        {
            var obj = Connection.TestService ().CreateTestObject ("jeb");
            obj.IntProperty = 42;
            int n = 1;
            var exn = Assert.Throws<ArgumentException> (
                          () => Connection.AddStream (() => obj.IntProperty * n));
            Assert.That (exn.Message, Does.Contain ("The factor must be a constant"));
            n = 2;
            Assert.Throws<ArgumentException> (
                () => Connection.AddStream (() => obj.IntProperty * n));
        }

        [Test]
        public void NullableValue ()
        {
            var x = Connection.AddStream (() => Connection.TestService ().EchoNullableInt (null));
            var y = Connection.AddStream (() => Connection.TestService ().EchoNullableString (null));
            for (int i = 0; i < 5; i++) {
                Assert.IsNull (x.Get ());
                Assert.IsNull (y.Get ());
                Wait ();
            }
        }

        [Test]
        public void NullableListElements ()
        {
            // A stream carries the value itself, so a null inside a collection reaches the
            // decoder as a presence bool where a null result does not
            var value = new List<int?> { 1, null, 3 };
            var x = Connection.AddStream (
                () => Connection.TestService ().EchoListOfNullableInts (value));
            for (int i = 0; i < 5; i++) {
                CollectionAssert.AreEqual (value, x.Get ());
                Wait ();
            }
        }

        [Test]
        public void NullableObjectListElements ()
        {
            // A stream is built from an expression, which has only the C# types of the
            // values it passes. A nullable element of a class type is invisible there, and
            // is read from the specs the generated service declares
            var obj = Connection.TestService ().CreateTestObject ("bob");
            var value = new List<TestClass> { obj, null };
            var x = Connection.AddStream (
                () => Connection.TestService ().EchoListOfNullableObjects (value));
            for (int i = 0; i < 5; i++) {
                CollectionAssert.AreEqual (value, x.Get ());
                Wait ();
            }
        }

        [Test]
        public void Counter ()
        {
            var count = 0;
            var x = Connection.AddStream (() => Connection.TestService ().Counter ("StreamTest.Counter", 1));
            for (int i = 0; i < 5; i++) {
                int repeat = 0;
                while (count == x.Get () && repeat < 1000) {
                    Wait ();
                    repeat++;
                }
                Assert.IsTrue (count < x.Get ());
                count = x.Get ();
                Wait ();
            }
        }

        [Test]
        public void Nested ()
        {
            var x0 = Connection.AddStream (() => Connection.TestService ().FloatToString (0.123f));
            var x1 = Connection.AddStream (() => Connection.TestService ().FloatToString (1.234f));
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual ("0.123", x0.Get ());
                Assert.AreEqual ("1.234", x1.Get ());
                Wait ();
            }
        }

        [Test]
        public void Interleaved ()
        {
            var s0 = Connection.AddStream (() => Connection.TestService ().Int32ToString (0));
            Assert.AreEqual ("0", s0.Get ());

            Wait ();
            Assert.AreEqual ("0", s0.Get ());

            var s1 = Connection.AddStream (() => Connection.TestService ().Int32ToString (1));
            Assert.AreEqual ("0", s0.Get ());
            Assert.AreEqual ("1", s1.Get ());

            Wait ();
            Assert.AreEqual ("0", s0.Get ());
            Assert.AreEqual ("1", s1.Get ());

            s1.Remove ();
            Assert.AreEqual ("0", s0.Get ());
            Assert.Throws<InvalidOperationException> (() => s1.Get ());

            Wait ();
            Assert.AreEqual ("0", s0.Get ());
            Assert.Throws<InvalidOperationException> (() => s1.Get ());

            var s2 = Connection.AddStream (() => Connection.TestService ().Int32ToString (2));
            Assert.AreEqual ("0", s0.Get ());
            Assert.Throws<InvalidOperationException> (() => s1.Get ());
            Assert.AreEqual ("2", s2.Get ());

            Wait ();
            Assert.AreEqual ("0", s0.Get ());
            Assert.Throws<InvalidOperationException> (() => s1.Get ());
            Assert.AreEqual ("2", s2.Get ());

            s0.Remove ();
            Assert.Throws<InvalidOperationException> (() => s0.Get ());
            Assert.Throws<InvalidOperationException> (() => s1.Get ());
            Assert.AreEqual ("2", s2.Get ());

            Wait ();
            Assert.Throws<InvalidOperationException> (() => s0.Get ());
            Assert.Throws<InvalidOperationException> (() => s1.Get ());
            Assert.AreEqual ("2", s2.Get ());

            s2.Remove ();
            Assert.Throws<InvalidOperationException> (() => s0.Get ());
            Assert.Throws<InvalidOperationException> (() => s1.Get ());
            Assert.Throws<InvalidOperationException> (() => s2.Get ());

            Wait ();
            Assert.Throws<InvalidOperationException> (() => s0.Get ());
            Assert.Throws<InvalidOperationException> (() => s1.Get ());
            Assert.Throws<InvalidOperationException> (() => s2.Get ());
        }

        [Test]
        public void RemoveStreamTwice ()
        {
            var s = Connection.AddStream (() => Connection.TestService ().Int32ToString (0));
            Assert.AreEqual ("0", s.Get ());

            Wait ();
            Assert.AreEqual ("0", s.Get ());

            s.Remove ();
            Assert.Throws<InvalidOperationException> (() => s.Get ());
            s.Remove ();
            Assert.Throws<InvalidOperationException> (() => s.Get ());
        }

        [Test]
        public void RemoveThroughAStaleHandle ()
        {
            var s0 = Connection.AddStream (() => Connection.TestService ().Int32ToString (44));
            Assert.AreEqual ("44", s0.Get ());
            s0.Remove ();
            // The server can give the same id to the stream added again, while it removes the first
            var s1 = Connection.AddStream (() => Connection.TestService ().Int32ToString (44));
            s0.Remove ();
            // Run on a thread so a regression fails here instead of hanging the suite
            string value = null;
            var thread = new Thread (() => value = s1.Get ());
            thread.IsBackground = true;
            thread.Start ();
            Assert.IsTrue (thread.Join (30000));
            Assert.AreEqual ("44", value);
            Assert.Throws<InvalidOperationException> (() => s0.Get ());
            s1.Remove ();
        }

        [Test]
        public void StartStreamRemovedByAnotherHandle ()
        {
            var s0 = Connection.AddStream (() => Connection.TestService ().Int32ToString (43));
            var s1 = Connection.AddStream (() => Connection.TestService ().Int32ToString (43));
            s0.Remove ();
            // Run on a thread so a regression fails here instead of hanging the suite
            Exception get = null;
            Exception start = null;
            var thread = new Thread (() => {
                try {
                    s1.Get ();
                } catch (Exception exn) {
                    get = exn;
                }
                try {
                    s1.Start ();
                } catch (Exception exn) {
                    start = exn;
                }
            });
            thread.IsBackground = true;
            thread.Start ();
            Assert.IsTrue (thread.Join (30000));
            Assert.IsInstanceOf<InvalidOperationException> (get);
            Assert.IsInstanceOf<InvalidOperationException> (start);
        }

        [Test]
        public void CallbackIsNotPassedAnError ()
        {
            Connection.TestService ().ResetInvalidOperationExceptionLater ();
            var s = Connection.AddStream (
                () => Connection.TestService ().ThrowInvalidOperationExceptionLater ());
            var received = new List<int> ();
            s.AddCallback (value => {
                lock (received)
                    received.Add (value);
            });
            var done = new ManualResetEvent (false);
            s.AddEndCallback (exn => done.Set ());
            s.Start (false);
            Assert.IsTrue (done.WaitOne (30000));
            Assert.Throws<InvalidOperationException> (() => s.Get ());
            lock (received)
                CollectionAssert.AreEqual (new [] { 0 }, received);
        }

        [Test]
        public void AddStreamTwice ()
        {
            var s0 = Connection.AddStream (() => Connection.TestService ().Int32ToString (42));
            Assert.AreEqual ("42", s0.Get ());
            Wait ();
            Assert.AreEqual ("42", s0.Get ());

            var s1 = Connection.AddStream (() => Connection.TestService ().Int32ToString (42));
            Assert.AreEqual (s0, s1);
            Assert.AreEqual ("42", s0.Get ());
            Assert.AreEqual ("42", s1.Get ());
            Wait ();
            Assert.AreEqual ("42", s0.Get ());
            Assert.AreEqual ("42", s1.Get ());

            var s2 = Connection.AddStream (() => Connection.TestService ().Int32ToString (43));
            Assert.AreNotEqual (s0, s2);
            Assert.AreEqual ("42", s0.Get ());
            Assert.AreEqual ("42", s1.Get ());
            Assert.AreEqual ("43", s2.Get ());
            Wait ();
            Assert.AreEqual ("42", s0.Get ());
            Assert.AreEqual ("42", s1.Get ());
            Assert.AreEqual ("43", s2.Get ());
        }

        [Test]
        public void InvalidOperationExceptionImmediately ()
        {
            var s = Connection.AddStream (
                () => Connection.TestService ().ThrowInvalidOperationException ());
            Assert.Throws<InvalidOperationException> (() => s.Get ());
        }

        [Test]
        public void InvalidOperationExceptionLater ()
        {
            Connection.TestService ().ResetInvalidOperationExceptionLater ();
            var s = Connection.AddStream (
                () => Connection.TestService ().ThrowInvalidOperationExceptionLater());
            Assert.AreEqual (0, s.Get ());
            Assert.Throws<InvalidOperationException> (
                () => {
                    while (true) {
                        Wait ();
                        s.Get ();
                    }
                });
        }

        [Test]
        public void CustomExceptionImmediately ()
        {
            var s = Connection.AddStream (
                () => Connection.TestService ().ThrowCustomException ());
            var exn = Assert.Throws<CustomException> (() => s.Get ());
            Assert.That (exn.Message, Does.Contain ("A custom kRPC exception"));
        }

        [Test]
        public void CustomExceptionLater ()
        {
            Connection.TestService ().ResetCustomExceptionLater ();
            var s = Connection.AddStream (
                () => Connection.TestService ().ThrowCustomExceptionLater());
            Assert.AreEqual (0, s.Get ());
            var exn = Assert.Throws<CustomException> (
                () => {
                    while (true) {
                        Wait ();
                        s.Get ();
                    }
                });
            Assert.That (exn.Message, Does.Contain ("A custom kRPC exception"));
        }

        [Test]
        public void YieldException ()
        {
            var s = Connection.AddStream (
                () => Connection.TestService ().BlockingProcedure(10, 0));
            for (var i = 0; i < 100; i++) {
                Assert.AreEqual (55, s.Get ());
                Wait ();
            }
        }

        [Test]
        public void TestWait () {
            var x = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestWait", 10));
            lock (x.Condition) {
                var count = x.Get ();
                Assert.Less(count, 10);
                while (count < 10) {
                    x.Wait ();
                    count++;
                    Assert.AreEqual(count, x.Get ());
                }
            }
        }

        [Test]
        public void TestWaitTimeoutShort () {
            var x = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestWaitTimeoutShort", 10));
            lock (x.Condition) {
                var count = x.Get ();
                Assert.False (x.Wait (0));
                Assert.AreEqual(count, x.Get ());
            }
        }

        [Test]
        public void TestWaitTimeoutLong () {
            var x = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestWaitTimeoutLong", 10));
            lock (x.Condition) {
                var count = x.Get ();
                Assert.Less(count, 10);
                while (count < 10) {
                    Assert.True (x.Wait (10));
                    count++;
                    Assert.AreEqual(count, x.Get ());
                }
            }
        }

        [Test]
        public void TestCallback () {
            var stop = new ManualResetEvent (false);
            var error = false;
            int value = -1;
            var s = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestCallback", 10));
            s.AddCallback (
                (int x) => {
                    if (x > 5) {
                        stop.Set ();
                    } else if (value+1 != x) {
                        error = true;
                        stop.Set ();
                    } else {
                        value++;
                    }
                });

            s.Start();
            stop.WaitOne ();
            s.Remove();
            Assert.False(error);
            Assert.AreEqual(value, 5);
        }

        [Test]
        public void TestCallbackThatThrows () {
            // A callback that throws must not take the update thread down with it, which
            // would stop every stream on the connection. The timeout catches that, as the
            // wait below would otherwise never be satisfied.
            var stop = new ManualResetEvent (false);
            var called = false;
            var s = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestCallbackThatThrows", 10));
            s.AddCallback ((int x) => {
                called = true;
                throw new System.InvalidOperationException ("callback failed");
            });
            s.AddCallback ((int x) => {
                if (x > 5)
                    stop.Set ();
            });
            s.Start ();
            Assert.IsTrue (stop.WaitOne (30000));
            s.Remove ();
            Assert.IsTrue (called);
        }

        [Test]
        public void TestRemoveWhileWaitingOnCondition () {
            // The update thread must not hold the update lock while waiting for a stream's
            // condition. A caller holding that condition, which is how waiting for an update
            // is documented to work, otherwise blocks forever on anything needing the update
            // lock, and the update thread blocks on the condition, deadlocking both. Run on a
            // thread so a regression fails here instead of hanging the suite.
            var done = new ManualResetEvent (false);
            var thread = new Thread (() => {
                var s = Connection.AddStream (
                    () => Connection.TestService ().Counter (
                        "StreamTest.TestRemoveWhileWaitingOnCondition", 1));
                s.Start ();
                lock (s.Condition) {
                    s.Wait ();
                    // Let the update thread reach the next update and block on this condition
                    Thread.Sleep (200);
                    s.Remove ();
                }
                done.Set ();
            });
            thread.IsBackground = true;
            thread.Start ();
            Assert.IsTrue (done.WaitOne (30000));
        }

        [Test]
        public void TestRemoveCallback () {
            var stop = new ManualResetEvent (false);
            var called1 = false;
            var called2 = false;
            var s = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestRemoveCallback", 10));
            s.AddCallback ((int x) => {
                called1 = true;
                stop.Set ();
            });
            int callback2Tag = s.AddCallback (
                (int x) => called2 = true
            );
            s.RemoveCallback (callback2Tag);
            s.Start();
            stop.WaitOne ();
            s.Remove();
            Assert.IsTrue(called1);
            Assert.IsFalse(called2);
        }

        [Test]
        public void TestRate () {
            var stop = new ManualResetEvent (false);
            var error = false;
            int value = 0;
            var s = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestRate", 1));
            s.AddCallback (
                (int x) => {
                    if (x > 5) {
                        stop.Set ();
                    } else if (value+1 != x) {
                        error = true;
                        stop.Set ();
                    } else {
                        value++;
                    }
                });
            s.Rate = 5;

            s.Start();
            var timer = new Stopwatch();
            timer.Start();
            stop.WaitOne ();
            s.Remove();
            var elapsed = timer.ElapsedMilliseconds;
            // Lower bound checks the rate limit was honored. The upper bound is a generous
            // hang detector, kept loose so the test does not flake under parallel load.
            Assert.Greater(elapsed, 1000);
            Assert.Less(elapsed, 3000);
            Assert.False(error);
            Assert.AreEqual(value, 5);
        }

        [Test]
        public void TestEquality () {
            var s0 = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestEquality0", 1));
            var s1 = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestEquality0", 1));
            var s2 = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestEquality1", 1));

            Assert.True (s0.Equals (s0));

            Assert.True (s0.Equals (s1));
            Assert.True (s0 == s1);
            Assert.False (s0 != s1);

            Assert.False (s0.Equals (s2));
            Assert.False (s0 == s2);
            Assert.True (s0 != s2);

            Assert.False (s0.Equals (null));
            Assert.False (s0 == null);
            Assert.True (s0 != null);
            Assert.False (null == s0);
            Assert.True (null != s0);

            Assert.AreEqual (s0.GetHashCode (), s1.GetHashCode ());
            Assert.AreNotEqual (s0.GetHashCode (), s2.GetHashCode ());
        }

        [Test]
        public void ErrorEndsTheStream ()
        {
            var s = Connection.AddStream (
                () => Connection.TestService ().ThrowInvalidOperationException ());
            var ended = new List<Exception> ();
            var called = new ManualResetEvent (false);
            s.AddEndCallback (exn => {
                lock (ended)
                    ended.Add (exn);
                called.Set ();
            });
            lock (s.Condition) {
                Assert.Throws<InvalidOperationException> (() => s.Wait ());
                Assert.Throws<InvalidOperationException> (() => s.Wait (1));
            }
            Assert.Throws<InvalidOperationException> (() => s.Get ());
            // The end callbacks run after the waiters wake
            Assert.IsTrue (called.WaitOne (10000));
            lock (ended) {
                Assert.AreEqual (1, ended.Count);
                Assert.IsInstanceOf<InvalidOperationException> (ended [0]);
            }
            // Added after the end, so called straight away
            s.AddEndCallback (exn => {
                lock (ended)
                    ended.Add (exn);
            });
            lock (ended)
                Assert.AreEqual (2, ended.Count);
            // Already removed by the server, so this does nothing
            s.Remove ();
            Assert.Throws<InvalidOperationException> (() => s.Get ());
        }

        [Test]
        public void EndCallbackOnRemove ()
        {
            var s = Connection.AddStream (() => Connection.TestService ().Int32ToString (46));
            Assert.AreEqual ("46", s.Get ());
            var ended = new List<Exception> ();
            s.AddEndCallback (exn => ended.Add (exn));
            s.Remove ();
            Assert.AreEqual (1, ended.Count);
            Assert.IsInstanceOf<InvalidOperationException> (ended [0]);
        }

        [Test]
        public void EndCallbackAddedAfterTheEnd ()
        {
            var s = Connection.AddStream (() => Connection.TestService ().Int32ToString (55));
            s.Remove ();
            var ended = new List<Exception> ();
            s.AddEndCallback (exn => ended.Add (exn));
            Assert.AreEqual (1, ended.Count);
            Assert.IsInstanceOf<InvalidOperationException> (ended [0]);
        }

        [Test]
        public void AValueThatFailsToDecodeEndsOnlyItsStream ()
        {
            var manager = Connection.StreamManager;
            var type = TypeSpec.For (typeof (uint));
            // Ids the server never sends a result for
            var failing = manager.GetStream (type, 1000001);
            var working = manager.GetStream (type, 1000002);
            var ended = new List<Exception> ();
            failing.AddEndCallback (exn => ended.Add (exn));
            // A varint that never ends
            manager.Update (1000001, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0xff) });
            manager.Update (1000002, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x2a) });
            Assert.AreEqual (1, ended.Count);
            Assert.Throws<ArgumentException> (() => { var unused = failing.Value; });
            Assert.AreEqual (42u, working.Value);
        }

        [Test]
        public void RemoveWakesAStreamWait ()
        {
            var x = Connection.AddStream (() => Connection.TestService ().Int32ToString (48));
            Assert.AreEqual ("48", x.Get ());
            var raised = new ManualResetEvent (false);
            var thread = new Thread (() => {
                lock (x.Condition) {
                    try {
                        x.Wait ();
                    } catch (InvalidOperationException) {
                        raised.Set ();
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start ();
            Thread.Sleep (200);
            x.Remove ();
            Assert.IsTrue (raised.WaitOne (10000));
        }

        [Test]
        public void CloseWakesAStreamWait ()
        {
            var connection = Connect ("CSharpClientTest");
            var x = connection.AddStream (() => connection.TestService ().Int32ToString (47));
            Assert.AreEqual ("47", x.Get ());
            var raised = new ManualResetEvent (false);
            var thread = new Thread (() => {
                lock (x.Condition) {
                    try {
                        x.Wait ();
                    } catch (ConnectionException) {
                        raised.Set ();
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start ();
            Thread.Sleep (200);
            connection.Dispose ();
            Assert.IsTrue (raised.WaitOne (10000));
            Assert.Throws<ConnectionException> (() => x.Get ());
            // Ended by the close, so this does nothing
            x.Remove ();
        }

        [Test]
        public void ConnectionFailureWakesAWaitingThread ()
        {
            using (var connection = Connect ("CSharpClientTest")) {
                var x = connection.AddStream (() => connection.TestService ().Int32ToString (47));
                Assert.AreEqual ("47", x.Get ());
                var ended = 0;
                var called = new ManualResetEvent (false);
                x.AddEndCallback (exn => {
                    Interlocked.Increment (ref ended);
                    called.Set ();
                });
                var raised = new ManualResetEvent (false);
                var thread = new Thread (() => {
                    lock (x.Condition) {
                        try {
                            x.Wait ();
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
                Assert.IsTrue (raised.WaitOne (10000));
                Assert.IsTrue (called.WaitOne (10000));
                Assert.AreEqual (1, ended);
                Assert.Throws<ConnectionException> (() => x.Get ());
            }
        }
    }
}

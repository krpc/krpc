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
        public void NoStreamServer ()
        {
            using (var connection = Connect ("CSharpClientTestNoStreamServer", stream: null)) {
                var exn = Assert.Throws<InvalidOperationException> (
                    () => connection.AddStream (() => connection.TestService ().FloatToString (3.14159f)));
                Assert.AreEqual ("The connection has no stream server", exn.Message);
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
            var call = Connection.GetCall (() => obj.IntProperty * 1);
            Assert.AreEqual (
                Connection.GetCall (() => obj.IntProperty).Procedure, call.Procedure);
            Assert.Throws<ArgumentException> (
                () => Connection.GetCall (() => obj.IntProperty * 2));
        }

        [Test]
        public void NarrowingConversionIsComputed ()
        {
            var obj = Connection.TestService ().CreateTestObject ("jeb");
            obj.IntProperty = 16777217;
            var x = Connection.AddStream (() => (int)(float)obj.IntProperty);
            Assert.AreEqual (16777216, x.Get ());
            Assert.AreNotEqual (Connection.AddStream (() => obj.IntProperty), x);
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
            Assert.AreEqual (84, Connection.AddStream (() => 2 * obj.IntProperty).Get ());
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
            var x = Connection.AddStream (() => obj.IntProperty * obj.IntProperty);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual (42 * 42, x.Get ());
                Wait ();
            }
        }

        [Test]
        public void MultiplyByVariable ()
        {
            var obj = Connection.TestService ().CreateTestObject ("jeb");
            obj.IntProperty = 42;
            int n = 2;
            var x = Connection.AddStream (() => obj.IntProperty * n);
            // The variable is captured when the stream is created
            n = 3;
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual (84, x.Get ());
                Wait ();
            }
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
        public void RemoveThenAdd ()
        {
            var s0 = Connection.AddStream (() => Connection.TestService ().Int32ToString (44));
            Assert.AreEqual ("44", s0.Get ());
            s0.Remove ();
            var s1 = Connection.AddStream (() => Connection.TestService ().Int32ToString (44));
            // Removing the first again leaves the stream added since
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
            Assert.IsTrue (done.WaitOne (30000));
            Assert.Throws<InvalidOperationException> (() => s.Get ());
            lock (received)
                CollectionAssert.AreEqual (new [] { 0 }, received);
        }

        [Test]
        public void AddStreamTwice ()
        {
            var s0 = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.AddStreamTwice", 1));
            var s1 = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.AddStreamTwice", 1));
            Assert.AreNotEqual (s0.stream.Id, s1.stream.Id);
            Assert.AreNotEqual (s0, s1);
            Assert.Greater (s0.Get (), 0);
            Assert.Greater (s1.Get (), 0);
            s0.Remove ();
            Assert.Throws<InvalidOperationException> (() => s0.Get ());
            lock (s1.Condition)
                Assert.IsTrue (s1.Wait (10));
            s1.Remove ();
        }

        [Test]
        public void ReadStraightAfterAdd ()
        {
            var s = Connection.AddStream (() => Connection.TestService ().Int32ToString (48));
            Assert.AreEqual ("48", s.Get ());
            s.Remove ();
        }

        [Test]
        public void CallbackAddedAfterTheFirstValue ()
        {
            var s = Connection.AddStream (() => Connection.TestService ().Int32ToString (49));
            Assert.AreEqual ("49", s.Get ());
            var values = new List<string> ();
            s.AddCallback (values.Add);
            CollectionAssert.AreEqual (new [] { "49" }, values);
            s.Remove ();
        }

        [Test]
        public void ACallbackIsNotCalledWithAnOlderValue ()
        {
            var manager = Connection.StreamManager;
            var stream = manager.GetStream (TypeSpec.For (typeof (uint)), 1000007);
            manager.Update (1000007, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x01) });
            manager.Update (1000007, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x02) });
            var values = new List<object> ();
            var key = stream.AddCallback (values.Add);
            // Stands in for the first call of a callback losing the race with an update
            stream.Call (stream.Callbacks [key], 1, 1u);
            manager.Update (1000007, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x03) });
            CollectionAssert.AreEqual (new object [] { 2u, 3u }, values);
        }

        [Test]
        public void AValueQueuedDuringTheFirstCallIsDeliveredAfterTheStreamEnds ()
        {
            var manager = Connection.StreamManager;
            var stream = manager.GetStream (TypeSpec.For (typeof (uint)), 1000012);
            manager.Update (1000012, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x01) });
            var values = new List<object> ();
            var running = new ManualResetEvent (false);
            var release = new ManualResetEvent (false);
            var adding = new Thread (() => stream.AddCallback (value => {
                values.Add (value);
                if (values.Count == 1) {
                    running.Set ();
                    release.WaitOne ();
                }
            }));
            adding.IsBackground = true;
            adding.Start ();
            Assert.IsTrue (running.WaitOne (10000));
            // Stands in for the update thread, while the first call runs
            manager.Update (1000012, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x02) });
            manager.Update (1000012, new KRPC.Schema.KRPC.ProcedureResult {
                Error = new KRPC.Schema.KRPC.Error { Description = "Ended" }
            });
            release.Set ();
            Assert.IsTrue (adding.Join (10000));
            CollectionAssert.AreEqual (new object [] { 1u, 2u }, values);
        }

        [Test]
        public void ACallbackIsNotCalledAfterTheStreamEnds ()
        {
            var manager = Connection.StreamManager;
            var stream = manager.GetStream (TypeSpec.For (typeof (uint)), 1000010);
            var values = new List<object> ();
            var key = stream.AddCallback (values.Add);
            manager.Update (1000010, new KRPC.Schema.KRPC.ProcedureResult {
                Error = new KRPC.Schema.KRPC.Error { Description = "Ended" }
            });
            // Stands in for a delivery that started before the ending
            stream.Call (stream.Callbacks [key], 1, 1u);
            CollectionAssert.IsEmpty (values);
        }

        static KRPC.Schema.KRPC.ProcedureResult ValueResult (byte value)
        {
            return new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (value) };
        }

        [Test]
        public void ValuesDuringAFirstCallAreQueued ()
        {
            var manager = Connection.StreamManager;
            var stream = manager.GetStream (TypeSpec.For (typeof (uint)), 1000013);
            manager.Update (1000013, ValueResult (0x01));
            var calls = new List<KeyValuePair<object, Thread>> ();
            var entered = new ManualResetEvent (false);
            var release = new ManualResetEvent (false);
            var adder = new Thread (() => stream.AddCallback (value => {
                calls.Add (new KeyValuePair<object, Thread> (value, Thread.CurrentThread));
                if ((uint)value == 1) {
                    entered.Set ();
                    release.WaitOne (10000);
                }
            }));
            adder.IsBackground = true;
            adder.Start ();
            Assert.IsTrue (entered.WaitOne (10000));
            var updater = new Thread (() => manager.Update (1000013, ValueResult (0x02)));
            updater.IsBackground = true;
            updater.Start ();
            // The update thread queues the value, with no wait for the first call
            Assert.IsTrue (updater.Join (5000));
            release.Set ();
            Assert.IsTrue (adder.Join (10000));
            CollectionAssert.AreEqual (new [] {
                new KeyValuePair<object, Thread> (1u, adder),
                new KeyValuePair<object, Thread> (2u, adder)
            }, calls);
        }

        [Test]
        public void CloseWhileAnUpdateWaitsForAFirstCall ()
        {
            var connection = Connect ("CSharpClientTest");
            var manager = connection.StreamManager;
            var stream = manager.GetStream (TypeSpec.For (typeof (uint)), 1000014);
            manager.Update (1000014, ValueResult (0x01));
            var entered = new ManualResetEvent (false);
            var release = new ManualResetEvent (false);
            var values = new List<object> ();
            var adder = new Thread (() => stream.AddCallback (value => {
                values.Add (value);
                entered.Set ();
                release.WaitOne (10000);
            }));
            adder.IsBackground = true;
            adder.Start ();
            Assert.IsTrue (entered.WaitOne (10000));
            var callback = stream.Callbacks [0];
            // Stands in for the window before the adding thread marks the first call
            callback.Adding = false;
            var updater = new Thread (() => stream.Call (callback, 2, 2u));
            updater.IsBackground = true;
            updater.Start ();
            Assert.IsFalse (updater.Join (100));
            connection.Dispose ();
            Assert.IsTrue (updater.Join (10000));
            release.Set ();
            Assert.IsTrue (adder.Join (10000));
            CollectionAssert.AreEqual (new object [] { 1u }, values);
        }

        [Test]
        public void ARemovedCallbackIsNotCalled ()
        {
            var stream = Connection.StreamManager.GetStream (TypeSpec.For (typeof (uint)), 1000015);
            var values = new List<object> ();
            var key = stream.AddCallback (values.Add);
            var callback = stream.Callbacks [key];
            stream.RemoveCallback (key);
            stream.Call (callback, 1, 1u);
            CollectionAssert.IsEmpty (values);
        }

        [Test]
        public void ACloserHoldingTheConditionKeepsAServerEnding ()
        {
            var connection = Connect ("CSharpClientTest");
            var manager = connection.StreamManager;
            var stream = manager.GetStream (TypeSpec.For (typeof (uint)), 1000016);
            var ending = new KRPC.Schema.KRPC.ProcedureResult {
                Error = new KRPC.Schema.KRPC.Error { Description = "Ended by the server" }
            };
            lock (stream.Condition) {
                var updater = new Thread (() => manager.Update (1000016, ending));
                updater.IsBackground = true;
                updater.Start ();
                // Closed once the ending is decoded, while the updating thread waits for the
                // condition the closing thread holds
                var timer = Stopwatch.StartNew ();
                while (manager.Registered (1000016) && timer.ElapsedMilliseconds < 10000)
                    Thread.Sleep (1);
                Assert.IsFalse (manager.Registered (1000016));
                connection.Dispose ();
                Assert.IsTrue (stream.Ended);
                Assert.Throws<RPCException> (() => stream.ThrowIfEnded ());
                Assert.IsTrue (updater.Join (10000));
            }
        }

        [Test]
        public void RemoveKeepsAServerEnding ()
        {
            var manager = Connection.StreamManager;
            var stream = manager.GetStream (TypeSpec.For (typeof (uint)), 1000017);
            var ending = new KRPC.Schema.KRPC.ProcedureResult {
                Error = new KRPC.Schema.KRPC.Error { Description = "Ended by the server" }
            };
            Thread updater;
            lock (stream.Condition) {
                updater = new Thread (() => manager.Update (1000017, ending));
                updater.IsBackground = true;
                updater.Start ();
                // Removed once the ending is decoded, while the updating thread waits for the
                // condition the removing thread holds
                var timer = Stopwatch.StartNew ();
                while (manager.Registered (1000017) && timer.ElapsedMilliseconds < 10000)
                    Thread.Sleep (1);
                Assert.IsFalse (manager.Registered (1000017));
                stream.Remove ();
                Assert.IsTrue (stream.Ended);
                Assert.Throws<RPCException> (() => stream.ThrowIfEnded ());
            }
            Assert.IsTrue (updater.Join (10000));
        }

        [Test]
        public void CloseFromACallback ()
        {
            var connection = Connect ("CSharpClientTest");
            var x = connection.AddStream (
                () => connection.TestService ().Counter ("StreamTest.CloseFromACallback", 1));
            x.Get ();
            var test = Thread.CurrentThread;
            var closed = new ManualResetEvent (false);
            x.AddCallback (value => {
                // The first call runs on this thread, and the later ones on the update thread
                if (Thread.CurrentThread != test) {
                    connection.Dispose ();
                    closed.Set ();
                }
            });
            Assert.IsTrue (closed.WaitOne (10000));
            Assert.Throws<ConnectionException> (() => x.Get ());
        }

        [Test]
        public void CloseFromAnEndCallback ()
        {
            var connection = Connect ("CSharpClientTest");
            var e = connection.TestService ().OnTimer (200);
            var test = Thread.CurrentThread;
            var closed = new ManualResetEvent (false);
            var onTest = false;
            e.AddEndCallback (exn => {
                onTest = Thread.CurrentThread == test;
                connection.Dispose ();
                closed.Set ();
            });
            Assert.IsTrue (closed.WaitOne (10000));
            Assert.IsFalse (onTest);
        }

        /// <summary>
        /// Start a thread that holds a's condition while it waits on b. Returns an event set when
        /// the wait throws ConnectionException.
        /// </summary>
        static ManualResetEvent HoldOneAndWaitOnAnother (Stream<string> a, Stream<string> b)
        {
            var raised = new ManualResetEvent (false);
            var thread = new Thread (() => {
                lock (a.Condition) {
                    lock (b.Condition) {
                        try {
                            b.Wait ();
                        } catch (ConnectionException) {
                            raised.Set ();
                        }
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start ();
            Thread.Sleep (200);
            return raised;
        }

        [Test]
        public void CloseWhileAThreadHoldsOneConditionAndWaitsOnAnother ()
        {
            var connection = Connect ("CSharpClientTest");
            // The stream held is registered first, so it is the first one the close tries
            var a = connection.AddStream (() => connection.TestService ().Int32ToString (1));
            var b = connection.AddStream (() => connection.TestService ().Int32ToString (2));
            a.Get ();
            b.Get ();
            var raised = HoldOneAndWaitOnAnother (a, b);
            var closing = new Thread (connection.Dispose);
            closing.IsBackground = true;
            closing.Start ();
            Assert.IsTrue (closing.Join (10000));
            Assert.IsTrue (raised.WaitOne (10000));
            Assert.Throws<ConnectionException> (() => a.Get ());
        }

        [Test]
        public void ConnectionFailureWhileAThreadHoldsOneConditionAndWaitsOnAnother ()
        {
            using (var connection = Connect ("CSharpClientTest")) {
                var a = connection.AddStream (() => connection.TestService ().Int32ToString (1));
                var b = connection.AddStream (() => connection.TestService ().Int32ToString (2));
                a.Get ();
                b.Get ();
                var ended = new ManualResetEvent (false);
                a.AddEndCallback (exn => ended.Set ());
                var raised = HoldOneAndWaitOnAnother (a, b);
                // Stands in for the server closing the connection
                var field = typeof (Connection).GetField (
                    "streamSocket", BindingFlags.NonPublic | BindingFlags.Instance);
                ((Socket)field.GetValue (connection)).Shutdown (SocketShutdown.Both);
                Assert.IsTrue (raised.WaitOne (10000));
                Assert.IsTrue (ended.WaitOne (10000));
                Assert.Throws<ConnectionException> (() => a.Get ());
            }
        }

        [Test]
        public void CloseFromAThreadHoldingAConditionACallbackWaitsFor ()
        {
            var connection = Connect ("CSharpClientTest");
            var x = connection.AddStream (
                () => connection.TestService ().Counter ("StreamTest.CloseFromAThreadHoldingAConditionACallbackWaitsFor", 1));
            var y = connection.AddStream (() => connection.TestService ().Int32ToString (3));
            x.Get ();
            y.Get ();
            var armed = new ManualResetEvent (false);
            var entered = new ManualResetEvent (false);
            x.AddCallback (value => {
                if (armed.WaitOne (0)) {
                    entered.Set ();
                    lock (y.Condition) {
                    }
                }
            });
            var closing = new Thread (() => {
                lock (y.Condition) {
                    armed.Set ();
                    entered.WaitOne (10000);
                    connection.Dispose ();
                }
            });
            closing.IsBackground = true;
            closing.Start ();
            Assert.IsTrue (closing.Join (10000));
            Assert.IsTrue (entered.WaitOne (0));
            Assert.Throws<ConnectionException> (() => x.Get ());
            Assert.Throws<ConnectionException> (() => y.Get ());
        }

        [Test]
        public void CloseFromAThreadHoldingARemovedStreamsConditionACallbackWaitsFor ()
        {
            var connection = Connect ("CSharpClientTest");
            var x = connection.AddStream (
                () => connection.TestService ().Counter ("StreamTest.CloseFromAThreadHoldingARemovedStreamsConditionACallbackWaitsFor", 1));
            var y = connection.AddStream (() => connection.TestService ().Int32ToString (4));
            x.Get ();
            y.Get ();
            y.Remove ();
            var armed = new ManualResetEvent (false);
            var entered = new ManualResetEvent (false);
            x.AddCallback (value => {
                if (armed.WaitOne (0)) {
                    entered.Set ();
                    lock (y.Condition) {
                    }
                }
            });
            var closing = new Thread (() => {
                lock (y.Condition) {
                    armed.Set ();
                    entered.WaitOne (10000);
                    connection.Dispose ();
                }
            });
            closing.IsBackground = true;
            closing.Start ();
            Assert.IsTrue (closing.Join (10000));
            Assert.IsTrue (entered.WaitOne (0));
            Assert.Throws<ConnectionException> (() => x.Get ());
        }

        [Test]
        public void CloseFromTheFirstCallOfACallback ()
        {
            var connection = Connect ("CSharpClientTest");
            var x = connection.AddStream (
                () => connection.TestService ().Counter ("StreamTest.CloseFromTheFirstCallOfACallback", 1));
            x.Get ();
            var first = true;
            var thread = new Thread (() => x.AddCallback (value => {
                if (!first)
                    return;
                first = false;
                // The update thread queues the update for this thread while the callback runs
                lock (x.Condition)
                    x.Wait (5);
                Thread.Sleep (100);
                connection.Dispose ();
            }));
            thread.IsBackground = true;
            thread.Start ();
            Assert.IsTrue (thread.Join (10000));
            Assert.Throws<ConnectionException> (() => x.Get ());
        }

        [Test]
        public void TheFirstCallOfACallbackWaitsForUpdates ()
        {
            var x = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TheFirstCallOfACallbackWaitsForUpdates", 1));
            x.Get ();
            var values = new List<int> ();
            var waited = new List<bool> ();
            var thread = new Thread (() => x.AddCallback (value => {
                lock (values) {
                    values.Add (value);
                    if (values.Count > 1)
                        return;
                }
                lock (x.Condition) {
                    waited.Add (x.Wait (5));
                    waited.Add (x.Wait (5));
                }
            }));
            thread.IsBackground = true;
            thread.Start ();
            Assert.IsTrue (thread.Join (20000));
            x.Remove ();
            CollectionAssert.AreEqual (new [] { true, true }, waited);
            lock (values) {
                // The updates during the first call are passed to the callback after it
                Assert.Greater (values.Count, 2);
                CollectionAssert.IsOrdered (values);
            }
        }

        // The tests that note fake ids use their own connection, as a noted id raises the
        // highest id the stream manager keeps results for

        [Test]
        public void ResultBeforeTheAddReplyIsKept ()
        {
            using (var connection = Connect ("CSharpClientTest")) {
                var manager = connection.StreamManager;
                var type = TypeSpec.For (typeof (uint));
                manager.NoteIssued (1000003);
                // One for an id noted but not registered, and one for an id not yet noted
                manager.Update (1000003, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x2a) });
                manager.Update (1000004, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x2b) });
                Assert.AreEqual (42u, manager.GetStream (type, 1000003).Value);
                Assert.AreEqual (43u, manager.GetStream (type, 1000004).Value);
            }
        }

        [Test]
        public void KeptResultsAreBounded ()
        {
            using (var connection = Connect ("CSharpClientTest")) {
                var manager = connection.StreamManager;
                var type = TypeSpec.For (typeof (uint));
                // Only the latest value is kept
                manager.NoteIssued (1000008);
                manager.Update (1000008, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x01) });
                manager.Update (1000008, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x02) });
                var s = manager.GetStream (type, 1000008);
                Assert.AreEqual (1, s.Version);
                Assert.AreEqual (2u, s.Value);
                // The value and the ending are applied, and a result after the ending is dropped
                manager.NoteIssued (1000009);
                manager.Update (1000009, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x03) });
                manager.Update (1000009, new KRPC.Schema.KRPC.ProcedureResult {
                    Error = new KRPC.Schema.KRPC.Error { Description = "Ended" }
                });
                manager.Update (1000009, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x04) });
                var ended = manager.GetStream (type, 1000009);
                Assert.AreEqual (1, ended.Version);
                Assert.IsTrue (ended.Ended);
                Assert.AreEqual ("Ended", Assert.Catch (() => { var unused = ended.Value; }).Message);
            }
        }

        [Test]
        public void ARemovedIdIsNotKept ()
        {
            using (var connection = Connect ("CSharpClientTest")) {
                var manager = connection.StreamManager;
                var call = Connection.GetCall (() => connection.TestService ().Int32ToString (52));
                var added = connection.Invoke ("KRPC", "AddStream", new [] {
                    Encoder.Encode (call, TypeSpec.For (typeof (KRPC.Schema.KRPC.ProcedureCall)))
                }, typeof (KRPC.Schema.KRPC.Stream));
                var id = KRPC.Schema.KRPC.Stream.Parser.ParseFrom (added.Value).Id;
                Assert.IsTrue (manager.Keeps (id));
                connection.Invoke ("KRPC", "RemoveStream", new [] { Encoder.Encode (id, TypeSpec.For (typeof (ulong))) });
                Assert.IsFalse (manager.Keeps (id));
                manager.Update (id, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x2a) });
                Assert.IsFalse (manager.Keeps (id));
            }
        }

        [Test]
        public void AnEventWithNoStreamIsNotNoted ()
        {
            var response = new KRPC.Schema.KRPC.Response ();
            response.Results.Add (new KRPC.Schema.KRPC.ProcedureResult {
                Value = new KRPC.Schema.KRPC.Event ().ToByteString ()
            });
            Assert.DoesNotThrow (() => Connection.NoteStreamId (response, typeof (Event)));
        }

        [Test]
        public void ResultForAnEndedStreamIsDropped ()
        {
            using (var connection = Connect ("CSharpClientTest")) {
                var manager = connection.StreamManager;
                var type = TypeSpec.For (typeof (uint));
                manager.NoteIssued (1000006);
                manager.GetStream (type, 1000006);
                manager.Update (1000005, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0x2a) });
                Assert.IsFalse (manager.GetStream (type, 1000005).Updated);
            }
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
            var called1 = 0;
            var called2 = 0;
            var s = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestRemoveCallback", 10));
            s.AddCallback ((int x) => Interlocked.Increment (ref called1));
            int callback2Tag = s.AddCallback ((int x) => Interlocked.Increment (ref called2));
            s.RemoveCallback (callback2Tag);
            lock (s.Condition) {
                // The callbacks of an update run before the next update is stored, so after
                // this one no update can still call the removed callback
                Assert.IsTrue (s.Wait (10));
                Interlocked.Exchange (ref called1, 0);
                Interlocked.Exchange (ref called2, 0);
                Assert.IsTrue (s.Wait (10));
                Assert.IsTrue (s.Wait (10));
            }
            s.Remove();
            Assert.Greater (called1, 0);
            Assert.AreEqual (0, called2);
        }

        [Test]
        public void TestRate () {
            var stop = new ManualResetEvent (false);
            var error = false;
            var values = new List<int> ();
            var s = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestRate", 1));
            s.Rate = 5;
            s.AddCallback (
                (int x) => {
                    // The stream can update before its rate is set, so counting starts from
                    // the first value passed
                    if (values.Count > 0 && values [values.Count - 1] + 1 != x) {
                        error = true;
                        stop.Set ();
                    }
                    values.Add (x);
                    if (values.Count > 5)
                        stop.Set ();
                });

            var timer = new Stopwatch();
            timer.Start();
            Assert.IsTrue (stop.WaitOne (10000));
            s.Remove();
            var elapsed = timer.ElapsedMilliseconds;
            // Lower bound checks the rate limit was honored. The upper bound is a generous
            // hang detector, kept loose so the test does not flake under parallel load.
            Assert.Greater(elapsed, 1000);
            Assert.Less(elapsed, 3000);
            Assert.False(error);
        }

        [Test]
        public void TestEquality () {
            var s0 = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestEquality0", 1));
            var s1 = new Stream<int> (Connection, s0.stream.Id);
            var s2 = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.TestEquality0", 1));

            Assert.True (s0.Equals (s0));

            Assert.True (s0.Equals (s1));
            Assert.True (s0 == s1);
            Assert.False (s0 != s1);

            // Each add is a separate stream, even of an equal call
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
        public void EndCallbackOnClose ()
        {
            var connection = Connect ("CSharpClientTest");
            var s = connection.AddStream (() => connection.TestService ().Int32ToString (53));
            Assert.AreEqual ("53", s.Get ());
            var ended = new List<Exception> ();
            s.AddEndCallback (exn => ended.Add (exn));
            connection.Dispose ();
            Assert.AreEqual (1, ended.Count);
            Assert.IsInstanceOf<ConnectionException> (ended [0]);
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
        public void RemoveEndCallback ()
        {
            var s = Connection.AddStream (() => Connection.TestService ().Int32ToString (54));
            Assert.AreEqual ("54", s.Get ());
            var ended = new List<Exception> ();
            var tag = s.AddEndCallback (exn => ended.Add (exn));
            s.RemoveEndCallback (tag);
            s.Remove ();
            CollectionAssert.IsEmpty (ended);
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
        public void AValueThatFailsToDecodeIsRemovedFromTheServer ()
        {
            var s = Connection.AddStream (
                () => Connection.TestService ().Counter ("StreamTest.AValueThatFailsToDecode", 1));
            s.Get ();
            // A varint that never ends
            Connection.StreamManager.Update (
                s.stream.Id, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0xff) });
            Assert.Throws<ArgumentException> (() => s.Get ());
            // The removal is sent from another thread
            Thread.Sleep (200);
            // The server has stopped evaluating the stream, so only these calls count
            var count = Connection.TestService ().Counter ("StreamTest.AValueThatFailsToDecode", 1);
            Thread.Sleep (200);
            Assert.AreEqual (count + 1, Connection.TestService ().Counter ("StreamTest.AValueThatFailsToDecode", 1));
        }

        [Test]
        public void AKeptResultThatFailsToDecodeIsRemovedFromTheServer ()
        {
            using (var connection = Connect ("CSharpClientTest")) {
                var manager = connection.StreamManager;
                const string name = "StreamTest.AKeptResultThatFailsToDecode";
                // The value stays the same, so the server sends it once
                var call = Connection.GetCall (() => connection.TestService ().Counter (name, 1000000));
                var added = connection.Invoke ("KRPC", "AddStream", new [] {
                    Encoder.Encode (call, TypeSpec.For (typeof (KRPC.Schema.KRPC.ProcedureCall)))
                }, typeof (KRPC.Schema.KRPC.Stream));
                var id = KRPC.Schema.KRPC.Stream.Parser.ParseFrom (added.Value).Id;
                Thread.Sleep (200);
                // A varint that never ends, kept in place of the value from the server
                manager.Update (id, new KRPC.Schema.KRPC.ProcedureResult { Value = ByteString.CopyFrom (0xff) });
                // A stream whose value is an event over the stream with the kept result
                var outer = manager.GetStream (TypeSpec.For (typeof (Event)), 1000011);
                var evnt = new KRPC.Schema.KRPC.Event { Stream = new KRPC.Schema.KRPC.Stream { Id = id } };
                // The removal is sent without the update lock, which a call can wait for
                var invokeLock = typeof (Connection).GetField (
                    "invokeLock", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (connection);
                lock (invokeLock) {
                    var thread = new Thread (() => manager.Update (
                        1000011, new KRPC.Schema.KRPC.ProcedureResult { Value = evnt.ToByteString () }));
                    thread.IsBackground = true;
                    thread.Start ();
                    Assert.IsTrue (thread.Join (5000));
                }
                Assert.Throws<ArgumentException> (() => ((Event)outer.Value).Stream.Get ());
                // The removal is sent from another thread
                Thread.Sleep (200);
                // The server has stopped evaluating the stream, so only these calls count
                var count = connection.TestService ().Counter (name, 1);
                Thread.Sleep (200);
                Assert.AreEqual (count + 1, connection.TestService ().Counter (name, 1));
            }
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
        public void CloseFromAThreadHoldingTheCondition ()
        {
            var connection = Connect ("CSharpClientTest");
            var x = connection.AddStream (
                () => connection.TestService ().Counter ("StreamTest.CloseFromAThreadHoldingTheCondition", 1));
            x.Get ();
            var ended = 0;
            x.AddEndCallback (exn => Interlocked.Increment (ref ended));
            var thread = new Thread (() => {
                lock (x.Condition)
                    connection.Dispose ();
            });
            thread.IsBackground = true;
            thread.Start ();
            Assert.IsTrue (thread.Join (10000));
            Assert.AreEqual (1, ended);
            Assert.Throws<ConnectionException> (() => x.Get ());
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

using System;
using System.Collections.Generic;
using System.Threading;
using KRPC.Client.Services.TestService;
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
        public void MultiplyByOne ()
        {
            var obj = Connection.TestService ().CreateTestObject ("alpha");
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
            var obj = Connection.TestService ().CreateTestObject ("alpha");
            obj.IntProperty = 16777217;
            var x = Connection.AddStream (() => (int)(float)obj.IntProperty);
            Assert.AreEqual (16777216, x.Get ());
            Assert.AreNotEqual (Connection.AddStream (() => obj.IntProperty), x);
        }

        [Test]
        public void MultiplyByConstant ()
        {
            var obj = Connection.TestService ().CreateTestObject ("alpha");
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
            var obj = Connection.TestService ().CreateTestObject ("alpha");
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
            var obj = Connection.TestService ().CreateTestObject ("alpha");
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
            var obj = Connection.TestService ().CreateTestObject ("alpha");
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
    }
}

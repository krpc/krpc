using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.TestService;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class StreamsTest : ServerTestCase
    {
        [SetUp]
        public void Init ()
        {
            // FIXME: there is a race condition in the server, if we call add stream too soon!
            Thread.Sleep (50);
        }

        void Wait ()
        {
            Thread.Sleep (50);
        }

        [Test]
        public void Method ()
        {
            var x = connection.AddStream (() => connection.TestService ().FloatToString (3.14159f));
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual ("3.14159", x.Get ());
                Wait ();
            }
        }

        [Test]
        public void Property ()
        {
            connection.TestService ().StringProperty = "foo";
            var x = connection.AddStream (() => connection.TestService ().StringProperty);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual ("foo", x.Get ());
                Wait ();
            }
        }

        [Test]
        public void ClassMethod ()
        {
            var obj = connection.TestService ().CreateTestObject ("bob");
            var x = connection.AddStream (() => obj.FloatToString (3.14159f));
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual ("bob3.14159", x.Get ());
                Wait ();
            }
        }

        [Test]
        public void ClassStaticMethod ()
        {
            // FIXME: have to specify optional parameter ""
            var x = connection.AddStream (() => TestClass.StaticMethod (connection, "foo", ""));
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual ("jebfoo", x.Get ());
                Wait ();
            }
        }

        [Test]
        public void ClassProperty ()
        {
            var obj = connection.TestService ().CreateTestObject ("jeb");
            obj.IntProperty = 42;
            var x = connection.AddStream (() => obj.IntProperty);
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual (42, x.Get ());
                Wait ();
            }
        }

        [Test]
        public void Counter ()
        {
            var count = -1;
            var x = connection.AddStream (() => connection.TestService ().Counter ());
            for (int i = 0; i < 5; i++) {
                Assert.IsTrue (count < x.Get ());
                count = x.Get ();
                Wait ();
            }
        }

        [Test]
        public void Nested ()
        {
            var x0 = connection.AddStream (() => connection.TestService ().FloatToString (0.123f));
            var x1 = connection.AddStream (() => connection.TestService ().FloatToString (1.234f));
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual ("0.123", x0.Get ());
                Assert.AreEqual ("1.234", x1.Get ());
                Wait ();
            }
        }

        [Test]
        public void Inerleaved ()
        {
            var s0 = connection.AddStream (() => connection.TestService ().Int32ToString (0));
            Assert.AreEqual ("0", s0.Get ());

            Wait ();
            Assert.AreEqual ("0", s0.Get ());

            var s1 = connection.AddStream (() => connection.TestService ().Int32ToString (1));
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

            var s2 = connection.AddStream (() => connection.TestService ().Int32ToString (2));
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
            var s = connection.AddStream (() => connection.TestService ().Int32ToString (0));
            Assert.AreEqual ("0", s.Get ());

            Wait ();
            Assert.AreEqual ("0", s.Get ());

            s.Remove ();
            Assert.Throws<InvalidOperationException> (() => s.Get ());
            s.Remove ();
            Assert.Throws<InvalidOperationException> (() => s.Get ());
        }

        [Test]
        public void AddStreamTwice ()
        {
            var s0 = connection.AddStream (() => connection.TestService ().Int32ToString (42));
            var streamId = s0.Id;
            Assert.AreEqual ("42", s0.Get ());

            Wait ();
            Assert.AreEqual ("42", s0.Get ());

            var s1 = connection.AddStream (() => connection.TestService ().Int32ToString (42));
            Assert.AreEqual (streamId, s1.Id);
            Assert.AreEqual ("42", s0.Get ());
            Assert.AreEqual ("42", s1.Get ());

            Wait ();
            Assert.AreEqual ("42", s0.Get ());
            Assert.AreEqual ("42", s1.Get ());
        }
    }
}

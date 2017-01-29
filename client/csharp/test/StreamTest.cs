using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using KRPC.Client.Services.TestService;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class StreamsTest : ServerTestCase
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
        public void Counter ()
        {
            var count = 0;
            var x = Connection.AddStream (() => Connection.TestService ().Counter (0));
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
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
        public void AddStreamTwice ()
        {
            var s0 = Connection.AddStream (() => Connection.TestService ().Int32ToString (42));
            var streamId = s0.Id;
            Assert.AreEqual ("42", s0.Get ());

            Wait ();
            Assert.AreEqual ("42", s0.Get ());

            var s1 = Connection.AddStream (() => Connection.TestService ().Int32ToString (42));
            Assert.AreEqual (streamId, s1.Id);
            Assert.AreEqual ("42", s0.Get ());
            Assert.AreEqual ("42", s1.Get ());

            Wait ();
            Assert.AreEqual ("42", s0.Get ());
            Assert.AreEqual ("42", s1.Get ());
        }
    }
}

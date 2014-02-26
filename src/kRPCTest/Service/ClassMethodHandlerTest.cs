using NUnit.Framework;
using System;
using KRPC.Service;

namespace KRPCTest.Service
{
    [TestFixture]
    public class ClassMethodHandlerTest
    {
        [Test]
        public void SimpleUsage ()
        {
            var instance = new TestService.TestClass ("foo");
            var object_id = ObjectStore.Instance.AddInstance (instance);
            var handler = new ClassMethodHandler (typeof(TestService.TestClass).GetMethod ("FloatToString"));
            Assert.AreEqual (new []{ typeof(ulong), typeof(float) }, handler.GetParameters ());
            Assert.AreEqual (typeof(string), handler.ReturnType);
            Assert.AreEqual ("foo3.14159", handler.Invoke (new object[] { object_id, 3.14159f }));
        }

        [Test]
        public void NoInstance ()
        {
            var handler = new ClassMethodHandler (typeof(TestService.TestClass).GetMethod ("FloatToString"));
            Assert.AreEqual (new []{ typeof(ulong), typeof(float) }, handler.GetParameters ());
            Assert.AreEqual (typeof(string), handler.ReturnType);
            Assert.Throws<ArgumentException> (() => handler.Invoke (new object[] { 1000UL, 3.14159f }));
        }
    }
}


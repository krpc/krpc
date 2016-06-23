using System;
using System.Linq;
using KRPC.Service;
using NUnit.Framework;

namespace KRPC.Test.Service
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
            Assert.AreEqual ("foo3.14159", handler.Invoke (new object[] { object_id, 3.14159f }));
        }

        [Test]
        public void DefaultArguments ()
        {
            var instance = new TestService.TestClass ("foo");
            var object_id = ObjectStore.Instance.AddInstance (instance);
            var handler = new ClassMethodHandler (typeof(TestService.TestClass).GetMethod ("IntToString"));
            Assert.AreEqual ("foo42", handler.Invoke (new object[] { object_id, Type.Missing }));
        }

        [Test]
        public void NoInstance ()
        {
            var handler = new ClassMethodHandler (typeof(TestService.TestClass).GetMethod ("FloatToString"));
            Assert.Throws<ArgumentException> (() => handler.Invoke (new object[] { 1000UL, 3.14159f }));
        }

        [Test]
        public void Properties ()
        {
            var handler = new ClassMethodHandler (typeof(TestService.TestClass).GetMethod ("FloatToString"));
            var parameters = handler.Parameters.ToList ();
            Assert.AreEqual (2, parameters.Count);
            Assert.AreEqual ("this", parameters [0].Name);
            Assert.AreEqual ("x", parameters [1].Name);
            Assert.AreEqual (typeof(ulong), parameters [0].Type);
            Assert.AreEqual (typeof(float), parameters [1].Type);
            Assert.IsFalse (parameters [0].HasDefaultValue);
            Assert.IsFalse (parameters [1].HasDefaultValue);
            Assert.AreEqual (typeof(string), handler.ReturnType);
        }

        [Test]
        public void PropertiesDefaultArgument ()
        {
            var handler = new ClassMethodHandler (typeof(TestService.TestClass).GetMethod ("IntToString"));
            var parameters = handler.Parameters.ToList ();
            Assert.AreEqual (2, parameters.Count);
            Assert.AreEqual ("this", parameters [0].Name);
            Assert.AreEqual ("x", parameters [1].Name);
            Assert.AreEqual (typeof(ulong), parameters [0].Type);
            Assert.AreEqual (typeof(int), parameters [1].Type);
            Assert.IsFalse (parameters [0].HasDefaultValue);
            Assert.IsTrue (parameters [1].HasDefaultValue);
            Assert.AreEqual (typeof(string), handler.ReturnType);
        }
    }
}

using System;
using System.Linq;
using System.Reflection;
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
            var classType = typeof(TestService.TestClass);
            var handler = new ClassMethodHandler (classType, classType.GetMethod ("FloatToString"), false);
            Assert.AreEqual ("foo3.14159", handler.Invoke (new object[] { instance, 3.14159f }));
        }

        [Test]
        public void DefaultArguments ()
        {
            var instance = new TestService.TestClass ("foo");
            var classType = typeof(TestService.TestClass);
            var handler = new ClassMethodHandler (classType, classType.GetMethod ("IntToString"), false);
            Assert.AreEqual ("foo42", handler.Invoke (new object[] { instance, Type.Missing }));
        }

        [Test]
        public void NoInstance ()
        {
            var classType = typeof(TestService.TestClass);
            var handler = new ClassMethodHandler (classType, classType.GetMethod ("FloatToString"), false);
            Assert.Throws<TargetException> (() => handler.Invoke (new object[] { null, 3.14159f }));
        }

        [Test]
        public void Properties ()
        {
            var classType = typeof(TestService.TestClass);
            var handler = new ClassMethodHandler (classType, classType.GetMethod ("FloatToString"), false);
            var parameters = handler.Parameters.ToList ();
            Assert.AreEqual (2, parameters.Count);
            Assert.AreEqual ("this", parameters [0].Name);
            Assert.AreEqual ("x", parameters [1].Name);
            Assert.AreEqual (typeof(TestService.TestClass), parameters [0].Type);
            Assert.AreEqual (typeof(float), parameters [1].Type);
            Assert.IsFalse (parameters [0].HasDefaultValue);
            Assert.IsFalse (parameters [1].HasDefaultValue);
            Assert.AreEqual (typeof(string), handler.ReturnType);
        }

        [Test]
        public void PropertiesDefaultArgument ()
        {
            var classType = typeof(TestService.TestClass);
            var handler = new ClassMethodHandler (classType, classType.GetMethod ("IntToString"), false);
            var parameters = handler.Parameters.ToList ();
            Assert.AreEqual (2, parameters.Count);
            Assert.AreEqual ("this", parameters [0].Name);
            Assert.AreEqual ("x", parameters [1].Name);
            Assert.AreEqual (typeof(TestService.TestClass), parameters [0].Type);
            Assert.AreEqual (typeof(int), parameters [1].Type);
            Assert.IsFalse (parameters [0].HasDefaultValue);
            Assert.IsTrue (parameters [1].HasDefaultValue);
            Assert.AreEqual (typeof(string), handler.ReturnType);
        }
    }
}

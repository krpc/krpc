using System.Linq;
using KRPC.Service;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ClassStaticMethodHandlerTest
    {
        [Test]
        public void StaticMethod ()
        {
            var classType = typeof(TestService.TestClass);
            var handler = new ClassStaticMethodHandler (classType.GetMethod ("StaticMethod"), false);
            Assert.IsFalse (handler.HasInstance);
            Assert.AreEqual ("jebfoo", handler.Invoke (null, new object [] { "foo" }));
        }

        [Test]
        public void ExtensionMethod ()
        {
            var instance = new TestService.TestClass ("foo");
            var method = typeof(TestServiceExtensions).GetMethod ("ExtensionMethod");
            var handler = new ClassStaticMethodHandler (method, false, true);
            // The instance is passed as argument 0, as it is the first argument of the static call
            Assert.IsFalse (handler.HasInstance);
            Assert.AreEqual ("foo42", handler.Invoke (null, new object [] { instance, 42 }));
        }

        [Test]
        public void ExtensionMethodProperties ()
        {
            var method = typeof(TestServiceExtensions).GetMethod ("ExtensionMethod");
            var handler = new ClassStaticMethodHandler (method, false, true);
            var parameters = handler.Parameters.ToList ();
            Assert.AreEqual (2, parameters.Count);
            Assert.AreEqual ("this", parameters [0].Name);
            Assert.AreEqual ("x", parameters [1].Name);
            Assert.AreEqual (typeof(TestService.TestClass), parameters [0].Type);
            Assert.AreEqual (typeof(int), parameters [1].Type);
            Assert.IsFalse (parameters [0].HasDefaultValue);
            Assert.IsFalse (parameters [0].Nullable);
            Assert.AreEqual (typeof(string), handler.ReturnType);
        }
    }
}

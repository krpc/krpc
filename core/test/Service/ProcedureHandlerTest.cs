using System;
using System.Linq;
using KRPC.Service;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ProcedureHandlerTest
    {
        public static float TestProcedure (int x, float y)
        {
            return x + y;
        }

        public static string TestProcedureWithDefaultArg (int x, string y = "foo")
        {
            return x + y;
        }

        [Test]
        public void SimpleUsage ()
        {
            var handler = new ProcedureHandler (typeof(ProcedureHandlerTest).GetMethod ("TestProcedure"), false);
            Assert.AreEqual (3.14159f, handler.Invoke (new object[] { 3, 0.14159f }));
        }

        [Test]
        public void DefaultArguments ()
        {
            var handler = new ProcedureHandler (typeof(ProcedureHandlerTest).GetMethod ("TestProcedureWithDefaultArg"), false);
            Assert.AreEqual ("42foo", handler.Invoke (new object[] { 42, Type.Missing }));
        }

        [Test]
        public void Properties ()
        {
            var handler = new ProcedureHandler (typeof(ProcedureHandlerTest).GetMethod ("TestProcedure"), false);
            var parameters = handler.Parameters.ToList ();
            Assert.AreEqual (2, parameters.Count);
            Assert.AreEqual ("x", parameters [0].Name);
            Assert.AreEqual ("y", parameters [1].Name);
            Assert.AreEqual (typeof(int), parameters [0].Type);
            Assert.AreEqual (typeof(float), parameters [1].Type);
            Assert.IsFalse (parameters [0].HasDefaultValue);
            Assert.IsFalse (parameters [1].HasDefaultValue);
            Assert.AreEqual (typeof(float), handler.ReturnType);
        }

        [Test]
        public void PropertiesDefaultArgument ()
        {
            var handler = new ProcedureHandler (typeof(ProcedureHandlerTest).GetMethod ("TestProcedureWithDefaultArg"), false);
            var parameters = handler.Parameters.ToList ();
            Assert.AreEqual (2, parameters.Count);
            Assert.AreEqual ("x", parameters [0].Name);
            Assert.AreEqual ("y", parameters [1].Name);
            Assert.AreEqual (typeof(int), parameters [0].Type);
            Assert.AreEqual (typeof(string), parameters [1].Type);
            Assert.IsFalse (parameters [0].HasDefaultValue);
            Assert.IsTrue (parameters [1].HasDefaultValue);
            Assert.AreEqual ("foo", parameters [1].DefaultValue);
            Assert.AreEqual (typeof(string), handler.ReturnType);
        }
    }
}

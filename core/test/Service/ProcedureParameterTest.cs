using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ProcedureParameterTest
    {
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUnusedParametersRule")]
        public static void MethodWithArg (int x)
        {
        }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUnusedParametersRule")]
        public static void MethodWithDefaultArg (string x = "foo")
        {
        }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUnusedParametersRule")]
        public static void MethodWithDefaultNullArg (string x = null)
        {
        }

        [Test]
        public void BasicUsage ()
        {
            var parameter = new ProcedureParameter (typeof(int), "foo");
            Assert.AreEqual ("foo", parameter.Name);
            Assert.AreEqual (typeof(int), parameter.Type);
            Assert.IsFalse (parameter.HasDefaultValue);
        }

        [Test]
        public void FromMethodInfo ()
        {
            var method = typeof(ProcedureParameterTest).GetMethod ("MethodWithArg");
            var parameter = new ProcedureParameter (method.GetParameters () [0]);
            Assert.AreEqual ("x", parameter.Name);
            Assert.AreEqual (typeof(int), parameter.Type);
            Assert.IsFalse (parameter.HasDefaultValue);
        }

        [Test]
        public void FromMethodDefaultArgument ()
        {
            var method = typeof(ProcedureParameterTest).GetMethod ("MethodWithDefaultArg");
            var parameter = new ProcedureParameter (method.GetParameters () [0]);
            Assert.AreEqual ("x", parameter.Name);
            Assert.AreEqual (typeof(string), parameter.Type);
            Assert.IsTrue (parameter.HasDefaultValue);
            Assert.AreEqual ("foo", parameter.DefaultValue);
        }

        [Test]
        public void FromMethodDefaultNullArgument ()
        {
            var method = typeof(ProcedureParameterTest).GetMethod ("MethodWithDefaultNullArg");
            var parameter = new ProcedureParameter (method.GetParameters () [0]);
            Assert.AreEqual ("x", parameter.Name);
            Assert.AreEqual (typeof(string), parameter.Type);
            Assert.IsTrue (parameter.HasDefaultValue);
            Assert.AreEqual (null, parameter.DefaultValue);
        }
    }
}

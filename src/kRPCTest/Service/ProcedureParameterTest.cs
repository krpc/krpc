using NUnit.Framework;
using System;
using KRPC.Service;

namespace KRPCTest.Service
{
    [TestFixture]
    public class ProcedureParameterTest
    {
        public static void MethodWithArg (int x)
        {
        }

        public static void MethodWithDefaultArg (string x = "foo")
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
        public void DefaultArgument ()
        {
            var parameter = new ProcedureParameter (typeof(int), "foo", 42);
            Assert.AreEqual ("foo", parameter.Name);
            Assert.AreEqual (typeof(int), parameter.Type);
            Assert.IsTrue (parameter.HasDefaultValue);
            Assert.AreEqual (42, parameter.DefaultValue);
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
    }
}


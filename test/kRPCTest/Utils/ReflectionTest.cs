using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using KRPC.Utils;
using KRPC.Service.Attributes;
using KRPCTest.Service;

namespace KRPCTest.Utils
{
    // TODO: relies on test fixtures from KRPCTest.Service, so these tests are not self contained
    [TestFixture]
    public class ReflectionTest
    {
        [Test]
        public void GetTypesWithAttribute ()
        {
            Assert.AreEqual (4, Reflection.GetTypesWith<KRPCServiceAttribute> ().Count ());
            Assert.AreEqual (3, Reflection.GetTypesWith<KRPCClassAttribute> ().Count ());
            Assert.AreEqual (0, Reflection.GetTypesWith<KRPCPropertyAttribute> ().Count ());
        }

        [Test]
        public void GetMethodsWithAttribute ()
        {
            Assert.AreEqual (24, Reflection.GetMethodsWith<KRPCProcedureAttribute> (typeof(TestService)).Count ());
            Assert.AreEqual (3, Reflection.GetMethodsWith<KRPCMethodAttribute> (typeof(TestService.TestClass)).Count ());
            Assert.AreEqual (0, Reflection.GetMethodsWith<KRPCProcedureAttribute> (typeof(TestService.TestClass)).Count ());
            Assert.AreEqual (0, Reflection.GetMethodsWith<KRPCProcedureAttribute> (typeof(string)).Count ());
        }

        [Test]
        public void GetPropertiesWithAttribute ()
        {
            Assert.AreEqual (3, Reflection.GetPropertiesWith<KRPCPropertyAttribute> (typeof(TestService)).Count ());
            Assert.AreEqual (2, Reflection.GetPropertiesWith<KRPCPropertyAttribute> (typeof(TestService.TestClass)).Count ());
            Assert.AreEqual (0, Reflection.GetPropertiesWith<KRPCPropertyAttribute> (typeof(string)).Count ());
        }

        [Test]
        public void GetAttribute ()
        {
            var attr = Reflection.GetAttribute<KRPCServiceAttribute> (typeof(TestService3));
            Assert.AreNotEqual (null, attr);
            Assert.AreEqual ("TestService3Name", attr.Name);
            Assert.Throws<ArgumentException> (() => Reflection.GetAttribute<KRPCServiceAttribute> (typeof(TestService.TestClass)));
        }

        [Test]
        public void HasAttribute ()
        {
            Assert.IsTrue (Reflection.HasAttribute<KRPCServiceAttribute> (typeof(TestService3)));
            Assert.IsFalse (Reflection.HasAttribute<KRPCServiceAttribute> (typeof(TestService.TestClass)));
        }

        public static class TestStaticClass
        {
        }

        public class TestNonStaticClass
        {
        }

        public static int TestStaticProperty { get; set; }

        public int TestNonStaticProperty { get; set; }

        public int TestPublicProperty { get; set; }

        public int TestPublicGetProperty { get; private set; }

        public int TestPublicSetProperty { private get; set; }

        [Test]
        public void IsStaticType ()
        {
            Assert.IsTrue (typeof(TestStaticClass).IsStatic ());
            Assert.IsFalse (typeof(TestNonStaticClass).IsStatic ());
        }

        [Test]
        public void IsStaticProperty ()
        {
            Assert.IsTrue (typeof(ReflectionTest).GetProperty ("TestStaticProperty").IsStatic ());
            Assert.IsFalse (typeof(ReflectionTest).GetProperty ("TestNonStaticProperty").IsStatic ());
        }

        [Test]
        public void IsPublicProperty ()
        {
            Assert.IsTrue (typeof(ReflectionTest).GetProperty ("TestPublicProperty").IsPublic ());
            Assert.IsTrue (typeof(ReflectionTest).GetProperty ("TestPublicGetProperty").IsPublic ());
            Assert.IsTrue (typeof(ReflectionTest).GetProperty ("TestPublicSetProperty").IsPublic ());
        }
    }
}


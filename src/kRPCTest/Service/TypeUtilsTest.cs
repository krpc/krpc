using System;
using NUnit.Framework;
using KRPC.Service;

namespace KRPCTest.Service
{
    [TestFixture]
    public class TypeUtilsTest
    {
        [Test]
        public void IsAValidType ()
        {
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(string)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(long)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(TestService)));
        }

        [Test]
        public void IsAClassType ()
        {
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(long)));
            Assert.IsTrue (TypeUtils.IsAClassType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(TestService)));
        }

        [Test]
        public void GetTypeName ()
        {
            Assert.AreEqual ("string", TypeUtils.GetTypeName (typeof(string)));
            Assert.AreEqual ("int64", TypeUtils.GetTypeName (typeof(long)));
            Assert.AreEqual ("uint64", TypeUtils.GetTypeName (typeof(TestService.TestClass)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetTypeName (typeof(TestService)));
        }

        [Test]
        public void ParameterTypeAttributes ()
        {
            Assert.AreEqual (new string[]{ }, TypeUtils.ParameterTypeAttributes (0, typeof(string)));
            Assert.AreEqual (new string[]{ }, TypeUtils.ParameterTypeAttributes (3, typeof(long)));
            Assert.AreEqual (new []{ "ParameterType(1).Class(TestService.TestClass)" }, TypeUtils.ParameterTypeAttributes (1, typeof(TestService.TestClass)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ParameterTypeAttributes (0, typeof(TestService)));
        }

        [Test]
        public void ReturnTypeAttributes ()
        {
            Assert.AreEqual (new string[]{ }, TypeUtils.ReturnTypeAttributes (typeof(string)));
            Assert.AreEqual (new string[]{ }, TypeUtils.ReturnTypeAttributes (typeof(long)));
            Assert.AreEqual (new []{ "ReturnType.Class(TestService.TestClass)" }, TypeUtils.ReturnTypeAttributes (typeof(TestService.TestClass)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ReturnTypeAttributes (typeof(TestService)));
        }
    }
}


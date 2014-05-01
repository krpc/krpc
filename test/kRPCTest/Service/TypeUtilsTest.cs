using System;
using NUnit.Framework;
using KRPC.Service;

namespace KRPCTest.Service
{
    [TestFixture]
    public class TypeUtilsTest
    {
        [Test]
        public void IsAValidIdentifier ()
        {
            Assert.IsTrue (TypeUtils.IsAValidIdentifier ("IdentifierName"));
            Assert.IsTrue (TypeUtils.IsAValidIdentifier ("Foo123"));
            Assert.IsFalse (TypeUtils.IsAValidIdentifier ("123Foo"));
            Assert.IsFalse (TypeUtils.IsAValidIdentifier (""));
            Assert.IsFalse (TypeUtils.IsAValidIdentifier ("_Foo"));
            Assert.IsFalse (TypeUtils.IsAValidIdentifier ("Foo%"));
        }

        [Test]
        public void IsAValidType ()
        {
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(string)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(long)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(TestService.TestClass)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(TestService.CSharpEnum)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(TestService)));
        }

        [Test]
        public void IsAClassType ()
        {
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(long)));
            Assert.IsTrue (TypeUtils.IsAClassType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(TestService.CSharpEnum)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(TestService)));
        }

        [Test]
        public void IsAnEnumType ()
        {
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(long)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(TestService.TestClass)));
            Assert.IsTrue (TypeUtils.IsAnEnumType (typeof(TestService.CSharpEnum)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(TestService)));
        }

        [Test]
        public void GetTypeName ()
        {
            Assert.AreEqual ("string", TypeUtils.GetTypeName (typeof(string)));
            Assert.AreEqual ("int64", TypeUtils.GetTypeName (typeof(long)));
            Assert.AreEqual ("uint64", TypeUtils.GetTypeName (typeof(TestService.TestClass)));
            Assert.AreEqual ("int32", TypeUtils.GetTypeName (typeof(TestService.CSharpEnum)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetTypeName (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetTypeName (typeof(TestService)));
        }

        [Test]
        public void ParameterTypeAttributes ()
        {
            Assert.AreEqual (new string[]{ }, TypeUtils.ParameterTypeAttributes (0, typeof(string)));
            Assert.AreEqual (new string[]{ }, TypeUtils.ParameterTypeAttributes (3, typeof(long)));
            Assert.AreEqual (new []{ "ParameterType(1).Class(TestService.TestClass)" }, TypeUtils.ParameterTypeAttributes (1, typeof(TestService.TestClass)));
            Assert.AreEqual (new [] { "ParameterType(2).Enum(TestService.CSharpEnum)" }, TypeUtils.ParameterTypeAttributes (2, typeof(TestService.CSharpEnum)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ParameterTypeAttributes (0, typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ParameterTypeAttributes (0, typeof(TestService)));
        }

        [Test]
        public void ReturnTypeAttributes ()
        {
            Assert.AreEqual (new string[]{ }, TypeUtils.ReturnTypeAttributes (typeof(string)));
            Assert.AreEqual (new string[]{ }, TypeUtils.ReturnTypeAttributes (typeof(long)));
            Assert.AreEqual (new []{ "ReturnType.Class(TestService.TestClass)" }, TypeUtils.ReturnTypeAttributes (typeof(TestService.TestClass)));
            Assert.AreEqual (new [] { "ReturnType.Enum(TestService.CSharpEnum)" }, TypeUtils.ReturnTypeAttributes (typeof(TestService.CSharpEnum)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ReturnTypeAttributes (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ReturnTypeAttributes (typeof(TestService)));
        }

        [Test]
        public void GetServiceName ()
        {
            Assert.AreEqual ("TestService", TypeUtils.GetServiceName (typeof(TestService)));
            Assert.AreEqual ("TestService2", TypeUtils.GetServiceName (typeof(TestService2)));
            Assert.AreEqual ("TestService3Name", TypeUtils.GetServiceName (typeof(TestService3)));
        }

        [Test]
        public void GetClassServiceName ()
        {
            Assert.AreEqual ("TestService", TypeUtils.GetClassServiceName (typeof(TestService.TestClass)));
            Assert.AreEqual ("TestService3Name", TypeUtils.GetClassServiceName (typeof(TestClass3)));
            Assert.AreEqual ("TestService", TypeUtils.GetClassServiceName (typeof(TestTopLevelClass)));
        }

        [Test]
        public void GetEnumServiceName ()
        {
            Assert.AreEqual ("TestService", TypeUtils.GetEnumServiceName (typeof(TestService.CSharpEnum)));
        }

        [Test]
        public void ValidateIdentifier ()
        {
            Assert.DoesNotThrow (() => TypeUtils.ValidateIdentifier ("IdentifierName"));
            Assert.DoesNotThrow (() => TypeUtils.ValidateIdentifier ("Foo123"));
            Assert.Throws<ServiceException> (() => TypeUtils.ValidateIdentifier ("123Foo"));
            Assert.Throws<ServiceException> (() => TypeUtils.ValidateIdentifier (""));
            Assert.Throws<ServiceException> (() => TypeUtils.ValidateIdentifier ("_Foo"));
            Assert.Throws<ServiceException> (() => TypeUtils.ValidateIdentifier ("Foo%"));
        }
    }
}


using System;
using System.Collections.Generic;
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
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(IList<string>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(IDictionary<int,string>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(IList<IDictionary<int,string>>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(IList<TestService.TestClass>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(IList<TestService.CSharpEnum>)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(IDictionary<double,string>)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(IDictionary<TestService.TestClass,string>)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(IList<TestService.CSharpEnumWithoutAttribute>)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(IEnumerable<string>)));
        }

        [Test]
        public void IsAValidKeyType ()
        {
            Assert.IsTrue (TypeUtils.IsAValidKeyType (typeof(string)));
            Assert.IsTrue (TypeUtils.IsAValidKeyType (typeof(int)));
            Assert.IsTrue (TypeUtils.IsAValidKeyType (typeof(uint)));
            Assert.IsTrue (TypeUtils.IsAValidKeyType (typeof(ulong)));
            Assert.IsTrue (TypeUtils.IsAValidKeyType (typeof(long)));
            Assert.IsTrue (TypeUtils.IsAValidKeyType (typeof(bool)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(float)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(double)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(TestService.CSharpEnum)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(TestService)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IList<string>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IDictionary<int,string>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IList<IDictionary<int,string>>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IList<TestService.TestClass>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IList<TestService.CSharpEnum>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IDictionary<double,string>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IDictionary<TestService.TestClass,string>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IList<TestService.CSharpEnumWithoutAttribute>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IEnumerable<string>)));
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
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(IList<string>)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(IDictionary<int,string>)));
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
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(IList<string>)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(IDictionary<int,string>)));
        }

        [Test]
        public void IsACollectionType ()
        {
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(long)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(TestService.CSharpEnum)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(TestService)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(IList<string>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(IDictionary<int,string>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(IList<IDictionary<int,string>>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(IList<TestService.TestClass>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(IList<TestService.CSharpEnum>)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(IDictionary<double,string>)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(IDictionary<TestService.TestClass,string>)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(IList<TestService.CSharpEnumWithoutAttribute>)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(IEnumerable<string>)));
        }

        [Test]
        public void IsAListCollectionType ()
        {
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(long)));
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(TestService.CSharpEnum)));
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(TestService)));
            Assert.IsTrue (TypeUtils.IsAListCollectionType (typeof(IList<string>)));
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(IDictionary<int,string>)));
            Assert.IsTrue (TypeUtils.IsAListCollectionType (typeof(IList<IDictionary<int,string>>)));
            Assert.IsTrue (TypeUtils.IsAListCollectionType (typeof(IList<TestService.TestClass>)));
            Assert.IsTrue (TypeUtils.IsAListCollectionType (typeof(IList<TestService.CSharpEnum>)));
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(IDictionary<double,string>)));
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(IDictionary<TestService.TestClass,string>)));
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(IList<TestService.CSharpEnumWithoutAttribute>)));
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(IEnumerable<string>)));
        }

        [Test]
        public void IsADictionaryCollectionType ()
        {
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(long)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(TestService.CSharpEnum)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(TestService)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IList<string>)));
            Assert.IsTrue (TypeUtils.IsADictionaryCollectionType (typeof(IDictionary<int,string>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IList<IDictionary<int,string>>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IList<TestService.TestClass>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IList<TestService.CSharpEnum>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IDictionary<double,string>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IDictionary<TestService.TestClass,string>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IList<TestService.CSharpEnumWithoutAttribute>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IEnumerable<string>)));
        }

        [Test]
        public void GetTypeName ()
        {
            Assert.AreEqual ("string", TypeUtils.GetTypeName (typeof(string)));
            Assert.AreEqual ("int64", TypeUtils.GetTypeName (typeof(long)));
            Assert.AreEqual ("uint64", TypeUtils.GetTypeName (typeof(TestService.TestClass)));
            Assert.AreEqual ("int32", TypeUtils.GetTypeName (typeof(TestService.CSharpEnum)));
            Assert.AreEqual ("KRPC.List", TypeUtils.GetTypeName (typeof(IList<string>)));
            Assert.AreEqual ("KRPC.Dictionary", TypeUtils.GetTypeName (typeof(IDictionary<int,string>)));
            Assert.AreEqual ("KRPC.List", TypeUtils.GetTypeName (typeof(IList<IDictionary<int,string>>)));
            Assert.AreEqual ("KRPC.List", TypeUtils.GetTypeName (typeof(IList<TestService.TestClass>)));
            Assert.AreEqual ("KRPC.List", TypeUtils.GetTypeName (typeof(IList<TestService.CSharpEnum>)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetTypeName (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetTypeName (typeof(TestService)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetTypeName (typeof(IDictionary<double,string>)));
        }

        [Test]
        public void GetKRPCTypeName ()
        {
            Assert.AreEqual ("string", TypeUtils.GetKRPCTypeName (typeof(string)));
            Assert.AreEqual ("int64", TypeUtils.GetKRPCTypeName (typeof(long)));
            Assert.AreEqual ("Class(TestService.TestClass)", TypeUtils.GetKRPCTypeName (typeof(TestService.TestClass)));
            Assert.AreEqual ("Enum(TestService.CSharpEnum)", TypeUtils.GetKRPCTypeName (typeof(TestService.CSharpEnum)));
            Assert.AreEqual ("List(string)", TypeUtils.GetKRPCTypeName (typeof(IList<string>)));
            Assert.AreEqual ("Dictionary(int32,string)", TypeUtils.GetKRPCTypeName (typeof(IDictionary<int,string>)));
            Assert.AreEqual ("List(Dictionary(int32,string))", TypeUtils.GetKRPCTypeName (typeof(IList<IDictionary<int,string>>)));
            Assert.AreEqual ("List(Class(TestService.TestClass))", TypeUtils.GetKRPCTypeName (typeof(IList<TestService.TestClass>)));
            Assert.AreEqual ("List(Enum(TestService.CSharpEnum))", TypeUtils.GetKRPCTypeName (typeof(IList<TestService.CSharpEnum>)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetKRPCTypeName (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetKRPCTypeName (typeof(TestService)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetKRPCTypeName (typeof(IDictionary<double,string>)));
        }

        [Test]
        public void ParameterTypeAttributes ()
        {
            Assert.AreEqual (new string[]{ }, TypeUtils.ParameterTypeAttributes (0, typeof(string)));
            Assert.AreEqual (new string[]{ }, TypeUtils.ParameterTypeAttributes (3, typeof(long)));
            Assert.AreEqual (new [] { "ParameterType(1).Class(TestService.TestClass)" }, TypeUtils.ParameterTypeAttributes (1, typeof(TestService.TestClass)));
            Assert.AreEqual (new [] { "ParameterType(2).Enum(TestService.CSharpEnum)" }, TypeUtils.ParameterTypeAttributes (2, typeof(TestService.CSharpEnum)));
            Assert.AreEqual (new [] { "ParameterType(0).List(string)" }, TypeUtils.ParameterTypeAttributes (0, typeof(IList<string>)));
            Assert.AreEqual (new [] { "ParameterType(1).Dictionary(int32,string)" }, TypeUtils.ParameterTypeAttributes (1, typeof(IDictionary<int,string>)));
            Assert.AreEqual (new [] { "ParameterType(1).Dictionary(int32,List(Class(TestService.TestClass)))" }, TypeUtils.ParameterTypeAttributes (1, typeof(IDictionary<int,IList<TestService.TestClass>>)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ParameterTypeAttributes (0, typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ParameterTypeAttributes (0, typeof(TestService)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ParameterTypeAttributes (0, typeof(IDictionary<double,string>)));
        }

        [Test]
        public void ReturnTypeAttributes ()
        {
            Assert.AreEqual (new string[]{ }, TypeUtils.ReturnTypeAttributes (typeof(string)));
            Assert.AreEqual (new string[]{ }, TypeUtils.ReturnTypeAttributes (typeof(long)));
            Assert.AreEqual (new [] { "ReturnType.Class(TestService.TestClass)" }, TypeUtils.ReturnTypeAttributes (typeof(TestService.TestClass)));
            Assert.AreEqual (new [] { "ReturnType.Enum(TestService.CSharpEnum)" }, TypeUtils.ReturnTypeAttributes (typeof(TestService.CSharpEnum)));
            Assert.AreEqual (new [] { "ReturnType.List(string)" }, TypeUtils.ReturnTypeAttributes (typeof(IList<string>)));
            Assert.AreEqual (new [] { "ReturnType.Dictionary(int32,string)" }, TypeUtils.ReturnTypeAttributes (typeof(IDictionary<int,string>)));
            Assert.AreEqual (new [] { "ReturnType.Dictionary(int32,List(Class(TestService.TestClass)))" }, TypeUtils.ReturnTypeAttributes (typeof(IDictionary<int,IList<TestService.TestClass>>)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ReturnTypeAttributes (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ReturnTypeAttributes (typeof(TestService)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ReturnTypeAttributes (typeof(IDictionary<double,string>)));
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


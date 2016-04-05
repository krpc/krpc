using System;
using System.Collections.Generic;
using NUnit.Framework;
using KRPC.Service;
using KRPC.Service.Messages;

namespace KRPC.Test.Service
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
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(Status)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(TestService.TestClass)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(TestService.TestEnum)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(TestService.TestEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(TestService)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(IList<string>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(IDictionary<int,string>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(HashSet<long>)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(Tuple<long>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(KRPC.Utils.Tuple<long>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(KRPC.Utils.Tuple<long,int>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(KRPC.Utils.Tuple<long,int,string>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(IList<IDictionary<int,string>>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(IList<TestService.TestClass>)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(IList<TestService.TestEnum>)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(IDictionary<double,string>)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(IDictionary<TestService.TestClass,string>)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(IList<TestService.TestEnumWithoutAttribute>)));
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
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(Status)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(TestService.TestEnum)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(TestService.TestEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(TestService)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IList<string>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IDictionary<int,string>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(HashSet<long>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(KRPC.Utils.Tuple<long>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(KRPC.Utils.Tuple<long,int>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(KRPC.Utils.Tuple<long,int,string>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IList<IDictionary<int,string>>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IList<TestService.TestClass>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IList<TestService.TestEnum>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IDictionary<double,string>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IDictionary<TestService.TestClass,string>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IList<TestService.TestEnumWithoutAttribute>)));
            Assert.IsFalse (TypeUtils.IsAValidKeyType (typeof(IEnumerable<string>)));
        }

        [Test]
        public void IsAClassType ()
        {
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(long)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(Status)));
            Assert.IsTrue (TypeUtils.IsAClassType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(TestService.TestEnum)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(TestService.TestEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(TestService)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(IList<string>)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(IDictionary<int,string>)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(HashSet<long>)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(KRPC.Utils.Tuple<long>)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(KRPC.Utils.Tuple<long,int>)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(KRPC.Utils.Tuple<long,int,string>)));
        }

        [Test]
        public void IsAnEnumType ()
        {
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(long)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(Status)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(TestService.TestClass)));
            Assert.IsTrue (TypeUtils.IsAnEnumType (typeof(TestService.TestEnum)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(TestService.TestEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(TestService)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(IList<string>)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(IDictionary<int,string>)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(HashSet<long>)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(KRPC.Utils.Tuple<long>)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(KRPC.Utils.Tuple<long,int>)));
            Assert.IsFalse (TypeUtils.IsAnEnumType (typeof(KRPC.Utils.Tuple<long,int,string>)));
        }

        [Test]
        public void IsACollectionType ()
        {
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(long)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(Status)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(TestService.TestEnum)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(TestService.TestEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(TestService)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(IList<string>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(IDictionary<int,string>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(HashSet<long>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(KRPC.Utils.Tuple<long>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(KRPC.Utils.Tuple<long,int>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(KRPC.Utils.Tuple<long,int,string>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(IList<IDictionary<int,string>>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(IList<TestService.TestClass>)));
            Assert.IsTrue (TypeUtils.IsACollectionType (typeof(IList<TestService.TestEnum>)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(IDictionary<double,string>)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(IDictionary<TestService.TestClass,string>)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(IList<TestService.TestEnumWithoutAttribute>)));
            Assert.IsFalse (TypeUtils.IsACollectionType (typeof(IEnumerable<string>)));
        }

        [Test]
        public void IsAListCollectionType ()
        {
            //FIXME: enable tests
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(string)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(long)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(Status)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(TestService.TestClass)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(TestService.TestEnum)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(TestService.TestEnumWithoutAttribute)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(TestService)));
//            Assert.IsTrue (TypeUtils.IsAListCollectionType (typeof(IList<string>)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(IDictionary<int,string>)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(HashSet<long>)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(KRPC.Utils.Tuple<long>)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(KRPC.Utils.Tuple<long,int>)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(KRPC.Utils.Tuple<long,int,string>)));
//            Assert.IsTrue (TypeUtils.IsAListCollectionType (typeof(IList<IDictionary<int,string>>)));
//            Assert.IsTrue (TypeUtils.IsAListCollectionType (typeof(IList<TestService.TestClass>)));
//            Assert.IsTrue (TypeUtils.IsAListCollectionType (typeof(IList<TestService.TestEnum>)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(IDictionary<double,string>)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(IDictionary<TestService.TestClass,string>)));
            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(IList<TestService.TestEnumWithoutAttribute>)));
//            Assert.IsFalse (TypeUtils.IsAListCollectionType (typeof(IEnumerable<string>)));
        }

        [Test]
        public void IsADictionaryCollectionType ()
        {
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(long)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(Status)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(TestService.TestEnum)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(TestService.TestEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(TestService)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IList<string>)));
            Assert.IsTrue (TypeUtils.IsADictionaryCollectionType (typeof(IDictionary<int,string>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(HashSet<long>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(KRPC.Utils.Tuple<long>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(KRPC.Utils.Tuple<long,int>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(KRPC.Utils.Tuple<long,int,string>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IList<IDictionary<int,string>>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IList<TestService.TestClass>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IList<TestService.TestEnum>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IDictionary<double,string>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IDictionary<TestService.TestClass,string>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IList<TestService.TestEnumWithoutAttribute>)));
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (typeof(IEnumerable<string>)));
        }

        [Test]
        public void IsATupleCollectionType ()
        {
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(long)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(Status)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(TestService.TestEnum)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(TestService.TestEnumWithoutAttribute)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(TestService)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(IList<string>)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(IDictionary<int,string>)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(HashSet<long>)));
            Assert.IsTrue (TypeUtils.IsATupleCollectionType (typeof(KRPC.Utils.Tuple<long>)));
            Assert.IsTrue (TypeUtils.IsATupleCollectionType (typeof(KRPC.Utils.Tuple<long,int>)));
            Assert.IsTrue (TypeUtils.IsATupleCollectionType (typeof(KRPC.Utils.Tuple<long,int,string>)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(IList<IDictionary<int,string>>)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(IList<TestService.TestClass>)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(IList<TestService.TestEnum>)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(IDictionary<double,string>)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(IDictionary<TestService.TestClass,string>)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(IList<TestService.TestEnumWithoutAttribute>)));
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (typeof(IEnumerable<string>)));
        }

        [Test]
        public void GetTypeName ()
        {
            Assert.AreEqual ("string", TypeUtils.GetTypeName (typeof(string)));
            Assert.AreEqual ("int64", TypeUtils.GetTypeName (typeof(long)));
            Assert.AreEqual ("KRPC.Status", TypeUtils.GetTypeName (typeof(Status)));
            Assert.AreEqual ("uint64", TypeUtils.GetTypeName (typeof(TestService.TestClass)));
            Assert.AreEqual ("int32", TypeUtils.GetTypeName (typeof(TestService.TestEnum)));
            Assert.AreEqual ("KRPC.List", TypeUtils.GetTypeName (typeof(IList<string>)));
            Assert.AreEqual ("KRPC.Dictionary", TypeUtils.GetTypeName (typeof(IDictionary<int,string>)));
            Assert.AreEqual ("KRPC.Set", TypeUtils.GetTypeName (typeof(HashSet<long>)));
            Assert.AreEqual ("KRPC.Tuple", TypeUtils.GetTypeName (typeof(KRPC.Utils.Tuple<long>)));
            Assert.AreEqual ("KRPC.Tuple", TypeUtils.GetTypeName (typeof(KRPC.Utils.Tuple<long,int>)));
            Assert.AreEqual ("KRPC.Tuple", TypeUtils.GetTypeName (typeof(KRPC.Utils.Tuple<long,int,string>)));
            Assert.AreEqual ("KRPC.List", TypeUtils.GetTypeName (typeof(IList<IDictionary<int,string>>)));
            Assert.AreEqual ("KRPC.List", TypeUtils.GetTypeName (typeof(IList<TestService.TestClass>)));
            Assert.AreEqual ("KRPC.List", TypeUtils.GetTypeName (typeof(IList<TestService.TestEnum>)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetTypeName (typeof(TestService.TestEnumWithoutAttribute)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetTypeName (typeof(TestService)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetTypeName (typeof(IDictionary<double,string>)));
        }

        [Test]
        public void GetFullTypeName ()
        {
            Assert.AreEqual ("string", TypeUtils.GetFullTypeName (typeof(string)));
            Assert.AreEqual ("int64", TypeUtils.GetFullTypeName (typeof(long)));
            Assert.AreEqual ("KRPC.Status", TypeUtils.GetFullTypeName (typeof(Status)));
            Assert.AreEqual ("Class(TestService.TestClass)", TypeUtils.GetFullTypeName (typeof(TestService.TestClass)));
            Assert.AreEqual ("Enum(TestService.TestEnum)", TypeUtils.GetFullTypeName (typeof(TestService.TestEnum)));
            Assert.AreEqual ("List(string)", TypeUtils.GetFullTypeName (typeof(IList<string>)));
            Assert.AreEqual ("Dictionary(int32,string)", TypeUtils.GetFullTypeName (typeof(IDictionary<int,string>)));
            Assert.AreEqual ("Set(int64)", TypeUtils.GetFullTypeName (typeof(HashSet<long>)));
            Assert.AreEqual ("Tuple(int64)", TypeUtils.GetFullTypeName (typeof(KRPC.Utils.Tuple<long>)));
            Assert.AreEqual ("Tuple(int64,int32)", TypeUtils.GetFullTypeName (typeof(KRPC.Utils.Tuple<long,int>)));
            Assert.AreEqual ("Tuple(int64,int32,string)", TypeUtils.GetFullTypeName (typeof(KRPC.Utils.Tuple<long,int,string>)));
            Assert.AreEqual ("List(Dictionary(int32,string))", TypeUtils.GetFullTypeName (typeof(IList<IDictionary<int,string>>)));
            Assert.AreEqual ("List(Class(TestService.TestClass))", TypeUtils.GetFullTypeName (typeof(IList<TestService.TestClass>)));
            Assert.AreEqual ("List(Enum(TestService.TestEnum))", TypeUtils.GetFullTypeName (typeof(IList<TestService.TestEnum>)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetFullTypeName (typeof(TestService.TestEnumWithoutAttribute)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetFullTypeName (typeof(TestService)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetFullTypeName (typeof(IDictionary<double,string>)));
        }

        [Test]
        public void ParameterTypeAttributes ()
        {
            Assert.AreEqual (new string[]{ }, TypeUtils.ParameterTypeAttributes (0, typeof(string)));
            Assert.AreEqual (new string[]{ }, TypeUtils.ParameterTypeAttributes (3, typeof(long)));
            Assert.AreEqual (new [] { "ParameterType(1).Class(TestService.TestClass)" }, TypeUtils.ParameterTypeAttributes (1, typeof(TestService.TestClass)));
            Assert.AreEqual (new [] { "ParameterType(2).Enum(TestService.TestEnum)" }, TypeUtils.ParameterTypeAttributes (2, typeof(TestService.TestEnum)));
            Assert.AreEqual (new [] { "ParameterType(0).List(string)" }, TypeUtils.ParameterTypeAttributes (0, typeof(IList<string>)));
            Assert.AreEqual (new [] { "ParameterType(1).Dictionary(int32,string)" }, TypeUtils.ParameterTypeAttributes (1, typeof(IDictionary<int,string>)));
            Assert.AreEqual (new [] { "ParameterType(2).Set(int64)" }, TypeUtils.ParameterTypeAttributes (2, typeof(HashSet<long>)));
            Assert.AreEqual (new [] { "ParameterType(1).Dictionary(int32,List(Class(TestService.TestClass)))" }, TypeUtils.ParameterTypeAttributes (1, typeof(IDictionary<int,IList<TestService.TestClass>>)));
            Assert.AreEqual (new [] { "ParameterType(3).Tuple(int64)" }, TypeUtils.ParameterTypeAttributes (3, typeof(KRPC.Utils.Tuple<long>)));
            Assert.AreEqual (new [] { "ParameterType(3).Tuple(int64,int32)" }, TypeUtils.ParameterTypeAttributes (3, typeof(KRPC.Utils.Tuple<long,int>)));
            Assert.AreEqual (new [] { "ParameterType(3).Tuple(int64,int32,bool)" }, TypeUtils.ParameterTypeAttributes (3, typeof(KRPC.Utils.Tuple<long,int,bool>)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ParameterTypeAttributes (0, typeof(TestService.TestEnumWithoutAttribute)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ParameterTypeAttributes (0, typeof(TestService)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ParameterTypeAttributes (0, typeof(IDictionary<double,string>)));
        }

        [Test]
        public void ReturnTypeAttributes ()
        {
            Assert.AreEqual (new string[]{ }, TypeUtils.ReturnTypeAttributes (typeof(string)));
            Assert.AreEqual (new string[]{ }, TypeUtils.ReturnTypeAttributes (typeof(long)));
            Assert.AreEqual (new [] { "ReturnType.Class(TestService.TestClass)" }, TypeUtils.ReturnTypeAttributes (typeof(TestService.TestClass)));
            Assert.AreEqual (new [] { "ReturnType.Enum(TestService.TestEnum)" }, TypeUtils.ReturnTypeAttributes (typeof(TestService.TestEnum)));
            Assert.AreEqual (new [] { "ReturnType.List(string)" }, TypeUtils.ReturnTypeAttributes (typeof(IList<string>)));
            Assert.AreEqual (new [] { "ReturnType.Dictionary(int32,string)" }, TypeUtils.ReturnTypeAttributes (typeof(IDictionary<int,string>)));
            Assert.AreEqual (new [] { "ReturnType.Set(int64)" }, TypeUtils.ReturnTypeAttributes (typeof(HashSet<long>)));
            Assert.AreEqual (new [] { "ReturnType.Dictionary(int32,List(Class(TestService.TestClass)))" }, TypeUtils.ReturnTypeAttributes (typeof(IDictionary<int,IList<TestService.TestClass>>)));
            Assert.AreEqual (new [] { "ReturnType.Tuple(int64)" }, TypeUtils.ReturnTypeAttributes (typeof(KRPC.Utils.Tuple<long>)));
            Assert.AreEqual (new [] { "ReturnType.Tuple(int64,int32)" }, TypeUtils.ReturnTypeAttributes (typeof(KRPC.Utils.Tuple<long,int>)));
            Assert.AreEqual (new [] { "ReturnType.Tuple(int64,int32,bool)" }, TypeUtils.ReturnTypeAttributes (typeof(KRPC.Utils.Tuple<long,int,bool>)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ReturnTypeAttributes (typeof(TestService.TestEnumWithoutAttribute)));
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
        public void GetServiceGameScene ()
        {
            Assert.AreEqual (GameScene.Flight, TypeUtils.GetServiceGameScene (typeof(TestService)));
            Assert.AreEqual (GameScene.All, TypeUtils.GetServiceGameScene (typeof(TestService2)));
            Assert.AreEqual (GameScene.Editor, TypeUtils.GetServiceGameScene (typeof(TestService3)));
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
            Assert.AreEqual ("TestService", TypeUtils.GetEnumServiceName (typeof(TestService.TestEnum)));
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


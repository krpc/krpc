using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using KRPC.Service.Messages;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class TypeUtilsTest
    {
        [TestCase ("IdentifierName")]
        [TestCase ("Foo123")]
        public void IsAValidIdentifier (string input)
        {
            Assert.IsTrue (TypeUtils.IsAValidIdentifier (input));
        }

        [TestCase ("123Foo")]
        [TestCase ("")]
        [TestCase ("_Foo")]
        [TestCase ("Foo%")]
        public void IsNotAValidIdentifier (string input)
        {
            Assert.IsFalse (TypeUtils.IsAValidIdentifier (input));
        }

        [TestCase (typeof(string))]
        [TestCase (typeof(long))]
        [TestCase (typeof(Status))]
        [TestCase (typeof(TestService.TestClass))]
        [TestCase (typeof(TestService.TestEnum))]
        [TestCase (typeof(IList<string>))]
        [TestCase (typeof(IDictionary<int,string>))]
        [TestCase (typeof(HashSet<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int,string>))]
        [TestCase (typeof(IList<IDictionary<int,string>>))]
        [TestCase (typeof(IList<TestService.TestClass>))]
        [TestCase (typeof(IList<TestService.TestEnum>))]
        public void IsAValidType (Type type)
        {
            Assert.IsTrue (TypeUtils.IsAValidType (type));
        }

        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(Tuple<long>))]
        [TestCase (typeof(IDictionary<double,string>))]
        [TestCase (typeof(IDictionary<TestService.TestClass,string>))]
        [TestCase (typeof(IList<TestService.TestEnumWithoutAttribute>))]
        [TestCase (typeof(IEnumerable<string>))]
        public void IsNotAValidType (Type type)
        {
            Assert.IsFalse (TypeUtils.IsAValidType (type));
        }

        [TestCase (typeof(string))]
        [TestCase (typeof(int))]
        [TestCase (typeof(uint))]
        [TestCase (typeof(ulong))]
        [TestCase (typeof(long))]
        [TestCase (typeof(bool))]
        public void IsAValidKeyType (Type type)
        {
            Assert.IsTrue (TypeUtils.IsAValidKeyType (type));
        }

        [TestCase (typeof(float))]
        [TestCase (typeof(double))]
        [TestCase (typeof(Status))]
        [TestCase (typeof(TestService.TestClass))]
        [TestCase (typeof(TestService.TestEnum))]
        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IList<string>))]
        [TestCase (typeof(IDictionary<int,string>))]
        [TestCase (typeof(HashSet<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int,string>))]
        [TestCase (typeof(IList<IDictionary<int,string>>))]
        [TestCase (typeof(IList<TestService.TestClass>))]
        [TestCase (typeof(IList<TestService.TestEnum>))]
        [TestCase (typeof(IDictionary<double,string>))]
        [TestCase (typeof(IDictionary<TestService.TestClass,string>))]
        [TestCase (typeof(IList<TestService.TestEnumWithoutAttribute>))]
        [TestCase (typeof(IEnumerable<string>))]
        public void IsNotAValidKeyType (Type type)
        {
            Assert.IsFalse (TypeUtils.IsAValidKeyType (type));
        }

        [TestCase (typeof(TestService.TestClass))]
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidUnnecessarySpecializationRule")]
        public void IsAClassType (Type type)
        {
            Assert.IsTrue (TypeUtils.IsAClassType (type));
        }

        [TestCase (typeof(string))]
        [TestCase (typeof(long))]
        [TestCase (typeof(Status))]
        [TestCase (typeof(TestService.TestEnum))]
        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IList<string>))]
        [TestCase (typeof(IDictionary<int,string>))]
        [TestCase (typeof(HashSet<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int,string>))]
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidUnnecessarySpecializationRule")]
        public void IsNotAClassType (Type type)
        {
            Assert.IsFalse (TypeUtils.IsAClassType (type));
        }

        [TestCase (typeof(TestService.TestEnum))]
        public void IsAnEnumType (Type type)
        {
            Assert.IsTrue (TypeUtils.IsAnEnumType (type));
        }

        [TestCase (typeof(string))]
        [TestCase (typeof(long))]
        [TestCase (typeof(Status))]
        [TestCase (typeof(TestService.TestClass))]
        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IList<string>))]
        [TestCase (typeof(IDictionary<int,string>))]
        [TestCase (typeof(HashSet<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int,string>))]
        public void IsNotAnEnumType (Type type)
        {
            Assert.IsFalse (TypeUtils.IsAnEnumType (type));
        }

        [TestCase (typeof(IList<string>))]
        [TestCase (typeof(IDictionary<int,string>))]
        [TestCase (typeof(HashSet<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int,string>))]
        [TestCase (typeof(IList<IDictionary<int,string>>))]
        [TestCase (typeof(IList<TestService.TestClass>))]
        [TestCase (typeof(IList<TestService.TestEnum>))]
        public void IsACollectionType (Type type)
        {
            Assert.IsTrue (TypeUtils.IsACollectionType (type));
        }

        [TestCase (typeof(string))]
        [TestCase (typeof(long))]
        [TestCase (typeof(Status))]
        [TestCase (typeof(TestService.TestClass))]
        [TestCase (typeof(TestService.TestEnum))]
        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IDictionary<double,string>))]
        [TestCase (typeof(IDictionary<TestService.TestClass,string>))]
        [TestCase (typeof(IList<TestService.TestEnumWithoutAttribute>))]
        [TestCase (typeof(IEnumerable<string>))]
        public void IsNotACollectionType (Type type)
        {
            Assert.IsFalse (TypeUtils.IsACollectionType (type));
        }

        [TestCase (typeof(IList<string>))]
        [TestCase (typeof(IList<IDictionary<int,string>>))]
        [TestCase (typeof(IList<TestService.TestClass>))]
        [TestCase (typeof(IList<TestService.TestEnum>))]
        public void IsAListCollectionType (Type type)
        {
            Assert.IsTrue (TypeUtils.IsAListCollectionType (type));
        }

        [TestCase (typeof(string))]
        [TestCase (typeof(long))]
        [TestCase (typeof(Status))]
        [TestCase (typeof(TestService.TestClass))]
        [TestCase (typeof(TestService.TestEnum))]
        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IDictionary<int,string>))]
        [TestCase (typeof(HashSet<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int,string>))]
        [TestCase (typeof(IDictionary<double,string>))]
        [TestCase (typeof(IDictionary<TestService.TestClass,string>))]
        [TestCase (typeof(IList<TestService.TestEnumWithoutAttribute>))]
        [TestCase (typeof(IEnumerable<string>))]
        public void IsNotAListCollectionType (Type type)
        {
            Assert.IsFalse (TypeUtils.IsAListCollectionType (type));
        }

        [TestCase (typeof(IDictionary<int,string>))]
        public void IsADictionaryCollectionType (Type type)
        {
            Assert.IsTrue (TypeUtils.IsADictionaryCollectionType (type));
        }

        [TestCase (typeof(string))]
        [TestCase (typeof(long))]
        [TestCase (typeof(Status))]
        [TestCase (typeof(TestService.TestClass))]
        [TestCase (typeof(TestService.TestEnum))]
        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IList<string>))]
        [TestCase (typeof(HashSet<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int,string>))]
        [TestCase (typeof(IList<IDictionary<int,string>>))]
        [TestCase (typeof(IList<TestService.TestClass>))]
        [TestCase (typeof(IList<TestService.TestEnum>))]
        [TestCase (typeof(IDictionary<double,string>))]
        [TestCase (typeof(IDictionary<TestService.TestClass,string>))]
        [TestCase (typeof(IList<TestService.TestEnumWithoutAttribute>))]
        [TestCase (typeof(IEnumerable<string>))]
        public void IsNotADictionaryCollectionType (Type type)
        {
            Assert.IsFalse (TypeUtils.IsADictionaryCollectionType (type));
        }

        [TestCase (typeof(KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int,string>))]
        public void IsATupleCollectionType (Type type)
        {
            Assert.IsTrue (TypeUtils.IsATupleCollectionType (type));
        }

        [TestCase (typeof(string))]
        [TestCase (typeof(long))]
        [TestCase (typeof(Status))]
        [TestCase (typeof(TestService.TestClass))]
        [TestCase (typeof(TestService.TestEnum))]
        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IList<string>))]
        [TestCase (typeof(IDictionary<int,string>))]
        [TestCase (typeof(HashSet<long>))]
        [TestCase (typeof(IList<IDictionary<int,string>>))]
        [TestCase (typeof(IList<TestService.TestClass>))]
        [TestCase (typeof(IList<TestService.TestEnum>))]
        [TestCase (typeof(IDictionary<double,string>))]
        [TestCase (typeof(IDictionary<TestService.TestClass,string>))]
        [TestCase (typeof(IList<TestService.TestEnumWithoutAttribute>))]
        [TestCase (typeof(IEnumerable<string>))]
        public void IsNotATupleCollectionType (Type type)
        {
            Assert.IsFalse (TypeUtils.IsATupleCollectionType (type));
        }

        [TestCase (typeof(string), "string")]
        [TestCase (typeof(long), "int64")]
        [TestCase (typeof(Status), "KRPC.Status")]
        [TestCase (typeof(TestService.TestClass), "uint64")]
        [TestCase (typeof(TestService.TestEnum), "int32")]
        [TestCase (typeof(IList<string>), "KRPC.List")]
        [TestCase (typeof(IDictionary<int,string>), "KRPC.Dictionary")]
        [TestCase (typeof(HashSet<long>), "KRPC.Set")]
        [TestCase (typeof(KRPC.Utils.Tuple<long>), "KRPC.Tuple")]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int>), "KRPC.Tuple")]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int,string>), "KRPC.Tuple")]
        [TestCase (typeof(IList<IDictionary<int,string>>), "KRPC.List")]
        [TestCase (typeof(IList<TestService.TestClass>), "KRPC.List")]
        [TestCase (typeof(IList<TestService.TestEnum>), "KRPC.List")]
        public void GetTypeName (Type type, string name)
        {
            Assert.AreEqual (name, TypeUtils.GetTypeName (type));
        }

        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IDictionary<double,string>))]
        public void InvalidGetTypeName (Type type)
        {
            Assert.Throws<ArgumentException> (() => TypeUtils.GetTypeName (type));
        }

        [TestCase ("string", typeof(string))]
        [TestCase ("int64", typeof(long))]
        [TestCase ("KRPC.Status", typeof(Status))]
        [TestCase ("Class(TestService.TestClass)", typeof(TestService.TestClass))]
        [TestCase ("Enum(TestService.TestEnum)", typeof(TestService.TestEnum))]
        [TestCase ("List(string)", typeof(IList<string>))]
        [TestCase ("Dictionary(int32,string)", typeof(IDictionary<int,string>))]
        [TestCase ("Set(int64)", typeof(HashSet<long>))]
        [TestCase ("Tuple(int64)", typeof(KRPC.Utils.Tuple<long>))]
        [TestCase ("Tuple(int64,int32)", typeof(KRPC.Utils.Tuple<long,int>))]
        [TestCase ("Tuple(int64,int32,string)", typeof(KRPC.Utils.Tuple<long,int,string>))]
        [TestCase ("List(Dictionary(int32,string))", typeof(IList<IDictionary<int,string>>))]
        [TestCase ("List(Class(TestService.TestClass))", typeof(IList<TestService.TestClass>))]
        [TestCase ("List(Enum(TestService.TestEnum))", typeof(IList<TestService.TestEnum>))]
        public void GetFullTypeName (string name, Type type)
        {
            Assert.AreEqual (name, TypeUtils.GetFullTypeName (type));
        }

        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IDictionary<double,string>))]
        public void InvalidGetFullTypeName (Type type)
        {
            Assert.Throws<ArgumentException> (() => TypeUtils.GetFullTypeName (type));
        }

        [TestCase (typeof(string), 0, new string[]{ })]
        [TestCase (typeof(long), 3, new string[]{ })]
        [TestCase (typeof(TestService.TestClass), 1, new [] { "ParameterType(1).Class(TestService.TestClass)" })]
        [TestCase (typeof(TestService.TestEnum), 2, new [] { "ParameterType(2).Enum(TestService.TestEnum)" })]
        [TestCase (typeof(IList<string>), 0, new [] { "ParameterType(0).List(string)" })]
        [TestCase (typeof(IDictionary<int,string>), 1, new [] { "ParameterType(1).Dictionary(int32,string)" })]
        [TestCase (typeof(HashSet<long>), 2, new [] { "ParameterType(2).Set(int64)" })]
        [TestCase (typeof(IDictionary<int,IList<TestService.TestClass>>), 1, new [] { "ParameterType(1).Dictionary(int32,List(Class(TestService.TestClass)))" })]
        [TestCase (typeof(KRPC.Utils.Tuple<long>), 3, new [] { "ParameterType(3).Tuple(int64)" })]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int>), 3, new [] { "ParameterType(3).Tuple(int64,int32)" })]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int,bool>), 3, new [] { "ParameterType(3).Tuple(int64,int32,bool)" })]
        public void ParameterTypeAttributes (Type type, int position, string[] attributes)
        {
            CollectionAssert.AreEqual (attributes, TypeUtils.ParameterTypeAttributes (position, type));
        }

        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IDictionary<double,string>))]
        public void InvalidParameterTypeAttributes (Type type)
        {
            Assert.Throws<ArgumentException> (() => TypeUtils.ParameterTypeAttributes (0, type));
        }

        [TestCase (typeof(string), new string[]{ })]
        [TestCase (typeof(long), new string[]{ })]
        [TestCase (typeof(TestService.TestClass), new [] { "ReturnType.Class(TestService.TestClass)" })]
        [TestCase (typeof(TestService.TestEnum), new [] { "ReturnType.Enum(TestService.TestEnum)" })]
        [TestCase (typeof(IList<string>), new [] { "ReturnType.List(string)" })]
        [TestCase (typeof(IDictionary<int,string>), new [] { "ReturnType.Dictionary(int32,string)" })]
        [TestCase (typeof(HashSet<long>), new [] { "ReturnType.Set(int64)" })]
        [TestCase (typeof(IDictionary<int,IList<TestService.TestClass>>), new [] { "ReturnType.Dictionary(int32,List(Class(TestService.TestClass)))" })]
        [TestCase (typeof(KRPC.Utils.Tuple<long>), new [] { "ReturnType.Tuple(int64)" })]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int>), new [] { "ReturnType.Tuple(int64,int32)" })]
        [TestCase (typeof(KRPC.Utils.Tuple<long,int,bool>), new [] { "ReturnType.Tuple(int64,int32,bool)" })]
        public void ReturnTypeAttributes (Type type, string[] attributes)
        {
            CollectionAssert.AreEqual (attributes, TypeUtils.ReturnTypeAttributes (type));
        }

        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IDictionary<double,string>))]
        public void InvalidReturnTypeAttributes (Type type)
        {
            Assert.Throws<ArgumentException> (() => TypeUtils.ReturnTypeAttributes (type));
        }

        [TestCase (typeof(TestService), "TestService")]
        [TestCase (typeof(TestService2), "TestService2")]
        [TestCase (typeof(TestService3), "TestService3Name")]
        public void GetServiceName (Type type, string name)
        {
            Assert.AreEqual (name, TypeUtils.GetServiceName (type));
        }

        [TestCase (typeof(TestService), GameScene.Flight)]
        [TestCase (typeof(TestService2), GameScene.All)]
        [TestCase (typeof(TestService3), GameScene.Editor)]
        public void GetServiceGameScene (Type type, GameScene gameScene)
        {
            Assert.AreEqual (gameScene, TypeUtils.GetServiceGameScene (type));
        }

        [TestCase (typeof(TestService.TestClass), "TestService")]
        [TestCase (typeof(TestClass3), "TestService3Name")]
        [TestCase (typeof(TestTopLevelClass), "TestService")]
        public void GetClassServiceName (Type type, string name)
        {
            Assert.AreEqual (name, TypeUtils.GetClassServiceName (type));
        }

        [TestCase (typeof(TestService.TestEnum), "TestService")]
        public void GetEnumServiceName (Type type, string name)
        {
            Assert.AreEqual (name, TypeUtils.GetEnumServiceName (type));
        }

        [TestCase ("IdentifierName")]
        [TestCase ("Foo123")]
        public void ValidIdentifier (string identifier)
        {
            Assert.DoesNotThrow (() => TypeUtils.ValidateIdentifier (identifier));
            Assert.DoesNotThrow (() => TypeUtils.ValidateIdentifier (identifier));
        }

        [TestCase ("123Foo")]
        [TestCase ("")]
        [TestCase ("_Foo")]
        [TestCase ("Foo%")]
        public void InvalidIdentifier (string identifier)
        {
            Assert.Throws<ServiceException> (() => TypeUtils.ValidateIdentifier (identifier));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using KRPC.Service.Messages;
using NUnit.Framework;
using Newtonsoft.Json;

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
        [TestCase ("foo")]
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
        [TestCase (typeof(global::KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int,string>))]
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
        [TestCase (typeof(global::KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int,string>))]
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
        [TestCase (typeof(global::KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int,string>))]
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
        [TestCase (typeof(global::KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int,string>))]
        public void IsNotAnEnumType (Type type)
        {
            Assert.IsFalse (TypeUtils.IsAnEnumType (type));
        }

        [TestCase (typeof(IList<string>))]
        [TestCase (typeof(IDictionary<int,string>))]
        [TestCase (typeof(HashSet<long>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int,string>))]
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
        [TestCase (typeof(global::KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int,string>))]
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
        [TestCase (typeof(global::KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int,string>))]
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

        [TestCase (typeof(global::KRPC.Utils.Tuple<long>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int>))]
        [TestCase (typeof(global::KRPC.Utils.Tuple<long,int,string>))]
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

        [TestCase ("{\"code\":\"STRING\"}", typeof(string))]
        [TestCase ("{\"code\":\"SINT64\"}", typeof(long))]
        [TestCase ("{\"code\":\"STATUS\"}", typeof(Status))]
        [TestCase ("{\"code\":\"CLASS\",\"service\":\"TestService\",\"name\":\"TestClass\"}",
                   typeof(TestService.TestClass))]
        [TestCase ("{\"code\":\"ENUMERATION\",\"service\":\"TestService\",\"name\":\"TestEnum\"}",
                   typeof(TestService.TestEnum))]
        [TestCase ("{\"code\":\"LIST\",\"types\":[{\"code\":\"STRING\"}]}", typeof(IList<string>))]
        [TestCase ("{\"code\":\"DICTIONARY\",\"types\":[{\"code\":\"SINT32\"},{\"code\":\"STRING\"}]}",
                   typeof(IDictionary<int,string>))]
        [TestCase ("{\"code\":\"SET\",\"types\":[{\"code\":\"SINT64\"}]}", typeof(HashSet<long>))]
        [TestCase ("{\"code\":\"TUPLE\",\"types\":[{\"code\":\"SINT64\"}]}", typeof(global::KRPC.Utils.Tuple<long>))]
        [TestCase ("{\"code\":\"TUPLE\",\"types\":[{\"code\":\"SINT64\"},{\"code\":\"SINT32\"}]}",
                   typeof(global::KRPC.Utils.Tuple<long,int>))]
        [TestCase ("{\"code\":\"TUPLE\",\"types\":[{\"code\":\"SINT64\"}," +
                   "{\"code\":\"SINT32\"},{\"code\":\"STRING\"}]}",
                   typeof(global::KRPC.Utils.Tuple<long,int,string>))]
        [TestCase ("{\"code\":\"LIST\",\"types\":[" +
                   "{\"code\":\"DICTIONARY\",\"types\":[{\"code\":\"SINT32\"},{\"code\":\"STRING\"}]}" +
                   "]}", typeof(IList<IDictionary<int,string>>))]
        [TestCase ("{\"code\":\"LIST\",\"types\":[" +
                   "{\"code\":\"CLASS\",\"service\":\"TestService\",\"name\":\"TestClass\"}" +
                   "]}", typeof(IList<TestService.TestClass>))]
        [TestCase ("{\"code\":\"LIST\",\"types\":[" +
                   "{\"code\":\"ENUMERATION\",\"service\":\"TestService\",\"name\":\"TestEnum\"}" +
                   "]}", typeof(IList<TestService.TestEnum>))]
        public void SerializeType (string name, Type type)
        {
            Assert.AreEqual (name, JsonConvert.SerializeObject (TypeUtils.SerializeType (type)));
        }

        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IDictionary<double,string>))]
        public void InvalidSerializeType (Type type)
        {
            Assert.Throws<ArgumentException> (() => TypeUtils.SerializeType (type));
        }
    }
}

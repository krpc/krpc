using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KRPC.Service;
using KRPC.Service.Attributes;
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
        [TestCase (typeof(Tuple<long>))]
        [TestCase (typeof(Tuple<long,int>))]
        [TestCase (typeof(Tuple<long,int,string>))]
        [TestCase (typeof(IList<IDictionary<int,string>>))]
        [TestCase (typeof(IList<TestService.TestClass>))]
        [TestCase (typeof(IList<TestService.TestEnum>))]
        [TestCase (typeof(TestService.TestStruct))]
        [TestCase (typeof(TestService.TestNestedStruct))]
        [TestCase (typeof(IList<TestService.TestStruct>))]
        // A Nullable<T> is valid wherever T is, at a position that allows a null
        [TestCase (typeof(int?))]
        [TestCase (typeof(IList<int?>))]
        [TestCase (typeof(IDictionary<string,int?>))]
        [TestCase (typeof(Tuple<int?,string>))]
        [TestCase (typeof(IList<TestService.TestEnum?>))]
        public void IsAValidType (Type type)
        {
            Assert.IsTrue (TypeUtils.IsAValidType (type));
        }

        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IDictionary<double,string>))]
        [TestCase (typeof(IDictionary<TestService.TestClass,string>))]
        [TestCase (typeof(IList<TestService.TestEnumWithoutAttribute>))]
        [TestCase (typeof(IEnumerable<string>))]
        [TestCase (typeof(StructWithoutFields))]
        [TestCase (typeof(IList<StructWithoutFields>))]
        // A set element and a dictionary key are the two positions that cannot hold a null,
        // so a Nullable<T> at either one is not a type a service can declare
        [TestCase (typeof(HashSet<int?>))]
        [TestCase (typeof(IDictionary<int?,string>))]
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
        [TestCase (typeof(Tuple<long>))]
        [TestCase (typeof(Tuple<long,int>))]
        [TestCase (typeof(Tuple<long,int,string>))]
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
        [TestCase (typeof(Tuple<long>))]
        [TestCase (typeof(Tuple<long,int>))]
        [TestCase (typeof(Tuple<long,int,string>))]
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
        [TestCase (typeof(Tuple<long>))]
        [TestCase (typeof(Tuple<long,int>))]
        [TestCase (typeof(Tuple<long,int,string>))]
        public void IsNotAnEnumType (Type type)
        {
            Assert.IsFalse (TypeUtils.IsAnEnumType (type));
        }

        [TestCase (typeof(IList<string>))]
        [TestCase (typeof(IDictionary<int,string>))]
        [TestCase (typeof(HashSet<long>))]
        [TestCase (typeof(Tuple<long>))]
        [TestCase (typeof(Tuple<long,int>))]
        [TestCase (typeof(Tuple<long,int,string>))]
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
        [TestCase (typeof(Tuple<long>))]
        [TestCase (typeof(Tuple<long,int>))]
        [TestCase (typeof(Tuple<long,int,string>))]
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
        [TestCase (typeof(Tuple<long>))]
        [TestCase (typeof(Tuple<long,int>))]
        [TestCase (typeof(Tuple<long,int,string>))]
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

        [TestCase (typeof(Tuple<long>))]
        [TestCase (typeof(Tuple<long,int>))]
        [TestCase (typeof(Tuple<long,int,string>))]
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

        [TestCase (typeof(TestService), 857051661)]
        [TestCase (typeof(TestService2), 333031042)]
        [TestCase (typeof(TestService3), 1234)]
        public void GetServiceId (Type type, int id)
        {
            Assert.AreEqual (id, TypeUtils.GetServiceId(type));
        }

        [TestCase (typeof(TestService), GameScene.Flight)]
        [TestCase (typeof(TestService2), GameScene.All)]
        [TestCase (typeof(TestService3), GameScene.Editor)]
        public void GetServiceGameScene (Type type, GameScene gameScene)
        {
            Assert.AreEqual (gameScene, TypeUtils.GetServiceGameScene (type));
        }

        [TestCase (typeof(TestService), "ProcedureAvailableInInheritedGameScene", GameScene.Flight)]
        [TestCase (typeof(TestService), "ProcedureAvailableInSpecifiedGameScene", GameScene.EditorVAB)]
        public void GetProcedureGameScene (Type service, string name, GameScene gameScene)
        {
            var serviceGameScene = TypeUtils.GetServiceGameScene (service);
            var method = service.GetMethod(name);
            Assert.AreEqual (gameScene, TypeUtils.GetProcedureGameScene (method, serviceGameScene));
        }

        [TestCase (typeof(TestService), "PropertyAvailableInInheritedGameScene", GameScene.Flight)]
        [TestCase (typeof(TestService), "PropertyAvailableInSpecifiedGameScene", GameScene.EditorVAB)]
        public void GetPropertyGameScene (Type service, string name, GameScene gameScene)
        {
            var serviceGameScene = TypeUtils.GetServiceGameScene (service);
            var property = service.GetProperty(name);
            Assert.AreEqual (gameScene, TypeUtils.GetPropertyGameScene (property, serviceGameScene));
        }

        [TestCase (typeof(TestService.TestClass), "TestService")]
        [TestCase (typeof(TestClass3), "TestService3Name")]
        [TestCase (typeof(TestTopLevelClass), "TestService")]
        public void GetClassServiceName (Type type, string name)
        {
            Assert.AreEqual (name, TypeUtils.GetClassServiceName (type));
        }

        [TestCase (typeof(TestService), typeof(TestService.TestClass), GameScene.Flight | GameScene.SpaceCenter)]
        [TestCase (typeof(TestService3), typeof(TestClass3), GameScene.Editor)]
        public void GetClassGameScene (Type service, Type cls, GameScene gameScene)
        {
            var serviceGameScene = TypeUtils.GetServiceGameScene (service);
            Assert.AreEqual (gameScene, TypeUtils.GetClassGameScene (cls, serviceGameScene));
        }

        [TestCase (typeof(TestService), typeof(TestService.TestClass), "MethodAvailableInInheritedGameScene", GameScene.Flight | GameScene.SpaceCenter)]
        [TestCase (typeof(TestService), typeof(TestService.TestClass), "MethodAvailableInSpecifiedGameScene", GameScene.EditorVAB)]
        public void GetClassMethodGameScene (Type service, Type cls, string name, GameScene gameScene)
        {
            var serviceGameScene = TypeUtils.GetServiceGameScene (service);
            var classGameScene = TypeUtils.GetClassGameScene (cls, serviceGameScene);
            var method = cls.GetMethod(name);
            Assert.AreEqual (gameScene, TypeUtils.GetMethodGameScene (cls, method, classGameScene));
        }

        [TestCase (typeof(TestService), typeof(TestService.TestClass), "ClassPropertyAvailableInInheritedGameScene", GameScene.Flight | GameScene.SpaceCenter)]
        [TestCase (typeof(TestService), typeof(TestService.TestClass), "ClassPropertyAvailableInSpecifiedGameScene", GameScene.EditorVAB)]
        public void GetClassPropertyGameScene (Type service, Type cls, string name, GameScene gameScene)
        {
            var serviceGameScene = TypeUtils.GetServiceGameScene (service);
            var classGameScene = TypeUtils.GetClassGameScene (cls, serviceGameScene);
            var property = cls.GetProperty(name);
            Assert.AreEqual (gameScene, TypeUtils.GetClassPropertyGameScene (cls, property, classGameScene));
        }

        [TestCase (typeof(TestService.TestEnum), "TestService")]
        public void GetEnumServiceName (Type type, string name)
        {
            Assert.AreEqual (name, TypeUtils.GetEnumServiceName (type));
        }

        [TestCase (typeof(TestService.TestStruct))]
        [TestCase (typeof(TestService.TestNestedStruct))]
        public void IsAStructType (Type type)
        {
            Assert.IsTrue (TypeUtils.IsAStructType (type));
        }

        [TestCase (typeof(string))]
        [TestCase (typeof(long))]
        [TestCase (typeof(Status))]
        [TestCase (typeof(TestService.TestClass))]
        [TestCase (typeof(TestService.TestEnum))]
        [TestCase (typeof(StructWithoutFields))]
        [TestCase (typeof(IList<TestService.TestStruct>))]
        public void IsNotAStructType (Type type)
        {
            Assert.IsFalse (TypeUtils.IsAStructType (type));
        }

        [TestCase (typeof(TestService.TestStruct), "TestService")]
        [TestCase (typeof(TestService.TestNestedStruct), "TestService")]
        public void GetStructServiceName (Type type, string name)
        {
            Assert.AreEqual (name, TypeUtils.GetStructServiceName (type));
        }

        [Test]
        public void GetStructFields ()
        {
            CollectionAssert.AreEqual (
                new [] { "IntField", "StringField", "EnumField", "ObjectField", "ListField" },
                TypeUtils.GetStructFields (typeof(TestService.TestStruct)).Select (x => x.Name).ToList ());
            CollectionAssert.AreEqual (
                new [] { "StructField", "IntField" },
                TypeUtils.GetStructFields (typeof(TestService.TestNestedStruct)).Select (x => x.Name).ToList ());
        }

        [TestCase (typeof(TestService.TestStruct))]
        [TestCase (typeof(TestService.TestNestedStruct))]
        public void ValidKRPCStruct (Type type)
        {
            Assert.DoesNotThrow (() => TypeUtils.ValidateKRPCStruct (type));
        }

        [Test]
        public void ValidateKRPCStructWithoutTheAttribute ()
        {
            Assert.Throws<ArgumentException> (
                () => TypeUtils.ValidateKRPCStruct (typeof(StructWithoutFields)));
        }

        // Structures whose fields break one of the rules. They do not carry the KRPCStruct
        // attribute, as the scanner finds every type that does and would report them for
        // every test in this assembly

        public struct StructWithoutFields
        {
            public int NotAField { get; set; }
        }

        public struct StructWithAnInvalidFieldType
        {
            [KRPCProperty]
            public TestService.TestEnumWithoutAttribute Field { get; set; }
        }

        public struct StructWithAFieldWithoutASetter
        {
            [KRPCProperty]
            public int Field {
                get { return 0; }
            }
        }

        public struct StructWithAGameSceneField
        {
            [KRPCProperty (GameScene = GameScene.Flight)]
            public int Field { get; set; }
        }

        public struct StructWithANullableFieldThatCannotBeNull
        {
            [KRPCProperty (Nullable = true)]
            public int Field { get; set; }
        }

        public struct StructWithANullableEnumFieldThatCannotBeNull
        {
            [KRPCProperty (Nullable = true)]
            public TestService.TestEnum Field { get; set; }
        }

        [TestCase (typeof(StructWithoutFields))]
        [TestCase (typeof(StructWithAnInvalidFieldType))]
        [TestCase (typeof(StructWithAFieldWithoutASetter))]
        [TestCase (typeof(StructWithAGameSceneField))]
        public void InvalidStructFields (Type type)
        {
            Assert.Throws<ServiceException> (() => TypeUtils.ValidateStructFields (type));
        }

        // A field marked nullable whose type holds no null of its own

        [TestCase (typeof(StructWithANullableFieldThatCannotBeNull))]
        [TestCase (typeof(StructWithANullableEnumFieldThatCannotBeNull))]
        public void NullableStructFieldThatCannotBeNull (Type type)
        {
            Assert.Throws<ServiceException> (
                () => TypeUtils.GetStructFieldSpec (type, type.GetProperty ("Field")));
        }

        [TestCase (typeof(TestService.TestStruct))]
        [TestCase (typeof(TestService.TestNestedStruct))]
        [TestCase (typeof(TestService.TestNullableStruct))]
        public void ValidStructFields (Type type)
        {
            Assert.DoesNotThrow (() => TypeUtils.ValidateStructFields (type));
        }

        static MethodInfo Extension (Type type, string name)
        {
            return type.GetMethod (name, BindingFlags.Public | BindingFlags.Static);
        }

        [TestCase ("ExtensionMethod")]
        [TestCase ("ExtensionNullableMethod")]
        [TestCase ("DeprecatedExtensionMethod")]
        [TestCase ("ExtensionMethodOnOtherServicesClass")]
        public void ValidExtensionMethod (string name)
        {
            Assert.DoesNotThrow (
                () => TypeUtils.ValidateKRPCExtensionMethod (Extension (typeof(TestServiceExtensions), name)));
        }

        [TestCase ("MethodWithoutThis")]
        [TestCase ("MethodExtendingAValueType")]
        [TestCase ("MethodExtendingAStruct")]
        [TestCase ("lowerCaseMethod")]
        public void InvalidExtensionMethod (string name)
        {
            Assert.Throws<ServiceException> (
                () => TypeUtils.ValidateKRPCExtensionMethod (Extension (typeof(InvalidExtensions), name)));
        }

        [TestCase ("GetExtensionProperty")]
        [TestCase ("GetExtensionReadWriteProperty")]
        [TestCase ("SetExtensionReadWriteProperty")]
        [TestCase ("GetExtensionNullableProperty")]
        [TestCase ("SetExtensionNullableProperty")]
        public void ValidExtensionProperty (string name)
        {
            Assert.DoesNotThrow (
                () => TypeUtils.ValidateKRPCExtensionProperty (Extension (typeof(TestServiceExtensions), name)));
        }

        [TestCase ("PropertyWithoutAnAccessorPrefix")]
        [TestCase ("GetPropertyWithTooManyParameters")]
        [TestCase ("GetPropertyWithNoReturnValue")]
        [TestCase ("SetPropertyWithAReturnValue")]
        [TestCase ("SetPropertyWithNoValue")]
        public void InvalidExtensionProperty (string name)
        {
            Assert.Throws<ServiceException> (
                () => TypeUtils.ValidateKRPCExtensionProperty (Extension (typeof(InvalidExtensions), name)));
        }

        [TestCase ("ExtensionMethod", typeof(TestService.TestClass))]
        [TestCase ("GetExtensionProperty", typeof(TestService.TestClass))]
        [TestCase ("ExtensionMethodOnOtherServicesClass", typeof(TestClass3))]
        public void GetExtensionTargetClass (string name, Type target)
        {
            Assert.AreEqual (target, TypeUtils.GetExtensionTargetClass (Extension (typeof(TestServiceExtensions), name)));
        }

        [TestCase ("GetExtensionProperty", false, "ExtensionProperty")]
        [TestCase ("SetExtensionReadWriteProperty", true, "ExtensionReadWriteProperty")]
        public void ExtensionPropertyName (string name, bool isSetter, string propertyName)
        {
            var method = Extension (typeof(TestServiceExtensions), name);
            Assert.AreEqual (isSetter, TypeUtils.IsAnExtensionPropertySetter (method));
            Assert.AreEqual (propertyName, TypeUtils.GetExtensionPropertyName (method));
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
        [TestCase ("{\"code\":\"TUPLE\",\"types\":[{\"code\":\"SINT64\"}]}", typeof(Tuple<long>))]
        [TestCase ("{\"code\":\"TUPLE\",\"types\":[{\"code\":\"SINT64\"},{\"code\":\"SINT32\"}]}",
                   typeof(Tuple<long,int>))]
        [TestCase ("{\"code\":\"TUPLE\",\"types\":[{\"code\":\"SINT64\"}," +
                   "{\"code\":\"SINT32\"},{\"code\":\"STRING\"}]}",
                   typeof(Tuple<long,int,string>))]
        [TestCase ("{\"code\":\"LIST\",\"types\":[" +
                   "{\"code\":\"DICTIONARY\",\"types\":[{\"code\":\"SINT32\"},{\"code\":\"STRING\"}]}" +
                   "]}", typeof(IList<IDictionary<int,string>>))]
        [TestCase ("{\"code\":\"LIST\",\"types\":[" +
                   "{\"code\":\"CLASS\",\"service\":\"TestService\",\"name\":\"TestClass\"}" +
                   "]}", typeof(IList<TestService.TestClass>))]
        [TestCase ("{\"code\":\"LIST\",\"types\":[" +
                   "{\"code\":\"ENUMERATION\",\"service\":\"TestService\",\"name\":\"TestEnum\"}" +
                   "]}", typeof(IList<TestService.TestEnum>))]
        [TestCase ("{\"code\":\"STRUCT\",\"service\":\"TestService\",\"name\":\"TestStruct\"}",
                   typeof(TestService.TestStruct))]
        [TestCase ("{\"code\":\"LIST\",\"types\":[" +
                   "{\"code\":\"STRUCT\",\"service\":\"TestService\",\"name\":\"TestStruct\"}" +
                   "]}", typeof(IList<TestService.TestStruct>))]
        public void SerializeType (string name, Type type)
        {
            Assert.AreEqual (name, JsonConvert.SerializeObject (TypeUtils.SerializeType (TypeSpec.Create (type))));
        }

        [TestCase (typeof(TestService.TestEnumWithoutAttribute))]
        [TestCase (typeof(TestService))]
        [TestCase (typeof(IDictionary<double,string>))]
        public void InvalidSerializeType (Type type)
        {
            Assert.Throws<ArgumentException> (() => TypeUtils.SerializeType (TypeSpec.Create (type)));
        }
    }
}

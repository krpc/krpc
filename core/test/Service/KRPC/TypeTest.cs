using System.Collections.Generic;
using System.Linq;
using KRPC.Service.KRPC;
using NUnit.Framework;

namespace KRPC.Test.Service.KRPC
{
    [TestFixture]
    public class TypeTest
    {
        [Test]
        public void ValueTypes ()
        {
            Assert.AreEqual (TypeCode.Double, Type.Double ().Code);
            Assert.AreEqual (TypeCode.Float, Type.Float ().Code);
            Assert.AreEqual (TypeCode.SInt32, Type.Int ().Code);
            Assert.AreEqual (TypeCode.SInt64, Type.Long ().Code);
            Assert.AreEqual (TypeCode.UInt32, Type.UInt ().Code);
            Assert.AreEqual (TypeCode.UInt64, Type.ULong ().Code);
            Assert.AreEqual (TypeCode.Bool, Type.Bool ().Code);
            Assert.AreEqual (TypeCode.String, Type.String ().Code);
            Assert.AreEqual (TypeCode.Bytes, Type.Bytes ().Code);
            Assert.AreEqual (string.Empty, Type.Double ().Service);
            Assert.AreEqual (string.Empty, Type.Double ().Name);
            Assert.AreEqual (0, Type.Double ().Types.Count);
        }

        [Test]
        public void EqualTypesShareAnObjectIdentifier ()
        {
            var store = global::KRPC.Service.ObjectStore.Instance;
            Assert.AreEqual (store.AddInstance (Type.Double ()),
                             store.AddInstance (Type.Double ()));
            Assert.AreEqual (store.AddInstance (Type.ListType (Type.Double ())),
                             store.AddInstance (Type.ListType (Type.Double ())));
            Assert.AreNotEqual (store.AddInstance (Type.Int ()),
                                store.AddInstance (Type.Long ()));
            Assert.AreNotEqual (store.AddInstance (Type.ListType (Type.Int ())),
                                store.AddInstance (Type.SetType (Type.Int ())));
            Assert.AreNotEqual (store.AddInstance (Type.String ()),
                                store.AddInstance (Type.NullableType (Type.String ())));
            Assert.AreEqual (store.AddInstance (Type.ListType (Type.NullableType (Type.String ()))),
                             store.AddInstance (Type.ListType (Type.NullableType (Type.String ()))));
            Assert.AreNotEqual (store.AddInstance (Type.ListType (Type.NullableType (Type.String ()))),
                                store.AddInstance (Type.ListType (Type.String ())));
        }

        [Test]
        public void ClassType ()
        {
            var type = Type.ClassType ("TestService", "TestClass");
            Assert.AreEqual (typeof (global::KRPC.Test.Service.TestService.TestClass), type.InternalType);
            Assert.AreEqual (TypeCode.Class, type.Code);
            Assert.AreEqual ("TestService", type.Service);
            Assert.AreEqual ("TestClass", type.Name);
            Assert.AreEqual (0, type.Types.Count);
        }

        [Test]
        public void EnumerationType ()
        {
            var type = Type.EnumerationType ("TestService", "TestEnum");
            Assert.AreEqual (typeof (global::KRPC.Test.Service.TestService.TestEnum), type.InternalType);
            Assert.AreEqual (TypeCode.Enumeration, type.Code);
            Assert.AreEqual ("TestService", type.Service);
            Assert.AreEqual ("TestEnum", type.Name);
            Assert.AreEqual (0, type.Types.Count);
        }

        [Test]
        public void StructType ()
        {
            var type = Type.StructType ("TestService", "TestStruct");
            Assert.AreEqual (typeof (global::KRPC.Test.Service.TestService.TestStruct), type.InternalType);
            Assert.AreEqual (TypeCode.Struct, type.Code);
            Assert.AreEqual ("TestService", type.Service);
            Assert.AreEqual ("TestStruct", type.Name);
            Assert.AreEqual (0, type.Types.Count);
        }

        [Test]
        public void UnknownServiceOrName ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Type.ClassType ("NoSuchService", "TestClass"));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Type.ClassType ("TestService", "NoSuchClass"));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Type.EnumerationType ("TestService", "NoSuchEnum"));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Type.StructType ("TestService", "NoSuchStruct"));
        }

        [Test]
        public void TupleType ()
        {
            var type = Type.TupleType (new List<Type> { Type.Int (), Type.Bool () });
            Assert.AreEqual (typeof (System.Tuple<int, bool>), type.InternalType);
            Assert.AreEqual (TypeCode.Tuple, type.Code);
            var types = type.Types;
            Assert.AreEqual (2, types.Count);
            Assert.AreEqual (TypeCode.SInt32, types [0].Code);
            Assert.AreEqual (TypeCode.Bool, types [1].Code);
        }

        [Test]
        public void TupleTypeElementCount ()
        {
            var types = new List<Type> ();
            for (var i = 0; i < 7; i++)
                types.Add (Type.Int ());
            Assert.DoesNotThrow (() => Type.TupleType (types));
            types.Add (Type.Int ());
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Type.TupleType (types));
            StringAssert.Contains ("cannot have more than 7 elements", exn.Message);
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Type.TupleType (new List<Type> ()));
        }

        [Test]
        public void ListType ()
        {
            var type = Type.ListType (Type.String ());
            Assert.AreEqual (typeof (IList<string>), type.InternalType);
            Assert.AreEqual (TypeCode.List, type.Code);
            Assert.AreEqual (TypeCode.String, type.Types.Single ().Code);
        }

        [Test]
        public void SetType ()
        {
            var type = Type.SetType (Type.Int ());
            Assert.AreEqual (typeof (HashSet<int>), type.InternalType);
            Assert.AreEqual (TypeCode.Set, type.Code);
            Assert.AreEqual (TypeCode.SInt32, type.Types.Single ().Code);
        }

        [Test]
        public void DictionaryType ()
        {
            var type = Type.DictionaryType (Type.String (), Type.Double ());
            Assert.AreEqual (typeof (IDictionary<string, double>), type.InternalType);
            Assert.AreEqual (TypeCode.Dictionary, type.Code);
            var types = type.Types;
            Assert.AreEqual (2, types.Count);
            Assert.AreEqual (TypeCode.String, types [0].Code);
            Assert.AreEqual (TypeCode.Double, types [1].Code);
        }

        [Test]
        public void InvalidDictionaryKeyType ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Type.DictionaryType (Type.Double (), Type.Int ()));
        }

        [Test]
        public void NullableValueType ()
        {
            Assert.AreEqual (TypeCode.SInt32, new Type (typeof (int?)).Code);
            var enumeration = new Type (
                typeof (global::KRPC.Test.Service.TestService.TestEnum?));
            Assert.AreEqual (TypeCode.Enumeration, enumeration.Code);
            Assert.AreEqual ("TestService", enumeration.Service);
            Assert.AreEqual ("TestEnum", enumeration.Name);
            var list = new Type (typeof (IList<int?>));
            Assert.AreEqual (TypeCode.List, list.Code);
            Assert.AreEqual (TypeCode.SInt32, list.Types.Single ().Code);
            Assert.IsTrue (new Type (typeof (int?)).Nullable);
            Assert.IsFalse (list.Nullable);
            Assert.IsTrue (list.Types.Single ().Nullable);
            Assert.IsFalse (Type.Int ().Nullable);
            Assert.IsFalse (Type.String ().Nullable);
        }

        [Test]
        public void NullableType ()
        {
            var type = Type.NullableType (Type.Int ());
            Assert.AreEqual (typeof (int?), type.InternalType);
            Assert.AreEqual (TypeCode.SInt32, type.Code);
            Assert.IsTrue (type.Nullable);
            var enumeration = Type.NullableType (Type.EnumerationType ("TestService", "TestEnum"));
            Assert.AreEqual (
                typeof (global::KRPC.Test.Service.TestService.TestEnum?), enumeration.InternalType);
            Assert.AreEqual (type, Type.NullableType (type));
        }

        [Test]
        public void NullableTypeOfAnyType ()
        {
            var types = new [] {
                Type.Bool (), Type.String (), Type.Bytes (),
                Type.ClassType ("TestService", "TestClass"),
                Type.StructType ("TestService", "TestStruct"),
                Type.ListType (Type.Int ()),
                Type.TupleType (new List<Type> { Type.Int (), Type.String () }),
                Type.DictionaryType (Type.String (), Type.Int ())
            };
            foreach (var valueType in types) {
                var type = Type.NullableType (valueType);
                Assert.IsTrue (type.Nullable);
                Assert.IsFalse (valueType.Nullable);
                Assert.AreEqual (valueType.Code, type.Code);
                Assert.AreNotEqual (valueType, type);
            }
            Assert.AreEqual (typeof (bool?), Type.NullableType (Type.Bool ()).InternalType);
            Assert.AreEqual (typeof (string), Type.NullableType (Type.String ()).InternalType);
        }

        [Test]
        public void NullableElementTypes ()
        {
            var nullableString = Type.NullableType (Type.String ());
            var list = Type.ListType (nullableString);
            Assert.IsFalse (list.Nullable);
            Assert.IsTrue (list.Types.Single ().Nullable);
            Assert.AreEqual (TypeCode.String, list.Types.Single ().Code);
            var tuple = Type.TupleType (new List<Type> { Type.Int (), nullableString });
            Assert.IsFalse (tuple.Types [0].Nullable);
            Assert.IsTrue (tuple.Types [1].Nullable);
            var dictionary = Type.DictionaryType (Type.String (), nullableString);
            Assert.IsFalse (dictionary.Types [0].Nullable);
            Assert.IsTrue (dictionary.Types [1].Nullable);
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Type.SetType (nullableString));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Type.DictionaryType (nullableString, Type.Int ()));
        }

        [Test]
        public void TupleTypeWithANullElement ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (
                () => Type.TupleType (new List<Type> { Type.Int (), null }));
        }

        [Test]
        public void NestedCollectionType ()
        {
            var type = Type.ListType (Type.TupleType (new List<Type> { Type.Double (), Type.ClassType ("TestService", "TestClass") }));
            Assert.AreEqual (TypeCode.List, type.Code);
            var element = type.Types.Single ();
            Assert.AreEqual (TypeCode.Tuple, element.Code);
            Assert.AreEqual (TypeCode.Double, element.Types [0].Code);
            Assert.AreEqual (TypeCode.Class, element.Types [1].Code);
            Assert.AreEqual ("TestClass", element.Types [1].Name);
        }
    }
}

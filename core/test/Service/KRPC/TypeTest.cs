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
            // A type is an immutable description, so naming one repeatedly reuses a
            // single entry in the object store rather than allocating one each time
            var store = global::KRPC.Service.ObjectStore.Instance;
            Assert.AreEqual (store.AddInstance (Type.Double ()),
                             store.AddInstance (Type.Double ()));
            Assert.AreEqual (store.AddInstance (Type.ListType (Type.Double ())),
                             store.AddInstance (Type.ListType (Type.Double ())));
            Assert.AreNotEqual (store.AddInstance (Type.Int ()),
                                store.AddInstance (Type.Long ()));
            Assert.AreNotEqual (store.AddInstance (Type.ListType (Type.Int ())),
                                store.AddInstance (Type.SetType (Type.Int ())));
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

using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class EncoderTest
    {
        [Test]
        public void EncodeMessage ()
        {
            var call = new Schema.KRPC.ProcedureCall ();
            call.Service = "ServiceName";
            call.Procedure = "ProcedureName";
            var data = Encoder.Encode (call, TypeSpec.For (typeof(Schema.KRPC.ProcedureCall)));
            const string expected = "0a0b536572766963654e616d65120d50726f6365647572654e616d65";
            Assert.AreEqual (expected, data.ToHexString ());
        }

        [Test]
        public void EncodeValue ()
        {
            var data = Encoder.Encode (300u, TypeSpec.For (typeof(uint)));
            Assert.AreEqual ("ac02", data.ToHexString ());
        }

        [Test]
        public void EncodeUnicodeString ()
        {
            var data = Encoder.Encode ("\u2122", TypeSpec.For (typeof(string)));
            Assert.AreEqual ("03e284a2", data.ToHexString ());
        }

        [Test]
        public void EncodeRemoteObject ()
        {
            var mockClient = new Mock<IConnection> ();
            var obj = new Services.SpaceCenter.Vessel (mockClient.Object, 300);
            Assert.AreEqual (300, obj.id);
            Assert.AreSame (mockClient.Object, obj.connection);
            var data = Encoder.Encode (obj, TypeSpec.For (typeof(Services.SpaceCenter.Vessel)));
            Assert.AreEqual ("ac02", data.ToHexString ());
        }

        [Test]
        public void EncodeNull ()
        {
            // A null value is signaled out-of-band by is_null; the encoder returns null to
            // indicate this, regardless of the type.
            Assert.IsNull (Encoder.Encode (null, TypeSpec.For (typeof(Services.SpaceCenter.Vessel))));
            Assert.IsNull (Encoder.Encode (null, TypeSpec.For (typeof(string))));
            Assert.IsNull (Encoder.Encode (null, TypeSpec.For (typeof(int?))));
            Assert.IsNull (Encoder.Encode (null, TypeSpec.For (typeof(IList<int>))));
        }

        [Test]
        public void NullableValue ()
        {
            // A Nullable<T> value encodes and decodes as its underlying type.
            int? value = 300;
            var data = Encoder.Encode (value, TypeSpec.For (typeof(int?)));
            Assert.AreEqual ("d804", data.ToHexString ());
            var result = (int?)Encoder.Decode (data, TypeSpec.For (typeof(int?)), null);
            Assert.AreEqual (300, result);
        }

        [Test]
        public void DecodeMessage ()
        {
            var message = "0a0b536572766963654e616d65120d50726f6365647572654e616d65".ToByteString ();
            var call = (Schema.KRPC.ProcedureCall)Encoder.Decode (message, TypeSpec.For (typeof(Schema.KRPC.ProcedureCall)), null);
            Assert.AreEqual ("ServiceName", call.Service);
            Assert.AreEqual ("ProcedureName", call.Procedure);
        }

        [Test]
        public void DecodeValue ()
        {
            var value = (uint)Encoder.Decode ("ac02".ToByteString (), TypeSpec.For (typeof(uint)), null);
            Assert.AreEqual (300, value);
        }

        [Test]
        public void DecodeUnicodeString ()
        {
            var value = (string)Encoder.Decode ("03e284a2".ToByteString (), TypeSpec.For (typeof(string)), null);
            Assert.AreEqual ("\u2122", value);
        }

        [Test]
        public void DecodeRemoteObject ()
        {
            var mockClient = new Mock<IConnection> ();
            var value = (Services.SpaceCenter.Vessel)Encoder.Decode ("ac02".ToByteString (), TypeSpec.For (typeof(Services.SpaceCenter.Vessel)), mockClient.Object);
            Assert.AreEqual (300, value.id);
            Assert.AreSame (mockClient.Object, value.connection);
        }

        [TestCase (3.14159265359f, "db0f4940")]
        [TestCase (-1.0f, "000080bf")]
        [TestCase (0.0f, "00000000")]
        [TestCase (float.PositiveInfinity, "0000807f")]
        [TestCase (float.NegativeInfinity, "000080ff")]
        [TestCase (float.NaN, "0000c0ff")]
        public void SingleValue (float value, string data)
        {
            var encodeResult = Encoder.Encode (value, TypeSpec.For (typeof(float)));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (float)Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(float)), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (0.0, "0000000000000000")]
        [TestCase (-1.0, "000000000000f0bf")]
        [TestCase (3.14159265359, "ea2e4454fb210940")]
        [TestCase (double.PositiveInfinity, "000000000000f07f")]
        [TestCase (double.NegativeInfinity, "000000000000f0ff")]
        [TestCase (double.NaN, "000000000000f8ff")]
        public void DoubleValue (double value, string data)
        {
            var encodeResult = Encoder.Encode (value, TypeSpec.For (typeof(double)));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (double)Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(double)), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (0, "00")]
        [TestCase (1, "02")]
        [TestCase (42, "54")]
        [TestCase (300, "d804")]
        [TestCase (-33, "41")]
        [TestCase (2147483647, "feffffff0f")]
        [TestCase (-2147483648, "ffffffff0f")]
        public void Int32Value (int value, string data)
        {
            var encodeResult = Encoder.Encode (value, TypeSpec.For (typeof(int)));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (int)Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(int)), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (0, "00")]
        [TestCase (1, "02")]
        [TestCase (42, "54")]
        [TestCase (300, "d804")]
        [TestCase (1234567890000L, "a091d89fee47")]
        [TestCase (-33, "41")]
        public void Int64Value (long value, string data)
        {
            var encodeResult = Encoder.Encode (value, TypeSpec.For (typeof(long)));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (long)Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(long)), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (0u, "00")]
        [TestCase (1u, "01")]
        [TestCase (42u, "2a")]
        [TestCase (300u, "ac02")]
        [TestCase (uint.MaxValue, "ffffffff0f")]
        public void UInt32Value (uint value, string data)
        {
            var encodeResult = Encoder.Encode (value, TypeSpec.For (typeof(uint)));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (uint)Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(uint)), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (-1)]
        [TestCase (-849)]
        public void InvalidUInt32Value (int value)
        {
            Assert.Throws<ArgumentException> (() => Encoder.Encode (value, TypeSpec.For (typeof(uint))));
        }

        [TestCase (0u, "00")]
        [TestCase (1u, "01")]
        [TestCase (42u, "2a")]
        [TestCase (300u, "ac02")]
        [TestCase (1234567890000ul, "d088ec8ff723")]
        [TestCase (ulong.MaxValue, "ffffffffffffffffff01")]
        public void UInt64Value (ulong value, string data)
        {
            var encodeResult = Encoder.Encode (value, TypeSpec.For (typeof(ulong)));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (ulong)Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(ulong)), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (-1)]
        [TestCase (-849)]
        public void InvalidUInt64Value (int value)
        {
            Assert.Throws<ArgumentException> (() => Encoder.Encode (value, TypeSpec.For (typeof(ulong))));
        }

        [TestCase (true, "01")]
        [TestCase (false, "00")]
        public void BooleanValue (bool value, string data)
        {
            var encodeResult = Encoder.Encode (value, TypeSpec.For (typeof(bool)));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (bool)Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(bool)), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase ("", "00")]
        [TestCase ("testing", "0774657374696e67")]
        [TestCase ("One small step for Kerbal-kind!", "1f4f6e6520736d616c6c207374657020666f72204b657262616c2d6b696e6421")]
        [TestCase ("\u2122", "03e284a2")]
        [TestCase ("Mystery Goo\u2122 Containment Unit", "1f4d79737465727920476f6fe284a220436f6e7461696e6d656e7420556e6974")]
        public void StringValue (string value, string data)
        {
            var encodeResult = Encoder.Encode (value, TypeSpec.For (typeof(string)));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (string)Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(string)), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase ("", "00")]
        [TestCase ("bada55", "03bada55")]
        [TestCase ("deadbeef", "04deadbeef")]
        public void BytesValue (string value, string data)
        {
            var encodeResult = Encoder.Encode (value.ToByteString ().ToByteArray (), TypeSpec.For (typeof(byte[])));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (byte[])Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(byte[])), null);
            Assert.AreEqual (value.ToByteString (), decodeResult);
        }

        [TestCase (new uint[] { }, "")]
        [TestCase (new uint[] { 1 }, "0a0101")]
        [TestCase (new uint[] { 1, 2, 3, 4 }, "0a01010a01020a01030a0104")]
        public void ListCollection (IList<uint> values, string data)
        {
            IList<uint> value = new List<uint> (values);
            var encodeResult = Encoder.Encode (value, TypeSpec.For (typeof(IList<uint>)));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (IList<uint>)Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(IList<uint>)), null);
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [TestCase (new string[] { }, new uint[]{ }, "")]
        [TestCase (new [] { "" }, new uint[]{ 0 }, "0a060a0100120100")]
        [TestCase (new [] { "foo", "bar", "baz" }, new uint[]{ 42, 365, 3 }, "0a090a0403666f6f12012a0a0a0a04036261721202ed020a090a040362617a120103")]
        public void DictionaryCollection (IList<string> keys, IList<uint> values, string data)
        {
            IDictionary<string,uint> value = new Dictionary<string,uint> ();
            for (int i = 0; i < keys.Count; i++)
                value [keys [i]] = values [i];
            var encodeResult = Encoder.Encode (value, TypeSpec.For (typeof(IDictionary<string,uint>)));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (IDictionary<string,uint>)Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(IDictionary<string,uint>)), null);
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [TestCase (new uint[] { }, "")]
        [TestCase (new uint[] { 1 }, "0a0101")]
        [TestCase (new uint[] { 1, 2, 3, 4 }, "0a01010a01020a01030a0104")]
        public void SetCollection (IList<uint> values, string data)
        {
            ISet<uint> value = new HashSet<uint> (values);
            var encodeResult = Encoder.Encode (value, TypeSpec.For (typeof(ISet<uint>)));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (ISet<uint>)Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(ISet<uint>)), null);
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [Test]
        public void TupleCollection1 ()
        {
            var value = new Tuple<uint> (1);
            const string data = "0a0101";
            var encodeResult = Encoder.Encode (value, TypeSpec.For (value.GetType ()));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Tuple<uint>)Encoder.Decode (data.ToByteString (), TypeSpec.For (value.GetType ()), null);
            Assert.AreEqual (value, decodeResult);
        }

        [Test]
        public void TupleCollection2 ()
        {
            var value = new Tuple<uint,string,bool> (1, "jeb", false);
            const string data = "0a01010a04036a65620a0100";
            var encodeResult = Encoder.Encode (value, TypeSpec.For (value.GetType ()));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Tuple<uint,string,bool>)Encoder.Decode (data.ToByteString (), TypeSpec.For (value.GetType ()), null);
            Assert.AreEqual (value, decodeResult);
        }

        // Check that the value encodes to the given data, and that the data decodes back to it
        static void CheckValue (object value, string data, TypeSpec spec)
        {
            Assert.AreEqual (data, Encoder.Encode (value, spec).ToHexString ());
            Assert.AreEqual (value, Encoder.Decode (data.ToByteString (), spec, null));
        }

        [Test]
        public void NullableSpecSharesItsType ()
        {
            // A nullable position holds a value of the type it holds anywhere else, so the
            // nullable form of a type is that type with the position marked
            Assert.AreEqual (typeof(string), TypeSpec.Null (typeof(string)).Type);
            Assert.AreEqual (typeof(uint), TypeSpec.For (typeof(uint?)).Type);
            Assert.AreEqual (
                typeof(Services.TestService.TestEnum),
                TypeSpec.For (typeof(Services.TestService.TestEnum?)).Type);
            Assert.IsTrue (TypeSpec.For (typeof(uint?)).Nullable);
            Assert.IsFalse (TypeSpec.For (typeof(uint)).Nullable);
        }

        [Test]
        public void NullableListElements ()
        {
            var spec = TypeSpec.For (typeof(IList<uint?>));
            CheckValue (new List<uint?> (), string.Empty, spec);
            CheckValue (new List<uint?> { null }, "0a0100", spec);
            // A zero is a value like any other, and is told apart from a null by the presence
            // bool
            CheckValue (new List<uint?> { 0 }, "0a020100", spec);
            CheckValue (new List<uint?> { 1, null, 3 }, "0a0201010a01000a020103", spec);
        }

        [Test]
        public void NullableDictionaryValues ()
        {
            CheckValue (
                new Dictionary<string,uint?> { { string.Empty, null } },
                "0a060a0100120100", TypeSpec.For (typeof(IDictionary<string,uint?>)));
        }

        [Test]
        public void NullableTupleItem ()
        {
            var spec = new TypeSpec (
                typeof(Tuple<uint,string>),
                TypeSpec.For (typeof(uint)), TypeSpec.Null (typeof(string)));
            CheckValue (new Tuple<uint,string> (1, "jeb"), "0a01010a0501036a6562", spec);
            CheckValue (new Tuple<uint,string> (1, null), "0a01010a0100", spec);
        }

        [Test]
        public void NullableCollectionValues ()
        {
            // A nullable position holding a collection carries the presence bool ahead of the
            // collection's own encoding
            var spec = new TypeSpec (
                typeof(IList<IList<uint>>), TypeSpec.Null (typeof(IList<uint>)));
            CheckValue (new List<IList<uint>> { null }, "0a0100", spec);
            CheckValue (new List<IList<uint>> { new List<uint> () }, "0a0101", spec);
            CheckValue (new List<IList<uint>> { new List<uint> { 1 } }, "0a04010a0101", spec);
        }

        [Test]
        public void NullableStructFields ()
        {
            // Each nullable field carries its own presence bool, and a null field is that
            // bool alone
            CheckValue (
                new Services.TestService.TestNullableStruct (1, 2, null, null, null),
                "0a01020a0201040a01000a01000a0100",
                TypeSpec.For (typeof(Services.TestService.TestNullableStruct)));
        }

        [Test]
        public void NullabilityIsReadAtEveryPosition ()
        {
            // A service declares no nullable set element and no nullable dictionary key. The
            // client reads what the type says at every position rather than naming the ones a
            // value can be null at
            CheckValue (
                new HashSet<uint?> { null }, "0a0100", TypeSpec.For (typeof(ISet<uint?>)));
            // A C# dictionary rejects a null key, so a key can only be read back where the
            // presence bool says it is there
            CheckValue (
                new Dictionary<string,uint> { { "foo", 1 } }, "0a0a0a050103666f6f120101",
                new TypeSpec (
                    typeof(IDictionary<string,uint>), TypeSpec.Null (typeof(string))));
        }

        [Test]
        public void NullAtANonNullablePosition ()
        {
            Assert.Throws<ArgumentException> (
                () => Encoder.Encode (
                    new List<string> { null }, TypeSpec.For (typeof(IList<string>))));
        }

        [Test]
        public void NullableValueWithoutPresenceBool ()
        {
            // A list holding one item of zero length
            Assert.Throws<ArgumentException> (
                () => Encoder.Decode (
                    "0a00".ToByteString (), TypeSpec.For (typeof(IList<uint?>)), null));
        }

        [Test]
        public void TupleCollectionWithMissingItems ()
        {
            // The same decoder reads a structure, where an older server sends too few items
            // for one this client has more fields for
            const string data = "0a0101";
            Assert.Throws<ArgumentException> (
                () => Encoder.Decode (data.ToByteString (), TypeSpec.For (typeof(Tuple<uint,string>)), null));
        }
    }
}

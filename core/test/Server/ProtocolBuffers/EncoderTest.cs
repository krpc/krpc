using System;
using System.Collections.Generic;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Test.Service;
using NUnit.Framework;

namespace KRPC.Test.Server.ProtocolBuffers
{
    [TestFixture]
    public class EncoderTest
    {
        [Test]
        public void EncodeMessage ()
        {
            var message = new KRPC.Service.Messages.Stream (42);
            var data = Encoder.Encode (message);
            const string expected = "082a";
            Assert.AreEqual (expected, data.ToHexString ());
        }

        [Test]
        public void EncodeValue ()
        {
            var data = Encoder.Encode (300u);
            Assert.AreEqual ("ac02", data.ToHexString ());
        }

        [Test]
        public void EncodeUnicodeString ()
        {
            var data = Encoder.Encode ("\u2122");
            Assert.AreEqual ("03e284a2", data.ToHexString ());
        }

        [Test]
        public void EncodeEnum ()
        {
            var data = Encoder.Encode (TestService.TestEnum.Z);
            Assert.AreEqual ("04", data.ToHexString ());
        }

        [Test]
        public void EncodeClass ()
        {
            var obj = new TestService.TestClass ("foo");
            var data = Encoder.Encode (obj);
            var expected = new [] { (byte)ObjectStore.Instance.AddInstance (obj) }.ToHexString ();
            Assert.AreEqual (expected, data.ToHexString ());
        }

        [Test]
        public void EncodeNull ()
        {
            Assert.Throws<ArgumentNullException> (() => Encoder.Encode (null));
        }

        [Test]
        public void DecodeMessage ()
        {
            var message = "0a0b5465737453657276696365121750726f6365647572654e6f417267734e6f52657475726e".ToByteString ();
            var call = (ProcedureCall)Encoder.Decode (message, typeof(ProcedureCall));
            Assert.AreEqual ("TestService", call.Service);
            Assert.AreEqual ("ProcedureNoArgsNoReturn", call.Procedure);
        }

        [Test]
        public void DecodeValue ()
        {
            var value = (uint)Encoder.Decode ("ac02".ToByteString (), typeof(uint));
            Assert.AreEqual (300, value);
        }

        [Test]
        public void DecodeUnicodeString ()
        {
            var value = (string)Encoder.Decode ("03e284a2".ToByteString (), typeof(string));
            Assert.AreEqual ("\u2122", value);
        }

        [Test]
        public void DecodeEnum ()
        {
            var value = Encoder.Decode ("04".ToByteString (), typeof(TestService.TestEnum));
            Assert.AreEqual (TestService.TestEnum.Z, value);
        }

        [Test]
        public void DecodeClass ()
        {
            var obj = new TestService.TestClass ("foo");
            var id = ObjectStore.Instance.AddInstance (obj);
            var value = Encoder.Decode (new [] { (byte)id }.ToHexString ().ToByteString (), typeof(TestService.TestClass));
            Assert.AreEqual (obj, value);
        }

        [Test]
        public void DecodeClassNone ()
        {
            var value = Encoder.Decode ("00".ToByteString (), typeof(TestService.TestClass));
            Assert.AreEqual (null, value);
        }

        [TestCase (3.14159265359f, "db0f4940")]
        [TestCase (-1.0f, "000080bf")]
        [TestCase (0.0f, "00000000")]
        [TestCase (float.PositiveInfinity, "0000807f")]
        [TestCase (float.NegativeInfinity, "000080ff")]
        [TestCase (float.NaN, "0000c0ff")]
        public void FloatValue (float value, string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (float)Encoder.Decode (data.ToByteString (), typeof(float));
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
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (double)Encoder.Decode (data.ToByteString (), typeof(double));
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
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (int)Encoder.Decode (data.ToByteString (), typeof(int));
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
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (long)Encoder.Decode (data.ToByteString (), typeof(long));
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (0u, "00")]
        [TestCase (1u, "01")]
        [TestCase (42u, "2a")]
        [TestCase (300u, "ac02")]
        [TestCase (uint.MaxValue, "ffffffff0f")]
        public void UInt32Value (uint value, string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (uint)Encoder.Decode (data.ToByteString (), typeof(uint));
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (0u, "00")]
        [TestCase (1u, "01")]
        [TestCase (42u, "2a")]
        [TestCase (300u, "ac02")]
        [TestCase (1234567890000ul, "d088ec8ff723")]
        [TestCase (ulong.MaxValue, "ffffffffffffffffff01")]
        public void UInt64Value (ulong value, string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (ulong)Encoder.Decode (data.ToByteString (), typeof(ulong));
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (true, "01")]
        [TestCase (false, "00")]
        public void BooleanValue (bool value, string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (bool)Encoder.Decode (data.ToByteString (), typeof(bool));
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase ("", "00")]
        [TestCase ("testing", "0774657374696e67")]
        [TestCase ("One small step for Kerbal-kind!", "1f4f6e6520736d616c6c207374657020666f72204b657262616c2d6b696e6421")]
        [TestCase ("\u2122", "03e284a2")]
        [TestCase ("Mystery Goo\u2122 Containment Unit", "1f4d79737465727920476f6fe284a220436f6e7461696e6d656e7420556e6974")]
        public void StringValue (string value, string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (string)Encoder.Decode (data.ToByteString (), typeof(string));
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase ("", "00")]
        [TestCase ("bada55", "03bada55")]
        [TestCase ("deadbeef", "04deadbeef")]
        public void BytesValue (string value, string data)
        {
            var encodeResult = Encoder.Encode (value.ToByteString ().ToByteArray ());
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (byte[])Encoder.Decode (data.ToByteString (), typeof(byte[]));
            Assert.AreEqual (value.ToByteString (), decodeResult);
        }

        [TestCase (new uint[] { }, "")]
        [TestCase (new uint[] { 1 }, "0a0101")]
        [TestCase (new uint[] { 1, 2, 3, 4 }, "0a01010a01020a01030a0104")]
        public void ListCollection (IList<uint> values, string data)
        {
            IList<uint> value = new List<uint> (values);
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (IList<uint>)Encoder.Decode (data.ToByteString (), typeof(IList<uint>));
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [TestCase (new string[] { }, new uint[]{ }, "")]
        [TestCase (new [] { "" }, new uint[]{ 0 }, "0a060a0100120100")]
        [TestCase (new [] { "foo", "bar", "baz" }, new []{ 42u, 365u, 3u }, "0a090a0403666f6f12012a0a0a0a04036261721202ed020a090a040362617a120103")]
        public void DictionaryCollection (IList<string> keys, IList<uint> values, string data)
        {
            IDictionary<string,uint> value = new Dictionary<string,uint> ();
            for (int i = 0; i < keys.Count; i++)
                value [keys [i]] = values [i];
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (IDictionary<string,uint>)Encoder.Decode (data.ToByteString (), typeof(IDictionary<string,uint>));
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [TestCase (new uint[] { }, "")]
        [TestCase (new [] { 1u }, "0a0101")]
        [TestCase (new [] { 1u, 2u, 3u, 4u }, "0a01010a01020a01030a0104")]
        public void SetCollection (IList<uint> values, string data)
        {
            ISet<uint> value = new HashSet<uint> (values);
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (ISet<uint>)Encoder.Decode (data.ToByteString (), typeof(HashSet<uint>));
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [Test]
        public void TupleCollection1 ()
        {
            var value = new Tuple<uint> (1);
            const string data = "0a0101";
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Tuple<uint>)Encoder.Decode (data.ToByteString (), value.GetType ());
            Assert.AreEqual (value.Item1, decodeResult.Item1);
        }

        [Test]
        public void TupleCollection2 ()
        {
            var value = new Tuple<uint,string,bool> (1, "jeb", false);
            const string data = "0a01010a04036a65620a0100";
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Tuple<uint,string,bool>)Encoder.Decode (data.ToByteString (), value.GetType ());
            Assert.AreEqual (value.Item1, decodeResult.Item1);
            Assert.AreEqual (value.Item2, decodeResult.Item2);
            Assert.AreEqual (value.Item3, decodeResult.Item3);
        }

        [Test]
        public void Struct ()
        {
            var obj = new TestService.TestClass ("foo");
            var value = new TestService.TestStruct {
                IntField = 42,
                StringField = "jeb",
                EnumField = TestService.TestEnum.Z,
                ObjectField = obj,
                ListField = new List<string> { "a", "b" }
            };
            var data = Encoder.Encode (value).ToHexString ();
            var decodeResult = (TestService.TestStruct)Encoder.Decode (
                data.ToByteString (), typeof(TestService.TestStruct));
            Assert.AreEqual (value.IntField, decodeResult.IntField);
            Assert.AreEqual (value.StringField, decodeResult.StringField);
            Assert.AreEqual (value.EnumField, decodeResult.EnumField);
            Assert.AreSame (obj, decodeResult.ObjectField);
            CollectionAssert.AreEqual (value.ListField, decodeResult.ListField);
        }

        [Test]
        public void StructIsEncodedAsATupleOfItsFields ()
        {
            var value = new TestService.TestNestedStruct {
                StructField = new TestService.TestStruct {
                    IntField = 1,
                    StringField = "jeb",
                    EnumField = TestService.TestEnum.X,
                    ObjectField = new TestService.TestClass ("foo"),
                    ListField = new List<string> ()
                },
                IntField = 2
            };
            var expected = Encoder.Encode (Tuple.Create (value.StructField, value.IntField)).ToHexString ();
            Assert.AreEqual (expected, Encoder.Encode (value).ToHexString ());
        }

        [Test]
        public void NestedStruct ()
        {
            var value = new TestService.TestNestedStruct {
                StructField = new TestService.TestStruct {
                    IntField = 1,
                    StringField = "jeb",
                    EnumField = TestService.TestEnum.Y,
                    ObjectField = new TestService.TestClass ("foo"),
                    ListField = new List<string> { "a" }
                },
                IntField = 2
            };
            var data = Encoder.Encode (value).ToHexString ();
            var decodeResult = (TestService.TestNestedStruct)Encoder.Decode (
                data.ToByteString (), typeof(TestService.TestNestedStruct));
            Assert.AreEqual (1, decodeResult.StructField.IntField);
            Assert.AreEqual ("jeb", decodeResult.StructField.StringField);
            Assert.AreEqual (2, decodeResult.IntField);
        }

        [Test]
        public void EncodeStructWithNullField ()
        {
            var value = new TestService.TestStruct {
                IntField = 42,
                StringField = null,
                EnumField = TestService.TestEnum.X,
                ObjectField = new TestService.TestClass ("foo"),
                ListField = new List<string> ()
            };
            Assert.Throws<ServiceException> (() => Encoder.Encode (value));
        }

        [Test]
        public void DecodeStructWithMissingFields ()
        {
            var data = Encoder.Encode (Tuple.Create (42, "jeb")).ToHexString ();
            Assert.Throws<ArgumentException> (
                () => Encoder.Decode (data.ToByteString (), typeof(TestService.TestStruct)));
        }

        [Test]
        public void DecodeStructWithNullField ()
        {
            // The object id 0 decodes to a null object, which a structure field can never be
            var data = Encoder.Encode (Tuple.Create (
                42, "jeb", TestService.TestEnum.Z, (ulong)0, new List<string> ())).ToHexString ();
            Assert.Throws<ArgumentException> (
                () => Encoder.Decode (data.ToByteString (), typeof(TestService.TestStruct)));
        }

        [Test]
        public void DecodeStructWithExtraFields ()
        {
            // A client generated against a newer definition of the structure sends the
            // fields that were appended to it, which are ignored
            var value = new TestService.TestStruct {
                IntField = 42,
                StringField = "jeb",
                EnumField = TestService.TestEnum.Z,
                ObjectField = new TestService.TestClass ("foo"),
                ListField = new List<string> { "a" }
            };
            var extended = Tuple.Create (
                value.IntField, value.StringField, value.EnumField, value.ObjectField,
                value.ListField, "an appended field");
            var data = Encoder.Encode (extended).ToHexString ();
            var decodeResult = (TestService.TestStruct)Encoder.Decode (
                data.ToByteString (), typeof(TestService.TestStruct));
            Assert.AreEqual (value.IntField, decodeResult.IntField);
            Assert.AreEqual (value.StringField, decodeResult.StringField);
            Assert.AreEqual (value.EnumField, decodeResult.EnumField);
            CollectionAssert.AreEqual (value.ListField, decodeResult.ListField);
        }
    }
}

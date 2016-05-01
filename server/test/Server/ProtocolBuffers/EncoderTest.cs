using System;
using System.Collections.Generic;
using Google.Protobuf;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;
using NUnit.Framework;
using KRPC.Test.Service;
using KRPC.Service;

namespace KRPC.Test.Server.ProtocolBuffers
{
    [TestFixture]
    public class EncoderTest
    {
        [Test]
        public void EncodeMessage ()
        {
            var request = new Request ();
            request.Service = "TestService";
            request.Procedure = "ProcedureNoArgsNoReturn";
            var data = Encoder.Encode (request);
            const string expected = "0a0b5465737453657276696365121750726f6365647572654e6f417267734e6f52657475726e";
            Console.WriteLine (data.ToHexString());
            Assert.AreEqual (expected, data.ToHexString ());
        }

        [Test]
        public void EncodeValue ()
        {
            var data = Encoder.Encode (300);
            Assert.AreEqual ("ac02", data.ToHexString ());
        }

        [Test]
        public void EncodeUnicodeString ()
        {
            var data = Encoder.Encode ("\u2122");
            Assert.AreEqual ("03e284a2", data.ToHexString ());
        }

        [Test]
        public void EncodeClass ()
        {
            var obj = new TestService.TestClass ("foo");
            var data = Encoder.Encode (obj);
            var expected = new [] { (byte)ObjectStore.Instance.AddInstance (obj) }.ToHexString();
            Assert.AreEqual (expected, data.ToHexString ());
        }

        [Test]
        public void EncodeClassNone ()
        {
            var data = Encoder.Encode (null);
            Assert.AreEqual ("00", data.ToHexString ());
        }

        [Test]
        public void DecodeMessage ()
        {
            var message = "0a0b5465737453657276696365121750726f6365647572654e6f417267734e6f52657475726e".ToByteString ();
            var request = (Request)Encoder.Decode (message, typeof(Request));
            Assert.AreEqual ("TestService", request.Service);
            Assert.AreEqual ("ProcedureNoArgsNoReturn", request.Procedure);
        }

        [Test]
        public void DecodeValue ()
        {
            var value = (UInt32)Encoder.Decode ("ac02".ToByteString (), typeof(UInt32));
            Assert.AreEqual (300, value);
        }

        [Test]
        public void DecodeUnicodeString ()
        {
            var value = (String)Encoder.Decode ("03e284a2".ToByteString (), typeof(String));
            Assert.AreEqual ("\u2122", value);
        }

        [Test]
        public void DecodeClass ()
        {
            var obj = new TestService.TestClass ("foo");
            var id = ObjectStore.Instance.AddInstance (obj);
            var value = Encoder.Decode (new [] { (byte)id }.ToHexString().ToByteString(), typeof(TestService.TestClass));
            Assert.AreEqual (obj, value);
        }

        [Test]
        public void DecodeClassNone ()
        {
            var value = Encoder.Decode ("00".ToByteString(), typeof(TestService.TestClass));
            Assert.AreEqual (null, value);
        }

        [Test, Sequential]
        public void SingleValue (
            [Values (3.14159265359f, -1.0f, 0.0f,
                Single.PositiveInfinity, Single.NegativeInfinity, Single.NaN)] Single value,
            [Values ("db0f4940", "000080bf", "00000000", "0000807f", "000080ff", "0000c0ff")] string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Single)Encoder.Decode (data.ToByteString (), typeof(Single));
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void DoubleValue (
            [Values (0.0, -1.0f, 3.14159265359,
                Double.PositiveInfinity, Double.NegativeInfinity, Double.NaN)] Double value,
            [Values ("0000000000000000", "000000000000f0bf", "ea2e4454fb210940",
                "000000000000f07f", "000000000000f0ff", "000000000000f8ff")] string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Double)Encoder.Decode (data.ToByteString (), typeof(Double));
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void Int32Value (
            [Values (0, 1, 42, 300, -33, Int32.MaxValue, Int32.MinValue)] Int32 value,
            [Values ("00", "01", "2a", "ac02", "dfffffffffffffffff01", "ffffffff07", "80808080f8ffffffff01")] string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Int32)Encoder.Decode (data.ToByteString (), typeof(Int32));
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void Int64Value (
            [Values (0, 1, 42, 300, 1234567890000L, -33, Int64.MaxValue, Int64.MinValue)] Int64 value,
            [Values ("00", "01", "2a", "ac02", "d088ec8ff723", "dfffffffffffffffff01", "ffffffffffffffff7f", "80808080808080808001")] string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Int64)Encoder.Decode (data.ToByteString (), typeof(Int64));
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void UInt32Value (
            [Values (0u, 1u, 42u, 300u, UInt32.MaxValue)] UInt32 value,
            [Values ("00", "01", "2a", "ac02", "ffffffff0f")] string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (UInt32)Encoder.Decode (data.ToByteString (), typeof(UInt32));
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void UInt64Value (
            [Values (0u, 1u, 42u, 300u, 1234567890000ul, UInt64.MaxValue)] UInt64 value,
            [Values ("00", "01", "2a", "ac02", "d088ec8ff723", "ffffffffffffffffff01")] string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (UInt64)Encoder.Decode (data.ToByteString (), typeof(UInt64));
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void BooleanValue (
            [Values (true, false)] Boolean value,
            [Values ("01", "00")] string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Boolean)Encoder.Decode (data.ToByteString (), typeof(Boolean));
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void StringValue (
            [Values ("", "testing", "One small step for Kerbal-kind!", "\u2122",
                "Mystery Goo\u2122 Containment Unit")]
            String value,
            [Values ("00", "0774657374696e67", "1f4f6e6520736d616c6c207374657020666f72204b657262616c2d6b696e6421",
                "03e284a2", "1f4d79737465727920476f6fe284a220436f6e7461696e6d656e7420556e6974")]
            string data)
        {
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (String)Encoder.Decode (data.ToByteString (), typeof(String));
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void BytesValue (
            [Values ("", "bada55", "deadbeef")] string value,
            [Values ("00", "03bada55", "04deadbeef")] string data)
        {
            var encodeResult = Encoder.Encode (value.ToByteString ().ToByteArray ());
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (byte[])Encoder.Decode (data.ToByteString (), typeof(byte[]));
            Assert.AreEqual (value.ToByteString (), decodeResult);
        }

        [Test, Sequential]
        public void ListCollection (
            [Values (new Int32[] { }, new [] { 1 }, new [] { 1, 2, 3, 4 })] IList<Int32> values,
            [Values ("", "0a0101", "0a01010a01020a01030a0104")] string data)
        {
            IList<Int32> value = new List<Int32> (values);
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (IList<Int32>)Encoder.Decode (data.ToByteString (), typeof(IList<Int32>));
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void DictionaryCollection (
            [Values (new String[] { }, new [] { "" }, new [] { "foo", "bar", "baz" })] IList<String> keys,
            [Values (new Int32[]{ }, new []{ 0 }, new []{ 42, 365, 3 })] IList<Int32> values,
            [Values ("", "0a060a0100120100", "0a090a0403666f6f12012a0a0a0a04036261721202ed020a090a040362617a120103")] string data)
        {
            IDictionary<String,Int32> value = new Dictionary<String,Int32> ();
            for (int i = 0; i < keys.Count; i++)
                value [keys [i]] = values [i];
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (IDictionary<String,Int32>)Encoder.Decode (data.ToByteString (), typeof(IDictionary<String,Int32>));
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void SetCollection (
            [Values (new Int32[] { }, new [] { 1 }, new [] { 1, 2, 3, 4 })] IList<Int32> value,
            [Values ("", "0a0101", "0a01010a01020a01030a0104")] string data)
        {
            ISet<Int32> setValue = new HashSet<Int32> ();
            foreach (var x in value)
                setValue.Add (x);
            var encodeResult = Encoder.Encode (setValue);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (ISet<Int32>)Encoder.Decode (data.ToByteString (), typeof(HashSet<Int32>));
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [Test]
        public void TupleCollection1 ()
        {
            var value = new KRPC.Utils.Tuple<Int32> (1);
            const string data = "0a0101";
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (KRPC.Utils.Tuple<Int32>)Encoder.Decode (data.ToByteString (), value.GetType ());
            Assert.AreEqual (value.Item1, decodeResult.Item1);
        }

        [Test]
        public void TupleCollection2 ()
        {
            var value = new KRPC.Utils.Tuple<Int32,String,Boolean> (1, "jeb", false);
            const string data = "0a01010a04036a65620a0100";
            var encodeResult = Encoder.Encode (value);
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (KRPC.Utils.Tuple<Int32,String,Boolean>)Encoder.Decode (data.ToByteString (), value.GetType ());
            Assert.AreEqual (value.Item1, decodeResult.Item1);
            Assert.AreEqual (value.Item2, decodeResult.Item2);
            Assert.AreEqual (value.Item3, decodeResult.Item3);
        }
    }
}

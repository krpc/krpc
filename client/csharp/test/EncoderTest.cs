using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using KRPC.Client;
using Moq;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class EncoderTest
    {
        [Test]
        public void RpcHelloMessage ()
        {
            Assert.AreEqual (12, Encoder.RPCHelloMessage.Length);
            Assert.AreEqual ("48454c4c4f2d525043000000", Encoder.RPCHelloMessage.ToHexString ());
        }

        [Test]
        public void StreamHelloMessage ()
        {
            Assert.AreEqual (12, Encoder.StreamHelloMessage.Length);
            Assert.AreEqual ("48454c4c4f2d53545245414d", Encoder.StreamHelloMessage.ToHexString ());
        }

        [Test]
        public void ClientName ()
        {
            var clientName = Encoder.EncodeClientName ("foo");
            Assert.AreEqual (32, clientName.Length);
            Assert.AreEqual ("666f6f" + new String ('0', 29 * 2), clientName.ToHexString ());
        }

        [Test]
        public void EmptyClientName ()
        {
            var clientName = Encoder.EncodeClientName ("");
            Assert.AreEqual (32, clientName.Length);
            Assert.AreEqual (new String ('0', 32 * 2), clientName.ToHexString ());
        }

        [Test]
        public void LongClientName ()
        {
            var clientName = Encoder.EncodeClientName (new String ('a', 33));
            Assert.AreEqual (32, clientName.Length);
            Assert.AreEqual (String.Concat (Enumerable.Repeat ("61", 32)), clientName.ToHexString ());
        }

        [Test]
        public void EncodeMessage ()
        {
            var request = new KRPC.Schema.KRPC.Request ();
            request.Service = "ServiceName";
            request.Procedure = "ProcedureName";
            var data = Encoder.Encode (request, typeof(KRPC.Schema.KRPC.Request));
            const string expected = "0a0b536572766963654e616d65120d50726f6365647572654e616d65";
            Assert.AreEqual (expected, data.ToHexString ());
        }

        [Test]
        public void EncodeValue ()
        {
            var data = Encoder.Encode (300, typeof(Int32));
            Assert.AreEqual ("ac02", data.ToHexString ());
        }

        [Test]
        public void EncodeUnicodeString ()
        {
            var data = Encoder.Encode ("\u2122", typeof(String));
            Assert.AreEqual ("03e284a2", data.ToHexString ());
        }

        [Test]
        public void EncodeRemoteObject ()
        {
            var mockClient = new Mock<IConnection> ();
            var obj = new KRPC.Client.Services.SpaceCenter.Vessel (mockClient.Object, 300);
            Assert.AreEqual (300, obj._ID);
            Assert.AreSame (mockClient.Object, obj.connection);
            var data = Encoder.Encode (obj, typeof(KRPC.Client.Services.SpaceCenter.Vessel));
            Assert.AreEqual ("ac02", data.ToHexString ());
        }

        [Test]
        public void EncodeNullRemoteObject ()
        {
            var data = Encoder.Encode (null, typeof(KRPC.Client.Services.SpaceCenter.Vessel));
            Assert.AreEqual ("00", data.ToHexString ());
        }

        [Test]
        public void DecodeMessage ()
        {
            var message = "0a0b536572766963654e616d65120d50726f6365647572654e616d65".ToByteString ();
            var request = (KRPC.Schema.KRPC.Request)Encoder.Decode (message, typeof(KRPC.Schema.KRPC.Request), null);
            Assert.AreEqual ("ServiceName", request.Service);
            Assert.AreEqual ("ProcedureName", request.Procedure);
        }

        [Test]
        public void DecodeValue ()
        {
            var value = (UInt32)Encoder.Decode ("ac02".ToByteString (), typeof(UInt32), null);
            Assert.AreEqual (300, value);
        }

        [Test]
        public void DecodeUnicodeString ()
        {
            var value = (String)Encoder.Decode ("03e284a2".ToByteString (), typeof(String), null);
            Assert.AreEqual ("\u2122", value);
        }

        [Test]
        public void DecodeRemoteObject ()
        {
            var mockClient = new Mock<IConnection> ();
            var value = (KRPC.Client.Services.SpaceCenter.Vessel)Encoder.Decode ("ac02".ToByteString (), typeof(KRPC.Client.Services.SpaceCenter.Vessel), mockClient.Object);
            Assert.AreEqual (300, value._ID);
            Assert.AreSame (mockClient.Object, value.connection);
        }

        [Test]
        public void DecodeNullRemoteObject ()
        {
            var mockClient = new Mock<IConnection> ();
            var value = (String)Encoder.Decode ("00".ToByteString (), typeof(KRPC.Client.Services.SpaceCenter.Vessel), mockClient.Object);
            Assert.IsNull (value);
        }

        [Test, Sequential]
        public void SingleValue (
            [Values (3.14159265359f, -1.0f, 0.0f,
                Single.PositiveInfinity, Single.NegativeInfinity, Single.NaN)] Single value,
            [Values ("db0f4940", "000080bf", "00000000", "0000807f", "000080ff", "0000c0ff")] string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(Single));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Single)Encoder.Decode (data.ToByteString (), typeof(Single), null);
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void DoubleValue (
            [Values (0.0, -1.0f, 3.14159265359,
                Double.PositiveInfinity, Double.NegativeInfinity, Double.NaN)] Double value,
            [Values ("0000000000000000", "000000000000f0bf", "ea2e4454fb210940",
                "000000000000f07f", "000000000000f0ff", "000000000000f8ff")] string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(Double));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Double)Encoder.Decode (data.ToByteString (), typeof(Double), null);
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void Int32Value (
            [Values (0, 1, 42, 300, -33, Int32.MaxValue, Int32.MinValue)] Int32 value,
            [Values ("00", "01", "2a", "ac02",
                "dfffffffffffffffff01", "ffffffff07", "80808080f8ffffffff01")] string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(Int32));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Int32)Encoder.Decode (data.ToByteString (), typeof(Int32), null);
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void Int64Value (
            [Values (0, 1, 42, 300, 1234567890000L, -33, Int64.MaxValue, Int64.MinValue)] Int64 value,
            [Values ("00", "01", "2a", "ac02", "d088ec8ff723", "dfffffffffffffffff01", "ffffffffffffffff7f", "80808080808080808001")] string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(Int64));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Int64)Encoder.Decode (data.ToByteString (), typeof(Int64), null);
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void UInt32Value (
            [Values (0u, 1u, 42u, 300u, UInt32.MaxValue)] UInt32 value,
            [Values ("00", "01", "2a", "ac02", "ffffffff0f")] string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(UInt32));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (UInt32)Encoder.Decode (data.ToByteString (), typeof(UInt32), null);
            Assert.AreEqual (value, decodeResult);
        }

        [Test]
        public void InvalidUInt32Value ()
        {
            Assert.Throws<ArgumentException> (() => Encoder.Encode (-1, typeof(UInt32)));
            Assert.Throws<ArgumentException> (() => Encoder.Encode (-849, typeof(UInt32)));
        }

        [Test, Sequential]
        public void UInt64Value (
            [Values (0u, 1u, 42u, 300u, 1234567890000ul, UInt64.MaxValue)] UInt64 value,
            [Values ("00", "01", "2a", "ac02", "d088ec8ff723", "ffffffffffffffffff01")] string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(UInt64));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (UInt64)Encoder.Decode (data.ToByteString (), typeof(UInt64), null);
            Assert.AreEqual (value, decodeResult);
        }

        [Test]
        public void InvalidUInt64Value ()
        {
            Assert.Throws<ArgumentException> (() => Encoder.Encode (-1, typeof(UInt64)));
            Assert.Throws<ArgumentException> (() => Encoder.Encode (-849, typeof(UInt64)));
        }

        [Test, Sequential]
        public void BooleanValue (
            [Values (true, false)] Boolean value,
            [Values ("01", "00")] string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(Boolean));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Boolean)Encoder.Decode (data.ToByteString (), typeof(Boolean), null);
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
            var encodeResult = Encoder.Encode (value, typeof(String));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (String)Encoder.Decode (data.ToByteString (), typeof(String), null);
            Assert.AreEqual (value, decodeResult);
        }

        [Test, Sequential]
        public void BytesValue (
            [Values ("", "bada55", "deadbeef")] string value,
            [Values ("00", "03bada55", "04deadbeef")] string data)
        {
            var encodeResult = Encoder.Encode (value.ToByteString ().ToByteArray (), typeof(byte[]));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (byte[])Encoder.Decode (data.ToByteString (), typeof(byte[]), null);
            Assert.AreEqual (value.ToByteString (), decodeResult);
        }

        [Test, Sequential]
        public void ListCollection (
            [Values (new Int32[] { }, new[] { 1 }, new [] { 1, 2, 3, 4 })] IList<Int32> values,
            [Values ("", "0a0101", "0a01010a01020a01030a0104")] string data)
        {
            IList<Int32> value = new List<Int32> (values);
            var encodeResult = Encoder.Encode (value, typeof(IList<Int32>));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (IList<Int32>)Encoder.Decode (data.ToByteString (), typeof(IList<Int32>), null);
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
            var encodeResult = Encoder.Encode (value, typeof(IDictionary<String,Int32>));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (IDictionary<String,Int32>)Encoder.Decode (data.ToByteString (), typeof(IDictionary<String,Int32>), null);
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
            var encodeResult = Encoder.Encode (setValue, typeof(ISet<Int32>));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (ISet<Int32>)Encoder.Decode (data.ToByteString (), typeof(ISet<Int32>), null);
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [Test]
        public void TupleCollection1 ()
        {
            var value = new Tuple<Int32> (1);
            const string data = "0a0101";
            var encodeResult = Encoder.Encode (value, value.GetType ());
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Tuple<Int32>)Encoder.Decode (data.ToByteString (), value.GetType (), null);
            Assert.AreEqual (value, decodeResult);
        }

        [Test]
        public void TupleCollection2 ()
        {
            var value = new Tuple<Int32,String,Boolean> (1, "jeb", false);
            const string data = "0a01010a04036a65620a0100";
            var encodeResult = Encoder.Encode (value, value.GetType ());
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Tuple<Int32,String,Boolean>)Encoder.Decode (data.ToByteString (), value.GetType (), null);
            Assert.AreEqual (value, decodeResult);
        }
    }
}

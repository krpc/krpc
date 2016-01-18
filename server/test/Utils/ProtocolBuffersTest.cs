using System;
using Google.Protobuf;
using KRPC.Utils;
using KRPCTest.Service;
using NUnit.Framework;

namespace KRPCTest.Utils
{
    [TestFixture]
    public class ProtocolBuffersTest
    {
        [Test]
        public void GetMessageTypeName ()
        {
            Assert.AreEqual ("KRPC.Request", ProtocolBuffers.GetMessageTypeName (typeof(KRPC.Schema.KRPC.Request)));
            Assert.AreEqual ("KRPC.Response", ProtocolBuffers.GetMessageTypeName (typeof(KRPC.Schema.KRPC.Response)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetMessageTypeName (typeof(Test.TestEnum)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetMessageTypeName (null));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetMessageTypeName (typeof(string)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetMessageTypeName (typeof(ProtocolBuffersTest)));
        }

        [Test]
        public void GetEnumTypeName ()
        {
            Assert.AreEqual ("Test.TestEnum", ProtocolBuffers.GetEnumTypeName (typeof(Test.TestEnum)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetEnumTypeName (typeof(KRPC.Schema.KRPC.Request)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetEnumTypeName (null));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetEnumTypeName (typeof(string)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetEnumTypeName (typeof(ProtocolBuffersTest)));
        }

        [Test]
        public void GetValueTypeName ()
        {
            Assert.AreEqual ("double", ProtocolBuffers.GetValueTypeName (typeof(double)));
            Assert.AreEqual ("float", ProtocolBuffers.GetValueTypeName (typeof(float)));
            Assert.AreEqual ("int32", ProtocolBuffers.GetValueTypeName (typeof(int)));
            Assert.AreEqual ("int64", ProtocolBuffers.GetValueTypeName (typeof(long)));
            Assert.AreEqual ("uint32", ProtocolBuffers.GetValueTypeName (typeof(uint)));
            Assert.AreEqual ("uint64", ProtocolBuffers.GetValueTypeName (typeof(ulong)));
            //Assert.AreEqual ("sint32", ProtocolBuffers.GetValueTypeName (typeof()));
            //Assert.AreEqual ("sint64", ProtocolBuffers.GetValueTypeName (typeof()));
            //Assert.AreEqual ("fixed32", ProtocolBuffers.GetValueTypeName (typeof()));
            //Assert.AreEqual ("fixed64", ProtocolBuffers.GetValueTypeName (typeof()));
            //Assert.AreEqual ("sfixed32", ProtocolBuffers.GetValueTypeName (typeof()));
            //Assert.AreEqual ("sfixed64", ProtocolBuffers.GetValueTypeName (typeof()));
            Assert.AreEqual ("bool", ProtocolBuffers.GetValueTypeName (typeof(bool)));
            Assert.AreEqual ("string", ProtocolBuffers.GetValueTypeName (typeof(string)));
            Assert.AreEqual ("bytes", ProtocolBuffers.GetValueTypeName (typeof(byte[])));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetValueTypeName (typeof(KRPC.Schema.KRPC.Request)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetValueTypeName (typeof(KRPC.Schema.KRPC.Response)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetValueTypeName (typeof(Test.TestEnum)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetValueTypeName (null));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetValueTypeName (typeof(ProtocolBuffersTest)));
        }

        [Test]
        public void IsAMessageType ()
        {
            Assert.IsTrue (ProtocolBuffers.IsAMessageType (typeof(KRPC.Schema.KRPC.Request)));
            Assert.IsTrue (ProtocolBuffers.IsAMessageType (typeof(KRPC.Schema.KRPC.Response)));
            Assert.IsFalse (ProtocolBuffers.IsAMessageType (typeof(Test.TestEnum)));
            Assert.IsFalse (ProtocolBuffers.IsAMessageType (null));
            Assert.IsFalse (ProtocolBuffers.IsAMessageType (typeof(string)));
            Assert.IsFalse (ProtocolBuffers.IsAMessageType (typeof(ProtocolBuffersTest)));
        }

        [Test]
        public void IsAnEnumType ()
        {
            Assert.IsFalse (ProtocolBuffers.IsAnEnumType (typeof(KRPC.Schema.KRPC.Request)));
            Assert.IsFalse (ProtocolBuffers.IsAnEnumType (typeof(KRPC.Schema.KRPC.Response)));
            Assert.IsTrue (ProtocolBuffers.IsAnEnumType (typeof(Test.TestEnum)));
            Assert.IsFalse (ProtocolBuffers.IsAnEnumType (typeof(TestService.CSharpEnumWithoutAttribute)));
            Assert.IsFalse (ProtocolBuffers.IsAnEnumType (null));
            Assert.IsFalse (ProtocolBuffers.IsAnEnumType (typeof(string)));
            Assert.IsFalse (ProtocolBuffers.IsAnEnumType (typeof(ProtocolBuffersTest)));
        }

        [Test]
        public void IsAValueType ()
        {
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof(double)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof(float)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof(int)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof(long)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof(uint)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof(ulong)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof(bool)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof(string)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof(byte[])));
            Assert.IsFalse (ProtocolBuffers.IsAValueType (typeof(KRPC.Schema.KRPC.Request)));
            Assert.IsFalse (ProtocolBuffers.IsAValueType (typeof(KRPC.Schema.KRPC.Response)));
            Assert.IsFalse (ProtocolBuffers.IsAValueType (typeof(Test.TestEnum)));
            Assert.IsFalse (ProtocolBuffers.IsAValueType (null));
            Assert.IsFalse (ProtocolBuffers.IsAValueType (typeof(ProtocolBuffersTest)));
        }

        [Test]
        public void IsAValidType ()
        {
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(KRPC.Schema.KRPC.Request)));
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(KRPC.Schema.KRPC.Response)));
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(Test.TestEnum)));
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(double)));
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(float)));
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(int)));
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(long)));
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(uint)));
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(ulong)));
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(bool)));
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(string)));
            Assert.IsTrue (ProtocolBuffers.IsAValidType (typeof(byte[])));
            Assert.IsFalse (ProtocolBuffers.IsAValidType (null));
            Assert.IsFalse (ProtocolBuffers.IsAValidType (typeof(ProtocolBuffersTest)));
        }

        static string ToHex (ByteString byteString)
        {
            byte[] array = byteString.ToByteArray ();
            string hex = BitConverter.ToString (array);
            return hex.Replace ("-", "");
        }

        static ByteString FromHex (string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
                bytes [i / 2] = Convert.ToByte (hex.Substring (i, 2), 16);
            return ByteString.CopyFrom (bytes);
        }

        [Test]
        public void WriteValue ()
        {
            Assert.AreEqual ("6E861BF0F9210940", ToHex (ProtocolBuffers.WriteValue (3.14159, typeof(double))));
            Assert.AreEqual ("D00F4940", ToHex (ProtocolBuffers.WriteValue (3.14159f, typeof(float))));
            Assert.AreEqual ("D6FFFFFFFFFFFFFFFF01", ToHex (ProtocolBuffers.WriteValue (-42, typeof(int))));
            Assert.AreEqual ("A2EAF7B6E0FEFFFFFF01", ToHex (ProtocolBuffers.WriteValue (-42834463454L, typeof(long))));
            Assert.AreEqual ("2A", ToHex (ProtocolBuffers.WriteValue (42U, typeof(uint))));
            Assert.AreEqual ("DE9588C99F01", ToHex (ProtocolBuffers.WriteValue (42834463454UL, typeof(ulong))));
            Assert.AreEqual ("01", ToHex (ProtocolBuffers.WriteValue (true, typeof(bool))));
            Assert.AreEqual ("00", ToHex (ProtocolBuffers.WriteValue (false, typeof(bool))));
            Assert.AreEqual ("086A65626564696168", ToHex (ProtocolBuffers.WriteValue ("jebediah", typeof(string))));
            Assert.AreEqual ("03BADA55", ToHex (ProtocolBuffers.WriteValue (new byte[] { 0xBA, 0xDA, 0x55 }, typeof(byte[]))));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.WriteValue ("foo", typeof(ProtocolBuffersTest)));
        }

        [Test]
        public void ReadValue ()
        {
            Assert.AreEqual (3.14159, ProtocolBuffers.ReadValue (FromHex ("6E861BF0F9210940"), typeof(double)));
            Assert.AreEqual (3.14159f, ProtocolBuffers.ReadValue (FromHex ("D00F4940"), typeof(float)));
            Assert.AreEqual (-42, ProtocolBuffers.ReadValue (FromHex ("D6FFFFFFFFFFFFFFFF01"), typeof(int)));
            Assert.AreEqual (-42834463454L, ProtocolBuffers.ReadValue (FromHex ("A2EAF7B6E0FEFFFFFF01"), typeof(long)));
            Assert.AreEqual (42, ProtocolBuffers.ReadValue (FromHex ("2A"), typeof(uint)));
            Assert.AreEqual (42834463454UL, ProtocolBuffers.ReadValue (FromHex ("DE9588C99F01"), typeof(ulong)));
            Assert.AreEqual (true, ProtocolBuffers.ReadValue (FromHex ("01"), typeof(bool)));
            Assert.AreEqual (false, ProtocolBuffers.ReadValue (FromHex ("00"), typeof(bool)));
            Assert.AreEqual ("jebediah", ProtocolBuffers.ReadValue (FromHex ("086A65626564696168"), typeof(string)));
            Assert.AreEqual (new byte[] { 0xBA, 0xDA, 0x55 }, ProtocolBuffers.ReadValue (FromHex ("03BADA55"), typeof(byte[])));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.ReadValue (FromHex (""), typeof(bool)));
        }

        [Test]
        public void ReadWriteValues ()
        {
            Assert.AreEqual (3.14159, ProtocolBuffers.ReadValue (ProtocolBuffers.WriteValue (3.14159, typeof(double)), typeof(double)));
            Assert.AreEqual (3.14159f, ProtocolBuffers.ReadValue (ProtocolBuffers.WriteValue (3.14159f, typeof(float)), typeof(float)));
            Assert.AreEqual (-42, ProtocolBuffers.ReadValue (ProtocolBuffers.WriteValue (-42, typeof(int)), typeof(int)));
            Assert.AreEqual (-42834463454, ProtocolBuffers.ReadValue (ProtocolBuffers.WriteValue (-42834463454L, typeof(long)), typeof(long)));
            Assert.AreEqual (42U, ProtocolBuffers.ReadValue (ProtocolBuffers.WriteValue (42U, typeof(uint)), typeof(uint)));
            Assert.AreEqual (42834463454UL, ProtocolBuffers.ReadValue (ProtocolBuffers.WriteValue (42834463454UL, typeof(ulong)), typeof(ulong)));
            Assert.AreEqual (true, ProtocolBuffers.ReadValue (ProtocolBuffers.WriteValue (true, typeof(bool)), typeof(bool)));
            Assert.AreEqual (false, ProtocolBuffers.ReadValue (ProtocolBuffers.WriteValue (false, typeof(bool)), typeof(bool)));
            Assert.AreEqual ("jebediah", ProtocolBuffers.ReadValue (ProtocolBuffers.WriteValue ("jebediah", typeof(string)), typeof(string)));
            Assert.AreEqual (new byte[] { 0xBA, 0xDA, 0x55 }, ProtocolBuffers.ReadValue (ProtocolBuffers.WriteValue (new byte[] {
                0xBA,
                0xDA,
                0x55
            }, typeof(byte[])), typeof(byte[])));
        }
    }
}

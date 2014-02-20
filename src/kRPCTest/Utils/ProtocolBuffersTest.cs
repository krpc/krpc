using NUnit.Framework;
using System;
using KRPC.Utils;

namespace KRPCTest.Utils
{
    [TestFixture]
    public class ProtocolBuffersTest
    {
        [Test]
        public void GetMessageTypeName ()
        {
            Assert.AreEqual ("KRPC.Request", ProtocolBuffers.GetMessageTypeName (typeof (KRPC.Schema.KRPC.Request)));
            Assert.AreEqual ("KRPC.Response", ProtocolBuffers.GetMessageTypeName (typeof (KRPC.Schema.KRPC.Response)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetMessageTypeName (null));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetMessageTypeName (typeof (string)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetMessageTypeName (typeof (ProtocolBuffersTest)));
        }

        [Test]
        public void GetValueTypeName ()
        {
            Assert.AreEqual ("double", ProtocolBuffers.GetValueTypeName (typeof (double)));
            Assert.AreEqual ("float", ProtocolBuffers.GetValueTypeName (typeof (float)));
            Assert.AreEqual ("int32", ProtocolBuffers.GetValueTypeName (typeof (int)));
            Assert.AreEqual ("int64", ProtocolBuffers.GetValueTypeName (typeof (long)));
            Assert.AreEqual ("uint32", ProtocolBuffers.GetValueTypeName (typeof (uint)));
            Assert.AreEqual ("uint64", ProtocolBuffers.GetValueTypeName (typeof (ulong)));
            //Assert.AreEqual ("sint32", ProtocolBuffers.GetValueTypeName (typeof()));
            //Assert.AreEqual ("sint64", ProtocolBuffers.GetValueTypeName (typeof()));
            //Assert.AreEqual ("fixed32", ProtocolBuffers.GetValueTypeName (typeof()));
            //Assert.AreEqual ("fixed64", ProtocolBuffers.GetValueTypeName (typeof()));
            //Assert.AreEqual ("sfixed32", ProtocolBuffers.GetValueTypeName (typeof()));
            //Assert.AreEqual ("sfixed64", ProtocolBuffers.GetValueTypeName (typeof()));
            Assert.AreEqual ("bool", ProtocolBuffers.GetValueTypeName (typeof (bool)));
            Assert.AreEqual ("string", ProtocolBuffers.GetValueTypeName (typeof (string)));
            Assert.AreEqual ("bytes", ProtocolBuffers.GetValueTypeName (typeof (byte[])));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetValueTypeName (typeof(KRPC.Schema.KRPC.Request)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetValueTypeName (typeof (KRPC.Schema.KRPC.Response)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetValueTypeName (null));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.GetValueTypeName (typeof (ProtocolBuffersTest)));
        }

        [Test]
        public void BuilderForMessageType ()
        {
            Assert.AreEqual (typeof(KRPC.Schema.KRPC.Request.Builder),
                ProtocolBuffers.BuilderForMessageType (typeof (KRPC.Schema.KRPC.Request)).GetType());
            Assert.AreEqual (typeof(KRPC.Schema.KRPC.Response.Builder),
                ProtocolBuffers.BuilderForMessageType (typeof (KRPC.Schema.KRPC.Response)).GetType());
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.BuilderForMessageType (null));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.BuilderForMessageType (typeof (string)));
            Assert.Throws<ArgumentException> (() => ProtocolBuffers.BuilderForMessageType (typeof (ProtocolBuffersTest)));
        }

        [Test]
        public void IsAMessageType ()
        {
            Assert.IsTrue (ProtocolBuffers.IsAMessageType (typeof (KRPC.Schema.KRPC.Request)));
            Assert.IsTrue (ProtocolBuffers.IsAMessageType (typeof (KRPC.Schema.KRPC.Response)));
            Assert.IsFalse (ProtocolBuffers.IsAMessageType (null));
            Assert.IsFalse (ProtocolBuffers.IsAMessageType (typeof (string)));
            Assert.IsFalse (ProtocolBuffers.IsAMessageType (typeof (ProtocolBuffersTest)));
        }

        [Test]
        public void IsAValueType ()
        {
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof (double)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof (float)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof (int)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof (long)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof (uint)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof (ulong)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof (bool)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof (string)));
            Assert.IsTrue (ProtocolBuffers.IsAValueType (typeof (byte[])));
            Assert.IsFalse (ProtocolBuffers.IsAValueType (typeof (KRPC.Schema.KRPC.Request)));
            Assert.IsFalse (ProtocolBuffers.IsAValueType (typeof (KRPC.Schema.KRPC.Response)));
            Assert.IsFalse (ProtocolBuffers.IsAValueType (null));
            Assert.IsFalse (ProtocolBuffers.IsAValueType (typeof (ProtocolBuffersTest)));
        }
    }
}


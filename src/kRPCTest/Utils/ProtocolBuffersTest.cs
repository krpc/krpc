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
    }
}


using System.IO;
using NUnit.Framework;
using KRPC.Schema.KRPC;

namespace KRPCTest.Schema
{
    [TestFixture]
    public class RpcTest
    {
        [Test]
        public void SimpleProtobufUsage ()
        {
            const string SERVICE = "a";
            const string METHOD = "b";

            var builder = Request.CreateBuilder ();
            builder.Service = SERVICE;
            builder.Procedure = METHOD;
            var request = builder.Build ();

            Assert.IsTrue (request.HasProcedure);
            Assert.IsTrue (request.HasService);
            Assert.AreEqual (METHOD, request.Procedure);
            Assert.AreEqual (SERVICE, request.Service);

            var stream = new MemoryStream ();
            request.WriteDelimitedTo (stream);

            stream.Seek (0, SeekOrigin.Begin);

            Request reqCopy = Request.ParseDelimitedFrom (stream);

            Assert.IsTrue (reqCopy.HasProcedure);
            Assert.IsTrue (reqCopy.HasService);
            Assert.AreEqual (METHOD, reqCopy.Procedure);
            Assert.AreEqual (SERVICE, reqCopy.Service);
        }
    }
}


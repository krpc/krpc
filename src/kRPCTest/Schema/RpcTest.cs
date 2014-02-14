using System;
using System.IO;
using NUnit.Framework;
using KRPC.Schema.RPC;

namespace KRPCTest.Schema
{
	[TestFixture]
	public class RpcTest
	{
		[Test]
		public void SimpleProtobufUsage ()
		{
			const string SERVICE = "my_service_name";
			const string METHOD = "my_method_name";

			byte[] bytes;

			Request req = Request.CreateBuilder ()
				.SetMethod (METHOD)
				.SetService (SERVICE)
				.BuildPartial ();

			Assert.IsTrue(req.HasMethod);
			Assert.IsTrue(req.HasService);
			Assert.AreEqual(METHOD, req.Method);
			Assert.AreEqual(SERVICE, req.Service);

			using (MemoryStream stream = new MemoryStream()) {
				req.WriteTo(stream);
				bytes = stream.ToArray();
			}

			Request reqCopy = Request.CreateBuilder().MergeFrom(bytes).BuildPartial();

			Assert.IsTrue(reqCopy.HasMethod);
			Assert.IsTrue(reqCopy.HasService);
			Assert.AreEqual(METHOD, reqCopy.Method);
			Assert.AreEqual(SERVICE, reqCopy.Service);
		}
	}
}


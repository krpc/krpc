using System;
using Google.ProtocolBuffers;
using KRPC.Service;

namespace KRPCTest.Service
{
	public class TestService
	{
		public static ITestService service;

		public static void MethodWithoutAttribute ()
		{
		}

		[KRPCMethod]
		public static void MethodNoArgsNoReturn ()
		{
			service.MethodNoArgsNoReturn ();
		}

		[KRPCMethod]
		public static void MethodArgsNoReturn (ByteString data)
		{
			service.MethodArgsNoReturn (data);
		}

		[KRPCMethod]
		public static IMessage MethodNoArgsReturns ()
		{
			return service.MethodNoArgsReturns ();
		}

		[KRPCMethod]
		public static IMessage MethodArgsReturns (ByteString data)
		{
			return service.MethodArgsReturns (data);
		}
	}
}


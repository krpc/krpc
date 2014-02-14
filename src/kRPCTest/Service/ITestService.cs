using System;
using Google.ProtocolBuffers;

namespace KRPCTest.Service
{
	public interface ITestService
	{
		void MethodNoArgsNoReturn ();
		void MethodArgsNoReturn (ByteString data);
		IMessage MethodNoArgsReturns ();
		IMessage MethodArgsReturns (ByteString data);
	}
}

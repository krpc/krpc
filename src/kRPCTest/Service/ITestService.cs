using System;
using Google.ProtocolBuffers;

namespace KRPCTest.Service
{
    public interface ITestService
    {
        void MethodNoArgsNoReturn ();
        void MethodArgsNoReturn (KRPC.Schema.KRPC.Response data);
        KRPC.Schema.KRPC.Response MethodNoArgsReturns ();
        KRPC.Schema.KRPC.Response MethodArgsReturns (KRPC.Schema.KRPC.Response data);
    }
}

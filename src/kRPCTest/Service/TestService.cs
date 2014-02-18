using System;
using Google.ProtocolBuffers;
using KRPC.Service;

namespace KRPCTest.Service
{
    [KRPCService]
    public class TestService
    {
        public static ITestService service;

        public static void MethodWithoutAttribute ()
        {
        }

        [KRPCProcedure]
        public static void MethodNoArgsNoReturn ()
        {
            service.MethodNoArgsNoReturn ();
        }

        [KRPCProcedure]
        public static void MethodArgsNoReturn (KRPC.Schema.KRPC.Response data)
        {
            service.MethodArgsNoReturn (data);
        }

        [KRPCProcedure]
        public static KRPC.Schema.KRPC.Response MethodNoArgsReturns ()
        {
            return service.MethodNoArgsReturns ();
        }

        [KRPCProcedure]
        public static KRPC.Schema.KRPC.Response MethodArgsReturns (KRPC.Schema.KRPC.Response data)
        {
            return service.MethodArgsReturns (data);
        }
    }
}


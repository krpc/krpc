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

        [KRPCMethod]
        public static void MethodNoArgsNoReturn ()
        {
            service.MethodNoArgsNoReturn ();
        }

        [KRPCMethod]
        public static void MethodArgsNoReturn (KRPC.Schema.KRPC.Response data)
        {
            service.MethodArgsNoReturn (data);
        }

        [KRPCMethod]
        public static KRPC.Schema.KRPC.Response MethodNoArgsReturns ()
        {
            return service.MethodNoArgsReturns ();
        }

        [KRPCMethod]
        public static KRPC.Schema.KRPC.Response MethodArgsReturns (KRPC.Schema.KRPC.Response data)
        {
            return service.MethodArgsReturns (data);
        }
    }
}


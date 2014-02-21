using System;
using Google.ProtocolBuffers;
using KRPC.Service;

namespace KRPCTest.Service
{
    [KRPCService]
    public class TestService
    {
        public static ITestService service;

        public static void ProcedureWithoutAttribute ()
        {
        }

        [KRPCProcedure]
        public static void ProcedureNoArgsNoReturn ()
        {
            service.ProcedureNoArgsNoReturn ();
        }

        [KRPCProcedure]
        public static void ProcedureSingleArgNoReturn (KRPC.Schema.KRPC.Response data)
        {
            service.ProcedureSingleArgNoReturn (data);
        }

        [KRPCProcedure]
        public static void ProcedureThreeArgsNoReturn (KRPC.Schema.KRPC.Response x,
            KRPC.Schema.KRPC.Request y, KRPC.Schema.KRPC.Response z)
        {
            service.ProcedureThreeArgsNoReturn (x, y, z);
        }

        [KRPCProcedure]
        public static KRPC.Schema.KRPC.Response ProcedureNoArgsReturns ()
        {
            return service.ProcedureNoArgsReturns ();
        }

        [KRPCProcedure]
        public static KRPC.Schema.KRPC.Response ProcedureSingleArgReturns (KRPC.Schema.KRPC.Response data)
        {
            return service.ProcedureSingleArgReturns (data);
        }
    }
}


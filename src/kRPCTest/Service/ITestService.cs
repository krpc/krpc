using System;
using Google.ProtocolBuffers;

namespace KRPCTest.Service
{
    public interface ITestService
    {
        void ProcedureNoArgsNoReturn ();
        void ProcedureSingleArgNoReturn (KRPC.Schema.KRPC.Response data);
        void ProcedureThreeArgsNoReturn (KRPC.Schema.KRPC.Response x,
            KRPC.Schema.KRPC.Request y, KRPC.Schema.KRPC.Response z);
        KRPC.Schema.KRPC.Response ProcedureNoArgsReturns ();
        KRPC.Schema.KRPC.Response ProcedureSingleArgReturns (KRPC.Schema.KRPC.Response data);
    }
}

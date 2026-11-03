using KRPC.Service.Attributes;

namespace KRPC.Test.Service
{
    // Classes for the procedure id checks. They are not KRPCClasses, so that the scan of the
    // test assembly skips them.

    public class InvalidProcedureIdsBase
    {
        [KRPCMethod (Id = 20)]
        public void MethodWithAnId ()
        {
        }

        [KRPCMethod]
        public void Method ()
        {
        }

        [KRPCProperty]
        public int Property { get; set; }

        [KRPCProperty]
        public int ReadOnlyProperty { get; private set; }
    }

    [KRPCProcedureId (nameof (InvalidProcedureIdsBase.Property), GetterId = 21)]
    public class ValidProcedureIds : InvalidProcedureIdsBase
    {
        [KRPCMethod (Id = 22)]
        public void OwnMethod ()
        {
        }
    }

    [KRPCProcedureId ("Missing", Id = 1)]
    public class ProcedureIdForAMissingMember : InvalidProcedureIdsBase
    {
    }

    [KRPCProcedureId (nameof (ProcedureIdForAnOwnMethod.OwnMethod), Id = 1)]
    public class ProcedureIdForAnOwnMethod : InvalidProcedureIdsBase
    {
        [KRPCMethod]
        public void OwnMethod ()
        {
        }
    }

    [KRPCProcedureId (nameof (InvalidProcedureIdsBase.Method))]
    public class ProcedureIdForAMethodWithoutId : InvalidProcedureIdsBase
    {
    }

    [KRPCProcedureId (nameof (InvalidProcedureIdsBase.Method), GetterId = 1)]
    public class ProcedureIdForAMethodWithGetterId : InvalidProcedureIdsBase
    {
    }

    [KRPCProcedureId (nameof (InvalidProcedureIdsBase.Property), Id = 1)]
    public class ProcedureIdForAPropertyWithId : InvalidProcedureIdsBase
    {
    }

    [KRPCProcedureId (nameof (InvalidProcedureIdsBase.Property))]
    public class ProcedureIdForAPropertyWithoutIds : InvalidProcedureIdsBase
    {
    }

    [KRPCProcedureId (nameof (InvalidProcedureIdsBase.ReadOnlyProperty), SetterId = 1)]
    public class ProcedureIdForAMissingSetter : InvalidProcedureIdsBase
    {
    }

    public class ProcedureIdOnABaseClass : ValidProcedureIds
    {
    }

    [KRPCProcedureId (nameof (InvalidProcedureIdsBase.Method), Id = 1)]
    [KRPCProcedureId (nameof (InvalidProcedureIdsBase.Method), Id = 2)]
    public class ProcedureIdAppliedTwice : InvalidProcedureIdsBase
    {
    }
}

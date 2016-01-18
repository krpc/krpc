using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC procedure.
    /// </summary>
    [AttributeUsage (AttributeTargets.Method)]
    public class KRPCProcedureAttribute : Attribute
    {
    }
}

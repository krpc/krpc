using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC procedure.
    /// </summary>
    [AttributeUsage (AttributeTargets.Method)]
    public sealed class KRPCProcedureAttribute : Attribute
    {
        /// <summary>
        /// Whether the return value can be null.
        /// </summary>
        public bool Nullable { get; set; }
    }
}

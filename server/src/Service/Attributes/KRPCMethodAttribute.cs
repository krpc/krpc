using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC method.
    /// </summary>
    [AttributeUsage (AttributeTargets.Method)]
    public sealed class KRPCMethodAttribute : Attribute
    {
        /// <summary>
        /// Whether the return value can be null.
        /// </summary>
        public bool Nullable { get; set; }
    }
}

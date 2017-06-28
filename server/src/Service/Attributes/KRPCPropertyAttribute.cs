using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC property.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class KRPCPropertyAttribute : Attribute
    {
        /// <summary>
        /// Whether the return value (of the getter) can be null.
        /// </summary>
        public bool Nullable { get; set; }
    }
}

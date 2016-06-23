using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC property.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class KRPCPropertyAttribute : Attribute
    {
    }
}

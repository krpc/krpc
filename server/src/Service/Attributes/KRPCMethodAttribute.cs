using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC method.
    /// </summary>
    [AttributeUsage (AttributeTargets.Method)]
    public sealed class KRPCMethodAttribute : Attribute
    {
    }
}

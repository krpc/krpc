using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A nullable parameter in a kRPC procedure, method or property.
    /// This attribute can be used to mark a parameter as being nullable.
    /// </summary>
    [AttributeUsage (AttributeTargets.Parameter)]
    public sealed class KRPCNullableAttribute : Attribute
    {
    }
}

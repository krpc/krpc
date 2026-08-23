using System;

namespace KRPC.Client.Attributes
{
    /// <summary>
    /// Attribute attached to a struct that a service defines as a structure type. The order the
    /// struct declares its properties in is the order their values are encoded in, and the struct
    /// has a constructor taking them in that order.
    /// </summary>
    [AttributeUsage (AttributeTargets.Struct)]
    public sealed class KRPCStructAttribute : Attribute
    {
    }
}

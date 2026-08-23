using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC structure. Its fields are the properties annotated with
    /// <see cref="KRPCPropertyAttribute"/>, and its value is serialized inline
    /// rather than through a server-side handle.
    /// </summary>
    [AttributeUsage (AttributeTargets.Struct)]
    public sealed class KRPCStructAttribute : Attribute
    {
        /// <summary>
        /// Name of the service in which the structure is declared.
        /// </summary>
        public string Service { get; set; }
    }
}

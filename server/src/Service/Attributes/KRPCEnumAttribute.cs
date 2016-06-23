using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC enum.
    /// </summary>
    [AttributeUsage (AttributeTargets.Enum)]
    public sealed class KRPCEnumAttribute : Attribute
    {
        /// <summary>
        /// Name of the service in which the enum is declared.
        /// </summary>
        public string Service { get; set; }
    }
}

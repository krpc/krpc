using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC class.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class)]
    public sealed class KRPCClassAttribute : Attribute
    {
        /// <summary>
        /// Name of the service in which the class is declared.
        /// </summary>
        public string Service { get; set; }
    }
}

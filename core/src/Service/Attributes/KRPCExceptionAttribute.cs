using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A kRPC exception.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class)]
    public sealed class KRPCExceptionAttribute : Attribute
    {
        /// <summary>
        /// Name of the service in which the class is declared.
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// Exception type to map onto this exception type.
        /// </summary>
        /// <remarks>
        /// For example, can be used to map built-in C# exception types onto
        /// custom kRPC exception types.
        /// </remarks>
        public Type MappedException { get; set; }
    }
}

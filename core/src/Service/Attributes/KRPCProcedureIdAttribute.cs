using System;

namespace KRPC.Service.Attributes
{
    /// <summary>
    /// The ids of the procedures for a member that a kRPC class inherits from a base class.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
    public sealed class KRPCProcedureIdAttribute : Attribute
    {
        /// <summary>
        /// The name of the inherited method or property.
        /// </summary>
        public string Member { get; private set; }

        /// <summary>
        /// The id of the method.
        /// </summary>
        public uint Id { get; set; }

        /// <summary>
        /// The id of the property getter.
        /// </summary>
        public uint GetterId { get; set; }

        /// <summary>
        /// The id of the property setter.
        /// </summary>
        public uint SetterId { get; set; }

        /// <summary>
        /// The ids of the procedures for a member that a kRPC class inherits from a base class.
        /// </summary>
        /// <param name="member">The name of the inherited method or property, e.g. from <c>nameof</c>.</param>
        public KRPCProcedureIdAttribute (string member)
        {
            Member = member;
        }
    }
}

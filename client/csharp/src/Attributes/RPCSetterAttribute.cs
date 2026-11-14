using System;

namespace KRPC.Client.Attributes
{
    /// <summary>
    /// Attribute attached to a property whose setter invokes a remote procedure call.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class RPCSetterAttribute : Attribute
    {
        /// <summary>
        /// The remote service the setter calls.
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// The remote procedure the setter calls.
        /// </summary>
        public string Procedure { get; set; }

        /// <summary>
        /// Construct a RPC setter attribute.
        /// </summary>
        public RPCSetterAttribute (string service, string procedure)
        {
            Service = service;
            Procedure = procedure;
        }
    }
}

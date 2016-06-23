using System;

namespace KRPC.Client.Attributes
{
    /// <summary>
    /// Attribute attached to methods and properties that invoke remote procedure calls.
    /// </summary>
    [AttributeUsage (AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class RPCAttribute : Attribute
    {
        /// <summary>
        /// The remote service the method/property calls.
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// The remote procedure the method/property calls.
        /// </summary>
        public string Procedure { get; set; }

        /// <summary>
        /// Construct a RPC attribute.
        /// </summary>
        public RPCAttribute (string service, string procedure)
        {
            Service = service;
            Procedure = procedure;
        }
    }
}

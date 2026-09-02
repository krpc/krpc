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
        /// The generated class holding the type specs of the service's procedures. A call
        /// built from an expression has only the C# type of a value, which cannot say that a
        /// reference-typed position inside a collection is nullable, so it reads the spec the
        /// stub itself names from here.
        /// </summary>
        public Type Types { get; set; }

        /// <summary>
        /// Construct a RPC attribute.
        /// </summary>
        public RPCAttribute (string service, string procedure)
        {
            Service = service;
            Procedure = procedure;
        }

        /// <summary>
        /// Construct a RPC attribute naming the class that holds the service's type specs.
        /// </summary>
        public RPCAttribute (string service, string procedure, Type types)
        {
            Service = service;
            Procedure = procedure;
            Types = types;
        }
    }
}

using System;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for an exception, including class name and documentation.
    /// </summary>
    [Serializable]
    public sealed class ExceptionSignature : ISerializable
    {
        /// <summary>
        /// Name of the exception, not including the service it is in.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Name of the exception including the service it is in.
        /// I.e. ServiceName.ClassName
        /// </summary>
        public string FullyQualifiedName { get; private set; }

        /// <summary>
        /// Documentation for the exception
        /// </summary>
        public string Documentation { get; private set; }

        /// <summary>
        /// Create an exception signature
        /// </summary>
        public ExceptionSignature (string serviceName, string className, string documentation)
        {
            Name = className;
            FullyQualifiedName = serviceName + "." + Name;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
        }

        /// <summary>
        /// Serialize the signature.
        /// </summary>
        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("documentation", Documentation);
        }
    }
}

using System;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a class, including class name and documentation.
    /// </summary>
    [Serializable]
    public sealed class ClassSignature : ISerializable
    {
        /// <summary>
        /// Name of the class, not including the service it is in.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Name of the class including the service it is in.
        /// I.e. ServiceName.ClassName
        /// </summary>
        public string FullyQualifiedName { get; private set; }

        /// <summary>
        /// Documentation for the class
        /// </summary>
        public string Documentation { get; private set; }

        /// <summary>
        /// Whether the class is deprecated.
        /// </summary>
        public bool Deprecated { get; private set; }

        /// <summary>
        /// If the class is deprecated, the reason for its deprecation (may be empty).
        /// </summary>
        public string DeprecatedReason { get; private set; }

        /// <summary>
        /// Create a class signature
        /// </summary>
        public ClassSignature (string serviceName, string className, string documentation, bool deprecated, string deprecatedReason)
        {
            Name = className;
            FullyQualifiedName = serviceName + "." + Name;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
            Deprecated = deprecated;
            DeprecatedReason = deprecatedReason;
        }

        /// <summary>
        /// Serialize the signature.
        /// </summary>
        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("documentation", Documentation);
            if (Deprecated) {
                info.AddValue ("deprecated", true);
                info.AddValue ("deprecated_reason", DeprecatedReason);
            }
        }
    }
}

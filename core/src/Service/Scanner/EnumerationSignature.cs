using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for an enumeration type, including name, values and documentation.
    /// </summary>
    [Serializable]
    public sealed class EnumerationSignature : ISerializable
    {
        /// <summary>
        /// Name of the enumeration, not including the service it is in.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Name of the enumeration including the service it is in.
        /// </summary>
        public string FullyQualifiedName { get; private set; }

        /// <summary>
        /// Signatures of the values in the enumeration.
        /// </summary>
        public IList<EnumerationValueSignature> Values { get; private set; }

        /// <summary>
        /// Documentation for the procedure
        /// </summary>
        public string Documentation { get; private set; }

        /// <summary>
        /// Whether the enumeration is deprecated.
        /// </summary>
        public bool Deprecated { get; private set; }

        /// <summary>
        /// If the enumeration is deprecated, the reason for its deprecation (may be empty).
        /// </summary>
        public string DeprecatedReason { get; private set; }

        /// <summary>
        /// Create an enumeration signature
        /// </summary>
        public EnumerationSignature (string serviceName, string enumName, IList<EnumerationValueSignature> values, string documentation, bool deprecated, string deprecatedReason)
        {
            Name = enumName;
            FullyQualifiedName = serviceName + "." + Name;
            Values = values;
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
            info.AddValue ("values", Values);
            if (Deprecated) {
                info.AddValue ("deprecated", true);
                info.AddValue ("deprecated_reason", DeprecatedReason);
            }
        }
    }
}

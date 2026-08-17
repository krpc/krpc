using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a structure type, including name, fields and documentation.
    /// </summary>
    [Serializable]
    public sealed class StructSignature : ISerializable
    {
        /// <summary>
        /// Name of the structure, not including the service it is in.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Name of the structure including the service it is in.
        /// </summary>
        public string FullyQualifiedName { get; private set; }

        /// <summary>
        /// Signatures of the fields of the structure, in the order they are declared, which is
        /// the order their values are serialized in.
        /// </summary>
        public IList<StructFieldSignature> Fields { get; private set; }

        /// <summary>
        /// Documentation for the structure.
        /// </summary>
        public string Documentation { get; private set; }

        /// <summary>
        /// Whether the structure is deprecated.
        /// </summary>
        public bool Deprecated { get; private set; }

        /// <summary>
        /// If the structure is deprecated, the reason for its deprecation (may be empty).
        /// </summary>
        public string DeprecatedReason { get; private set; }

        /// <summary>
        /// Create a structure signature
        /// </summary>
        public StructSignature (string serviceName, string structName, IList<StructFieldSignature> fields, string documentation, bool deprecated, string deprecatedReason)
        {
            Name = structName;
            FullyQualifiedName = serviceName + "." + Name;
            Fields = fields;
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
            info.AddValue ("fields", Fields);
            if (Deprecated) {
                info.AddValue ("deprecated", true);
                info.AddValue ("deprecated_reason", DeprecatedReason);
            }
        }
    }
}

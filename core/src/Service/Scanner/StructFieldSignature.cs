using System;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a field of a structure type.
    /// </summary>
    [Serializable]
    public sealed class StructFieldSignature : ISerializable
    {
        /// <summary>
        /// Name of the field.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Name of the field including the service and structure it is in.
        /// </summary>
        public string FullyQualifiedName { get; private set; }

        /// <summary>
        /// Type of the field.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Documentation for the field.
        /// </summary>
        public string Documentation { get; private set; }

        /// <summary>
        /// Whether the field is deprecated.
        /// </summary>
        public bool Deprecated { get; private set; }

        /// <summary>
        /// If the field is deprecated, the reason for its deprecation (may be empty).
        /// </summary>
        public string DeprecatedReason { get; private set; }

        /// <summary>
        /// Create a signature for a field of a structure.
        /// </summary>
        public StructFieldSignature (string serviceName, string structName, string fieldName, Type type, string documentation, bool deprecated, string deprecatedReason)
        {
            Name = fieldName;
            FullyQualifiedName = serviceName + "." + structName + "." + Name;
            Type = type;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
            Deprecated = deprecated;
            DeprecatedReason = deprecatedReason;
        }

        /// <summary>
        /// Serialize the signature.
        /// </summary>
        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("name", Name);
            info.AddValue ("type", TypeUtils.SerializeType (TypeSpec.Create (Type)));
            info.AddValue ("documentation", Documentation);
            if (Deprecated) {
                info.AddValue ("deprecated", true);
                info.AddValue ("deprecated_reason", DeprecatedReason);
            }
        }
    }
}

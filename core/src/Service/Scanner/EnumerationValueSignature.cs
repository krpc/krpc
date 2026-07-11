using System;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for an enumeration type, including name, values and documentation.
    /// </summary>
    [Serializable]
    public sealed class EnumerationValueSignature : ISerializable
    {
        /// <summary>
        /// Name of the enumeration value.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Name of the enumeration value including the service and enum it is in.
        /// </summary>
        public string FullyQualifiedName { get; private set; }

        /// <summary>
        /// Integer value of the enumeration value.
        /// </summary>
        public int Value { get; private set; }

        /// <summary>
        /// Documentation for the enumeration value.
        /// </summary>
        public string Documentation { get; private set; }

        /// <summary>
        /// Whether the enumeration value is deprecated.
        /// </summary>
        public bool Deprecated { get; private set; }

        /// <summary>
        /// If the enumeration value is deprecated, the reason for its deprecation (may be empty).
        /// </summary>
        public string DeprecatedReason { get; private set; }

        /// <summary>
        /// Create a signature for an enumeration value.
        /// </summary>
        public EnumerationValueSignature (string serviceName, string enumName, string valueName, int value, string documentation, bool deprecated, string deprecatedReason)
        {
            Name = valueName;
            FullyQualifiedName = serviceName + "." + enumName + "." + Name;
            Value = value;
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
            info.AddValue ("value", Value);
            info.AddValue ("documentation", Documentation);
            if (Deprecated) {
                info.AddValue ("deprecated", true);
                info.AddValue ("deprecated_reason", DeprecatedReason);
            }
        }
    }
}

using System.Collections.Specialized;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for an enumeration type, including name, values and documentation.
    /// </summary>
    public sealed class EnumerationValueSignature
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
            TypeUtils.ValidateName (Name, "a value of " + serviceName + "." + enumName);
            Value = value;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
            Deprecated = deprecated;
            DeprecatedReason = deprecatedReason;
        }

        /// <summary>
        /// The values of the signature in a service definition.
        /// </summary>
        public OrderedDictionary Serialize ()
        {
            var result = new OrderedDictionary ();
            result ["name"] = Name;
            result ["value"] = Value;
            result ["documentation"] = Documentation;
            if (Deprecated) {
                result ["deprecated"] = true;
                result ["deprecated_reason"] = DeprecatedReason;
            }
            return result;
        }
    }
}

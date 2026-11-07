using System.Collections.Generic;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a field of a structure type.
    /// </summary>
    public sealed class StructFieldSignature
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
        /// The type of the field, with the nullability of every position inside it.
        /// </summary>
        public TypeSpec Spec { get; private set; }

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
        public StructFieldSignature (string serviceName, string structName, string fieldName, TypeSpec spec, string documentation, bool deprecated, string deprecatedReason)
        {
            Name = fieldName;
            FullyQualifiedName = serviceName + "." + structName + "." + Name;
            Spec = spec;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
            Deprecated = deprecated;
            DeprecatedReason = deprecatedReason;
        }

        /// <summary>
        /// The values of the signature in a service definition.
        /// </summary>
        public IDictionary<string, object> Serialize ()
        {
            var result = new Dictionary<string, object> ();
            result ["name"] = Name;
            result ["type"] = TypeUtils.SerializeType (Spec);
            result ["documentation"] = Documentation;
            if (Deprecated) {
                result ["deprecated"] = true;
                result ["deprecated_reason"] = DeprecatedReason;
            }
            return result;
        }
    }
}

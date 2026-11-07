using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for an enumeration type, including name, values and documentation.
    /// </summary>
    public sealed class EnumerationSignature
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
        /// The CLR type of the enumeration.
        /// </summary>
        public Type UnderlyingType { get; private set; }

        /// <summary>
        /// Create an enumeration signature
        /// </summary>
        public EnumerationSignature (string serviceName, string enumName, Type enumType, IList<EnumerationValueSignature> values, string documentation, bool deprecated, string deprecatedReason)
        {
            Name = enumName;
            FullyQualifiedName = serviceName + "." + Name;
            UnderlyingType = enumType;
            Values = values;
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
            result ["documentation"] = Documentation;
            result ["values"] = Values.Select (x => x.Serialize ()).ToList ();
            if (Deprecated) {
                result ["deprecated"] = true;
                result ["deprecated_reason"] = DeprecatedReason;
            }
            return result;
        }
    }
}

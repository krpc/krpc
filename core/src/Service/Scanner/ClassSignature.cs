using System;
using System.Collections.Specialized;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a class, including class name and documentation.
    /// </summary>
    public sealed class ClassSignature
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
        /// The CLR type of the class.
        /// </summary>
        public Type UnderlyingType { get; private set; }

        /// <summary>
        /// Create a class signature
        /// </summary>
        public ClassSignature (string serviceName, string className, Type classType, string documentation, bool deprecated, string deprecatedReason)
        {
            Name = className;
            FullyQualifiedName = serviceName + "." + Name;
            UnderlyingType = classType;
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
            if (Deprecated) {
                result ["deprecated"] = true;
                result ["deprecated_reason"] = DeprecatedReason;
            }
            return result;
        }
    }
}

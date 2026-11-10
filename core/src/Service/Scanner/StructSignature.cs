using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a structure type, including name, fields and documentation.
    /// </summary>
    public sealed class StructSignature
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
        /// Id of the structure. Uniquely identifies the type within the service.
        /// </summary>
        public uint Id { get; private set; }

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
        /// The CLR type of the structure.
        /// </summary>
        public Type UnderlyingType { get; private set; }

        /// <summary>
        /// Create a structure signature
        /// </summary>
        public StructSignature (string serviceName, string structName, Type structType, IList<StructFieldSignature> fields, string documentation, bool deprecated, string deprecatedReason)
        {
            Name = structName;
            FullyQualifiedName = serviceName + "." + Name;
            Id = TypeUtils.GetTypeId (structType);
            UnderlyingType = structType;
            Fields = fields;
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
            result ["id"] = Id;
            result ["documentation"] = Documentation;
            result ["fields"] = Fields.Select (x => x.Serialize ()).ToList ();
            if (Deprecated) {
                result ["deprecated"] = true;
                result ["deprecated_reason"] = DeprecatedReason;
            }
            return result;
        }
    }
}

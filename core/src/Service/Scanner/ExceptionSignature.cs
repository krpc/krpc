using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for an exception, including class name and documentation.
    /// </summary>
    public sealed class ExceptionSignature
    {
        /// <summary>
        /// Name of the exception, not including the service it is in.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Name of the exception including the service it is in.
        /// I.e. ServiceName.ClassName
        /// </summary>
        public string FullyQualifiedName { get; private set; }

        /// <summary>
        /// Documentation for the exception
        /// </summary>
        public string Documentation { get; private set; }

        /// <summary>
        /// Whether the exception is deprecated.
        /// </summary>
        public bool Deprecated { get; private set; }

        /// <summary>
        /// If the exception is deprecated, the reason for its deprecation (may be empty).
        /// </summary>
        public string DeprecatedReason { get; private set; }

        /// <summary>
        /// The CLR type of the exception.
        /// </summary>
        public Type UnderlyingType { get; private set; }

        /// <summary>
        /// The full name of the CLR exception type mapped onto the exception, or empty.
        /// </summary>
        public string MappedType { get; private set; }

        /// <summary>
        /// Create an exception signature
        /// </summary>
        public ExceptionSignature (string serviceName, string className, Type exnType, string documentation, bool deprecated, string deprecatedReason)
        {
            Name = className;
            FullyQualifiedName = serviceName + "." + Name;
            UnderlyingType = exnType;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
            Deprecated = deprecated;
            DeprecatedReason = deprecatedReason;
            var mappedType = Reflection.GetAttribute<KRPCExceptionAttribute> (exnType).MappedException;
            MappedType = mappedType != null ? mappedType.FullName : string.Empty;
        }

        /// <summary>
        /// The values of the signature in a service definition.
        /// </summary>
        public IDictionary<string, object> Serialize ()
        {
            var result = new Dictionary<string, object> ();
            result ["documentation"] = Documentation;
            if (Deprecated) {
                result ["deprecated"] = true;
                result ["deprecated_reason"] = DeprecatedReason;
            }
            if (MappedType.Length > 0)
                result ["mapped_type"] = MappedType;
            return result;
        }
    }
}

using System.Collections.Generic;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for an enumeration type, including name, values and documentation.
    /// </summary>
    class EnumerationSignature
    {
        /// <summary>
        /// Name of the enumeration, not including the service it is in.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Name of the enumeration including the service it is in.
        /// I.e. ServiceName.ClassName
        /// </summary>
        public string FullyQualifiedName { get; private set; }

        public IDictionary<string, int> Values { get; private set; }

        /// <summary>
        /// Documentation for the procedure
        /// </summary>
        public string Documentation { get; private set; }

        public EnumerationSignature (string serviceName, string enumName, IDictionary<string, int> values, string documentation)
        {
            Name = enumName;
            FullyQualifiedName = serviceName + "." + Name;
            Values = values;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
        }
    }
}

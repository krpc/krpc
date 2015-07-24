using System.Collections.Generic;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for an enumeration type, including name, values and documentation.
    /// </summary>
    class EnumerationValueSignature
    {
        /// <summary>
        /// Name of the enumeration value.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Integer value of the enumeration value.
        /// </summary>
        public int Value { get; private set; }

        /// <summary>
        /// Documentation for the enumeration value.
        /// </summary>
        public string Documentation { get; private set; }

        public EnumerationValueSignature (string name, int value, string documentation)
        {
            Name = name;
            Value = value;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
        }
    }
}

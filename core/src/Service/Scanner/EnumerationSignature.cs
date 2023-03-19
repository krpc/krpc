using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for an enumeration type, including name, values and documentation.
    /// </summary>
    [Serializable]
    public sealed class EnumerationSignature : ISerializable
    {
        /// <summary>
        /// Name of the enumeration, not including the service it is in.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Name of the enumeration including the service it is in.
        /// </summary>
        public string FullyQualifiedName { get; private set; }

        public IList<EnumerationValueSignature> Values { get; private set; }

        /// <summary>
        /// Documentation for the procedure
        /// </summary>
        public string Documentation { get; private set; }

        public EnumerationSignature (string serviceName, string enumName, IList<EnumerationValueSignature> values, string documentation)
        {
            Name = enumName;
            FullyQualifiedName = serviceName + "." + Name;
            Values = values;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
        }

        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("documentation", Documentation);
            info.AddValue ("values", Values);
        }
    }
}

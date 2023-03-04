using System;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for an enumeration type, including name, values and documentation.
    /// </summary>
    [Serializable]
    sealed class EnumerationValueSignature : ISerializable
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

        public EnumerationValueSignature (string serviceName, string enumName, string valueName, int value, string documentation)
        {
            Name = valueName;
            FullyQualifiedName = serviceName + "." + enumName + "." + Name;
            Value = value;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
        }

        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("name", Name);
            info.AddValue ("value", Value);
            info.AddValue ("documentation", Documentation);
        }
    }
}

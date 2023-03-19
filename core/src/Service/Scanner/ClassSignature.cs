using System;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a class, including class name and documentation.
    /// </summary>
    [Serializable]
    sealed class ClassSignature : ISerializable
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

        public ClassSignature (string serviceName, string className, string documentation)
        {
            Name = className;
            FullyQualifiedName = serviceName + "." + Name;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
        }

        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("documentation", Documentation);
        }
    }
}

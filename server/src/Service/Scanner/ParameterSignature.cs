using System;
using KRPC.Utils;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a parameter.
    /// </summary>
    class ParameterSignature : ISerializable
    {
        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Type of the parameter.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// True if this parameter is optional and has a default argument.
        /// </summary>
        public bool HasDefaultArgument { get; private set; }

        /// <summary>
        /// Default argument, if <see cref="HasDefaultArgument"/> is true.
        /// </summary>
        public object DefaultArgument { get; private set; }

        public ParameterSignature (string fullProcedureName, ProcedureParameter parameter)
        {
            Name = parameter.Name;
            Type = parameter.Type;

            // Check the parameter type is valid
            if (!TypeUtils.IsAValidType (Type))
                throw new ServiceException (Type + " is not a valid Procedure parameter type, in " + fullProcedureName);

            HasDefaultArgument = parameter.HasDefaultValue;
            if (parameter.HasDefaultValue)
                DefaultArgument = parameter.DefaultValue;
        }

        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("name", Name);
            info.AddValue ("type", TypeUtils.GetTypeName (Type));
            if (HasDefaultArgument)
                info.AddValue ("default_argument", global::KRPC.ProtoBuf.ProtocolBuffers.Encode (DefaultArgument, Type));
        }
    }
}

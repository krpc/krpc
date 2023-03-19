using System;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a parameter.
    /// </summary>
    [Serializable]
    public sealed class ParameterSignature : ISerializable
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
        public bool HasDefaultValue { get; private set; }

        /// <summary>
        /// Default argument, if <see cref="HasDefaultValue"/> is true.
        /// </summary>
        public object DefaultValue { get; private set; }

        /// <summary>
        /// True if this parameter is nullable.
        /// </summary>
        public bool Nullable { get; private set; }

        /// <summary>
        /// Create a parameter signature for a reflected parameter.
        /// </summary>
        public ParameterSignature (string fullProcedureName, ProcedureParameter parameter)
        {
            Name = parameter.Name;
            Type = parameter.Type;

            // Check the parameter type is valid
            if (!TypeUtils.IsAValidType (Type))
                throw new ServiceException (Type + " is not a valid Procedure parameter type, in " + fullProcedureName);

            HasDefaultValue = parameter.HasDefaultValue;
            if (HasDefaultValue)
                DefaultValue = parameter.DefaultValue;
            Nullable = parameter.Nullable;
        }

        /// <summary>
        /// Serialize the signature.
        /// </summary>
        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("name", Name);
            info.AddValue ("type", TypeUtils.SerializeType (Type));
            if (HasDefaultValue)
                info.AddValue ("default_value", Server.ProtocolBuffers.Encoder.Encode (DefaultValue).ToByteArray ());
            if (Nullable) {
                info.AddValue ("nullable", Nullable);
            }
        }
    }
}

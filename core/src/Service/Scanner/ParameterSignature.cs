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
        /// True if this parameter is optional and has a default argument.
        /// </summary>
        public bool HasDefaultValue { get; private set; }

        /// <summary>
        /// Default argument, if <see cref="HasDefaultValue"/> is true.
        /// </summary>
        public object DefaultValue { get; private set; }

        /// <summary>
        /// The type of the parameter, together with whether it can be null.
        /// </summary>
        public TypeSpec Spec { get; private set; }

        /// <summary>
        /// Create a parameter signature for a reflected parameter.
        /// </summary>
        public ParameterSignature (string fullProcedureName, ProcedureParameter parameter)
        {
            Name = parameter.Name;

            // Check the parameter type is valid
            if (!TypeUtils.IsAValidType (parameter.Type))
                throw new ServiceException (parameter.Type + " is not a valid Procedure parameter type, in " + fullProcedureName);

            HasDefaultValue = parameter.HasDefaultValue;
            if (HasDefaultValue)
                DefaultValue = parameter.DefaultValue;
            Spec = TypeSpec.Create (parameter.Type, parameter.Nullable);
        }

        /// <summary>
        /// Serialize the signature.
        /// </summary>
        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("name", Name);
            info.AddValue ("type", TypeUtils.SerializeType (Spec));
            if (HasDefaultValue) {
                if (DefaultValue == null)
                    info.AddValue ("default_value", (object)null);
                else
                    info.AddValue ("default_value", Server.ProtocolBuffers.Encoder.Encode (DefaultValue).ToByteArray ());
            }
        }
    }
}

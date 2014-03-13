using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.ProtocolBuffers;
using KRPC.Utils;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a parameter.
    /// </summary>
    class ParameterSignature
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
        /// Serialized value of its default argument, or null if it has no default argument.
        /// </summary>
        public ByteString DefaultArgument { get; private set; }

        /// <summary>
        /// True if this parameter is optional.
        /// </summary>
        public bool HasDefaultArgument {
            get { return DefaultArgument != null; }
        }

        public ParameterSignature (string fullProcedureName, ProcedureParameter parameter)
        {
            Name = parameter.Name;
            Type = parameter.Type;

            // Check the parameter type is valid
            if (!TypeUtils.IsAValidType (Type))
                throw new ServiceException (Type + " is not a valid Procedure parameter type, in " + fullProcedureName);

            // Encode the default value as a ByteString
            if (parameter.HasDefaultValue) {
                var value = parameter.DefaultValue;
                if (TypeUtils.IsAClassType (Type))
                    DefaultArgument = ProtocolBuffers.WriteValue (ObjectStore.Instance.AddInstance (value), typeof(ulong));
                else if (ProtocolBuffers.IsAMessageType (Type))
                    DefaultArgument = ProtocolBuffers.WriteMessage (value as IMessage);
                else
                    DefaultArgument = ProtocolBuffers.WriteValue (value, Type);
            }
        }
    }
}

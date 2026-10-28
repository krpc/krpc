using System;
using System.Collections;
using System.Globalization;
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
        /// The type of the parameter, with the nullability of every position inside it.
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
            var location = "parameter " + Name + " of " + fullProcedureName;
            Spec = TypeSpec.Create (
                parameter.Type, parameter.Nullable, parameter.NullablePaths, location);
            if (HasDefaultValue)
                CheckEnumerationValues (DefaultValue, Spec, location);
        }

        // A default value holds only values its enumerations declare, as a client has no
        // value of an enumeration for any other number
        static void CheckEnumerationValues (object value, TypeSpec spec, string location)
        {
            if (value == null)
                return;
            switch (spec.Kind) {
            case TypeKind.Enum:
                if (!Enum.IsDefined (spec.Type, Enum.ToObject (spec.Type, value)))
                    throw new ServiceException (
                        "The default value of " + location + " holds " +
                        Convert.ToInt64 (value, CultureInfo.InvariantCulture).ToString (CultureInfo.InvariantCulture) +
                        ", which is not a value of the enumeration " + spec.Type.Name);
                break;
            case TypeKind.List:
            case TypeKind.Set:
                foreach (var item in (IEnumerable)value)
                    CheckEnumerationValues (item, spec.Types [0], location);
                break;
            case TypeKind.Dictionary:
                foreach (DictionaryEntry entry in (IDictionary)value) {
                    CheckEnumerationValues (entry.Key, spec.Types [0], location);
                    CheckEnumerationValues (entry.Value, spec.Types [1], location);
                }
                break;
            case TypeKind.Tuple:
                for (int i = 0; i < spec.Types.Count; i++)
                    CheckEnumerationValues (value.GetType ().GetProperty ("Item" + (i + 1)).GetValue (value, null),
                        spec.Types [i], location);
                break;
            }
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
                    info.AddValue ("default_value", Server.ProtocolBuffers.Encoder.Encode (DefaultValue, Spec).ToByteArray ());
            }
        }
    }
}

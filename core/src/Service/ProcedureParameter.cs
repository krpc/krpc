using System;
using System.Linq;
using System.Reflection;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// Information about a procedure parameter.
    /// </summary>
    public sealed class ProcedureParameter
    {
        /// <summary>
        /// Type of the parameter.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Default value of the parameter.
        /// </summary>
        public object DefaultValue { get; private set; }

        /// <summary>
        /// Whether the parameter has a Default value.
        /// </summary>
        public bool HasDefaultValue {
            get { return DefaultValue != DBNull.Value; }
        }

        /// <summary>
        /// Whether the parameters value could be null.
        /// </summary>
        public bool Nullable { get; internal set; }

        /// <summary>
        /// Create parameter information from a reflected parameter.
        /// </summary>
        public ProcedureParameter (ParameterInfo parameter)
        {
            Type = parameter.ParameterType;
            Name = parameter.Name;
            // A Nullable<T> value-type parameter is represented by its underlying type T, and is
            // implicitly nullable.
            var underlyingType = System.Nullable.GetUnderlyingType (Type);
            bool nullableValueType = underlyingType != null;
            if (nullableValueType)
                Type = underlyingType;
            bool hasDefaultValue = parameter.IsOptional && (parameter.Attributes & ParameterAttributes.HasDefault) == ParameterAttributes.HasDefault;
            DefaultValue = hasDefaultValue ? parameter.DefaultValue : DBNull.Value;
            if (Reflection.HasAttribute<KRPCDefaultValueAttribute> (parameter))
                DefaultValue = Reflection.GetAttribute<KRPCDefaultValueAttribute> (parameter).Value;
            // A parameter is nullable if its type is Nullable<T>, or it has a null default value
            // (itself a declaration that null is valid), or it is marked [KRPCNullable].
            Nullable = nullableValueType
                || Reflection.HasAttribute<KRPCNullableAttribute> (parameter)
                || (HasDefaultValue && DefaultValue == null);
        }

        /// <summary>
        /// Create parameter information from its type and name.
        /// </summary>
        public ProcedureParameter (Type type, string name)
        {
            Type = type;
            Name = name;
            DefaultValue = DBNull.Value;
        }
    }
}

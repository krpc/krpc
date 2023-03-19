using System;
using System.Diagnostics.CodeAnalysis;
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
        public bool Nullable { get; private set; }

        /// <summary>
        /// Create parameter information from a reflected parameter.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidUnnecessarySpecializationRule")]
        public ProcedureParameter (ParameterInfo parameter)
        {
            Type = parameter.ParameterType;
            Name = parameter.Name;
            bool hasDefaultValue = parameter.IsOptional && (parameter.Attributes & ParameterAttributes.HasDefault) == ParameterAttributes.HasDefault;
            DefaultValue = hasDefaultValue ? parameter.DefaultValue : DBNull.Value;
            if (Reflection.HasAttribute<KRPCDefaultValueAttribute> (parameter))
                DefaultValue = Reflection.GetAttribute<KRPCDefaultValueAttribute> (parameter).Value;
            Nullable = Reflection.HasAttribute<KRPCNullableAttribute> (parameter);
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

        public ProcedureParameter (Type type, string name, object defaultValue)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service
{
    public sealed class ProcedureParameter
    {
        public Type Type { get; private set; }

        public string Name { get; private set; }

        public object DefaultValue { get; private set; }

        public bool HasDefaultValue {
            get { return DefaultValue != DBNull.Value; }
        }

        public bool Nullable { get; private set; }

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

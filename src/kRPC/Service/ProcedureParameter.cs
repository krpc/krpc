using System;
using System.Reflection;

namespace KRPC.Service
{
    public class ProcedureParameter
    {
        public Type Type { get; private set; }

        public string Name { get; private set; }

        public object DefaultValue { get; private set; }

        public bool HasDefaultValue {
            get { return DefaultValue != DBNull.Value; }
        }

        public ProcedureParameter (ParameterInfo info)
        {
            Type = info.ParameterType;
            Name = info.Name;
            bool hasDefaultValue = info.IsOptional && (info.Attributes & ParameterAttributes.HasDefault) == ParameterAttributes.HasDefault;
            DefaultValue = hasDefaultValue ? info.DefaultValue : DBNull.Value;
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


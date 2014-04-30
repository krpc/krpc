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
            get { return DefaultValue != null; }
        }

        public ProcedureParameter (ParameterInfo info)
        {
            Type = info.ParameterType;
            Name = info.Name;
            bool hasDefaultValue = info.IsOptional && (info.Attributes & ParameterAttributes.HasDefault) == ParameterAttributes.HasDefault;
            DefaultValue = hasDefaultValue ? info.DefaultValue : null;
        }

        public ProcedureParameter (Type type, string name, object defaultValue = null)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}


using System;
using System.Collections.Generic;
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
        /// The type the parameter is declared with, which a Nullable&lt;T&gt; keeps.
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
        /// Whether the parameter is declared nullable, by an attribute or a null default value.
        /// </summary>
        public bool Nullable { get; internal set; }

        /// <summary>
        /// The nullable positions inside the parameter's type, each named by a path.
        /// </summary>
        public IEnumerable<IList<Position>> NullablePaths { get; internal set; }

        /// <summary>
        /// Create parameter information from a reflected parameter.
        /// </summary>
        public ProcedureParameter (ParameterInfo parameter)
        {
            Type = parameter.ParameterType;
            Name = parameter.Name;
            bool hasDefaultValue = parameter.IsOptional && (parameter.Attributes & ParameterAttributes.HasDefault) == ParameterAttributes.HasDefault;
            DefaultValue = hasDefaultValue ? parameter.DefaultValue : DBNull.Value;
            if (Reflection.HasAttribute<KRPCDefaultValueAttribute> (parameter))
                DefaultValue = Reflection.GetAttribute<KRPCDefaultValueAttribute> (parameter).Value;
            // A parameter is nullable if it has a null default value, itself a declaration that
            // null is valid, or it is marked [KRPCNullable]. The spec reads a Nullable<T> type
            // as nullable on its own
            NullablePaths = TypeUtils.GetNullablePaths (parameter).ToList ();
            Nullable = NullablePaths.Any (path => path.Count == 0)
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
            NullablePaths = new List<IList<Position>> ();
        }
    }
}

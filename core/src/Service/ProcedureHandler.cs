using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using KRPC.Service.Attributes;

namespace KRPC.Service
{
    /// <summary>
    /// Used to invoke a static method with the KRPCProcedure attribute.
    /// </summary>
    sealed class ProcedureHandler : IProcedureHandler
    {
        readonly Func<object, object[], object> invoker;
        readonly ProcedureParameter[] parameters;

        public ProcedureHandler (MethodInfo methodInfo, bool returnIsNullable, IEnumerable<IList<Position>> returnNullablePaths = null)
        {
            invoker = BuildInvoker (methodInfo);
            parameters = methodInfo.GetParameters ().Select (x => new ProcedureParameter (x)).ToArray ();
            ReturnType = methodInfo.ReturnType;
            ReturnIsNullable = returnIsNullable;
            ReturnNullablePaths = returnNullablePaths ?? new List<IList<Position>> ();
        }

        public bool HasInstance { get => false; }

        public object Invoke (object instance, object[] arguments)
        {
            return invoker (instance, arguments);
        }

        public IEnumerable<ProcedureParameter> Parameters {
            get { return parameters; }
        }

        public Type ReturnType { get; private set; }

        public bool ReturnIsNullable { get; private set; }

        public IEnumerable<IList<Position>> ReturnNullablePaths { get; private set; }

        /// <summary>
        /// Give the property value parameter (the last parameter) the nullability the property
        /// declares. Used for the setter of a property, whose synthesized value parameter
        /// cannot carry [KRPCNullable] itself.
        /// </summary>
        public void SetValueParameterNullability (bool nullable, IEnumerable<IList<Position>> nullablePaths)
        {
            var parameter = parameters [parameters.Length - 1];
            parameter.Nullable = nullable;
            parameter.NullablePaths = nullablePaths;
        }

        static Func<object, object[], object> BuildInvoker (MethodInfo method)
        {
            var instanceParam = Expression.Parameter (typeof(object), "instance");
            var argsParam = Expression.Parameter (typeof(object[]), "args");
            var methodParams = method.GetParameters ();
            var argExprs = new Expression [methodParams.Length];
            for (int i = 0; i < methodParams.Length; i++)
                argExprs [i] = Expression.Convert (
                    Expression.ArrayIndex (argsParam, Expression.Constant (i)),
                    methodParams [i].ParameterType);
            Expression call = Expression.Call (method, argExprs);
            Expression body = method.ReturnType == typeof(void)
                ? (Expression)Expression.Block (call, Expression.Constant (null, typeof(object)))
                : Expression.Convert (call, typeof(object));
            return Expression.Lambda<Func<object, object[], object>> (body, instanceParam, argsParam).Compile ();
        }
    }
}

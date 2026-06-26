using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace KRPC.Service
{
    /// <summary>
    /// Used to invoke a static method with the KRPCProcedure attribute.
    /// </summary>
    sealed class ProcedureHandler : IProcedureHandler
    {
        readonly Func<object, object[], object> invoker;
        readonly ProcedureParameter[] parameters;

        public ProcedureHandler (MethodInfo methodInfo, bool returnIsNullable)
        {
            invoker = BuildInvoker (methodInfo);
            parameters = methodInfo.GetParameters ().Select (x => new ProcedureParameter (x)).ToArray ();
            ReturnType = methodInfo.ReturnType;
            ReturnIsNullable = returnIsNullable;
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

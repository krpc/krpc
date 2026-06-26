using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace KRPC.Service
{
    /// <summary>
    /// Used to invoke a class method with the KRPCMethod attribute.
    /// Invoke() gets the instance of the class using the guid
    /// (which is always the first parameter) and runs the method.
    /// </summary>
    sealed class ClassMethodHandler : IProcedureHandler
    {
        readonly Func<object, object[], object> invoker;
        readonly ProcedureParameter[] parameters;

        public ClassMethodHandler (Type classType, MethodInfo methodInfo, bool returnIsNullable)
        {
            invoker = BuildInvoker (classType, methodInfo);
            var parameterList = methodInfo.GetParameters ().Select (x => new ProcedureParameter (x)).ToList ();
            parameterList.Insert (0, new ProcedureParameter (classType, "this"));
            parameters = parameterList.ToArray ();
            ReturnType = methodInfo.ReturnType;
            ReturnIsNullable = returnIsNullable;
        }

        public bool HasInstance { get => true; }

        /// <summary>
        /// Invokes a method on an object. The first parameter must be the object's instance,
        /// and the remaining parameters are passed to the method.
        /// </summary>
        public object Invoke (object instance, object[] arguments)
        {
            return invoker (instance, arguments);
        }

        public IEnumerable<ProcedureParameter> Parameters {
            get { return parameters; }
        }

        public Type ReturnType { get; private set; }

        public bool ReturnIsNullable { get; private set; }

        static Func<object, object[], object> BuildInvoker (Type classType, MethodInfo method)
        {
            var instanceParam = Expression.Parameter (typeof(object), "instance");
            var argsParam = Expression.Parameter (typeof(object[]), "args");
            var castInstance = Expression.Convert (instanceParam, classType);
            var methodParams = method.GetParameters ();
            var argExprs = new Expression [methodParams.Length];
            for (int i = 0; i < methodParams.Length; i++)
                argExprs [i] = Expression.Convert (
                    Expression.ArrayIndex (argsParam, Expression.Constant (i)),
                    methodParams [i].ParameterType);
            Expression call = Expression.Call (castInstance, method, argExprs);
            Expression body = method.ReturnType == typeof(void)
                ? (Expression)Expression.Block (call, Expression.Constant (null, typeof(object)))
                : Expression.Convert (call, typeof(object));
            return Expression.Lambda<Func<object, object[], object>> (body, instanceParam, argsParam).Compile ();
        }
    }
}

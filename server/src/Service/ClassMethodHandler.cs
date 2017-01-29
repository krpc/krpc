using System;
using System.Collections.Generic;
using System.Linq;
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
        readonly MethodInfo method;
        readonly ProcedureParameter[] parameters;
        readonly object[] methodArguments;

        public ClassMethodHandler (MethodInfo methodInfo)
        {
            method = methodInfo;
            var parameterList = method.GetParameters ().Select (x => new ProcedureParameter (methodInfo, x)).ToList ();
            parameterList.Insert (0, new ProcedureParameter (typeof(ulong), "this"));
            parameters = parameterList.ToArray ();
            methodArguments = new object[parameters.Length - 1];
        }

        /// <summary>
        /// Invokes a method on an object. The first parameter must be an the objects GUID, which is
        /// used to fetch the instance, and the remaining parameters are passed to the method.
        /// </summary>
        public object Invoke (params object[] arguments)
        {
            var instanceGuid = (ulong)arguments [0];
            // TODO: should be able to invoke default arguments using Type.Missing, but get "System.ArgumentException : failed to convert parameters"
            for (int i = 1; i < arguments.Length; i++)
                methodArguments [i - 1] = (arguments [i] == Type.Missing) ? parameters [i].DefaultValue : arguments [i];
            return method.Invoke (ObjectStore.Instance.GetInstance (instanceGuid), methodArguments);
        }

        public IEnumerable<ProcedureParameter> Parameters {
            get { return parameters; }
        }

        public Type ReturnType {
            get { return method.ReturnType; }
        }
    }
}


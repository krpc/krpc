using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KRPC.Service
{
    /// <summary>
    /// Used to invoke a static method with the KRPCMethod attribute.
    /// Invoke() and runs the static method.
    /// </summary>
    sealed class ClassStaticMethodHandler : IProcedureHandler
    {
        readonly MethodInfo method;
        readonly ProcedureParameter[] parameters;
        readonly object[] methodArguments;

        public ClassStaticMethodHandler (MethodInfo methodInfo, bool returnIsNullable)
        {
            method = methodInfo;
            parameters = method.GetParameters ().Select (x => new ProcedureParameter (x)).ToArray ();
            methodArguments = new object[parameters.Length];
            ReturnIsNullable = returnIsNullable;
        }

        /// <summary>
        /// Invokes the static method.
        /// </summary>
        public object Invoke (params object[] arguments)
        {
            // TODO: should be able to invoke default arguments using Type.Missing, but get "System.ArgumentException : failed to convert parameters"
            for (int i = 0; i < arguments.Length; i++)
                methodArguments [i] = (arguments [i] == Type.Missing) ? parameters [i].DefaultValue : arguments [i];
            return method.Invoke (null, methodArguments);
        }

        public IEnumerable<ProcedureParameter> Parameters {
            get { return parameters; }
        }

        public Type ReturnType {
            get { return method.ReturnType; }
        }

        public bool ReturnIsNullable { get; private set; }
    }
}

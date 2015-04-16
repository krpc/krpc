using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace KRPC.Service
{
    /// <summary>
    /// Used to invoke a static method with the KRPCMethod attribute.
    /// Invoke() and runs the static method.
    /// </summary>
    class ClassStaticMethodHandler : IProcedureHandler
    {
        readonly MethodInfo method;

        public ClassStaticMethodHandler (MethodInfo method)
        {
            this.method = method;
        }

        /// <summary>
        /// Invokes the static method.
        /// </summary>
        public object Invoke (params object[] arguments)
        {
            // TODO: should be able to invoke default arguments using Type.Missing, but get "System.ArgumentException : failed to convert parameters"
            var parameters = Parameters.ToArray ();
            var methodArguments = new object[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
                methodArguments [i] = (arguments [i] == Type.Missing) ? parameters [i].DefaultValue : arguments [i];
            return method.Invoke (null, methodArguments);
        }

        public IEnumerable<ProcedureParameter> Parameters {
            get { return method.GetParameters ().Select (x => new ProcedureParameter (x)).ToList (); }
        }

        public Type ReturnType {
            get { return method.ReturnType; }
        }
    }
}

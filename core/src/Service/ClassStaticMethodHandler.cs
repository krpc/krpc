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

        public ClassStaticMethodHandler (MethodInfo methodInfo, bool returnIsNullable)
        {
            method = methodInfo;
            parameters = method.GetParameters ().Select (x => new ProcedureParameter (x)).ToArray ();
            ReturnIsNullable = returnIsNullable;
        }

        public bool HasInstance { get => false; }

        /// <summary>
        /// Invokes the static method.
        /// </summary>
        public object Invoke (object instance, object [] arguments)
        {
            return method.Invoke (instance, arguments);
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

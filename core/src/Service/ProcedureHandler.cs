using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KRPC.Service
{
    /// <summary>
    /// Used to invoke a static method with the KRPCProcedure attribute.
    /// </summary>
    sealed class ProcedureHandler : IProcedureHandler
    {
        readonly MethodInfo method;
        readonly ProcedureParameter[] parameters;

        public ProcedureHandler (MethodInfo methodInfo, bool returnIsNullable)
        {
            method = methodInfo;
            parameters = method.GetParameters ().Select (x => new ProcedureParameter (x)).ToArray ();
            ReturnIsNullable = returnIsNullable;
        }

        public bool HasInstance { get => false; }

        public object Invoke (object instance, object[] arguments)
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

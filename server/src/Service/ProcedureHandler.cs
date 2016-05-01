using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KRPC.Service
{
    /// <summary>
    /// Used to invoke a static method with the KRPCProcedure attribute.
    /// </summary>
    class ProcedureHandler : IProcedureHandler
    {
        readonly MethodInfo method;

        public ProcedureHandler (MethodInfo method)
        {
            this.method = method;
        }

        public object Invoke (params object[] arguments)
        {
            // TODO: should be able to invoke default arguments using Type.Missing, but get "System.ArgumentException : failed to convert parameters"
            var parameters = Parameters.ToArray ();
            var newArguments = new object[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
                newArguments [i] = (arguments [i] == Type.Missing) ? parameters [i].DefaultValue : arguments [i];
            return method.Invoke (null, newArguments);
        }

        public IEnumerable<ProcedureParameter> Parameters {
            get {
                return method.GetParameters ().Select (x => new ProcedureParameter (x));
            }
        }

        public Type ReturnType {
            get { return method.ReturnType; }
        }
    }
}


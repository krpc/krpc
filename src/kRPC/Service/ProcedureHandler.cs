using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace KRPC.Service
{
    /// <summary>
    /// Used to invoke a static method with the KRPCProcedure attribute.
    /// </summary>
    public class ProcedureHandler : IProcedureHandler
    {
        readonly MethodInfo method;

        public ProcedureHandler (MethodInfo method)
        {
            this.method = method;
        }

        public object Invoke (params object[] parameters)
        {
            return method.Invoke (null, parameters);
        }

        public IEnumerable<Type> GetParameters ()
        {
            return method.GetParameters ().Select (x => x.ParameterType);
        }

        public Type ReturnType {
            get { return method.ReturnType; }
        }
    }
}


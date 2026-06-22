using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KRPC.Service.Attributes;
using KRPC.Utils;

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

        public ClassMethodHandler (Type classType, MethodInfo methodInfo, bool returnIsNullable)
        {
            method = methodInfo;
            var parameterList = method.GetParameters ().Select (x => new ProcedureParameter (x)).ToList ();
            parameterList.Insert (0, new ProcedureParameter (classType, "this"));
            parameters = parameterList.ToArray ();
            ReturnIsNullable = returnIsNullable;
        }

        public bool HasInstance { get => true;}

        /// <summary>
        /// Invokes a method on an object. The first parameter must be an the objects GUID, which is
        /// used to fetch the instance, and the remaining parameters are passed to the method.
        /// </summary>
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

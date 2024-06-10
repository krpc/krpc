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
        readonly object[] methodArguments;

        public ClassMethodHandler (Type classType, MethodInfo methodInfo, bool returnIsNullable)
        {
            method = methodInfo;
            var parameterList = method.GetParameters ().Select (x => new ProcedureParameter (x)).ToList ();
            parameterList.Insert (0, new ProcedureParameter (classType, "this"));
            parameters = parameterList.ToArray ();
            methodArguments = new object[parameters.Length - 1];
            ReturnIsNullable = returnIsNullable;
        }

        /// <summary>
        /// Invokes a method on an object. The first parameter must be an the objects GUID, which is
        /// used to fetch the instance, and the remaining parameters are passed to the method.
        /// </summary>
        public object Invoke (params object[] arguments)
        {
            object instance = arguments [0];
            // TODO: should be able to invoke default arguments using Type.Missing, but get "System.ArgumentException : failed to convert parameters"
            for (int i = 1; i < arguments.Length; i++)
                methodArguments [i - 1] = (arguments [i] == Type.Missing) ? parameters [i].DefaultValue : arguments [i];
            return method.Invoke (instance, methodArguments);
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

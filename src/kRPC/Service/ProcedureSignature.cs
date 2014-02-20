using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Google.ProtocolBuffers;
using KRPC.Schema.KRPC;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// Signature information for a procedure, including procedure name,
    /// parameter types and return types.
    /// </summary>
    class ProcedureSignature
    {
        /// <summary>
        /// Name of the procedure, not including the service it is in.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Name of the procedure including the service it is in.
        /// I.e. ServiceName.ProcedureName
        /// </summary>
        public string FullyQualifiedName { get; private set; }

        /// <summary>
        /// The method that implements the procedure.
        /// </summary>
        public MethodInfo Handler { get; private set; }

        public IList<Type> ParameterTypes { get; private set; }
        public bool HasReturnType { get; private set; }
        public Type ReturnType { get; private set; }

        /// <summary>
        /// Protocol buffer builder objects, used to deserialize parameter values.
        /// </summary>
        public IList<IBuilder> ParameterBuilders { get; private set; }

        /// <summary>
        /// Protocol buffer builder objects, used to serialize the return value.
        /// </summary>
        public IBuilder ReturnBuilder { get; private set; }

        public ProcedureSignature (string serviceName, MethodInfo method) {
            Name = method.Name;
            FullyQualifiedName = serviceName + "." + Name;
            Handler = method;
            ParameterTypes = method.GetParameters ()
                .Select (x => x.ParameterType).ToArray ();
            if (ParameterTypes.Any (x => !ProtocolBuffers.IsAMessageType(x))) {
                Type type = ParameterTypes.Where (x => !ProtocolBuffers.IsAMessageType(x)).First ();
                throw new ServiceException (
                    type.ToString() + " is not a valid Procedure parameter type, " +
                    "in " + FullyQualifiedName);
            }
            ParameterBuilders = ParameterTypes
                .Select (x => {
                    try {
                        return ProtocolBuffers.BuilderForMessageType(x);
                    } catch (ArgumentException) {
                        throw new ServiceException ("Failed to instantiate a message builder for type " + x.Name);
                    }
                }).ToArray ();
            HasReturnType = (method.ReturnType != typeof(void));
            if (HasReturnType) {
                ReturnType = method.ReturnType;
                if (!ProtocolBuffers.IsAMessageType(ReturnType)) {
                    throw new ServiceException (
                        ReturnType.ToString() + " is not a valid Procedure return type, " +
                        "in " + FullyQualifiedName);
                }
                ReturnBuilder = ProtocolBuffers.BuilderForMessageType (ReturnType);
            }
        }
    }
}

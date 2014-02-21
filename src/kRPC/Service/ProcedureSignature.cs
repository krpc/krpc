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
            // Check the parameter types are valid
            if (ParameterTypes.Any (x => !ProtocolBuffers.IsAValidType(x))) {
                Type type = ParameterTypes.Where (x => !ProtocolBuffers.IsAValidType(x)).First ();
                throw new ServiceException (
                    type.ToString() + " is not a valid Procedure parameter type, " +
                    "in " + FullyQualifiedName);
            }
            // Create builders for the parameter types that are message types
            ParameterBuilders = ParameterTypes
                .Select (x => {
                    try {
                        if (ProtocolBuffers.IsAMessageType (x))
                            return ProtocolBuffers.BuilderForMessageType (x);
                        else
                            return null;
                    } catch (ArgumentException) {
                        throw new ServiceException ("Failed to instantiate a message builder for parameter type " + x.Name);
                    }
                }).ToArray ();
            HasReturnType = (method.ReturnType != typeof(void));
            if (HasReturnType) {
                ReturnType = method.ReturnType;
                // Check it's a valid return type
                if (!ProtocolBuffers.IsAValidType(ReturnType)) {
                    throw new ServiceException (
                        ReturnType.ToString() + " is not a valid Procedure return type, " +
                        "in " + FullyQualifiedName);
                }
                // Create a builder if it's a message type
                if (ProtocolBuffers.IsAMessageType (ReturnType)) {
                    try {
                        ReturnBuilder = ProtocolBuffers.BuilderForMessageType (ReturnType);
                    } catch (ArgumentException) {
                        throw new ServiceException ("Failed to instantiate a message builder for return type " + ReturnType.Name);
                    }
                }
            }
        }
    }
}

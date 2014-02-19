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
    class ProcedureSignature
    {
        public string Name { get; private set; }
        public string FullyQualifiedName { get; private set; }

        public MethodInfo Handler { get; private set; }
        public ICollection<Type> ParameterTypes { get; private set; }
        public bool HasReturnType { get; private set; }
        public Type ReturnType { get; private set; }

        public ICollection<IBuilder> ParameterBuilders { get; private set; }
        public IBuilder ReturnBuilder { get; private set; }

        public ProcedureSignature (string serviceName, MethodInfo method) {
            Name = method.Name;
            FullyQualifiedName = serviceName + "." + Name;
            Handler = method;
            ParameterTypes = method.GetParameters ()
                .Select (x => x.ParameterType).ToArray ();
            if (ParameterTypes.Any (x => !Reflection.IsAMessageType(x))) {
                Type type = ParameterTypes.Where (x => !Reflection.IsAMessageType(x)).First ();
                throw new ServiceException (
                    type.ToString() + " is not a valid Procedure parameter type, " +
                    "in " + FullyQualifiedName);
            }
            ParameterBuilders = ParameterTypes
                .Select (x => {
                    try {
                        return Reflection.GetBuilderForType(x);
                    } catch (ArgumentException) {
                        throw new ServiceException ("Failed to instantiate a message builder for type " + x.Name);
                    }
                }).ToArray ();
            HasReturnType = (method.ReturnType != typeof(void));
            if (HasReturnType) {
                ReturnType = method.ReturnType;
                if (!Reflection.IsAMessageType(ReturnType)) {
                    throw new ServiceException (
                        ReturnType.ToString() + " is not a valid Procedure return type, " +
                        "in " + FullyQualifiedName);
                }
                ReturnBuilder = Reflection.GetBuilderForType (ReturnType);
            }
        }
    }
}

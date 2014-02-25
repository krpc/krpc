using System;
using System.Collections.Generic;
using System.Reflection;
using Google.ProtocolBuffers;
using KRPC.Schema.KRPC;
using KRPC.Service.Scanner;
using KRPC.Utils;

namespace KRPC.Service
{
    class Services
    {
        public IDictionary<string, ServiceSignature> Signatures { get; private set; }

        static Services instance;

        public static Services Instance {
            get {
                if (instance == null)
                    instance = new Services ();
                return instance;
            }
        }

        /// <summary>
        /// Create a Services instance. Scans the loaded assemblies for services, procedures etc.
        /// </summary>
        Services ()
        {
            Signatures = Scanner.Scanner.GetServices ();
        }

        /// <summary>
        /// Executes the given request and returns a response builder with the relevant
        /// fields populated. Throws RPCException if processing the request fails.
        /// </summary>
        public Response.Builder HandleRequest (Request request)
        {
            // Get the service definition
            if (!Signatures.ContainsKey (request.Service))
                throw new RPCException ("Service " + request.Service + " not found");
            var service = Signatures [request.Service];

            // Get the procedure definition
            if (!service.Procedures.ContainsKey (request.Procedure))
                throw new RPCException ("Procedure " + request.Procedure + " not found, " +
                "in Service " + request.Service);
            var procedure = service.Procedures [request.Procedure];

            // Invoke the procedure
            object[] parameters = DecodeParameters (procedure, request.ParametersList);
            object returnValue;
            try {
                returnValue = procedure.Handler.Invoke (parameters);
            } catch (TargetInvocationException e) {
                throw new RPCException ("Procedure '" + procedure.FullyQualifiedName + "' threw an exception. " +
                e.InnerException.GetType () + ": " + e.InnerException.Message);
            }
            var responseBuilder = Response.CreateBuilder ();
            if (procedure.HasReturnType) {
                responseBuilder.ReturnValue = EncodeReturnValue (procedure, returnValue);
            }
            return responseBuilder;
        }

        /// <summary>
        /// Decode the parameters for a procedure from a serialized request
        /// </summary>
        object[] DecodeParameters (ProcedureSignature procedure, IList<ByteString> parameters)
        {
            // Check number of parameters is correct
            if (parameters.Count != procedure.ParameterTypes.Count) {
                throw new RPCException (
                    "Incorrect number of parameters for " + procedure.FullyQualifiedName + ". " +
                    "Expected " + procedure.ParameterTypes.Count + "; got " + parameters.Count + ".");
            }

            // Attempt to decode them
            var decodedParameters = new object[parameters.Count];
            for (int i = 0; i < parameters.Count; i++) {
                try {
                    if (TypeUtils.IsAClassType (procedure.ParameterTypes [i])) {
                        decodedParameters [i] = ObjectStore.GetInstance ((ulong)ProtocolBuffers.ReadValue (parameters [i], typeof(ulong)));
                    } else if (ProtocolBuffers.IsAMessageType (procedure.ParameterTypes [i])) {
                        var builder = procedure.ParameterBuilders [i];
                        decodedParameters [i] = builder.WeakMergeFrom (parameters [i]).WeakBuild ();
                    } else {
                        decodedParameters [i] = ProtocolBuffers.ReadValue (parameters [i], procedure.ParameterTypes [i]);
                    }
                } catch (Exception e) {
                    throw new RPCException (
                        "Failed to decode parameter " + i + " for " + procedure.FullyQualifiedName + ". " +
                        "Expected a parameter of type " + ProtocolBuffers.GetTypeName (procedure.ParameterTypes [i]) + ". " +
                        e.GetType ().Name + ": " + e.Message);
                }
            }
            return decodedParameters;
        }

        /// <summary>
        /// Encodes the value returned by a procedure handler into a ByteString
        /// </summary>
        ByteString EncodeReturnValue (ProcedureSignature procedure, object returnValue)
        {
            // Check the return value is missing
            if (returnValue == null) {
                throw new RPCException (
                    procedure.FullyQualifiedName + " returned null. " +
                    "Expected an object of type " + procedure.ReturnType);
            }

            // Check if the return value is of a valid type
            if (!TypeUtils.IsAValidType (procedure.ReturnType)) {
                throw new RPCException (
                    procedure.FullyQualifiedName + " returned an object of an invalid type. " +
                    "Expected " + procedure.ReturnType + "; got " + returnValue.GetType ());
            }

            // Encode it as a ByteString
            if (TypeUtils.IsAClassType (procedure.ReturnType))
                return ProtocolBuffers.WriteValue (ObjectStore.AddInstance (returnValue), typeof(ulong));
            else if (ProtocolBuffers.IsAMessageType (procedure.ReturnType))
                return ProtocolBuffers.WriteMessage (returnValue as IMessage);
            else
                return ProtocolBuffers.WriteValue (returnValue, procedure.ReturnType);
        }
    }
}


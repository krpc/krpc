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
    class Services
    {
        public IDictionary<string, ServiceSignature> Signatures { get; private set; }

        private static Services instance;
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
        private Services ()
        {
            var serviceTypes = Reflection.GetTypesWith<KRPCService> ();
            try {
                Signatures = serviceTypes
                    .Select (x => new ServiceSignature (x))
                    .ToDictionary (x => x.Name);
            } catch (ArgumentException) {
                // Handle service name clashes
                // TODO: move into Utils
                var duplicates = serviceTypes
                        .Select (x => x.Name)
                        .GroupBy (x => x)
                        .Where (group => group.Count() > 1)
                        .Select (group => group.Key)
                        .ToArray ();
                throw new ServiceException (
                    "Multiple Services have the same name. " +
                    "Duplicates are " + String.Join(", ", duplicates));
            }
            // Check tha the main KRPC service was found
            if (!Signatures.ContainsKey("KRPC"))
                throw new ServiceException ("KRPC service could not be found");
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
            // TODO: catch exceptions from the following call
            object returnValue = procedure.Handler.Invoke (null, parameters);
            var responseBuilder = Response.CreateBuilder ();
            if (procedure.HasReturnType) {
                responseBuilder.ReturnValue = EncodeReturnValue (procedure, returnValue);
            }
            return responseBuilder;
        }

        /// <summary>
        /// Decode the parameters for a procedure from a serialized request
        /// </summary>
        private object[] DecodeParameters(ProcedureSignature procedure, IList<ByteString> parameters)
        {
            // Check number of parameters is correct
            if (parameters.Count != procedure.ParameterTypes.Count) {
                throw new RPCException (
                    "Incorrect number of parameters for " + procedure.FullyQualifiedName + ". " +
                    "Expected " + procedure.ParameterTypes.Count + "; got " + parameters.Count + ".");
            }

            // Attempt to decode them
            object[] decodedParameters = new object[parameters.Count];
            for (int i = 0; i < parameters.Count; i++) {
                var builder = procedure.ParameterBuilders [i];
                try {
                    decodedParameters[i] = builder.WeakMergeFrom (parameters [i]).WeakBuild ();
                } catch (Exception e) {
                    throw new RPCException (
                        "Failed to decode parameter " + i + " for " + procedure.FullyQualifiedName + ". " +
                        "Expected a parameter of type " + ProtocolBuffers.GetMessageTypeName (procedure.ParameterTypes [i]) + ". " +
                        e.GetType ().Name + ": " + e.Message);
                }
            }
            return decodedParameters;
        }

        /// <summary>
        /// Encodes the value returned by a procedure handler into a ByteString
        /// </summary>
        private ByteString EncodeReturnValue (ProcedureSignature procedure, object returnValue)
        {
            // Check the return value is missing
            if (returnValue == null) {
                throw new RPCException (
                    procedure.FullyQualifiedName + " returned null. " +
                    "Expected an object of type " + procedure.ReturnType);
            }

            // Check if the return value is of a valid type
            IMessage message = returnValue as IMessage;
            if (message == null || !procedure.ReturnType.IsAssignableFrom(message.GetType())) {
                throw new RPCException (
                    procedure.FullyQualifiedName + " returned an object of an invalid type. " +
                    "Expected " + procedure.ReturnType + "; got " + message.GetType());
            }

            // Encode it as a ByteString
            byte[] returnBytes;
            using (MemoryStream stream = new MemoryStream ()) {
                message.WriteTo (stream);
                returnBytes = stream.ToArray ();
            }
            return ByteString.CopyFrom (returnBytes);
        }
    }
}


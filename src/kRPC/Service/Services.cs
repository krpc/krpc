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
        private Dictionary<string, ServiceSignature> services;

        /// <summary>
        /// Create a Services instance. Scans the loaded assemblies for services, procedures etc.
        /// </summary>
        public Services ()
        {
            var serviceTypes = Reflection.GetTypesWith<KRPCService> ();
            try {
                services = serviceTypes
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
            if (!services.ContainsKey("KRPC"))
                throw new ServiceException ("KRPC service could not be found");
        }

        /// <summary>
        /// Executes the given request and returns a response builder with the relevant
        /// fields populated. Throw RPCException if processing the request fails.
        /// </summary>
        public Response.Builder HandleRequest (Request request)
        {
            // Get the service definition
            if (!services.ContainsKey (request.Service))
               throw new RPCException ("Service " + request.Service + " not found");
            var service = services [request.Service];

            // Get the procedure definition
            if (!service.Procedures.ContainsKey (request.Procedure))
               throw new RPCException ("Procedure " + request.Procedure + " not found, " +
                   "in Service " + request.Service);
            var procedure = service.Procedures [request.Procedure];

            // Invoke the procedure
            object[] parameters = DecodeParameters (procedure, request.Request_);
            // TODO: catch exceptions from the following call
            object returnValue = procedure.Handler.Invoke (null, parameters);
            var responseBuilder = Response.CreateBuilder ();
            if (procedure.HasReturnType) {
                responseBuilder.Response_ = EncodeReturnValue (procedure, returnValue);
            }
            return responseBuilder;
        }

        /// <summary>
        /// Decode the parameters for a procedure from a serialized request
        /// </summary>
        private object[] DecodeParameters(ProcedureSignature procedure, ByteString request)
        {
            // TODO: check the request has enough parameters
            object[] parameters = new object[procedure.ParameterTypes.Count];

            // TODO: Allow multiple parameters
            if (procedure.ParameterTypes.Count > 1) {
                throw new NotImplementedException ();
            }

            int i = 0;
            foreach (var builder in procedure.ParameterBuilders) {
                // FIXME: need to decode multiple values - therefore use delimited mergeing
                parameters[i] = builder.WeakMergeFrom (request).WeakBuild ();
                i++;
            }
            return parameters;
        }

        /// <summary>
        /// Encodes the value returned by a procedure handler into a ByteString
        /// </summary>
        private ByteString EncodeReturnValue (ProcedureSignature procedure, object returnValue)
        {
            // TODO: Check the return value is valid properly
            if (returnValue == null || (returnValue as IMessage) == null) {
                throw new RPCException (procedure.FullyQualifiedName + " returned an invalid return value");
            }

            byte[] returnBytes;
            using (MemoryStream stream = new MemoryStream ()) {
                ((IMessage) returnValue).WriteTo (stream);
                returnBytes = stream.ToArray ();
            }
            return ByteString.CopyFrom (returnBytes);
        }
    }
}


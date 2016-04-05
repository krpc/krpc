using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using KRPC.Continuations;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;
using KRPC.Utils;

namespace KRPC.Service
{
    class Services
    {
        internal IDictionary<string, ServiceSignature> Signatures { get; private set; }

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

        public ProcedureSignature GetProcedureSignature (Request request)
        {
            if (!Signatures.ContainsKey (request.Service))
                throw new RPCException ("Service " + request.Service + " not found");
            var service = Signatures [request.Service];
            if (!service.Procedures.ContainsKey (request.Procedure))
                throw new RPCException ("Procedure " + request.Procedure + " not found, " +
                "in Service " + request.Service);
            return service.Procedures [request.Procedure];
        }

        /// <summary>
        /// Executes the given request and returns a response builder with the relevant
        /// fields populated. Throws YieldException, containing a continuation, if the request yields.
        /// Throws RPCException if processing the request fails.
        /// </summary>
        public Response HandleRequest (ProcedureSignature procedure, Request request)
        {
            return HandleRequest (procedure, DecodeArguments (procedure, request.Arguments));
        }

        /// <summary>
        /// Executes a request (from an array of decoded arguments) and returns a response builder with the relevant
        /// fields populated. Throws YieldException, containing a continuation, if the request yields.
        /// Throws RPCException if processing the request fails.
        /// </summary>
        public Response HandleRequest (ProcedureSignature procedure, object[] arguments)
        {
            if ((KRPCServer.Context.GameScene & procedure.GameScene) == 0)
                throw new RPCException (procedure, "Procedure not available in game scene '" + KRPCServer.Context.GameScene + "'");
            object returnValue;
            try {
                returnValue = procedure.Handler.Invoke (arguments);
            } catch (TargetInvocationException e) {
                if (e.InnerException.GetType () == typeof(YieldException))
                    throw e.InnerException;
                throw new RPCException (procedure, e.InnerException);
            }
            var response = new Response ();
            if (procedure.HasReturnType) {
                response.HasReturnValue = true;
                response.ReturnValue = EncodeReturnValue (procedure, returnValue);
            }
            return response;
        }

        /// <summary>
        /// Executes the request, continuing using the given continuation. Returns a response builder with the relevant
        /// fields populated. Throws YieldException, containing a continuation, if the request yields.
        /// Throws RPCException if processing the request fails.
        /// </summary>
        public Response HandleRequest (ProcedureSignature procedure, IContinuation continuation)
        {
            object returnValue;
            try {
                returnValue = continuation.RunUntyped ();
            } catch (YieldException) {
                throw;
            } catch (Exception e) {
                throw new RPCException (procedure, e);
            }
            var response = new Response ();
            if (procedure.HasReturnType) {
                response.HasReturnValue = true;
                response.ReturnValue = EncodeReturnValue (procedure, returnValue);
            }
            return response;
        }

        /// <summary>
        /// Decode the arguments for a request
        /// </summary>
        public object[] DecodeArguments (ProcedureSignature procedure, Request request)
        {
            return DecodeArguments (procedure, request.Arguments);
        }

        /// <summary>
        /// Decode the arguments for a procedure from a serialized request
        /// </summary>
        object[] DecodeArguments (ProcedureSignature procedure, IList<Argument> arguments)
        {
            // Rearrange argument values
            var argumentValues = new ByteString [procedure.Parameters.Count];
            foreach (var argument in arguments)
                argumentValues [argument.Position] = ByteString.CopyFrom (argument.Value);

            var decodedArgumentValues = new object[procedure.Parameters.Count];
            for (int i = 0; i < procedure.Parameters.Count; i++) {
                var type = procedure.Parameters [i].Type;
                var value = argumentValues [i];
                if (value == null) {
                    // Handle default arguments
                    if (!procedure.Parameters [i].HasDefaultArgument)
                        throw new RPCException (procedure, "Argument not specified for parameter " + procedure.Parameters [i].Name + " in " + procedure.FullyQualifiedName + ". ");
                    decodedArgumentValues [i] = Type.Missing;
                } else {
                    // Decode argument
                    try {
                        decodedArgumentValues [i] = ProtoBuf.Encoder.Decode (value.ToByteArray (), type);
                    } catch (Exception e) {
                        throw new RPCException (
                            procedure,
                            "Failed to decode argument for parameter " + procedure.Parameters [i].Name + " in " + procedure.FullyQualifiedName + ". " +
                            "Expected an argument of type " + TypeUtils.GetTypeName (type) + ". " +
                            e.GetType ().Name + ": " + e.Message);
                    }
                }
            }
            return decodedArgumentValues;
        }

        /// <summary>
        /// Encodes the value returned by a procedure handler into a ByteString
        /// </summary>
        byte[] EncodeReturnValue (ProcedureSignature procedure, object returnValue)
        {
            // Check the return value is missing
            if (returnValue == null && !TypeUtils.IsAClassType (procedure.ReturnType)) {
                throw new RPCException (
                    procedure,
                    procedure.FullyQualifiedName + " returned null. " +
                    "Expected an object of type " + procedure.ReturnType);
            }

            // Check if the return value is of a valid type
            if (!TypeUtils.IsAValidType (procedure.ReturnType)) {
                throw new RPCException (
                    procedure,
                    procedure.FullyQualifiedName + " returned an object of an invalid type. " +
                    "Expected " + procedure.ReturnType + "; got " + returnValue.GetType ());
            }

            return ProtoBuf.Encoder.Encode (returnValue, procedure.ReturnType);
        }
    }
}


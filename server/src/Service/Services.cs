using System;
using System.Collections.Generic;
using System.Reflection;
using KRPC.Continuations;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

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

        public ProcedureSignature GetProcedureSignature (string service, string procedure)
        {
            if (!Signatures.ContainsKey (service))
                throw new RPCException ("Service " + service + " not found");
            var serviceSignature = Signatures [service];
            if (!serviceSignature.Procedures.ContainsKey (procedure))
                throw new RPCException ("Procedure " + procedure + " not found, in Service " + service);
            return serviceSignature.Procedures [procedure];
        }

        /// <summary>
        /// Executes the given request and returns a response builder with the relevant
        /// fields populated. Throws YieldException, containing a continuation, if the request yields.
        /// Throws RPCException if processing the request fails.
        /// </summary>
        public Response HandleRequest (ProcedureSignature procedure, Request request)
        {
            return HandleRequest (procedure, GetArguments (procedure, request.Arguments));
        }

        /// <summary>
        /// Executes a request (from an array of decoded arguments) and returns a response builder with the relevant
        /// fields populated. Throws YieldException, containing a continuation, if the request yields.
        /// Throws RPCException if processing the request fails.
        /// </summary>
        public Response HandleRequest (ProcedureSignature procedure, object[] arguments)
        {
            if ((KRPCCore.Context.GameScene & procedure.GameScene) == 0)
                throw new RPCException (procedure, "Procedure not available in game scene '" + KRPCCore.Context.GameScene + "'");
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
                CheckReturnValue (procedure, returnValue);
                response.ReturnValue = returnValue;
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
                CheckReturnValue (procedure, returnValue);
                response.ReturnValue = returnValue;
            }
            return response;
        }

        /// <summary>
        /// Get the arguments for a procedure from a list of argument messages.
        /// </summary>
        public object[] GetArguments (ProcedureSignature procedure, IList<Argument> arguments)
        {
            // TODO: this could probably be optimized

            // Re-order arguments
            var argumentValues = new object [procedure.Parameters.Count];
            var argumentSet = new bool [procedure.Parameters.Count];
            foreach (var argument in arguments) {
                argumentValues [argument.Position] = argument.Value;
                argumentSet [argument.Position] = true;
            }

            // Build arguments array, including default argument values and check the types of the argument values
            var completeArgumentValues = new object [procedure.Parameters.Count];
            for (int i = 0; i < procedure.Parameters.Count; i++) {
                var value = argumentValues [i];
                var type = procedure.Parameters [i].Type;
                if (!argumentSet [i]) {
                    if (!procedure.Parameters [i].HasDefaultValue)
                        throw new RPCException (procedure, "Argument not specified for parameter " + procedure.Parameters [i].Name + " in " + procedure.FullyQualifiedName + ". ");
                    value = Type.Missing;
                } else if (value != null && !type.IsAssignableFrom (value.GetType ())) {
                    throw new RPCException (
                        procedure,
                        "Incorrect argument type for parameter " + procedure.Parameters [i].Name + " in " + procedure.FullyQualifiedName + ". " +
                        "Expected an argument of type " + type + ", got " + value.GetType ());
                } else if (value == null && !TypeUtils.IsAClassType (type)) {
                    throw new RPCException (
                        procedure,
                        "Incorrect argument type for parameter " + procedure.Parameters [i].Name + " in " + procedure.FullyQualifiedName + ". " +
                        "Expected an argument of type " + type + ", got null");
                }
                completeArgumentValues [i] = value;
            }
            return completeArgumentValues;
        }

        /// <summary>
        /// Check the value returned by a procedure handler.
        /// </summary>
        void CheckReturnValue (ProcedureSignature procedure, object returnValue)
        {
            // Check if the type of the return value is valid
            if (returnValue != null && !procedure.ReturnType.IsAssignableFrom (returnValue.GetType ())) {
                throw new RPCException (
                    procedure,
                    "Incorrect value returned by " + procedure.FullyQualifiedName + ". " +
                    "Expected a value of type " + procedure.ReturnType + ", got " + returnValue.GetType ());
            } else if (returnValue == null && !TypeUtils.IsAClassType (procedure.ReturnType)) {
                throw new RPCException (
                    procedure,
                    "Incorrect value returned by " + procedure.FullyQualifiedName + ". " +
                    "Expected a value of type " + procedure.ReturnType + ", got null");
            }
        }
    }
}


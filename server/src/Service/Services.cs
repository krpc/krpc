using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using KRPC.Continuations;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    sealed class Services
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
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public Response HandleRequest (ProcedureSignature procedure, object[] arguments)
        {
            if ((CallContext.GameScene & procedure.GameScene) == 0)
                throw new RPCException (procedure, "Procedure not available in game scene '" + CallContext.GameScene + "'");
            object returnValue;
            try {
                returnValue = procedure.Handler.Invoke (arguments);
            } catch (TargetInvocationException e) {
                if (e.InnerException is YieldException)
                    throw e.InnerException;
                throw new RPCException (procedure, e.InnerException);
            }
            var response = new Response ();
            if (procedure.HasReturnType) {
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
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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
                CheckReturnValue (procedure, returnValue);
                response.ReturnValue = returnValue;
            }
            return response;
        }

        /// <summary>
        /// Get the arguments for a procedure from a list of argument messages.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public object[] GetArguments (ProcedureSignature procedure, IList<Argument> arguments)
        {
            // Get list of supplied argument values and whether they were set
            var numParameters = procedure.Parameters.Count;
            var argumentValues = new object [numParameters];
            var argumentSet = new BitVector32 (0);
            foreach (var argument in arguments) {
                argumentValues [argument.Position] = argument.Value;
                argumentSet [1 << (int)argument.Position] = true;
            }

            var mask = BitVector32.CreateMask ();
            for (int i = 0; i < numParameters; i++) {
                var value = argumentValues [i];
                var parameter = procedure.Parameters [i];
                var type = parameter.Type;
                if (!argumentSet [mask]) {
                    // If the argument is not set, set it to the default value
                    if (!parameter.HasDefaultValue)
                        throw new RPCException (procedure, "Argument not specified for parameter " + parameter.Name + " in " + procedure.FullyQualifiedName + ". ");
                    argumentValues [i] = parameter.DefaultValue;
                } else if (value != null && !type.IsInstanceOfType (value)) {
                    // Check the type of the non-null argument value
                    throw new RPCException (
                        procedure,
                        "Incorrect argument type for parameter " + parameter.Name + " in " + procedure.FullyQualifiedName + ". " +
                        "Expected an argument of type " + type + ", got " + value.GetType ());
                } else if (value == null && !TypeUtils.IsAClassType (type)) {
                    // Check the type of the null argument value
                    throw new RPCException (
                        procedure,
                        "Incorrect argument type for parameter " + parameter.Name + " in " + procedure.FullyQualifiedName + ". " +
                        "Expected an argument of type " + type + ", got null");
                }
                mask = BitVector32.CreateMask (mask);
            }
            return argumentValues;
        }

        /// <summary>
        /// Check the value returned by a procedure handler.
        /// </summary>
        static void CheckReturnValue (ProcedureSignature procedure, object returnValue)
        {
            // Check if the type of the return value is valid
            if (returnValue != null && !procedure.ReturnType.IsInstanceOfType (returnValue)) {
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

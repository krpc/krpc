using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;
using KRPC.Utils;
using KRPC.Service.Attributes;

namespace KRPC.Service
{
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    sealed class Services
    {
        internal IDictionary<string, ServiceSignature> Signatures { get; private set; }
        internal IDictionary<uint, ServiceSignature> ServicesById { get; private set; }
        internal IDictionary<string, IDictionary<uint, ProcedureSignature>> ProceduresById { get; private set; }
        internal IDictionary<Type, Type> MappedExceptionTypes { get; private set; }

        static Services instance;

        public static void Init()
        {
            if (instance == null)
                instance = new Services ();
        }

        public static Services Instance {
            get {
                Init();
                return instance;
            }
        }

        /// <summary>
        /// Create a Services instance. Scans the loaded assemblies for services, procedures etc.
        /// </summary>
        Services ()
        {
            Signatures = Scanner.Scanner.GetServices ();
            ServicesById = new Dictionary<uint, ServiceSignature> ();
            ProceduresById = new Dictionary<string, IDictionary<uint, ProcedureSignature>> ();
            foreach (var service in Signatures.Values) {
                ServicesById [service.Id] = service;
                var procedures = new Dictionary<uint, ProcedureSignature> ();
                foreach (var procedure in service.Procedures.Values)
                    procedures [procedure.Id] = procedure;
                ProceduresById [service.Name] = procedures;
            }
            MappedExceptionTypes = Scanner.Scanner.GetMappedExceptionTypes ();
        }

        public ServiceSignature GetServiceSignature (ProcedureCall call)
        {
            string service = call.Service;
            if (call.ServiceId > 0)
                service = GetServiceNameById (call.ServiceId);
            if (!Signatures.ContainsKey (service))
                throw new RPCException ("Service \"" + service + "\" not found");
            return Signatures [service];
        }

        public ProcedureSignature GetProcedureSignature (ProcedureCall call)
        {
            var serviceSignature = GetServiceSignature (call);
            var service = serviceSignature.Name;
            string procedure = call.Procedure;
            if (call.ProcedureId > 0)
                procedure = GetProcedureNameById (service, call.ProcedureId);
            if (!serviceSignature.Procedures.ContainsKey (procedure))
                throw new RPCException ("Procedure \"" + procedure + "\" not found, in service \"" + service + "\"");
            return serviceSignature.Procedures [procedure];
        }

        string GetServiceNameById (uint id) {
            if (!ServicesById.ContainsKey (id))
                throw new RPCException ("Service with id " + id + " not found");
            return ServicesById [id].Name;
        }

        string GetProcedureNameById (string service, uint procedureId)
        {
            if (!ProceduresById.ContainsKey (service))
                throw new RPCException ("Service \"" + service + "\" not found");
            if (!ProceduresById [service].ContainsKey (procedureId))
                throw new RPCException ("Procedure with id " + procedureId + " not found");
            return ProceduresById [service] [procedureId].Name;
        }

        public Type GetMappedExceptionType (Type exnType)
        {
            return MappedExceptionTypes.ContainsKey(exnType) ? MappedExceptionTypes [exnType] : exnType;
        }

        /// <summary>
        /// Executes a procedure call and returns the result.
        /// Throws YieldException, containing a continuation, if the call yields.
        /// Throws RPCException if the call fails.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]

        public ProcedureResult ExecuteCall (ProcedureSignature procedure, ProcedureCall call)
        {
            try {
                return ExecuteCall (procedure, GetArguments (procedure, call.Arguments));
            } catch (YieldException) {
                throw;
            } catch (RPCException e) {
                return new ProcedureResult { Error = HandleException (e) };
            } catch (System.Exception e) {
                return new ProcedureResult { Error = HandleException (e) };
            }
        }

        /// <summary>
        /// Executes a procedure call and returns the result.
        /// Throws YieldException, containing a continuation, if the call yields.
        /// Throws RPCException if the call fails.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public ProcedureResult ExecuteCall (ProcedureSignature procedure, object[] arguments)
        {
            try {
                if ((CallContext.GameScene & procedure.GameScene) == 0)
                    throw new RPCException ("Procedure not available in game scene '" + GameSceneUtils.Name(CallContext.GameScene) + "'");
                object returnValue;
                try {
                    returnValue = procedure.Handler.Invoke (arguments);
                } catch (TargetInvocationException e) {
                    if (e.InnerException is YieldException)
                        throw e.InnerException;
                    throw new RPCException (e.InnerException);
                }
                var result = new ProcedureResult ();
                if (procedure.HasReturnType) {
                    CheckReturnValue (procedure, returnValue);
                    result.Value = returnValue;
                }
                return result;
            } catch (YieldException) {
                throw;
            } catch (RPCException e) {
                return new ProcedureResult { Error = HandleException (e) };
            } catch (System.Exception e) {
                return new ProcedureResult { Error = HandleException (e) };
            }
        }

        /// <summary>
        /// Executes a procedure call and returns the result.
        /// Throws YieldException, containing a continuation, if the call yields.
        /// Throws RPCException if the call fails.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public ProcedureResult ExecuteCall (ProcedureSignature procedure, Func<object> continuation)
        {
            object returnValue;
            try {
                returnValue = continuation ();
            } catch (YieldException) {
                throw;
            } catch (System.Exception e) {
                throw new RPCException (e);
            }
            var result = new ProcedureResult ();
            if (procedure.HasReturnType) {
                CheckReturnValue (procedure, returnValue);
                result.Value = returnValue;
            }
            return result;
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
                        throw new RPCException ("Argument not specified for parameter " + parameter.Name + " in " + procedure.FullyQualifiedName);
                    argumentValues [i] = parameter.DefaultValue;
                } else if (value != null && !type.IsInstanceOfType (value)) {
                    // Check the type of the non-null argument value
                    throw new RPCException (
                        "Incorrect argument type for parameter " + parameter.Name + " in " + procedure.FullyQualifiedName + ". " +
                        "Expected an argument of type " + type + ", got " + value.GetType ());
                } else if (value == null && !TypeUtils.IsAClassType (type)) {
                    // Check the type of the null argument value
                    throw new RPCException (
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
                    "Incorrect value returned by " + procedure.FullyQualifiedName + ". " +
                    "Expected a value of type " + procedure.ReturnType + ", got " + returnValue.GetType ());
            }
            if (returnValue == null && !TypeUtils.IsAClassType (procedure.ReturnType)) {
                throw new RPCException (
                    "Incorrect value returned by " + procedure.FullyQualifiedName + ". " +
                    "Expected a value of type " + procedure.ReturnType + ", got null");
            }
            // Check if the return value is null, but the procedure is not marked as nullable
            if (returnValue == null && !procedure.ReturnIsNullable)
                throw new RPCException (
                    "Incorrect value returned by " + procedure.FullyQualifiedName + ". " +
                    "Expected a non-null value of type " + procedure.ReturnType + ", got null, " +
                    "but the procedure is not marked as nullable.");
        }

        /// <summary>
        /// Convert an exception thrown by an RPC into an error message.
        /// </summary>
        internal Error HandleException(System.Exception exn)
        {
            if (exn is RPCException && exn.InnerException != null)
                exn = exn.InnerException;
            var message = exn.Message;
            var verboseErrors = Configuration.Instance.VerboseErrors;
            var stackTrace = verboseErrors ? exn.StackTrace : string.Empty;
            if (Logger.ShouldLog (Logger.Severity.Debug)) {
                Logger.WriteLine (message, Logger.Severity.Debug);
                if (verboseErrors)
                    Logger.WriteLine (stackTrace, Logger.Severity.Debug);
            }
            var mappedType = GetMappedExceptionType(exn.GetType());
            var type = mappedType ?? exn.GetType();
            Error error;
            if (Reflection.HasAttribute<KRPCExceptionAttribute>(type))
                error = new Error(TypeUtils.GetExceptionServiceName(type), type.Name, message, stackTrace);
            else
                error = new Error(message, stackTrace);
            return error;
        }
    }
}

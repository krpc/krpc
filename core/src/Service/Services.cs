using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;
using KRPC.Utils;
using KRPC.Service.Attributes;

namespace KRPC.Service
{
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
            ServiceSignature serviceSignature;
            if (!Signatures.TryGetValue (service, out serviceSignature))
                throw new RPCException ("Service \"" + service + "\" not found");
            return serviceSignature;
        }

        public ProcedureSignature GetProcedureSignature (ProcedureCall call)
        {
            var serviceSignature = GetServiceSignature (call);
            var service = serviceSignature.Name;
            string procedure = call.Procedure;
            if (call.ProcedureId > 0)
                procedure = GetProcedureNameById (service, call.ProcedureId);
            ProcedureSignature procedureSignature;
            if (!serviceSignature.Procedures.TryGetValue (procedure, out procedureSignature))
                throw new RPCException ("Procedure \"" + procedure + "\" not found, in service \"" + service + "\"");
            return procedureSignature;
        }

        string GetServiceNameById (uint id)
        {
            ServiceSignature sig;
            if (!ServicesById.TryGetValue (id, out sig))
                throw new RPCException ("Service with id " + id + " not found");
            return sig.Name;
        }

        string GetProcedureNameById (string service, uint procedureId)
        {
            IDictionary<uint, ProcedureSignature> procedures;
            if (!ProceduresById.TryGetValue (service, out procedures))
                throw new RPCException ("Service \"" + service + "\" not found");
            ProcedureSignature sig;
            if (!procedures.TryGetValue (procedureId, out sig))
                throw new RPCException ("Procedure with id " + procedureId + " not found");
            return sig.Name;
        }

        public Type GetMappedExceptionType (Type exnType)
        {
            Type mappedType;
            return MappedExceptionTypes.TryGetValue (exnType, out mappedType) ? mappedType : exnType;
        }

        /// <summary>
        /// Executes a procedure call and returns the result.
        /// Throws YieldException, containing a continuation, if the call yields.
        /// Throws RPCException if the call fails.
        /// </summary>

        static object instanceBuffer;
        static object[] argumentBuffer;
        const int argumentBufferCacheSize = 16;
        readonly static object[][] argumentBuffers = Enumerable.Range(0, argumentBufferCacheSize).Select(i => new object[i]).ToArray();

        public ProcedureResult ExecuteCall (ProcedureSignature procedure, ProcedureCall call)
        {
            try
            {
                SetArguments(procedure, call.Arguments);
                return ExecuteCall (procedure, instanceBuffer, argumentBuffer);
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
        public ProcedureResult ExecuteCall (ProcedureSignature procedure, object instance, object[] arguments)
        {
            try {
                object returnValue = Invoke (procedure, instance, arguments);
                var result = new ProcedureResult ();
                if (procedure.HasReturnType) {
                    result.Spec = procedure.ReturnSpec;
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
        /// Executes a procedure call and returns its return value.
        /// Throws YieldException, containing a continuation, if the call yields.
        /// Throws the exception the procedure throws if the call fails.
        /// </summary>
        internal object Invoke (ProcedureSignature procedure, object instance, object[] arguments)
        {
            if ((CallContext.GameScene & procedure.GameScene) == 0)
                throw new KRPC.UnsupportedSceneException ("Procedure not available in game scene '" + GameSceneUtils.Name(CallContext.GameScene) + "'");
            object returnValue = procedure.Handler.Invoke (instance, arguments);
            if (procedure.HasReturnType)
                CheckReturnValue (procedure, returnValue);
            return returnValue;
        }

        /// <summary>
        /// Checks that a procedure is available in the current game scene, throwing
        /// RPCException if not. Called from a compiled server side function before each
        /// call to the procedure's method.
        /// </summary>
        public static void CheckExpressionGameScene (ProcedureSignature procedure)
        {
            if (procedure == null)
                throw new ArgumentNullException (nameof (procedure));
            if ((CallContext.GameScene & procedure.GameScene) == 0)
                throw new RPCException ("Procedure not available in game scene '" + GameSceneUtils.Name (CallContext.GameScene) + "'");
        }

        /// <summary>
        /// Checks an argument computed by a compiled server side function, throwing a
        /// NullReferenceException if it is null. Only applied to a parameter that is not
        /// nullable.
        /// </summary>
        public static T CheckExpressionArgument<T> (ProcedureSignature procedure, int position, T value)
            where T : class
        {
            if (ReferenceEquals (value, null))
                throw NullArgumentException (procedure, position);
            return value;
        }

        /// <summary>
        /// Checks a nullable value computed by a compiled server side function for a
        /// parameter that is not nullable, throwing a NullReferenceException if it is null.
        /// </summary>
        public static T CheckExpressionValueArgument<T> (ProcedureSignature procedure, int position, T? value)
            where T : struct
        {
            if (!value.HasValue)
                throw NullArgumentException (procedure, position);
            return value.Value;
        }

        static System.Exception NullArgumentException (ProcedureSignature procedure, int position)
        {
            if (procedure == null)
                throw new ArgumentNullException (nameof (procedure));
            var parameter = procedure.Parameters [position];
            return new KRPC.NullReferenceException (
                "Incorrect argument type for parameter " + parameter.Name + " in " +
                procedure.FullyQualifiedName + ". " +
                "Expected an argument of type " + parameter.Spec.Type + ", got null");
        }

        /// <summary>
        /// Checks a return value produced by a procedure invoked from a compiled
        /// server side function, throwing RPCException if it is null and the
        /// procedure is not permitted to return null.
        /// </summary>
        public static void CheckExpressionReturnValue (ProcedureSignature procedure, object returnValue)
        {
            if (ReferenceEquals (returnValue, null))
                CheckReturnValue (procedure, null);
        }

        /// <summary>
        /// Executes a procedure call and returns the result.
        /// Throws YieldException, containing a continuation, if the call yields.
        /// Throws RPCException if the call fails.
        /// </summary>
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
                result.Spec = procedure.ReturnSpec;
                result.Value = returnValue;
            }
            return result;
        }

        /// <summary>
        /// Like ExecuteCall(procedure, call) but writes the result into an existing
        /// ProcedureResult instead of allocating a new one.
        /// Throws YieldException if the procedure yields (result is unchanged).
        /// </summary>
        internal void ExecuteCallInto (ProcedureSignature procedure, ProcedureCall call, ProcedureResult result)
        {
            SetArguments (procedure, call.Arguments);
            ExecuteCallInto (procedure, instanceBuffer, argumentBuffer, result);
        }

        void ExecuteCallInto (ProcedureSignature procedure, object instance, object[] arguments, ProcedureResult result)
        {
            object returnValue;
            try {
                if ((CallContext.GameScene & procedure.GameScene) == 0)
                    throw new KRPC.UnsupportedSceneException ("Procedure not available in game scene '" + GameSceneUtils.Name (CallContext.GameScene) + "'");
                returnValue = procedure.Handler.Invoke (instance, arguments);
            } catch (YieldException) {
                throw;
            } catch (System.Exception e) {
                // Not logged for an error that holds a stream, which a held stream reads on
                // every update
                result.Reset ();
                result.Error = HandleException (e, !IsHeld (e));
                return;
            }
            result.Reset ();
            if (procedure.HasReturnType) {
                try {
                    CheckReturnValue (procedure, returnValue);
                } catch (RPCException e) {
                    result.Error = HandleException (e);
                    return;
                }
                result.Spec = procedure.ReturnSpec;
                result.Value = returnValue;
            }
        }

        /// <summary>
        /// Like ExecuteCall(procedure, continuation) but writes into an existing ProcedureResult.
        /// Throws YieldException if the continuation yields (result is unchanged).
        /// Throws RPCException on error (result is unchanged; caller must handle).
        /// </summary>
        internal void ExecuteCallInto (ProcedureSignature procedure, Func<object> continuation, ProcedureResult result)
        {
            object returnValue;
            try {
                returnValue = continuation ();
            } catch (YieldException) {
                throw;
            } catch (System.Exception e) {
                throw new RPCException (e);
            }
            result.Reset ();
            if (procedure.HasReturnType) {
                CheckReturnValue (procedure, returnValue);
                result.Spec = procedure.ReturnSpec;
                result.Value = returnValue;
            }
        }

        /// <summary>
        /// Set the arguments for a procedure handler from a list of argument messages.
        /// </summary>
        public void SetArguments(ProcedureSignature procedure, IList<Argument> arguments)
        {
            instanceBuffer = null;
            argumentBuffer = null;

            var numParameters = procedure.Parameters.Count;
            if (numParameters == 0)
                return;

            var hasInstance = procedure.Handler.HasInstance;
            var numArguments = hasInstance ? numParameters - 1 : numParameters;

            if (numArguments > 0)
            {
                if (numArguments < argumentBufferCacheSize)
                    argumentBuffer = argumentBuffers[numArguments];
                else
                    argumentBuffer = new object[numArguments];
            }

            int filledMask = 0;
            foreach (var argument in arguments)
            {
                var position = argument.Position;
                var value = argument.Value;
                var parameter = procedure.Parameters[(int)position];
                var type = parameter.Spec.Type;
                if (value != null && !type.IsInstanceOfType (value))
                    throw new RPCException (
                        "Incorrect argument type for parameter " + parameter.Name + " in " + procedure.FullyQualifiedName + ". " +
                        "Expected an argument of type " + type + ", got " + value.GetType ());
                if (value == null && !parameter.Spec.Nullable)
                    throw new RPCException (
                        "Incorrect argument type for parameter " + parameter.Name + " in " + procedure.FullyQualifiedName + ". " +
                        "Expected an argument of type " + type + ", got null");
                if (hasInstance && position == 0)
                    instanceBuffer = value;
                else
                {
                    int bufferIndex = hasInstance ? (int)position - 1 : (int)position;
                    argumentBuffer[bufferIndex] = value;
                    filledMask |= (1 << bufferIndex);
                }
            }
            // Fill unset slots with default values. Type.Missing does not work on Mono.
            for (int i = 0; i < numArguments; i++)
            {
                if ((filledMask & (1 << i)) == 0)
                {
                    var parameter = procedure.Parameters[hasInstance ? i + 1 : i];
                    if (!parameter.HasDefaultValue)
                        throw new RPCException ("Argument not specified for parameter " + parameter.Name + " in " + procedure.FullyQualifiedName);
                    argumentBuffer[i] = parameter.DefaultValue;
                }
            }
        }

        /// <summary>
        /// Get the arguments for a procedure handler from a list of argument messages.
        /// </summary>
        public object[] GetArguments(ProcedureSignature procedure, IList<Argument> arguments)
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
                var type = parameter.Spec.Type;
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
                } else if (value == null && !parameter.Spec.Nullable) {
                    // Reject a null argument for a parameter that is not nullable
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
            if (returnValue != null && !procedure.ReturnSpec.Type.IsInstanceOfType (returnValue)) {
                throw new RPCException (
                    "Incorrect value returned by " + procedure.FullyQualifiedName + ". " +
                    "Expected a value of type " + procedure.ReturnSpec.Type + ", got " + returnValue.GetType ());
            }
            // Check if the return value is null, but the procedure is not marked as nullable
            if (returnValue == null && !procedure.ReturnSpec.Nullable)
                throw new RPCException (
                    "Incorrect value returned by " + procedure.FullyQualifiedName + ". " +
                    "Expected a non-null value of type " + procedure.ReturnSpec.Type + ", got null, " +
                    "but the procedure is not marked as nullable.");
        }

        /// <summary>
        /// Whether an exception puts a stream on hold: an object is not loaded, or the procedure
        /// is not available in the current game scene.
        /// </summary>
        internal static bool IsHeld (System.Exception exn)
        {
            if (exn is RPCException && exn.InnerException != null)
                exn = exn.InnerException;
            return exn is KRPC.ObjectNotLoadedException || exn is KRPC.UnsupportedSceneException;
        }

        /// <summary>
        /// Whether an error holds a stream, as for <see cref="IsHeld(System.Exception)" />.
        /// </summary>
        internal static bool IsHeld (Error error)
        {
            return error.Service == "KRPC" &&
                (error.Name == "ObjectNotLoadedException" || error.Name == "UnsupportedSceneException");
        }

        /// <summary>
        /// Convert an exception thrown by an RPC into an error message, and log it at debug
        /// level if log is set.
        /// </summary>
        internal Error HandleException(System.Exception exn, bool log = true)
        {
            if (exn is RPCException && exn.InnerException != null)
                exn = exn.InnerException;
            var message = exn.Message;
            var verboseErrors = Configuration.Instance.VerboseErrors;
            // An exception that was never thrown has no stack trace
            var stackTrace = verboseErrors ? exn.StackTrace ?? string.Empty : string.Empty;
            if (log && Logger.ShouldLog (Logger.Severity.Debug)) {
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

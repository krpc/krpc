using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.ProtocolBuffers;
using KRPC.Continuations;
using KRPC.Schema.KRPC;
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
        public Response.Builder HandleRequest (ProcedureSignature procedure, Request request)
        {
            return HandleRequest (procedure, DecodeArguments (procedure, request.ArgumentsList));
        }

        /// <summary>
        /// Executes a request (from an array of decoded arguments) and returns a response builder with the relevant
        /// fields populated. Throws YieldException, containing a continuation, if the request yields.
        /// Throws RPCException if processing the request fails.
        /// </summary>
        public Response.Builder HandleRequest (ProcedureSignature procedure, object[] arguments)
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
            var responseBuilder = Response.CreateBuilder ();
            if (procedure.HasReturnType)
                responseBuilder.ReturnValue = EncodeReturnValue (procedure, returnValue);
            return responseBuilder;
        }

        /// <summary>
        /// Executes the request, continuing using the given continuation. Returns a response builder with the relevant
        /// fields populated. Throws YieldException, containing a continuation, if the request yields.
        /// Throws RPCException if processing the request fails.
        /// </summary>
        public Response.Builder HandleRequest (ProcedureSignature procedure, IContinuation continuation)
        {
            object returnValue;
            try {
                returnValue = continuation.RunUntyped ();
            } catch (YieldException) {
                throw;
            } catch (Exception e) {
                throw new RPCException (procedure, e);
            }
            var responseBuilder = Response.CreateBuilder ();
            if (procedure.HasReturnType)
                responseBuilder.ReturnValue = EncodeReturnValue (procedure, returnValue);
            return responseBuilder;
        }

        /// <summary>
        /// Decode the arguments for a request
        /// </summary>
        public object[] DecodeArguments (ProcedureSignature procedure, Request request)
        {
            return DecodeArguments (procedure, request.ArgumentsList);
        }

        /// <summary>
        /// Decode the arguments for a procedure from a serialized request
        /// </summary>
        object[] DecodeArguments (ProcedureSignature procedure, IList<Schema.KRPC.Argument> arguments)
        {
            // Rearrange argument values
            var argumentValues = new ByteString [procedure.Parameters.Count];
            foreach (var argument in arguments)
                argumentValues [argument.Position] = argument.Value;

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
                        decodedArgumentValues [i] = Decode (procedure, i, type, value);
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
        /// Decode a serialized value
        /// </summary>
        object Decode (ProcedureSignature procedure, int i, Type type, ByteString value)
        {
            if (TypeUtils.IsAClassType (type)) {
                return ObjectStore.Instance.GetInstance ((ulong)ProtocolBuffers.ReadValue (value, typeof(ulong)));
            } else if (TypeUtils.IsACollectionType (type)) {
                return DecodeCollection (procedure, i, type, value);
            } else if (ProtocolBuffers.IsAMessageType (type)) {
                var builder = procedure.ParameterBuilders [i];
                builder.WeakMergeFrom (value);
                var built = builder.WeakBuild ();
                builder.WeakClear ();
                return built;
            } else if (ProtocolBuffers.IsAnEnumType (type) || TypeUtils.IsAnEnumType (type)) {
                // TODO: Assumes it's underlying type is int
                var enumValue = ProtocolBuffers.ReadValue (value, typeof(int));
                if (!Enum.IsDefined (type, enumValue))
                    throw new RPCException (procedure, "Failed to convert value " + enumValue + " to enumeration type " + type);
                return Enum.ToObject (type, enumValue);
            } else {
                return ProtocolBuffers.ReadValue (value, type);
            }
        }

        /// <summary>
        /// Decode a serialized collection
        /// </summary>
        object DecodeCollection (ProcedureSignature procedure, int i, Type type, ByteString value)
        {
            if (TypeUtils.IsAListCollectionType (type)) {
                var builder = Schema.KRPC.List.CreateBuilder ();
                var encodedList = builder.MergeFrom (value).Build ();
                var list = (System.Collections.IList)(typeof(System.Collections.Generic.List<>)
                    .MakeGenericType (type.GetGenericArguments ().Single ())
                    .GetConstructor (Type.EmptyTypes)
                    .Invoke (null));
                foreach (var item in encodedList.ItemsList)
                    list.Add (Decode (procedure, i, type.GetGenericArguments ().Single (), item));
                return list;
            } else if (TypeUtils.IsADictionaryCollectionType (type)) {
                var builder = Schema.KRPC.Dictionary.CreateBuilder ();
                var encodedDictionary = builder.MergeFrom (value).Build ();
                var dictionary = (System.Collections.IDictionary)(typeof(System.Collections.Generic.Dictionary<,>)
                    .MakeGenericType (type.GetGenericArguments () [0], type.GetGenericArguments () [1])
                    .GetConstructor (Type.EmptyTypes)
                    .Invoke (null));
                foreach (var entry in encodedDictionary.EntriesList) {
                    var k = Decode (procedure, i, type.GetGenericArguments () [0], entry.Key);
                    var v = Decode (procedure, i, type.GetGenericArguments () [1], entry.Value);
                    dictionary [k] = v;
                }
                return dictionary;
            } else if (TypeUtils.IsASetCollectionType (type)) {
                var builder = Schema.KRPC.Set.CreateBuilder ();
                var encodedSet = builder.MergeFrom (value).Build ();

                var set = (System.Collections.IEnumerable)(typeof(System.Collections.Generic.HashSet<>)
                    .MakeGenericType (type.GetGenericArguments ().Single ())
                    .GetConstructor (Type.EmptyTypes)
                    .Invoke (null));

                MethodInfo methodInfo = type.GetMethod ("Add");

                foreach (var item in encodedSet.ItemsList) {
                    var decodedItem = Decode (procedure, i, type.GetGenericArguments ().Single (), item);
                    methodInfo.Invoke (set, new [] { decodedItem });
                }
                return set;
            } else { // a tuple
                // TODO: this is ugly

                var builder = Schema.KRPC.Tuple.CreateBuilder ();
                var encodedTuple = builder.MergeFrom (value).Build ();
                var valueTypes = type.GetGenericArguments ().ToArray ();
                var genericType = Type.GetType ("KRPC.Utils.Tuple`" + valueTypes.Length);

                Object[] values = new Object[valueTypes.Length];
                for (int j = 0; j < valueTypes.Length; j++) {
                    var item = encodedTuple.ItemsList [j];
                    values [j] = Decode (procedure, i, valueTypes [j], item);
                }

                var tuple = genericType
                    .MakeGenericType (valueTypes)
                    .GetConstructor (valueTypes)
                    .Invoke (values);
                return tuple;
            }
        }

        /// <summary>
        /// Encodes the value returned by a procedure handler into a ByteString
        /// </summary>
        ByteString EncodeReturnValue (ProcedureSignature procedure, object returnValue)
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

            // Encode it as a ByteString
            return Encode (procedure.ReturnType, returnValue);
        }

        /// <summary>
        /// Encode a value
        /// </summary>
        ByteString Encode (Type type, object value)
        {
            if (TypeUtils.IsAClassType (type))
                return ProtocolBuffers.WriteValue (ObjectStore.Instance.AddInstance (value), typeof(ulong));
            else if (TypeUtils.IsACollectionType (type))
                return EncodeCollection (type, value);
            else if (ProtocolBuffers.IsAMessageType (type))
                return ProtocolBuffers.WriteMessage (value as IMessage);
            else if (ProtocolBuffers.IsAnEnumType (type) || TypeUtils.IsAnEnumType (type)) {
                // TODO: Assumes it's underlying type is int
                return ProtocolBuffers.WriteValue ((int)value, typeof(int));
            } else
                return ProtocolBuffers.WriteValue (value, type);
        }

        /// <summary>
        /// Encode a collection
        /// </summary>
        ByteString EncodeCollection (Type type, object value)
        {
            if (TypeUtils.IsAListCollectionType (type)) {
                var builder = Schema.KRPC.List.CreateBuilder ();
                var list = (System.Collections.IList)value;
                var valueType = type.GetGenericArguments ().Single ();
                foreach (var item in list)
                    builder.AddItems (Encode (valueType, item));
                return ProtocolBuffers.WriteMessage (builder.Build ());
            } else if (TypeUtils.IsADictionaryCollectionType (type)) {
                var keyType = type.GetGenericArguments () [0];
                var valueType = type.GetGenericArguments () [1];
                var builder = Schema.KRPC.Dictionary.CreateBuilder ();
                var entryBuilder = Schema.KRPC.DictionaryEntry.CreateBuilder ();
                foreach (System.Collections.DictionaryEntry entry in (System.Collections.IDictionary) value) {
                    entryBuilder.SetKey (Encode (keyType, entry.Key));
                    entryBuilder.SetValue (Encode (valueType, entry.Value));
                    builder.AddEntries (entryBuilder.Build ());
                }
                return ProtocolBuffers.WriteMessage (builder.Build ());
            } else if (TypeUtils.IsASetCollectionType (type)) {
                var builder = Schema.KRPC.Set.CreateBuilder ();
                var set = (System.Collections.IEnumerable)value;
                var valueType = type.GetGenericArguments ().Single ();
                foreach (var item in set)
                    builder.AddItems (Encode (valueType, item));
                return ProtocolBuffers.WriteMessage (builder.Build ());
            } else { // a tuple
                // TODO: this is ugly
                var builder = Schema.KRPC.Set.CreateBuilder ();
                var valueTypes = type.GetGenericArguments ().ToArray ();
                var genericType = Type.GetType ("KRPC.Utils.Tuple`" + valueTypes.Length);
                var tupleType = genericType.MakeGenericType (valueTypes);
                for (int i = 0; i < valueTypes.Length; i++) {
                    var property = tupleType.GetProperty ("Item" + (i + 1));
                    var item = property.GetGetMethod ().Invoke (value, null);
                    builder.AddItems (Encode (valueTypes [i], item));
                }
                return ProtocolBuffers.WriteMessage (builder.Build ());
            }
        }
    }
}


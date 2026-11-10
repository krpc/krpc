using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Server.ProtocolBuffers
{
    static class MessageExtensions
    {
        public static Google.Protobuf.IMessage ToProtobufMessage (this IMessage message)
        {
            var type = message.GetType ();
            if (type == typeof(Service.Messages.Services))
                return ((Service.Messages.Services)message).ToProtobufMessage ();
            throw new ArgumentException ("Cannot convert a " + type + " to a protobuf message");
        }

        public static Schema.KRPC.Response ToProtobufMessage (this Response response)
        {
            var result = new Schema.KRPC.Response ();
            result.Id = response.Id;
            if (response.HasError)
                result.Error = response.Error.ToProtobufMessage ();
            result.Results.Add (response.Results.Select (ToProtobufMessage));
            return result;
        }

        public static Schema.KRPC.ProcedureResult ToProtobufMessage (this ProcedureResult procedureResult)
        {
            var result = new Schema.KRPC.ProcedureResult ();
            if (procedureResult.HasValue) {
                if (procedureResult.Value == null)
                    result.IsNull = true;
                else
                    result.Value = procedureResult.EncodedValue ??
                        Encoder.Encode (procedureResult.Value, procedureResult.Spec);
            }
            if (procedureResult.HasError)
                result.Error = procedureResult.Error.ToProtobufMessage ();
            return result;
        }

        public static Schema.KRPC.Error ToProtobufMessage (this Error error)
        {
            return new Schema.KRPC.Error {
                Service = error.Service,
                Exception = error.Exception,
                ServiceId = error.ServiceId,
                ExceptionId = error.ExceptionId,
                Description = error.Description,
                StackTrace = error.StackTrace
            };
        }

        public static Schema.KRPC.StreamUpdate ToProtobufMessage (this StreamUpdate streamUpdate)
        {
            var result = new Schema.KRPC.StreamUpdate ();
            foreach (var r in streamUpdate.Results)
                result.Results.Add (r.ToProtobufMessage ());
            return result;
        }

        public static Schema.KRPC.StreamResult ToProtobufMessage (this StreamResult streamResult)
        {
            var result = new Schema.KRPC.StreamResult ();
            result.Id = streamResult.Id;
            result.Result = streamResult.Result.ToProtobufMessage ();
            return result;
        }

        public static Schema.KRPC.Services ToProtobufMessage (this Service.Messages.Services services)
        {
            var result = new Schema.KRPC.Services ();
            result.Services_.Add (services.ServicesList.Select (ToProtobufMessage));
            return result;
        }

        public static Schema.KRPC.Service ToProtobufMessage (this Service.Messages.Service service)
        {
            var result = new Schema.KRPC.Service ();
            result.Name = service.Name;
            result.Id = service.Id;
            result.Procedures.Add (service.Procedures.Select (ToProtobufMessage));
            result.Classes.Add (service.Classes.Select (ToProtobufMessage));
            result.Enumerations.Add (service.Enumerations.Select (ToProtobufMessage));
            result.Structs.Add (service.Structs.Select (ToProtobufMessage));
            result.Exceptions.Add (service.Exceptions.Select (ToProtobufMessage));
            result.Documentation = service.Documentation;
            result.Deprecated = service.Deprecated;
            result.DeprecatedReason = service.DeprecatedReason;
            return result;
        }

        public static Schema.KRPC.Procedure ToProtobufMessage (this Procedure procedure)
        {
            var result = new Schema.KRPC.Procedure ();
            result.Name = procedure.Name;
            result.Id = procedure.Id;
            result.Parameters.Add (procedure.Parameters.Select (ToProtobufMessage));
            if (procedure.ReturnType != null)
                result.ReturnType = procedure.ReturnType.ToProtobufMessage ();
            result.GameScenes.Add (ToProtobufMessage (procedure.GameScene));
            result.Documentation = procedure.Documentation;
            result.Deprecated = procedure.Deprecated;
            result.DeprecatedReason = procedure.DeprecatedReason;
            result.Internal = procedure.Internal;
            return result;
        }

        /// <summary>
        /// Convert a game scene mask into wire enumeration values. Scenes are matched by the
        /// same names used by the JSON service definitions, so the wire enumeration only needs
        /// its values to be named consistently to stay in step; any scene it does not name is
        /// omitted. A procedure available in every scene encodes as an empty list, which the
        /// protocol defines to mean all scenes.
        /// </summary>
        static IList<Schema.KRPC.Procedure.Types.GameScene> ToProtobufMessage (GameScene scenes)
        {
            var result = new List<Schema.KRPC.Procedure.Types.GameScene> ();
            if (scenes == GameScene.All)
                return result;
            var descriptor = Schema.KRPC.Procedure.Descriptor.EnumTypes.Single ();
            foreach (var name in GameSceneUtils.Serialize (scenes)) {
                var value = descriptor.FindValueByName (name);
                if (value != null)
                    result.Add ((Schema.KRPC.Procedure.Types.GameScene)value.Number);
            }
            return result;
        }

        public static Schema.KRPC.Parameter ToProtobufMessage (this Parameter parameter)
        {
            var result = new Schema.KRPC.Parameter ();
            result.Name = parameter.Name;
            result.Type = parameter.Type.ToProtobufMessage ();
            result.HasDefaultValue = parameter.HasDefaultValue;
            if (parameter.HasDefaultValue) {
                if (parameter.DefaultValue == null)
                    result.DefaultValueIsNull = true;
                else
                    result.DefaultValue = Encoder.Encode (parameter.DefaultValue, parameter.Type);
            }
            return result;
        }

        public static Schema.KRPC.Class ToProtobufMessage (this Class cls)
        {
            var result = new Schema.KRPC.Class ();
            result.Name = cls.Name;
            result.Id = cls.Id;
            result.Documentation = cls.Documentation;
            result.Deprecated = cls.Deprecated;
            result.DeprecatedReason = cls.DeprecatedReason;
            return result;
        }

        public static Schema.KRPC.Enumeration ToProtobufMessage (this Enumeration enumeration)
        {
            var result = new Schema.KRPC.Enumeration ();
            result.Name = enumeration.Name;
            result.Id = enumeration.Id;
            result.Values.Add (enumeration.Values.Select (ToProtobufMessage));
            result.Documentation = enumeration.Documentation;
            result.Deprecated = enumeration.Deprecated;
            result.DeprecatedReason = enumeration.DeprecatedReason;
            result.Flags = enumeration.Flags;
            return result;
        }

        public static Schema.KRPC.EnumerationValue ToProtobufMessage (this EnumerationValue enumerationValue)
        {
            var result = new Schema.KRPC.EnumerationValue ();
            result.Name = enumerationValue.Name;
            result.Value = enumerationValue.Value;
            result.Documentation = enumerationValue.Documentation;
            result.Deprecated = enumerationValue.Deprecated;
            result.DeprecatedReason = enumerationValue.DeprecatedReason;
            return result;
        }

        public static Schema.KRPC.Struct ToProtobufMessage (this Service.Messages.Struct str)
        {
            var result = new Schema.KRPC.Struct ();
            result.Name = str.Name;
            result.Id = str.Id;
            result.Fields.Add (str.Fields.Select (ToProtobufMessage));
            result.Documentation = str.Documentation;
            result.Deprecated = str.Deprecated;
            result.DeprecatedReason = str.DeprecatedReason;
            return result;
        }

        public static Schema.KRPC.StructField ToProtobufMessage (this StructField field)
        {
            var result = new Schema.KRPC.StructField ();
            result.Name = field.Name;
            result.Type = field.Type.ToProtobufMessage ();
            result.Documentation = field.Documentation;
            result.Deprecated = field.Deprecated;
            result.DeprecatedReason = field.DeprecatedReason;
            return result;
        }

        public static Schema.KRPC.Exception ToProtobufMessage (this Service.Messages.Exception exception)
        {
            var result = new Schema.KRPC.Exception ();
            result.Name = exception.Name;
            result.Id = exception.Id;
            result.Documentation = exception.Documentation;
            result.Deprecated = exception.Deprecated;
            result.DeprecatedReason = exception.DeprecatedReason;
            result.MappedType = exception.MappedType;
            return result;
        }

        /// <summary>
        /// Set the service id and type id of a class, enumeration or structure type, from the
        /// signature of its service. Both stay 0 for a type of a service that did not load.
        /// </summary>
        static void SetTypeIds (Schema.KRPC.Type result)
        {
            ServiceSignature service;
            if (!Service.Services.Instance.Signatures.TryGetValue (result.Service, out service))
                return;
            ClassSignature cls;
            EnumerationSignature enumeration;
            StructSignature str;
            if (result.Code == Schema.KRPC.Type.Types.TypeCode.Class &&
                service.Classes.TryGetValue (result.Name, out cls))
                result.Id = cls.Id;
            else if (result.Code == Schema.KRPC.Type.Types.TypeCode.Enumeration &&
                     service.Enumerations.TryGetValue (result.Name, out enumeration))
                result.Id = enumeration.Id;
            else if (result.Code == Schema.KRPC.Type.Types.TypeCode.Struct &&
                     service.Structs.TryGetValue (result.Name, out str))
                result.Id = str.Id;
            else
                return;
            result.ServiceId = service.Id;
        }

        public static Schema.KRPC.Type ToProtobufMessage (this TypeSpec spec)
        {
            var type = spec.Type;
            var result = new Schema.KRPC.Type ();
            result.Nullable = spec.Nullable;
            if (TypeUtils.IsAValueType (type)) {
                switch (Type.GetTypeCode (type)) {
                case TypeCode.Single:
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Float;
                    break;
                case TypeCode.Double:
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Double;
                    break;
                case TypeCode.Int32:
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Sint32;
                    break;
                case TypeCode.Int64:
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Sint64;
                    break;
                case TypeCode.UInt32:
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Uint32;
                    break;
                case TypeCode.UInt64:
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Uint64;
                    break;
                case TypeCode.Boolean:
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Bool;
                    break;
                case TypeCode.String:
                    result.Code = Schema.KRPC.Type.Types.TypeCode.String;
                    break;
                }
                if (type == typeof(byte[]))
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Bytes;
            } else if (TypeUtils.IsAMessageType (type)) {
                if (type == typeof(Service.Messages.Event))
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Event;
                else if (type == typeof(ProcedureCall))
                    result.Code = Schema.KRPC.Type.Types.TypeCode.ProcedureCall;
                else if (type == typeof(Service.Messages.Stream))
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Stream;
                else if (type == typeof(Service.Messages.Services))
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Services;
                else
                    throw new ArgumentException ("Type " + type + " is not valid");
            } else if (TypeUtils.IsAClassType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.Class;
                result.Service = TypeUtils.GetClassServiceName (type);
                result.Name = type.Name;
                SetTypeIds (result);
            } else if (TypeUtils.IsAnEnumType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.Enumeration;
                result.Service = TypeUtils.GetEnumServiceName (type);
                result.Name = type.Name;
                SetTypeIds (result);
            } else if (TypeUtils.IsAStructType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.Struct;
                result.Service = TypeUtils.GetStructServiceName (type);
                result.Name = type.Name;
                SetTypeIds (result);
            } else if (TypeUtils.IsAListCollectionType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.List;
                result.Types_.Add (spec.Types.Select (ToProtobufMessage));
            } else if (TypeUtils.IsADictionaryCollectionType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.Dictionary;
                result.Types_.Add (spec.Types.Select (ToProtobufMessage));
            } else if (TypeUtils.IsASetCollectionType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.Set;
                result.Types_.Add (spec.Types.Select (ToProtobufMessage));
            } else if (TypeUtils.IsATupleCollectionType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.Tuple;
                result.Types_.Add (spec.Types.Select (ToProtobufMessage));
            }
            return result;
        }

        public static Request ToMessage (this Schema.KRPC.Request request)
        {
            var result = new Request ();
            result.Id = request.Id;
            foreach (var call in request.Calls) {
                try {
                    result.Calls.Add (call.ToMessage ());
                } catch (System.Exception e) {
                    // Fails only this call when it runs
                    result.Calls.Add (new ProcedureCall (call.Service, call.ServiceId, call.Procedure, call.ProcedureId) {
                        DecodeException = e
                    });
                }
            }
            return result;
        }

        public static ProcedureCall ToMessage (this Schema.KRPC.ProcedureCall call)
        {
            // Note: a call to an invalid procedure returns with no decoded arguments, and an invalid argument throws
            var result = new ProcedureCall (call.Service, call.ServiceId, call.Procedure, call.ProcedureId);
            try {
                var procedureSignature = Service.Services.Instance.GetProcedureSignature (result);
                result.CachedSignature = procedureSignature;
                foreach (var argument in call.Arguments) {
                    if (argument.Position >= (uint)procedureSignature.Parameters.Count)
                        throw new ArgumentException (
                            "Argument position " + argument.Position + " out of range" +
                            " for " + procedureSignature.FullyQualifiedName);
                    if (HasArgument (result, argument.Position))
                        throw new ArgumentException (
                            "Duplicate argument position " + argument.Position +
                            " for " + procedureSignature.FullyQualifiedName);
                    var spec = procedureSignature.Parameters [(int)argument.Position].Spec;
                    result.Arguments.Add (argument.ToMessage (spec));
                }
            } catch (RPCException) {
            }
            return result;
        }

        static bool HasArgument (ProcedureCall call, uint position)
        {
            var arguments = call.Arguments;
            for (var i = 0; i < arguments.Count; i++) {
                if (arguments [i].Position == position)
                    return true;
            }
            return false;
        }

        public static Argument ToMessage (this Schema.KRPC.Argument argument, TypeSpec spec)
        {
            var value = argument.IsNull ? null : Encoder.Decode (argument.Value, spec);
            return new Argument (argument.Position, value);
        }
    }
}

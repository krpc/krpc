using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    static class MessageExtensions
    {
        public static Google.Protobuf.IMessage ToProtobufMessage (this IMessage message)
        {
            var type = message.GetType ();
            if (type == typeof(Service.Messages.Event))
                return ((Service.Messages.Event)message).ToProtobufMessage ();
            if (type == typeof(Service.Messages.Services))
                return ((Service.Messages.Services)message).ToProtobufMessage ();
            if (type == typeof(Service.Messages.Stream))
                return ((Service.Messages.Stream)message).ToProtobufMessage ();
            if (type == typeof(Status))
                return ((Status)message).ToProtobufMessage ();
            throw new ArgumentException ("Cannot convert a " + type + " to a protobuf message");
        }

        public static Schema.KRPC.Response ToProtobufMessage (this Response response)
        {
            var result = new Schema.KRPC.Response ();
            if (response.HasError)
                result.Error = response.Error.ToProtobufMessage ();
            result.Results.Add (response.Results.Select (ToProtobufMessage));
            return result;
        }

        public static Schema.KRPC.ProcedureResult ToProtobufMessage (this ProcedureResult procedureResult)
        {
            var result = new Schema.KRPC.ProcedureResult ();
            if (procedureResult.HasValue)
                result.Value = Encoder.Encode (procedureResult.Value);
            if (procedureResult.HasError)
                result.Error = procedureResult.Error.ToProtobufMessage ();
            return result;
        }

        public static Schema.KRPC.Error ToProtobufMessage (this Error error)
        {
            return new Schema.KRPC.Error {
                Service = error.Service,
                Name = error.Name,
                Description = error.Description,
                StackTrace = error.StackTrace
            };
        }

        public static Schema.KRPC.StreamUpdate ToProtobufMessage (this StreamUpdate streamUpdate)
        {
            var result = new Schema.KRPC.StreamUpdate ();
            result.Results.Add (streamUpdate.Results.Select (ToProtobufMessage));
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
            result.Procedures.Add (service.Procedures.Select (ToProtobufMessage));
            result.Classes.Add (service.Classes.Select (ToProtobufMessage));
            result.Enumerations.Add (service.Enumerations.Select (ToProtobufMessage));
            result.Exceptions.Add (service.Exceptions.Select (ToProtobufMessage));
            result.Documentation = service.Documentation;
            return result;
        }

        public static Schema.KRPC.Procedure ToProtobufMessage (this Procedure procedure)
        {
            var result = new Schema.KRPC.Procedure ();
            result.Name = procedure.Name;
            result.Parameters.Add (procedure.Parameters.Select (ToProtobufMessage));
            if (procedure.ReturnType != null)
                result.ReturnType = procedure.ReturnType.ToProtobufMessage ();
            result.ReturnIsNullable = procedure.ReturnIsNullable;
            result.Documentation = procedure.Documentation;
            return result;
        }

        public static Schema.KRPC.Parameter ToProtobufMessage (this Parameter parameter)
        {
            var result = new Schema.KRPC.Parameter ();
            result.Name = parameter.Name;
            result.Type = parameter.Type.ToProtobufMessage ();
            if (parameter.HasDefaultValue)
                result.DefaultValue = Encoder.Encode (parameter.DefaultValue);
            result.Nullable = parameter.Nullable;
            return result;
        }

        public static Schema.KRPC.Class ToProtobufMessage (this Class cls)
        {
            var result = new Schema.KRPC.Class ();
            result.Name = cls.Name;
            result.Documentation = cls.Documentation;
            return result;
        }

        public static Schema.KRPC.Enumeration ToProtobufMessage (this Enumeration enumeration)
        {
            var result = new Schema.KRPC.Enumeration ();
            result.Name = enumeration.Name;
            result.Values.Add (enumeration.Values.Select (ToProtobufMessage));
            result.Documentation = enumeration.Documentation;
            return result;
        }

        public static Schema.KRPC.EnumerationValue ToProtobufMessage (this EnumerationValue enumerationValue)
        {
            var result = new Schema.KRPC.EnumerationValue ();
            result.Name = enumerationValue.Name;
            result.Value = enumerationValue.Value;
            result.Documentation = enumerationValue.Documentation;
            return result;
        }

        public static Schema.KRPC.Exception ToProtobufMessage (this Service.Messages.Exception exception)
        {
            var result = new Schema.KRPC.Exception ();
            result.Name = exception.Name;
            result.Documentation = exception.Documentation;
            return result;
        }

        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidComplexMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static Schema.KRPC.Type ToProtobufMessage (this Type type)
        {
            var result = new Schema.KRPC.Type ();
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
                else if (type == typeof(Status))
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Status;
                else if (type == typeof(Service.Messages.Services))
                    result.Code = Schema.KRPC.Type.Types.TypeCode.Services;
                else
                    throw new ArgumentException ("Type " + type + " is not valid");
            } else if (TypeUtils.IsAClassType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.Class;
                result.Service = TypeUtils.GetClassServiceName (type);
                result.Name = type.Name;
            } else if (TypeUtils.IsAnEnumType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.Enumeration;
                result.Service = TypeUtils.GetEnumServiceName (type);
                result.Name = type.Name;
            } else if (TypeUtils.IsAListCollectionType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.List;
                result.Types_.Add (type.GetGenericArguments () [0].ToProtobufMessage ());
            } else if (TypeUtils.IsADictionaryCollectionType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.Dictionary;
                result.Types_.Add (type.GetGenericArguments () [0].ToProtobufMessage ());
                result.Types_.Add (type.GetGenericArguments () [1].ToProtobufMessage ());
            } else if (TypeUtils.IsASetCollectionType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.Set;
                result.Types_.Add (type.GetGenericArguments () [0].ToProtobufMessage ());
            } else if (TypeUtils.IsATupleCollectionType (type)) {
                result.Code = Schema.KRPC.Type.Types.TypeCode.Tuple;
                foreach (var subType in type.GetGenericArguments())
                    result.Types_.Add (subType.ToProtobufMessage ());
            }
            return result;
        }

        public static Schema.KRPC.Event ToProtobufMessage (this Service.Messages.Event evnt)
        {
            var result = new Schema.KRPC.Event ();
            result.Stream = evnt.Stream.ToProtobufMessage ();
            return result;
        }

        public static Schema.KRPC.Stream ToProtobufMessage (this Service.Messages.Stream stream)
        {
            var result = new Schema.KRPC.Stream ();
            result.Id = stream.Id;
            return result;
        }

        public static Schema.KRPC.Status ToProtobufMessage (this Status status)
        {
            var result = new Schema.KRPC.Status ();
            result.Version = status.Version;
            result.BytesRead = status.BytesRead;
            result.BytesWritten = status.BytesWritten;
            result.BytesReadRate = status.BytesReadRate;
            result.BytesWrittenRate = status.BytesWrittenRate;
            result.RpcsExecuted = status.RpcsExecuted;
            result.RpcRate = status.RpcRate;
            result.OneRpcPerUpdate = status.OneRpcPerUpdate;
            result.MaxTimePerUpdate = status.MaxTimePerUpdate;
            result.AdaptiveRateControl = status.AdaptiveRateControl;
            result.BlockingRecv = status.BlockingRecv;
            result.RecvTimeout = status.RecvTimeout;
            result.TimePerRpcUpdate = status.TimePerRpcUpdate;
            result.PollTimePerRpcUpdate = status.PollTimePerRpcUpdate;
            result.ExecTimePerRpcUpdate = status.ExecTimePerRpcUpdate;
            result.StreamRpcs = status.StreamRpcs;
            result.StreamRpcsExecuted = status.StreamRpcsExecuted;
            result.StreamRpcRate = status.StreamRpcRate;
            result.TimePerStreamUpdate = status.TimePerStreamUpdate;
            return result;
        }

        public static Request ToMessage (this Schema.KRPC.Request request)
        {
            var result = new Request ();
            foreach (var call in request.Calls)
                result.Calls.Add (call.ToMessage ());
            return result;
        }

        public static ProcedureCall ToMessage (this Schema.KRPC.ProcedureCall call)
        {
            // Note: this method must not throw an exception, if the call is to an invalid
            // procedure. If the ProcedureSignature cannot be obtained, returns a
            // ProcedureCall with no decoded arguments
            var result = new ProcedureCall (call.Service, call.ServiceId, call.Procedure, call.ProcedureId);
            try {
                var procedureSignature = Service.Services.Instance.GetProcedureSignature (result);
                foreach (var argument in call.Arguments) {
                    var position = (int)argument.Position;
                    // Ignore the argument if its position is not valid
                    if (position >= procedureSignature.Parameters.Count)
                        continue;
                    var type = procedureSignature.Parameters [position].Type;
                    result.Arguments.Add (argument.ToMessage (type));
                }
            } catch (RPCException) {
            }
            return result;
        }

        public static Argument ToMessage (this Schema.KRPC.Argument argument, Type type)
        {
            return new Argument (argument.Position, Encoder.Decode (argument.Value, type));
        }
    }
}

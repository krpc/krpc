using System;
using System.Linq;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    static class MessageExtensions
    {
        public static Google.Protobuf.IMessage ToProtobufMessage (this IMessage message)
        {
            var type = message.GetType ();
            if (type == typeof(Request))
                return ((Request)message).ToProtobufMessage ();
            else if (type == typeof(Response))
                return ((Response)message).ToProtobufMessage ();
            else if (type == typeof(Services))
                return ((Services)message).ToProtobufMessage ();
            else if (type == typeof(Status))
                return ((Status)message).ToProtobufMessage ();
            throw new ArgumentException ("Cannot convert a " + type + " to a protobuf message");
        }

        public static Schema.KRPC.Request ToProtobufMessage (this Request request)
        {
            var result = new Schema.KRPC.Request ();
            result.Service = request.Service;
            result.Procedure = request.Procedure;
            result.Arguments.Add (request.Arguments.Select (ToProtobufMessage));
            return result;
        }

        public static Schema.KRPC.Argument ToProtobufMessage (this Argument argument)
        {
            var result = new Schema.KRPC.Argument ();
            result.Position = argument.Position;
            result.Value = Encoder.Encode (argument.Value);
            return result;
        }

        public static Schema.KRPC.Response ToProtobufMessage (this Response response)
        {
            var result = new Schema.KRPC.Response ();
            result.Time = response.Time;
            result.HasError = response.HasError;
            result.Error = response.Error;
            result.HasReturnValue = response.HasReturnValue;
            if (response.HasReturnValue)
                result.ReturnValue = Encoder.Encode (response.ReturnValue);
            return result;
        }

        public static Schema.KRPC.StreamMessage ToProtobufMessage (this StreamMessage streamMessage)
        {
            var result = new Schema.KRPC.StreamMessage ();
            result.Responses.Add (streamMessage.Responses.Select (ToProtobufMessage));
            return result;
        }

        public static Schema.KRPC.StreamResponse ToProtobufMessage (this StreamResponse streamResponse)
        {
            var result = new Schema.KRPC.StreamResponse ();
            result.Id = streamResponse.Id;
            result.Response = streamResponse.Response.ToProtobufMessage ();
            return result;
        }

        public static Schema.KRPC.Services ToProtobufMessage (this Services services)
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
            result.Documentation = service.Documentation;
            return result;
        }

        public static Schema.KRPC.Procedure ToProtobufMessage (this Procedure procedure)
        {
            var result = new Schema.KRPC.Procedure ();
            result.Name = procedure.Name;
            result.Parameters.Add (procedure.Parameters.Select (ToProtobufMessage));
            result.HasReturnType = procedure.HasReturnType;
            result.ReturnType = procedure.ReturnType;
            result.Attributes.Add (procedure.Attributes);
            result.Documentation = procedure.Documentation;
            return result;
        }

        public static Schema.KRPC.Parameter ToProtobufMessage (this Parameter parameter)
        {
            var result = new Schema.KRPC.Parameter ();
            result.Name = parameter.Name;
            result.Type = parameter.Type;
            result.HasDefaultValue = parameter.HasDefaultValue;
            if (result.HasDefaultValue)
                result.DefaultValue = Encoder.Encode (parameter.DefaultValue);
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
            var procedureSignature = Service.Services.Instance.GetProcedureSignature (request.Service, request.Procedure);
            var result = new Request (request.Service, request.Procedure);
            foreach (var argument in request.Arguments) {
                var type = procedureSignature.Parameters [(int)argument.Position].Type;
                result.Arguments.Add (argument.ToMessage (type));
            }
            return result;
        }

        public static Argument ToMessage (this Schema.KRPC.Argument argument, Type type)
        {
            return new Argument (argument.Position, Encoder.Decode (argument.Value, type));
        }
    }
}

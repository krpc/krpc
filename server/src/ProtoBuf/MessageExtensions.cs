using System;
using KRPC.Service.Messages;
using Google.Protobuf;
using System.Collections.Generic;
using System.Linq;

namespace KRPC.ProtoBuf
{
    internal static class MessageExtensions
    {
        public static Schema.KRPC.Request ToProtobufRequest (this Request request)
        {
            var result = new Schema.KRPC.Request ();
            result.Service = request.Service;
            result.Procedure = request.Procedure;
            result.Arguments.Add (request.Arguments.Select (ToProtobufArgument));
            return result;
        }

        public static Schema.KRPC.Argument ToProtobufArgument (this Argument argument)
        {
            var result = new Schema.KRPC.Argument ();
            result.Position = argument.Position;
            result.Value = Encoder.Encode (argument.Value);
            return result;
        }

        public static Schema.KRPC.Response ToProtobufResponse (this Response response)
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

        public static Schema.KRPC.Services ToProtobufServices (this Services services)
        {
            var result = new Schema.KRPC.Services ();
            result.Services_.Add (services.Services_.Select (x => x.ToProtobufService ()));
            return result;
        }

        public static Schema.KRPC.Service ToProtobufService (this Service.Messages.Service service)
        {
            var result = new Schema.KRPC.Service ();
            result.Name = service.Name;
            result.Procedures.Add (service.Procedures.Select (x => x.ToProtobufProcedure ()));
            result.Classes.Add (service.Classes.Select (ToProtobufClass));
            result.Enumerations.Add (service.Enumerations.Select (ToProtobufEnumeration));
            result.Documentation = service.Documentation;
            return result;
        }

        public static Schema.KRPC.Procedure ToProtobufProcedure (this Procedure procedure)
        {
            var result = new Schema.KRPC.Procedure ();
            result.Name = procedure.Name;
            result.Parameters.Add (procedure.Parameters.Select (ToProtobufParameter));
            result.HasReturnType = procedure.HasReturnType;
            result.ReturnType = procedure.ReturnType;
            result.Attributes.Add (procedure.Attributes);
            result.Documentation = procedure.Documentation;
            return result;
        }

        public static Schema.KRPC.Parameter ToProtobufParameter (this Parameter parameter)
        {
            var result = new Schema.KRPC.Parameter ();
            result.Name = parameter.Name;
            result.Type = parameter.Type;
            result.HasDefaultValue = parameter.HasDefaultValue;
            if (result.HasDefaultValue)
                result.DefaultValue = Encoder.Encode (parameter.DefaultValue);
            return result;
        }

        public static Schema.KRPC.Class ToProtobufClass (this Class cls)
        {
            var result = new Schema.KRPC.Class ();
            result.Name = cls.Name;
            result.Documentation = cls.Documentation;
            return result;
        }

        public static Schema.KRPC.Enumeration ToProtobufEnumeration (this Enumeration enumeration)
        {
            var result = new Schema.KRPC.Enumeration ();
            result.Name = enumeration.Name;
            result.Values.Add (enumeration.Values.Select (ToProtobufEnumerationValue));
            result.Documentation = enumeration.Documentation;
            return result;
        }

        public static Schema.KRPC.EnumerationValue ToProtobufEnumerationValue (this EnumerationValue enumerationValue)
        {
            var result = new Schema.KRPC.EnumerationValue ();
            result.Name = enumerationValue.Name;
            result.Value = enumerationValue.Value;
            result.Documentation = enumerationValue.Documentation;
            return result;
        }

        public static Schema.KRPC.StreamMessage ToProtobufStreamMessage (this StreamMessage streamMessage)
        {
            var result = new Schema.KRPC.StreamMessage ();
            result.Responses.Add (streamMessage.Responses.Select (ToProtobufStreamResponse));
            return result;
        }

        public static Schema.KRPC.StreamResponse ToProtobufStreamResponse (this StreamResponse streamResponse)
        {
            var result = new Schema.KRPC.StreamResponse ();
            result.Id = streamResponse.Id;
            result.Response = streamResponse.Response.ToProtobufResponse ();
            return result;
        }

        public static Schema.KRPC.Status ToProtobufStatus (this Status status)
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

        public static Request ToRequest (this Schema.KRPC.Request request)
        {
            var procedureSignature = KRPC.Service.Services.Instance.GetProcedureSignature (request.Service, request.Procedure);
            var result = new Request ();
            result.Service = request.Service;
            result.Procedure = request.Procedure;
            foreach (var argument in request.Arguments) {
                var type = procedureSignature.Parameters [(int)argument.Position].Type;
                result.Arguments.Add (argument.ToArgument (type));
            }
            return result;
        }

        public static Argument ToArgument (this Schema.KRPC.Argument argument, Type type)
        {
            var result = new Argument ();
            result.Position = argument.Position;
            result.Value = Encoder.Decode (argument.Value, type);
            return result;
        }

        public static Response ToResponse (this Schema.KRPC.Response response)
        {
            var result = new Response ();
            result.Time = response.Time;
            result.HasError = response.HasError;
            result.Error = response.Error;
            result.HasReturnValue = response.HasError;
            result.ReturnValue = Encoder.Encode (response.ReturnValue);
            return result;
        }

        public static Procedure ToProcedure (this Schema.KRPC.Procedure procedure)
        {
            var result = new Procedure ();
            result.Name = procedure.Name;
            foreach (var parameter in procedure.Parameters)
                result.Parameters.Add (parameter.ToParameter ());
            result.HasReturnType = procedure.HasReturnType;
            result.ReturnType = procedure.ReturnType;
            foreach (var attribute in procedure.Attributes)
                result.Attributes.Add (attribute);
            result.Documentation = procedure.Documentation;
            return result;
        }

        public static Parameter ToParameter (this Schema.KRPC.Parameter parameter)
        {
            var result = new Parameter ();
            result.Name = parameter.Name;
            result.Type = parameter.Type;
            result.HasDefaultValue = parameter.HasDefaultValue;
            result.DefaultValue = Encoder.Encode (parameter.DefaultValue);
            return result;
        }
    }
}

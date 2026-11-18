using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;
using SchemaMessage = KRPC.Schema.KRPC.Message;
using Services = KRPC.Service.Services;

namespace KRPC.Benchmarks
{
    /// <summary>
    /// Times one step of the server's path for a call on its own, so that the cost of a call
    /// on the game thread splits into the steps that could run on another thread.
    /// </summary>
    static class Steps
    {
        internal static readonly IList<string> Names = new [] {
            "parse", "decode", "execute", "encode", "serialize"
        };

        internal static IDictionary<string, double> Measure (ProcedureCall call, string step, uint iterations)
        {
            if (call == null)
                throw new ArgumentNullException (nameof (call));
            var procedure = Services.Instance.GetProcedureSignature (call);
            var bytes = RequestBytes (procedure, call);
            var message = SchemaMessage.Parser.ParseFrom (bytes);
            var decoded = message.Request.ToMessage ().Calls [0];
            var result = Services.Instance.ExecuteCall (procedure, decoded);
            if (result.HasError)
                throw new ArgumentException (
                    "Call to " + procedure.FullyQualifiedName + " failed: " + result.Error.Description);
            var response = new Response ();
            response.Results.Add (result);
            var envelope = new MessageEnvelope ().Set (
                SchemaMessage.ResponseFieldNumber, response.ToProtobufMessage ());
            var output = new MemoryStream ();
            var stream = new CodedOutputStream (output, true);
            switch (step) {
            case "parse":
                return Timer.Measure (() => { Timer.ObjectSink = SchemaMessage.Parser.ParseFrom (bytes); }, iterations);
            case "decode":
                return Timer.Measure (() => { Timer.ObjectSink = message.Request.ToMessage (); }, iterations);
            case "execute":
                return Timer.Measure (() => { Timer.ObjectSink = Services.Instance.ExecuteCall (procedure, decoded); }, iterations);
            case "encode":
                return Timer.Measure (() => {
                    // A result encodes once, and keeps its encoding
                    result.EncodedValue = null;
                    Timer.ObjectSink = response.ToProtobufMessage ();
                }, iterations);
            case "serialize":
                return Timer.Measure (() => {
                    output.SetLength (0);
                    stream.WriteMessage (envelope);
                    stream.Flush ();
                }, iterations);
            default:
                throw new ArgumentException ("Unknown step " + step);
            }
        }

        // The request as a client sends it, with each argument encoded
        static byte[] RequestBytes (ProcedureSignature procedure, ProcedureCall call)
        {
            var schemaCall = new Schema.KRPC.ProcedureCall {
                Service = call.Service, ServiceId = call.ServiceId,
                Procedure = call.Procedure, ProcedureId = call.ProcedureId
            };
            foreach (var argument in call.Arguments) {
                var schemaArgument = new Schema.KRPC.Argument { Position = argument.Position };
                if (argument.Value == null)
                    schemaArgument.IsNull = true;
                else
                    schemaArgument.Value = Encoder.Encode (
                        argument.Value, procedure.Parameters [(int)argument.Position].Spec);
                schemaCall.Arguments.Add (schemaArgument);
            }
            var request = new Schema.KRPC.Request { Id = 1 };
            request.Calls.Add (schemaCall);
            return new SchemaMessage { Request = request }.ToByteArray ();
        }
    }
}

using System;
using Google.Protobuf;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    sealed class ProcedureCallStream : Stream
    {
        ProcedureSignature procedure;
        StreamContinuation continuation;

        public ProcedureCallStream (ProcedureCall call)
        {
            var services = Services.Instance;
            procedure = services.GetProcedureSignature (call);
            if (!procedure.HasReturnType)
                throw new KRPC.InvalidOperationException (
                    "Cannot create a stream for a procedure that does not return a value.");
            // Each evaluation would create a stream
            var created = CreatedStreamKind (procedure);
            if (created != null)
                throw new KRPC.InvalidOperationException (
                    procedure.FullyQualifiedName + " returns " + created + ", and cannot be streamed.");
            // Evaluating these each update would repeat their effects
            var name = procedure.FullyQualifiedName;
            if (name == "KRPC.Expression_static_StateVariable")
                throw new KRPC.InvalidOperationException (name + " cannot be streamed.");
            if (name == "KRPC.RunFunction")
                throw new KRPC.InvalidOperationException (
                    name + " cannot be streamed. Use KRPC.AddFunctionStream to stream a function.");
            // Decoded to check them when the stream is added
            services.GetArguments (procedure, call.Arguments);
            continuation = new StreamContinuation (call);
        }

        /// <summary>
        /// "a stream" or "an event" if the procedure returns one, or null.
        /// </summary>
        internal static string CreatedStreamKind (ProcedureSignature procedure)
        {
            var type = procedure.ReturnSpec.Type;
            if (type == typeof (Messages.Stream))
                return "a stream";
            if (type == typeof (Messages.Event))
                return "an event";
            return null;
        }

        public override void UpdateInternal() {
            var result = StreamResult.Result;
            bool wasSet = result.HasValue;
            object oldValue = result.Value;
            var oldSpec = result.Spec;
            var oldEncodedValue = result.EncodedValue;

            try {
                continuation.RunInto (result);
            } catch (YieldException) {
                return;
            } catch (System.Exception e) {
                if (Services.IsHeld (e))
                    Restore (wasSet, oldValue, oldSpec, oldEncodedValue);
                else
                    SetError (e);
                return;
            }
            // An object that is not loaded, or a procedure not available in the scene, holds
            // the stream at its last value
            if (result.HasError && Services.IsHeld (result.Error)) {
                Restore (wasSet, oldValue, oldSpec, oldEncodedValue);
                return;
            }

            if (!result.HasValue) {
                Changed |= result.HasError;
                return;
            }
            // An encoding error, such as a null element in a list of objects, fails only this
            // stream. The update sends this encoding.
            ByteString encoded;
            try {
                encoded = Encode (result);
            } catch (ServiceException e) {
                SetError (e);
                return;
            }
            Changed |= !wasSet || !Equals (encoded, oldEncodedValue);
        }

        void Restore (bool wasSet, object value, TypeSpec spec, ByteString encodedValue)
        {
            var result = StreamResult.Result;
            result.Reset ();
            if (!wasSet)
                return;
            result.Spec = spec;
            result.Value = value;
            result.EncodedValue = encodedValue;
        }
    }
}

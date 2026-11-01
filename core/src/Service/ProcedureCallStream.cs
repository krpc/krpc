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
            var name = procedure.FullyQualifiedName;
            // Internal procedures implement streams and functions for the client libraries
            if (procedure.Internal)
                throw new KRPC.InvalidOperationException (name + " is internal, and cannot be streamed.");
            // Each evaluation would create an event
            if (procedure.HasReturnType && procedure.ReturnSpec.Type == typeof (Messages.Event))
                throw new KRPC.InvalidOperationException (name + " returns an event, and cannot be streamed.");
            // Evaluating this each update would repeat its effects
            if (name == "KRPC.Expression_static_StateVariable")
                throw new KRPC.InvalidOperationException (name + " cannot be streamed.");
            // Only a client making a call can hold the tick
            if (name == "KRPC.HoldTick" || name == "KRPC.NextTick" || name == "KRPC.ReleaseTick")
                throw new KRPC.InvalidOperationException (name + " cannot be streamed.");
            // Decoded to check them when the stream is added
            services.GetArguments (procedure, call.Arguments);
            continuation = new StreamContinuation (call);
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
                if (result.HasError)
                    Changed = true;
                else
                    RanWithNoValue ();
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

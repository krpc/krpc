using System;
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

            try {
                continuation.RunInto (result);
            } catch (YieldException) {
                return;
            } catch (System.Exception e) {
                SetError (e);
                return;
            }

            if (result.HasValue) {
                if (!wasSet)
                    Changed = true;
                else if (!ReferenceEquals (result.Value, null))
                    Changed |= !ValueUtils.Equal (result.Value, oldValue);
                else
                    Changed |= !ReferenceEquals (oldValue, null);
            } else {
                Changed |= result.HasError;
            }

            // An encoding error, such as a null element in a list of objects, fails only this
            // stream. The update sends this encoding.
            if (Changed && result.HasValue && !ReferenceEquals (result.Value, null)) {
                try {
                    result.EncodedValue = Server.ProtocolBuffers.Encoder.Encode (result.Value, result.Spec);
                } catch (ServiceException e) {
                    SetError (e);
                }
            }
        }
    }
}

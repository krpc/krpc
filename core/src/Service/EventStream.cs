using System;
using KRPC;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    sealed class EventStream : Stream {
        // An event carries whether it has fired, which is the type its result is encoded as
        static readonly TypeSpec BoolSpec = TypeSpec.Create (typeof(bool));

        Func<bool> continuation;
        bool shouldRemove;
        System.Exception pendingError;

        public EventStream ()
        {
            // The first update sends whether the event has fired
            SetValue (false);
        }

        public EventStream (Func<bool> eventContinuation) : this ()
        {
            continuation = eventContinuation;
        }

        public override void UpdateInternal() {
            // An error is sent after the firing it came with
            if (pendingError != null) {
                if (!HasUnsentFiring)
                    SetError (pendingError);
                return;
            }
            // An event that is ending does not fire again, so the end is sent
            if (continuation != null && !shouldRemove) {
                try {
                    if (continuation ())
                        Trigger ();
                } catch (System.Exception e) {
                    // An object that is not loaded, or a procedure not available in the scene,
                    // holds the event
                    if (Services.IsHeld (e))
                        return;
                    if (HasUnsentFiring)
                        pendingError = e;
                    else
                        SetError (e);
                    return;
                }
            }
            // A firing and an error both reset the result, so the end waits for an update
            // with no firing to send
            if (shouldRemove && !HasUnsentFiring)
                SetError (new KRPC.StreamRemovedException ("The event has finished"));
        }

        bool HasUnsentFiring {
            get { return Changed && Result.HasValue && (bool)Result.Value; }
        }

        public void Trigger () {
            SetValue (true);
        }

        // An error and a value are sent together, so setting the value clears the error
        void SetValue (bool value)
        {
            var result = Result;
            result.Reset ();
            result.Spec = BoolSpec;
            result.Value = value;
            Changed = true;
        }

        public void Remove () {
            shouldRemove = true;
        }

        public override void Sent () {
            Changed = false;
            var result = Result;
            if (result.HasValue && (bool)result.Value)
                result.Value = false;
        }
    }
}

using System;
using KRPC;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    sealed class EventStream : Stream {
        Func<bool> continuation;
        bool shouldRemove;

        public EventStream ()
        {
            Changed = false;
        }

        public EventStream (Func<bool> eventContinuation)
        {
            Changed = false;
            continuation = eventContinuation;
        }

        public override bool Equals (Service.Stream other)
        {
            return ReferenceEquals (this, other);
        }

        public override int GetHashCode ()
        {
            return 0;
        }

        public override void UpdateInternal() {
            if (continuation != null) {
                try {
                    bool triggered = continuation ();
                    // Replace an error left by an earlier update with a value. The
                    // client holds the last thing the stream sent it, and would go on
                    // reporting the error on every wait if it were only dropped here
                    if (Result.HasError) {
                        Result.Reset ();
                        Result.Value = false;
                        Changed = true;
                    }
                    if (triggered)
                        Trigger ();
                } catch (YieldException e) {
                    // Evaluating the expression again from the start is the only way
                    // to resume it, and that repeats everything it did before the
                    // procedure paused, so report the pause rather than retrying
                    SetError (new InvalidOperationException (
                        global::KRPC.Service.KRPC.Expression.YieldedMessage, e));
                } catch (System.Exception e) {
                    SetError (e);
                }
            }
            if (shouldRemove)
                Core.Instance.RemoveStream (Id);
        }

        void SetError (System.Exception exn)
        {
            var result = Result;
            result.Reset ();
            result.Error = Services.Instance.HandleException (exn);
            Changed = true;
        }

        public void Trigger () {
            Result.Value = true;
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

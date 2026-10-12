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
                    if (continuation ())
                        Trigger ();
                } catch (System.Exception e) {
                    SetError (e);
                }
            }
            if (shouldRemove)
                Core.Instance.RemoveStream (Id);
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

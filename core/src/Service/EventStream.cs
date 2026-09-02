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
            if (continuation != null && continuation())
                Trigger();
            if (shouldRemove)
                Core.Instance.RemoveStream (Id);
        }

        public void Trigger () {
            Result.Spec = BoolSpec;
            Result.Value = true;
            Changed = true;
        }

        public void Remove () {
            shouldRemove = true;
        }

        public override void Sent () {
            Changed = false;
            var result = Result;
            if ((bool)result.Value)
                result.Value = false;
        }
    }
}

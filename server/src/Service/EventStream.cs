using System.Diagnostics.CodeAnalysis;
using KRPC;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    sealed class EventStream : Stream {
        bool shouldRemove;

        public EventStream ()
        {
            Changed = false;
        }

        public override bool Equals (Service.Stream other)
        {
            return ReferenceEquals (this, other);
        }

        public override int GetHashCode ()
        {
            return 0;
        }

        public override void Update() {
            if (shouldRemove)
                Core.Instance.RemoveStream (Id);
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

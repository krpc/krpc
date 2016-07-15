using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Utils
{
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    sealed class RoundRobinScheduler<T> : IScheduler<T>
    {
        LinkedListNode<T> next;
        readonly LinkedList<T> items = new LinkedList<T> ();

        public bool Empty {
            get { return items.Count == 0; }
        }

        public T Next ()
        {
            if (next == null)
                throw new InvalidOperationException ();
            T result = next.Value;
            Advance ();
            return result;
        }

        public void Add (T item)
        {
            if (items.Count == 0) {
                items.AddFirst (item);
                next = items.First;
            } else {
                if (items.Contains (item))
                    throw new InvalidOperationException ();
                items.AddBefore (next, item);
            }
        }

        public void Remove (T item)
        {
            if (next == null) {
                // Case: empty list
                throw new InvalidOperationException ();
            }
            // Case: non-empty list
            if (next.Value.Equals (item)) {
                // Case: equal to next
                if (items.Count > 1) {
                    // Case: will not empty the list
                    Advance ();
                    items.Remove (item);
                } else {
                    // Case: will empty the list
                    next = null;
                    items.Clear ();
                }
            } else {
                // Case: not equal to current (cannot empty the list)
                if (!items.Remove (item))
                    throw new InvalidOperationException ();
            }
        }

        void Advance ()
        {
            next = next.Next;
            if (next == null)
                next = items.First;
        }

        public IEnumerator<T> GetEnumerator ()
        {
            return items.GetEnumerator ();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        public LinkedList<T> Items {
            get { return items; }
        }
    }
}

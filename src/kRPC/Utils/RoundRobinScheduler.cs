using System;
using System.Collections.Generic;

namespace KRPC.Utils
{
    sealed class RoundRobinScheduler<T> : IScheduler<T>
    {
        private LinkedListNode<T> next;
        private LinkedList<T> items = new LinkedList<T> ();

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

        private void Advance ()
        {
            next = next.Next;
            if (next == null)
                next = items.First;
        }
    }
}


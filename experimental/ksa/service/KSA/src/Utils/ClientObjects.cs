using System;
using System.Collections.Generic;
using KRPC.Server;
using KRPC.Service;

namespace KRPC.KSA.Utils
{
    /// <summary>
    /// A list of objects added by clients, and the client that owns each. A client's objects
    /// are removed when it disconnects.
    /// </summary>
    sealed class ClientObjects<T> where T : class
    {
        sealed class Entry
        {
            public T Object;
            public IClient Owner;
        }

        readonly List<Entry> entries = new List<Entry> ();
        readonly Action<T> removed;
        bool subscribed;

        /// <summary>
        /// Create the list. The action is called on each object as it leaves the list.
        /// </summary>
        public ClientObjects (Action<T> removed)
        {
            this.removed = removed;
        }

        /// <summary>
        /// The number of objects.
        /// </summary>
        public int Count {
            get { return entries.Count; }
        }

        /// <summary>
        /// The object at an index, in the order added.
        /// </summary>
        public T this [int index] {
            get { return entries [index].Object; }
        }

        /// <summary>
        /// Add an object, owned by the calling client.
        /// </summary>
        public TObject Add<TObject> (TObject obj) where TObject : T
        {
            if (!subscribed) {
                Core.Instance.OnClientDisconnected += (s, e) => RemoveAll (x => x.Owner == e.Client);
                subscribed = true;
            }
            entries.Add (new Entry { Object = obj, Owner = CallContext.Client });
            return obj;
        }

        public void Remove (T obj)
        {
            RemoveAll (x => ReferenceEquals (x.Object, obj));
        }

        /// <summary>
        /// Remove every object, or only those owned by the calling client.
        /// </summary>
        public void Clear (bool clientOnly)
        {
            var client = CallContext.Client;
            RemoveAll (x => !clientOnly || x.Owner == client);
        }

        void RemoveAll (Predicate<Entry> match)
        {
            var count = entries.RemoveAll (x => {
                if (!match (x))
                    return false;
                removed (x.Object);
                return true;
            });
            // Calls through the removed objects then raise an object destroyed exception
            if (count > 0)
                GameState.Sweep ();
        }
    }
}

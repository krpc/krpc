using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Server;
using KRPC.Service;

namespace KRPC.Utils
{
    /// <summary>
    /// A collection of objects, each tagged with the client that added it, so that an
    /// object can be released when its client disconnects. Objects added outside an
    /// RPC have a null owner and are only released when the collection is cleared.
    /// </summary>
    public sealed class ClientOwnedObjects<T> : IClientOwnedCollection where T : class
    {
        sealed class Entry
        {
            public T Object;
            public IClient Owner;
        }

        readonly List<Entry> entries = new List<Entry> ();
        readonly Action<T> release;

        /// <summary>
        /// Create a collection. The release action, if any, is invoked on each object
        /// released by <see cref="Sweep" /> and <see cref="Clear" /> (but not on
        /// objects removed with <see cref="Remove" />, whose callers do their own
        /// teardown).
        /// </summary>
        public ClientOwnedObjects (Action<T> release = null)
        {
            this.release = release;
        }

        /// <summary>
        /// Add an object, owned by the client making the current RPC (or unowned if
        /// there is no current RPC).
        /// </summary>
        public void Add (T obj)
        {
            entries.Add (new Entry { Object = obj, Owner = CallContext.Client });
        }

        /// <summary>
        /// Whether the collection contains the given object and it is owned by the
        /// client making the current RPC.
        /// </summary>
        public bool OwnedByCaller (T obj)
        {
            var entry = entries.Find (x => ReferenceEquals (x.Object, obj));
            return entry != null && entry.Owner == CallContext.Client;
        }

        /// <summary>
        /// Remove an object, regardless of its owner, without invoking the release
        /// action. Returns false if it was not in the collection.
        /// </summary>
        public bool Remove (T obj)
        {
            var index = entries.FindIndex (x => ReferenceEquals (x.Object, obj));
            if (index < 0)
                return false;
            entries.RemoveAt (index);
            return true;
        }

        /// <summary>
        /// Remove all objects matching the predicate, without invoking the release
        /// action.
        /// </summary>
        public void RemoveAll (Predicate<T> predicate)
        {
            entries.RemoveAll (x => predicate (x.Object));
        }

        /// <summary>
        /// A snapshot of the objects in the collection, safe to iterate while the
        /// collection is modified.
        /// </summary>
        public IEnumerable<T> Items {
            get { return entries.Select (x => x.Object).ToList (); }
        }

        /// <summary>
        /// Release the objects whose owning client has disconnected.
        /// </summary>
        public void Sweep ()
        {
            for (var i = entries.Count - 1; i >= 0; i--) {
                var entry = entries [i];
                if (ClientConnections.Disconnected (entry.Owner)) {
                    entries.RemoveAt (i);
                    if (release != null)
                        release (entry.Object);
                }
            }
        }

        /// <summary>
        /// Release all objects.
        /// </summary>
        public void Clear ()
        {
            var released = entries.ToList ();
            entries.Clear ();
            if (release != null)
                foreach (var entry in released)
                    release (entry.Object);
        }
    }
}

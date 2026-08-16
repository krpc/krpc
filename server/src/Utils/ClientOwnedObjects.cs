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
        readonly List<Entry> detached = new List<Entry> ();
        readonly Action<T> release;
        readonly bool givenToClients;

        /// <summary>
        /// Create a collection. The release action, if any, is invoked on each object
        /// released by <see cref="Sweep" /> and <see cref="Clear()" /> (but not on
        /// objects removed with <see cref="Remove" />, whose callers do their own
        /// teardown). The collection adds itself to <see cref="ClientOwnedState" />, so
        /// that its state is released along with everything else when there is no game
        /// left to hold it.
        ///
        /// An object a client is given holds an identifier in the object store, so the
        /// collection letting go of one asks for the store to be swept. A collection whose
        /// objects are never handed out passes false, having nothing there to sweep.
        /// </summary>
        public ClientOwnedObjects (Action<T> release = null, bool givenToClients = true)
        {
            this.release = release;
            this.givenToClients = givenToClients;
            ClientOwnedState.Register (this);
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
            LetGo (1);
            return true;
        }

        /// <summary>
        /// Remove all objects matching the predicate, without invoking the release
        /// action.
        /// </summary>
        public void RemoveAll (Predicate<T> predicate)
        {
            LetGo (entries.RemoveAll (x => predicate (x.Object)));
        }

        /// <summary>
        /// Remove the objects that stand for something the game has destroyed, without
        /// invoking the release action: what they held is already gone, so there is
        /// nothing left to tear down. An object that does not say what the game holds for
        /// it is kept, as is one that is merely not loaded, which the game can bring back.
        /// </summary>
        /// <remarks>
        /// An addon that acts on its objects every frame calls this first, so that a
        /// destroyed object is dropped rather than reached into from the frame loop, where
        /// there is no client call to report the failure to.
        /// </remarks>
        public void RemoveDestroyed ()
        {
            LetGo (entries.RemoveAll (x => {
                var obj = x.Object as IGameObjectState;
                return obj != null && obj.GameObjectState == GameObjectState.Destroyed;
            }));
        }

        /// <summary>
        /// Record that the collection has let go of the given number of objects, so that
        /// the object store is swept.
        /// </summary>
        /// <remarks>
        /// What the collection lets go of is either already gone from the game or torn
        /// down by the release action, so the objects standing for it are due to leave the
        /// object store as well. Nothing else says so: a client removing its own line,
        /// panel or force destroys nothing the game raises an event for, so without this
        /// the object would sit in the store until something unrelated was destroyed.
        /// </remarks>
        void LetGo (int count)
        {
            if (count > 0 && givenToClients)
                GameState.RequestSweep ();
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
            var released = 0;
            for (var i = entries.Count - 1; i >= 0; i--) {
                var entry = entries [i];
                if (ClientConnections.Disconnected (entry.Owner)) {
                    entries.RemoveAt (i);
                    released++;
                    if (release != null)
                        release (entry.Object);
                }
            }
            LetGo (released);
        }

        /// <summary>
        /// Release the objects owned by the given client.
        /// </summary>
        public void Clear (IClient owner)
        {
            var released = 0;
            for (var i = entries.Count - 1; i >= 0; i--) {
                var entry = entries [i];
                if (entry.Owner == owner) {
                    entries.RemoveAt (i);
                    released++;
                    if (release != null)
                        release (entry.Object);
                }
            }
            LetGo (released);
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
            LetGo (released.Count);
        }

        /// <summary>
        /// Take every object out of the collection, to be released by a later call to
        /// <see cref="ReleaseDetached" />.
        /// </summary>
        public void Detach ()
        {
            detached.AddRange (entries);
            entries.Clear ();
        }

        /// <summary>
        /// Release the objects taken out by <see cref="Detach" />.
        /// </summary>
        public void ReleaseDetached ()
        {
            var released = detached.ToList ();
            detached.Clear ();
            if (release != null)
                foreach (var entry in released)
                    release (entry.Object);
            LetGo (released.Count);
        }
    }
}

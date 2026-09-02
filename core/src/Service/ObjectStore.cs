using System;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.Service
{
    sealed class ObjectStore
    {
        readonly IDictionary<object, ulong> instances = new Dictionary<object, ulong> ();
        readonly IDictionary<ulong, object> objectIds = new Dictionary<ulong, object> ();
        ulong nextObjectId = 1;
        static ObjectStore instance;

        public static ObjectStore Instance {
            get {
                if (instance == null)
                    instance = new ObjectStore ();
                return instance;
            }
        }

        public static void Clear ()
        {
            var store = Instance;
            store.nextObjectId = 1;
            store.objectIds.Clear ();
            store.instances.Clear ();
        }

        /// <summary>
        /// Register an instance with the object store, associating a unique object
        /// identifier with the instance that can be passed to clients.
        /// If the instance has already been added, this just returns it's object identifier.
        /// A null is not an instance: it belongs to the position a value sits in.
        /// </summary>
        public ulong AddInstance (object obj)
        {
            if (obj == null)
                throw new ArgumentNullException (nameof (obj));
            ulong existingId;
            if (instances.TryGetValue (obj, out existingId))
                return existingId;
            var objectId = nextObjectId;
            nextObjectId++;
            instances [obj] = objectId;
            objectIds [objectId] = obj;
            return objectId;
        }

        /// <summary>
        /// Remove an instance from the object store.
        /// Note: this doesn't destroy the instance, just removes the reference to it.
        /// </summary>
        public void RemoveInstance (object obj)
        {
            if (obj == null)
                return;
            ulong objectId;
            if (instances.TryGetValue (obj, out objectId)) {
                instances.Remove (obj);
                objectIds.Remove (objectId);
            }
        }

        /// <summary>
        /// The number of instances in the store.
        /// </summary>
        public int Count {
            get { return instances.Count; }
        }

        /// <summary>
        /// Remove every instance whose game object no longer exists, and return how many
        /// were removed. Instances that do not implement <see cref="IGameObjectState"/>
        /// are kept, as are those reporting anything but
        /// <see cref="GameObjectState.Destroyed"/>.
        /// </summary>
        public int Sweep ()
        {
            List<object> dead = null;
            foreach (var entry in instances) {
                var instance = entry.Key as IGameObjectState;
                if (instance == null)
                    continue;
                GameObjectState state;
                try {
                    state = instance.GameObjectState;
                } catch (Exception e) {
                    // One pass covers the whole store, so an instance that throws must not
                    // stop the rest being checked. Keep it: an instance wrongly kept costs
                    // memory until the next sweep, where one wrongly removed is gone
                    Logger.WriteLine (
                        "Failed to determine whether an instance in the object store still " +
                        "exists, so it was kept: " + e.Message, Logger.Severity.Error);
                    continue;
                }
                if (state != GameObjectState.Destroyed)
                    continue;
                if (dead == null)
                    dead = new List<object> ();
                dead.Add (entry.Key);
            }
            if (dead == null)
                return 0;
            foreach (var obj in dead)
                RemoveInstance (obj);
            return dead.Count;
        }

        /// <summary>
        /// Get an instance by it's unique object identifier.
        /// </summary>
        public object GetInstance (ulong id)
        {
            // Identifiers are allocated from 1, so 0 was never issued
            if (id == 0ul)
                throw new ArgumentException ("0 is not an object identifier");
            object result;
            if (objectIds.TryGetValue (id, out result))
                return result;
            // Identifiers are allocated in sequence and never reused, so an identifier below
            // the next one to be allocated was issued and has since been removed. The object
            // it referred to is gone
            if (id < nextObjectId)
                throw new global::KRPC.Service.KRPC.ObjectDestroyedException (
                    "The object with id " + id + " no longer exists, " +
                    "as the game object it referred to was destroyed");
            throw new ArgumentException ("Instance not found");
        }

        /// <summary>
        /// Get the object identifier for a given instance.
        /// </summary>
        public ulong GetObjectId (object obj)
        {
            if (obj == null)
                throw new ArgumentNullException (nameof (obj));
            ulong id;
            if (!instances.TryGetValue (obj, out id))
                throw new ArgumentException ("Instance not found");
            return id;
        }
    }
}

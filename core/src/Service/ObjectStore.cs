using System;
using System.Collections.Generic;

namespace KRPC.Service
{
    sealed class ObjectStore
    {
        readonly IDictionary<object, ulong> instances = new Dictionary<object, ulong> ();
        readonly IDictionary<ulong, object> objectIds = new Dictionary<ulong, object> ();
        // Note: 0 is reserved to represent null values
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
        /// </summary>
        public ulong AddInstance (object obj)
        {
            if (obj == null)
                return 0;
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
        /// Get an instance by it's unique object identifier.
        /// </summary>
        public object GetInstance (ulong id)
        {
            if (id == 0ul)
                return null;
            object result;
            if (!objectIds.TryGetValue (id, out result))
                throw new ArgumentException ("Instance not found");
            return result;
        }

        /// <summary>
        /// Get the object identifier for a given instance.
        /// </summary>
        public ulong GetObjectId (object obj)
        {
            if (obj == null)
                return 0;
            ulong id;
            if (!instances.TryGetValue (obj, out id))
                throw new ArgumentException ("Instance not found");
            return id;
        }
    }
}

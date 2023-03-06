using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
            if (instances.ContainsKey (obj))
                return instances [obj];
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
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void RemoveInstance (object obj)
        {
            if (obj == null)
                return;
            if (instances.ContainsKey (obj)) {
                var objectId = instances [obj];
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
            if (!objectIds.ContainsKey (id))
                throw new ArgumentException ("Instance not found");
            return objectIds [id];
        }

        /// <summary>
        /// Get the object identifier for a given instance.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public ulong GetObjectId (object obj)
        {
            if (obj == null)
                return 0;
            if (!instances.ContainsKey (obj))
                throw new ArgumentException ("Instance not found");
            return instances [obj];
        }
    }
}

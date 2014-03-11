using System;
using System.Collections.Generic;

namespace KRPC.Service
{
    class ObjectStore
    {
        readonly IDictionary<object, ulong> instances = new Dictionary<object, ulong> ();
        readonly IDictionary<ulong, object> objectIds = new Dictionary<ulong, object> ();
        ulong nextObjectId = 1; // 0 is reserved to represent null values
        static ObjectStore instance;

        public static ObjectStore Instance {
            get {
                if (instance == null)
                    instance = new ObjectStore ();
                return instance;
            }
        }

        /// <summary>
        /// Register an instance with the object store, associating a unique object
        /// identifier with the instance that can be passed to clients.
        /// If the instance has already been added, this just returns it's object identifier.
        /// </summary>
        public ulong AddInstance (object instance)
        {
            if (instance == null)
                return 0;
            if (instances.ContainsKey (instance))
                return instances [instance];
            var objectId = nextObjectId;
            nextObjectId++;
            instances [instance] = objectId;
            objectIds [objectId] = instance;
            return objectId;
        }

        /// <summary>
        /// Remove an instance from the object store.
        /// Note: this doesn't destroy the instance, just removes the reference to it.
        /// </summary>
        public void RemoveInstance (object instance)
        {
            if (instance == null)
                return;
            if (instances.ContainsKey (instance)) {
                var objectId = instances [instance];
                instances.Remove (instance);
                objectIds.Remove (objectId);
            }
        }

        /// <summary>
        /// Get an instance by it's unique object identifier.
        /// </summary>
        public object GetInstance (ulong objectId)
        {
            if (objectId == 0ul)
                return null;
            if (!objectIds.ContainsKey (objectId))
                throw new ArgumentException ("Instance not found");
            return objectIds [objectId];
        }

        /// <summary>
        /// Get the object identifier for a given instance.
        /// </summary>
        public ulong GetObjectId (object instance)
        {
            if (instance == null)
                return 0;
            if (!instances.ContainsKey (instance))
                throw new ArgumentException ("Instance not found");
            return instances [instance];
        }
    }
}


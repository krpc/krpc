using System;
using System.Collections.Generic;

namespace KRPC.Service
{
    static class ObjectStore
    {
        readonly static IDictionary<object, ulong> instances = new Dictionary<object, ulong> ();
        readonly static IDictionary<ulong, object> objectIds = new Dictionary<ulong, object> ();
        static ulong nextObjectId;

        /// <summary>
        /// Register an instance with the object store, associating a unique object
        /// identifier with the instance that can be passed to clients.
        /// If the instance has already been added, this just returns it's object identifier.
        /// </summary>
        public static ulong AddInstance (object instance)
        {
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
        public static void RemoveInstance (object instance)
        {
            if (instances.ContainsKey (instance)) {
                var objectId = instances [instance];
                instances.Remove (instance);
                objectIds.Remove (objectId);
            }
        }

        /// <summary>
        /// Get an instance by it's unique object identifier.
        /// </summary>
        public static object GetInstance (ulong objectId)
        {
            if (!objectIds.ContainsKey (objectId))
                throw new ArgumentException ("Instance not found");
            return objectIds [objectId];
        }

        /// <summary>
        /// Get the object identifier for a given instance.
        /// </summary>
        public static object GetObjectId (object instance)
        {
            if (!instances.ContainsKey (instance))
                throw new ArgumentException ("Instance not found");
            return instances [instance];
        }
    }
}


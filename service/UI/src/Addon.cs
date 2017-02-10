using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Server;
using KRPC.Service;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// Addon for managing the UI
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class Addon : MonoBehaviour
    {
        static AssetBundle prefabs;

        /// <summary>
        /// Load and instantiate the named prefab, and set its parent game object.
        /// </summary>
        internal static GameObject Instantiate (GameObject parent, string prefabName)
        {
            if (prefabs == null) {
                var dir = System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);
                var path = System.IO.Path.Combine (dir, "KRPC.UI.ksp");
                prefabs = new WWW ("file://" + path).assetBundle;
            }
            var prefab = prefabs.LoadAsset<GameObject> (prefabName);
            var obj = Instantiate (prefab);
            obj.transform.SetParent (parent.transform, false);
            return obj;
        }

        static IDictionary<IClient, IList<Object>> objects = new Dictionary<IClient, IList<Object>> ();

        internal static void Add (Object obj)
        {
            var client = CallContext.Client;
            if (!objects.ContainsKey (client))
                objects [client] = new List<Object> ();
            objects [client].Add (obj);
        }

        internal static void Remove (Object obj)
        {
            var client = CallContext.Client;
            if (!objects.ContainsKey (client) || !objects [client].Contains (obj))
                throw new ArgumentException ("UI object not found");
            obj.Destroy ();
            objects [client].Remove (obj);
        }

        internal static void Clear ()
        {
            foreach (var clientObjects in objects.Values)
                foreach (var obj in clientObjects)
                    obj.Destroy ();
            objects.Clear ();
        }

        internal static void Clear (IClient client)
        {
            if (objects.ContainsKey (client)) {
                foreach (var obj in objects [client])
                    obj.Destroy ();
                objects.Remove (client);
            }
        }

        /// <summary>
        /// Wake the addon
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void Awake ()
        {
        }

        /// <summary>
        /// Update the addon
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void Update ()
        {
            if (!objects.Any ())
                return;

            var disconnectedClients = objects.Keys.Where (x => !x.Connected).ToList ();
            foreach (var client in disconnectedClients)
                Clear (client);
        }

        /// <summary>
        /// Destroy the addon
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void OnDestroy ()
        {
            Clear ();
        }
    }
}

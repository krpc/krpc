using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Server;
using KSP.UI;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.UI
{
    /// <summary>
    /// Addon for managing the UI
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class Addon : MonoBehaviour
    {
        internal static GameObject Canvas {
            get { return UIMasterController.Instance.appCanvas.gameObject; }
        }

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
            var obj = UnityEngine.Object.Instantiate (prefab);
            obj.transform.SetParent (parent.transform, false);
            return obj;
        }

        static IDictionary<IClient, IList<UIObject>> objects = new Dictionary<IClient, IList<UIObject>> ();

        internal static void AddObject (UIObject obj)
        {
            var client = KRPCCore.Context.RPCClient;
            if (!objects.ContainsKey (client))
                objects [client] = new List<UIObject> ();
            objects [client].Add (obj);
        }

        internal static void RemoveObject (UIObject obj)
        {
            var client = KRPCCore.Context.RPCClient;
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
        public void Awake ()
        {
        }

        /// <summary>
        /// Update the addon
        /// </summary>
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
        public void OnDestroy ()
        {
            Clear ();
        }
    }
}

using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// Addon for managing the UI
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class Addon : ClientCleanupAddon
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
                
                // Disable warning as replacement is a UnityAsyncOperation
                // and this works fine.
#pragma warning disable 618
                using (var www = new WWW("file://" + path))
#pragma warning restore 618
                {
                    prefabs = www.assetBundle;
                }
            }
            var prefab = prefabs.LoadAsset<GameObject> (prefabName);
            var obj = Instantiate (prefab);
            obj.transform.SetParent (parent.transform, false);
            return obj;
        }

        static readonly ClientOwnedObjects<Object> objects =
            new ClientOwnedObjects<Object> (obj => obj.Destroy ());

        static readonly IClientOwnedCollection[] collections = { objects };

        /// <summary>
        /// The UI objects.
        /// </summary>
        protected override IEnumerable<IClientOwnedCollection> Collections {
            get { return collections; }
        }

        internal static void Add (Object obj)
        {
            objects.Add (obj);
        }

        internal static void Remove (Object obj)
        {
            if (!objects.OwnedByCaller (obj))
                throw new ArgumentException ("UI object not found");
            obj.Destroy ();
            objects.Remove (obj);
        }

        internal static void Clear (bool clientOnly)
        {
            if (clientOnly)
                objects.Clear (CallContext.Client);
            else
                objects.Clear ();
        }

        /// <summary>
        /// Update the addon: destroy the objects of clients that have disconnected.
        /// </summary>
        public void Update ()
        {
            Sweep ();
        }
    }
}

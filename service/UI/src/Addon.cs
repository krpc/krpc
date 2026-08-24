using System.Collections.Generic;
using KRPC.Service;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// Addon for managing the UI
    /// </summary>
    [KSPAddon (KSPAddon.Startup.AllGameScenes, false)]
    public sealed class Addon : ClientCleanupAddon
    {
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
            objects.RemoveOwnedByCaller (obj, "UI object", x => x.Destroy ());
        }

        internal static void Clear (bool clientOnly)
        {
            if (clientOnly)
                objects.Clear (CallContext.Client);
            else
                objects.Clear ();
        }

        /// <summary>
        /// Update the addon: destroy the objects of clients that have disconnected, and
        /// drop the ones the game no longer has.
        /// </summary>
        public void Update ()
        {
            Sweep ();
            objects.RemoveDestroyed ();
        }
    }
}

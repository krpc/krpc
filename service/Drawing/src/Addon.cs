using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Utils;

namespace KRPC.Drawing
{
    /// <summary>
    /// Addon for doing the drawing
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class Addon : ClientCleanupAddon
    {
        static readonly ClientOwnedObjects<IDrawable> objects =
            new ClientOwnedObjects<IDrawable> (obj => obj.Destroy ());

        static readonly IClientOwnedCollection[] collections = { objects };

        /// <summary>
        /// The drawing objects.
        /// </summary>
        protected override IEnumerable<IClientOwnedCollection> Collections {
            get { return collections; }
        }

        internal static void AddObject (IDrawable obj)
        {
            objects.Add (obj);
        }

        internal static void RemoveObject (IDrawable obj)
        {
            if (!objects.OwnedByCaller (obj))
                throw new ArgumentException ("Drawing object not found");
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
        /// Update the addon: destroy the objects of clients that have disconnected,
        /// and update the rest.
        /// </summary>
        public void Update ()
        {
            Sweep ();
            foreach (var obj in objects.Items)
                obj.Update ();
        }
    }
}

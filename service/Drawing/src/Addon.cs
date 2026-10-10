using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Utils;

namespace KRPC.Drawing
{
    /// <summary>
    /// Addon for doing the drawing
    /// </summary>
    [KSPAddon (KSPAddon.Startup.AllGameScenes, false)]
    public sealed class Addon : ClientCleanupAddon
    {
        static readonly ClientOwnedObjects<IDrawable> objects =
            new ClientOwnedObjects<IDrawable> (obj => obj.Destroy ());

        // Images are kept across scene changes, so they are swept here but are not one of
        // the collections given up on entering a scene
        static readonly ClientOwnedObjects<Image> images =
            new ClientOwnedObjects<Image> (image => image.Destroy ());

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
            objects.RemoveOwnedByCaller (obj, "Drawing object", x => x.Destroy ());
        }

        internal static void AddImage (Image image)
        {
            images.Add (image);
        }

        internal static void RemoveImage (Image image)
        {
            images.RemoveOwnedByCaller (image, "Image", x => x.Destroy ());
        }

        internal static void Clear (bool clientOnly)
        {
            if (clientOnly)
                objects.Clear (CallContext.Client);
            else
                objects.Clear ();
        }

        /// <summary>
        /// Update the addon: destroy the objects of clients that have disconnected, drop
        /// the ones the game no longer has, and update the rest.
        /// </summary>
        public void Update ()
        {
            Sweep ();
            images.Sweep ();
            objects.RemoveDestroyed ();
            View.Update ();
            foreach (var obj in objects.Items)
                obj.Update ();
        }

        /// <summary>
        /// Update the map markers, after the cameras have moved for the frame.
        /// </summary>
        public void LateUpdate ()
        {
            foreach (var marker in objects.Items.OfType<MapMarker> ())
                marker.LateUpdate ();
        }
    }
}

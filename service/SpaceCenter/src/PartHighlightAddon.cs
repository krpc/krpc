using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to highlight parts.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class PartHighlightAddon : ClientCleanupAddon
    {
        static readonly ClientOwnedObjects<Part> highlighted =
            new ClientOwnedObjects<Part> (Unhighlight);

        static readonly IClientOwnedCollection[] collections = { highlighted };

        /// <summary>
        /// The highlighted parts.
        /// </summary>
        protected override IEnumerable<IClientOwnedCollection> Collections {
            get { return collections; }
        }

        /// <summary>
        /// Remove the highlighting of clients that have disconnected. Runs in Update,
        /// rather than FixedUpdate, so highlighting is also removed while the game is
        /// paused.
        /// </summary>
        public void Update ()
        {
            Sweep ();
        }

        static void Unhighlight (Part part)
        {
            if (part == null)
                return;
            part.highlightType = Part.HighlightType.OnMouseOver;
            part.SetHighlight (false, false);
        }

        static internal void Add (Part part)
        {
            highlighted.Add (part);
            part.highlightType = Part.HighlightType.AlwaysOn;
            part.SetHighlight (true, false);
        }

        static internal void Remove (Part part)
        {
            highlighted.Remove (part);
            Unhighlight (part);
        }
    }
}

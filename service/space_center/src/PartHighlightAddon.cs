using System.Collections.Generic;
using KRPC.Server;
using KRPC.Service;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to highlight parts.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class PartHighlightAddon : MonoBehaviour
    {
        static readonly IDictionary<IClient, IList<Part>> highlight = new Dictionary<IClient, IList<Part>> ();

        /// <summary>
        /// Set up the addon
        /// </summary>
        public void Awake ()
        {
            Core.Instance.OnClientDisconnected += (s, e) => Remove (e.Client);
        }

        /// <summary>
        /// Destroy the addon
        /// </summary>
        public void OnDestroy ()
        {
            highlight.Clear ();
        }

        static internal void Add (Part part)
        {
            if (!highlight.ContainsKey (CallContext.Client))
                highlight [CallContext.Client] = new List<Part> ();
            highlight [CallContext.Client].Add (part);
            part.highlightType = Part.HighlightType.AlwaysOn;
            part.SetHighlight (true, false);
        }

        static internal void Remove (Part part)
        {
            if (highlight.ContainsKey (CallContext.Client))
                highlight [CallContext.Client].Remove (part);
            part.highlightType = Part.HighlightType.OnMouseOver;
            part.SetHighlight (false, false);
        }

        static internal void Remove (IClient client)
        {
            if (!highlight.ContainsKey (client))
                return;
            foreach (var part in highlight [client]) {
                part.highlightType = Part.HighlightType.OnMouseOver;
                part.SetHighlight (false, false);
            }
            highlight.Remove (client);
        }
    }
}

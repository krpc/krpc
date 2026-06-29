using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon that manages an <see cref="AutoPilotInfoWindow"/> for each vessel whose auto-pilot has
    /// <see cref="Services.AutoPilot.ShowInfoUI"/> enabled, creating and destroying the windows as the
    /// set of enabled, loaded vessels changes.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class AutoPilotInfoAddon : MonoBehaviour
    {
        readonly IDictionary<Guid, AutoPilotInfoWindow> windows = new Dictionary<Guid, AutoPilotInfoWindow> ();

        /// <summary>
        /// Reconcile the set of windows with the vessels that have the info window enabled.
        /// </summary>
        public void Update ()
        {
            // Create windows for newly-enabled, loaded vessels.
            int index = windows.Count;
            foreach (var vessel in FlightGlobals.Vessels) {
                if (vessel.rootPart == null || !Services.AutoPilot.IsInfoUIEnabled (vessel.id))
                    continue;
                if (windows.ContainsKey (vessel.id))
                    continue;
                var window = gameObject.AddComponent<AutoPilotInfoWindow> ();
                window.VesselId = vessel.id;
                window.Closable = true;
                // Open on the right of the screen, down a little and in from the edge, so the window
                // clears the application-launcher toolbar (top-right) and does not cover the server
                // window's default top-left position. Subsequent windows cascade down and left. The
                // rendered width is the base width scaled by the UI scale, so anchor with that.
                float scale = GameSettings.UI_SCALE;
                float width = 250f * scale;
                float x = Screen.width - width - 60f * scale - 30f * index;
                float y = 80f * scale + 30f * index;
                window.Position = new Rect (x, y, 250f, 0f);
                // Closing the window (via its close button) also clears the flag.
                window.OnHide += (s, e) => Services.AutoPilot.DisableInfoUI (vessel.id);
                window.Visible = true;
                windows [vessel.id] = window;
                index++;
            }

            // Destroy windows for vessels that are no longer enabled or no longer loaded.
            foreach (var id in windows.Keys.ToList ()) {
                if (Services.AutoPilot.IsInfoUIEnabled (id) && IsLoaded (id))
                    continue;
                Destroy (windows [id]);
                windows.Remove (id);
            }
        }

        static bool IsLoaded (Guid id)
        {
            foreach (var vessel in FlightGlobals.Vessels)
                if (vessel.id == id && vessel.rootPart != null)
                    return true;
            return false;
        }

        /// <summary>
        /// Destroy all windows when the addon is destroyed.
        /// </summary>
        public void OnDestroy ()
        {
            foreach (var window in windows.Values)
                Destroy (window);
            windows.Clear ();
        }
    }
}

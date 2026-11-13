using KRPC.Utils;
using UnityEngine;
using MonitorAPI = KRPC.RasterPropMonitor.API.Monitor;
using PageAPI = KRPC.RasterPropMonitor.API.Page;

namespace KRPC.RasterPropMonitor
{
    /// <summary>
    /// A Raster Prop Monitor page and background handler that shows a kRPC display. A page
    /// names it in its <c>PAGEHANDLER</c> and <c>BACKGROUNDHANDLER</c> nodes. With a
    /// <c>display</c> value in those nodes, the page shows the vessel's display of that name,
    /// and otherwise the monitor's own display.
    /// </summary>
    public sealed class KRPCDisplay : InternalModule
    {
        string displayName;
        DisplayState state;

        /// <summary>
        /// Read the handler's configuration. Raster Prop Monitor calls this when it creates
        /// the handler.
        /// </summary>
        public void Configure (ConfigNode node)
        {
            var name = node.GetValue ("display");
            displayName = string.IsNullOrEmpty (name) ? null : name.Trim ();
        }

        // The display, found on first use. RPM creates the handler while it builds the
        // monitor's pages, before it can be matched to its monitor.
        DisplayState State {
            get {
                if (state == null)
                    state = displayName != null
                        ? DisplayState.Of (vessel.id, displayName)
                        : DisplayState.Of (part.flightID, internalProp.propID, MonitorIndex ());
                return state;
            }
        }

        // The place of the monitor whose page uses this handler, among the prop's monitor
        // modules
        int MonitorIndex ()
        {
            int index = 0;
            foreach (var module in internalProp.internalModules) {
                if (!KRPC.RasterPropMonitor.API.IsInstance (MonitorAPI.Type, module))
                    continue;
                foreach (var page in MonitorAPI.Pages (module)) {
                    if (ReferenceEquals (PageAPI.PageHandler (page), this) ||
                        ReferenceEquals (PageAPI.BackgroundHandler (page), this))
                        return index;
                }
                index++;
            }
            return 0;
        }

        /// <summary>
        /// The page text. Raster Prop Monitor formats its variables.
        /// </summary>
        public string Text (int screenWidth, int screenHeight)
        {
            return ModAPI.Get<API> ().IsAvailable ? State.Text : string.Empty;
        }

        /// <summary>
        /// Draw the display's image as the page background, scaled to the screen. Returns
        /// false with no image, so that Raster Prop Monitor clears the screen.
        /// </summary>
        public bool RenderBackground (RenderTexture screen, float aspect)
        {
            var texture = ModAPI.Get<API> ().IsAvailable ? State.Texture : null;
            if (texture == null)
                return false;
            GL.Clear (true, true, Color.black);
            Graphics.DrawTexture (new Rect (0, 0, screen.width, screen.height), texture);
            return true;
        }

        /// <summary>
        /// A press of a monitor button while the page is shown.
        /// </summary>
        public void ButtonClick (int buttonID)
        {
            if (ModAPI.Get<API> ().IsAvailable)
                State.Press (buttonID);
        }

        /// <summary>
        /// A release of a monitor button while the page is shown.
        /// </summary>
        public void ButtonRelease (int buttonID)
        {
            if (ModAPI.Get<API> ().IsAvailable)
                State.Release (buttonID);
        }

        /// <summary>
        /// The page is shown or hidden.
        /// </summary>
        public void PageActive (bool active, int pageNumber)
        {
            if (ModAPI.Get<API> ().IsAvailable)
                State.SetShown (this, active);
        }

        /// <summary>
        /// The IVA is destroyed.
        /// </summary>
        public void OnDestroy ()
        {
            if (state != null)
                state.SetShown (this, false);
        }
    }
}

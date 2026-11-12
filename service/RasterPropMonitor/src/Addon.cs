using KRPC.Utils;

namespace KRPC.RasterPropMonitor
{
    /// <summary>
    /// kRPC Raster Prop Monitor addon.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public sealed class Addon : LoadOnceAddon<Addon>
    {
        /// <summary>
        /// Load the Raster Prop Monitor API.
        /// </summary>
        protected override void Load()
        {
            API.Load();
        }
    }
}

using KRPC.Utils;

namespace KRPC.ScanSat
{
    /// <summary>
    /// kRPC SCANsat addon.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public sealed class Addon : LoadOnceAddon<Addon>
    {
        /// <summary>
        /// Load the SCANsat API.
        /// </summary>
        protected override void Load()
        {
            API.Load();
        }
    }
}

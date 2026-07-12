using KRPC.Utils;

namespace KRPC.LiDAR
{
    /// <summary>
    /// kRPC LiDAR addon.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public sealed class Addon : LoadOnceAddon<Addon>
    {
        /// <summary>
        /// Load the LiDAR API.
        /// </summary>
        protected override void Load()
        {
            API.Load();
        }
    }
}

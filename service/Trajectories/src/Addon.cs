using KRPC.Utils;

namespace KRPC.Trajectories
{
    /// <summary>
    /// kRPC Trajectories addon.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public sealed class Addon : LoadOnceAddon<Addon>
    {
        /// <summary>
        /// Load the Trajectories API.
        /// </summary>
        protected override void Load()
        {
            API.Load();
        }
    }
}

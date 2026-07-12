using KRPC.Utils;

namespace KRPC.DockingCamera
{
    /// <summary>
    /// kRPC Camera addon.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public sealed class Addon : LoadOnceAddon<Addon>
    {
        /// <summary>
        /// Load the Camera API.
        /// </summary>
        protected override void Load()
        {
            API.Load();
        }
    }
}

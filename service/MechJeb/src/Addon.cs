using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// kRPC MechJeb addon.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.AllGameScenes, false)]
    public sealed class Addon : LoadOnceAddon<Addon>
    {
        /// <summary>
        /// Load the MechJeb API.
        /// </summary>
        protected override void Load ()
        {
            MechJeb.Load ();
        }
    }
}

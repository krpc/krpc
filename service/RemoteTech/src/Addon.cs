using KRPC.Utils;

namespace KRPC.RemoteTech
{
    /// <summary>
    /// kRPC RemoteTech addon.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.AllGameScenes, false)]
    public sealed class Addon : LoadOnceAddon<Addon>
    {
        /// <summary>
        /// Load the RemoteTech API.
        /// </summary>
        protected override void Load ()
        {
            API.Load ();
        }
    }
}

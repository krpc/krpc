using KRPC.Utils;

namespace KRPC.KerbalEngineerRedux
{
    /// <summary>
    /// kRPC Kerbal Engineer Redux addon.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public sealed class Addon : LoadOnceAddon<Addon>
    {
        /// <summary>
        /// Load the Kerbal Engineer Redux API.
        /// </summary>
        protected override void Load()
        {
            API.Load();
        }
    }
}

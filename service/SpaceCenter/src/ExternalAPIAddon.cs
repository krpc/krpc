using KRPC.Utils;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon to load external APIs.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.AllGameScenes, false)]
    public sealed class ExternalAPIAddon : LoadOnceAddon<ExternalAPIAddon>
    {
        /// <summary>
        /// Load external APIs. The APIs are resolved purely from the loaded
        /// assemblies, so they do not depend on the flight runtime being active.
        /// </summary>
        protected override void Load ()
        {
            ExternalAPI.AGExt.Load ();
            ExternalAPI.FAR.Load ();
            ExternalAPI.RemoteTech.Load ();
        }
    }
}

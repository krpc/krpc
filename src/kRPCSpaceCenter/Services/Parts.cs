using KRPC.Service.Attributes;

namespace KRPCSpaceCenter.Services
{
    /// <summary>
    /// Class used to manage the individual parts on a vessel.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Parts
    {
        internal Parts (global::Vessel vessel)
        {
        }
    }
}

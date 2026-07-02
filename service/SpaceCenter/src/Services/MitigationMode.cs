using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Controls one of the auto-pilot's individually-toggleable oscillation mitigations
    /// (<see cref="AutoPilot.OscillationBandwidthFloorMode"/>,
    /// <see cref="AutoPilot.OscillationFeedforwardMode"/>,
    /// <see cref="AutoPilot.OscillationOutputFilterMode"/>).
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum MitigationMode
    {
        /// <summary>
        /// The default: the mitigation engages automatically, driven by the runtime oscillation
        /// detector and the hold gate. Rigid vessels are left untouched.
        /// </summary>
        Automatic,
        /// <summary>
        /// The mitigation never engages. The other mitigations are unaffected.
        /// </summary>
        Off,
        /// <summary>
        /// The mitigation is fully engaged at all times, regardless of what the oscillation
        /// detector reports.
        /// </summary>
        Forced
    }
}

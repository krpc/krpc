using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Controls the auto-pilot's rate-feedback filtering for an axis group — the mitigation that
    /// removes a structural oscillation (wobble) from the measured angular velocity before the
    /// control loops consume it.
    /// See <see cref="AutoPilot.PitchYawRateFilterMode"/> and <see cref="AutoPilot.RollRateFilterMode"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum RateFilterMode
    {
        /// <summary>
        /// The default. The auto-pilot detects structural oscillation at runtime, estimates its
        /// frequency and routes it to the appropriate filter: a notch for a low-frequency mode
        /// near the control band, a low-pass for a high-frequency mode, or a broadband low-pass
        /// while the frequency is not yet known. Rigid vessels are left untouched.
        /// </summary>
        Automatic,
        /// <summary>
        /// No rate filtering. The other oscillation mitigations are unaffected.
        /// </summary>
        Off,
        /// <summary>
        /// Force a notch filter at the manually set frequency
        /// (<see cref="AutoPilot.PitchYawOscillationFrequency"/> /
        /// <see cref="AutoPilot.RollOscillationFrequency"/>), for a vessel whose structural mode
        /// is known in advance.
        /// </summary>
        Notch,
        /// <summary>
        /// Force a low-pass filter derived from the manually set frequency.
        /// </summary>
        LowPass
    }
}

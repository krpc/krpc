using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The mode used to control the auto-pilot's suppression of structural oscillation
    /// (the wobble of a structurally flexible vessel). Used by
    /// <see cref="AutoPilot.PitchYawOscillationControl"/> and
    /// <see cref="AutoPilot.RollOscillationControl"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum OscillationControl
    {
        /// <summary>
        /// The auto-pilot automatically detects structural oscillation at runtime, estimates its
        /// frequency, and routes it to the appropriate suppression tool: a notch filter for a
        /// low-frequency mode near the control band, or a second-order low-pass for a
        /// high-frequency mode. This is the default.
        /// </summary>
        Automatic,
        /// <summary>
        /// Oscillation suppression is disabled. The vessel is controlled with full authority,
        /// which gives the most responsive control but allows a structurally flexible vessel
        /// to wobble.
        /// </summary>
        Off,
        /// <summary>
        /// A notch filter is applied unconditionally (without waiting for detection) at the
        /// group's oscillation frequency. Use this when a vessel is known in advance to have a
        /// low-frequency structural mode near the control band.
        /// </summary>
        Notch,
        /// <summary>
        /// A second-order low-pass filter is applied unconditionally (without waiting for
        /// detection) with its corner derived from the group's oscillation frequency. Use this
        /// when a vessel is known in advance to have a high-frequency structural mode well above
        /// the control band.
        /// </summary>
        LowPass
    }
}

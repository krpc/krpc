using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Whether a vehicle can be flown, or why not. See <see cref="Vehicle.ControlState" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum ControlState
    {
        /// <summary>
        /// The vehicle can be flown.
        /// </summary>
        Controllable,
        /// <summary>
        /// The vehicle has no control module, such as a capsule or a probe core.
        /// </summary>
        NoControlModule,
        /// <summary>
        /// The capsules have no crew, and no probe core is on.
        /// </summary>
        NoCrew,
        /// <summary>
        /// Every kitten in the capsules is incapacitated, and no probe core is on.
        /// </summary>
        CrewIncapacitated,
        /// <summary>
        /// The probe core has no power.
        /// </summary>
        ProbeCoreUnpowered,
        /// <summary>
        /// The probe core is switched off.
        /// </summary>
        ProbeCoreOff,
        /// <summary>
        /// The probe core has failed from its radiation dose.
        /// </summary>
        ProbeCoreFailed,
        /// <summary>
        /// The probe core is on standby for a radiation storm.
        /// </summary>
        ProbeCoreStormStandby,
        /// <summary>
        /// The probe core is rebooting after a radiation upset.
        /// </summary>
        ProbeCoreRebooting
    }
}

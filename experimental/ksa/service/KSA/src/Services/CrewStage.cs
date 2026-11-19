using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// What happens to a kitten in a crew event. See <see cref="CrewEvent.Stage" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum CrewStage
    {
        /// <summary>
        /// The supply runs low.
        /// </summary>
        Low,
        /// <summary>
        /// The kitten is weakened, and unable to go on EVA. For radiation, the kitten gets
        /// radiation sickness.
        /// </summary>
        Degraded,
        /// <summary>
        /// The kitten is unable to fly the vehicle or move supplies.
        /// </summary>
        Incapacitated,
        /// <summary>
        /// The kitten dies.
        /// </summary>
        Dead
    }
}

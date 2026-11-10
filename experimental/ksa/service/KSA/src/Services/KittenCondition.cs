using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The effect on a kitten of going without a supply. See <see cref="Kitten.Condition"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum KittenCondition
    {
        /// <summary>
        /// Unaffected.
        /// </summary>
        Healthy,
        /// <summary>
        /// Going without, with no effect yet.
        /// </summary>
        Warning,
        /// <summary>
        /// Weakened, and unable to go on EVA.
        /// </summary>
        Degraded,
        /// <summary>
        /// Unable to fly the vehicle or move supplies.
        /// </summary>
        Incapacitated,
        /// <summary>
        /// Dead.
        /// </summary>
        Dead
    }
}

using KRPC.Service.Attributes;
using System;

namespace KRPC.Trajectories
{
    /// <summary>
    /// The reference for the angle of a descent profile node.
    /// </summary>
    [Serializable]
    [KRPCEnum(Service = "Trajectories")]
    public enum DescentProfileMode
    {
        /// <summary>
        /// The angle of attack, from prograde.
        /// </summary>
        AngleOfAttack,
        /// <summary>
        /// The angle from the horizon.
        /// </summary>
        Horizon
    }
}

using KRPC.Service.Attributes;
using System;

namespace KRPC.Trajectories
{
    /// <summary>
    /// Trajectories descent mode. See <see cref="DescentProfileNode.Mode"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum(Service = "Trajectories")]
    public enum DescentProfileMode
    {
        /// <summary>
        /// Angle of attack mode.
        /// </summary>
        AngleOfAttack,
        /// <summary>
        /// Angle from horizon mode.
        /// </summary>
        Horizon
    }
}

using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of a docking port. See <see cref="DockingPort.State"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum DockingPortState
    {
        /// <summary>
        /// The docking port is ready to dock to another docking port.
        /// </summary>
        Ready,
        /// <summary>
        /// The docking port is docked to another docking port, or docked to
        /// another part (from the VAB/SPH).
        /// </summary>
        Docked,
        /// <summary>
        /// The docking port is very close to another docking port,
        /// but has not docked. It is using magnetic force to acquire a solid dock.
        /// </summary>
        Docking,
        /// <summary>
        /// The docking port has just been undocked from another docking port,
        /// and is disabled until it moves away by a sufficient distance
        /// (<see cref="DockingPort.ReengageDistance"/>).
        /// </summary>
        Undocking,
        /// <summary>
        /// The docking port has a shield, and the shield is closed.
        /// </summary>
        Shielded,
        /// <summary>
        /// The docking ports shield is currently opening/closing.
        /// </summary>
        Moving
    }
}

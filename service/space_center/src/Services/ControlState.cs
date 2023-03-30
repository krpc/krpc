using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The control state of a vessel.
    /// See <see cref="Control.State"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ControlState
    {
        /// <summary>
        /// Full controllable.
        /// </summary>
        Full,
        /// <summary>
        /// Partially controllable.
        /// </summary>
        Partial,
        /// <summary>
        /// Not controllable.
        /// </summary>
        None
    }
}

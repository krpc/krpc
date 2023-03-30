using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// See <see cref="Camera.Mode"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum CameraMode
    {
        /// <summary>
        /// The camera is showing the active vessel, in "auto" mode.
        /// </summary>
        Automatic,
        /// <summary>
        /// The camera is showing the active vessel, in "free" mode.
        /// </summary>
        Free,
        /// <summary>
        /// The camera is showing the active vessel, in "chase" mode.
        /// </summary>
        Chase,
        /// <summary>
        /// The camera is showing the active vessel, in "locked" mode.
        /// </summary>
        Locked,
        /// <summary>
        /// The camera is showing the active vessel, in "orbital" mode.
        /// </summary>
        Orbital,
        /// <summary>
        /// The Intra-Vehicular Activity view is being shown.
        /// </summary>
        IVA,
        /// <summary>
        /// The map view is being shown.
        /// </summary>
        Map
    }
}

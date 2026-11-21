using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The mode of the camera. See <see cref="Camera.Mode" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum CameraMode
    {
        /// <summary>
        /// Orbits the focused object.
        /// </summary>
        Orbit,
        /// <summary>
        /// Flies freely.
        /// </summary>
        Free,
        /// <summary>
        /// The map view.
        /// </summary>
        Map,
        /// <summary>
        /// Inside the vehicle, from a crew seat.
        /// </summary>
        IVA,
        /// <summary>
        /// Fixed to a part of the vehicle, such as a docking port.
        /// </summary>
        Fixed
    }
}

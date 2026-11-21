using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The situation of a vehicle: its contact with the surface, and whether the game is
    /// simulating it with physics or propagating it along its orbit.
    /// See <see cref="Vehicle.Situation" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum Situation
    {
        /// <summary>
        /// No surface contact, simulated with physics.
        /// </summary>
        Maneuvering = 0,
        /// <summary>
        /// No surface contact, on rails.
        /// </summary>
        Freefall = 1,
        /// <summary>
        /// On terrain, simulated with physics.
        /// </summary>
        Rolling = 2,
        /// <summary>
        /// At rest on terrain.
        /// </summary>
        Landed = 3,
        /// <summary>
        /// In the ocean, simulated with physics.
        /// </summary>
        Sailing = 4,
        /// <summary>
        /// At rest in the ocean.
        /// </summary>
        Floating = 5,
        /// <summary>
        /// On terrain and in the ocean, simulated with physics.
        /// </summary>
        Dragging = 6,
        /// <summary>
        /// At rest on terrain and in the ocean.
        /// </summary>
        Bottomed = 7
    }
}

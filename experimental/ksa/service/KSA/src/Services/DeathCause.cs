using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// What killed a kitten. See <see cref="Kitten.CauseOfDeath"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum DeathCause
    {
        /// <summary>
        /// Not recorded.
        /// </summary>
        Unknown,
        /// <summary>
        /// Lack of oxygen.
        /// </summary>
        NoOxygen,
        /// <summary>
        /// Carbon dioxide, with no scrubber canisters left.
        /// </summary>
        NoScrubber,
        /// <summary>
        /// Lack of water.
        /// </summary>
        NoWater,
        /// <summary>
        /// Lack of food.
        /// </summary>
        NoFood,
        /// <summary>
        /// A lethal radiation dose.
        /// </summary>
        Radiation,
        /// <summary>
        /// Hitting the ground.
        /// </summary>
        GroundImpact,
        /// <summary>
        /// Hitting the ocean.
        /// </summary>
        OceanImpact,
        /// <summary>
        /// A collision.
        /// </summary>
        Collision,
        /// <summary>
        /// Excessive acceleration.
        /// </summary>
        ExcessiveGForce,
        /// <summary>
        /// Aerodynamic forces breaking up the vehicle.
        /// </summary>
        AerodynamicForces,
        /// <summary>
        /// Hydrodynamic forces breaking up the vehicle.
        /// </summary>
        HydrodynamicForces,
        /// <summary>
        /// A fall on EVA.
        /// </summary>
        Fall,
        /// <summary>
        /// Left in a vehicle that was abandoned.
        /// </summary>
        Abandoned
    }
}

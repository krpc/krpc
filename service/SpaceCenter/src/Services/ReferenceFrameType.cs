using System;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Type of a reference frame.
    /// </summary>
    [Serializable]
    public enum ReferenceFrameType
    {
        /// <summary>
        /// Reference frame centered on and rotating with a celestial body.
        /// </summary>
        CelestialBody,
        /// <summary>
        /// Reference frame centered on a celestial body with zero angular velocity.
        /// </summary>
        CelestialBodyNonRotating,
        /// <summary>
        /// Centered on a celestial body and oriented with its orbital
        /// directions around the sun.
        /// </summary>
        CelestialBodyOrbital,
        /// <summary>
        /// Centered on and rotating with a vessel.
        /// </summary>
        Vessel,
        /// <summary>
        /// Centered on a vessel and oriented with its orbital directions.
        /// </summary>
        VesselOrbital,
        /// <summary>
        /// Centered on a vessel and oriented with its surface directions.
        /// </summary>
        VesselSurface,
        /// <summary>
        /// Centered on a vessel and oriented with its surface directions,
        /// including rotation of the parent body.
        /// </summary>
        VesselSurfaceVelocity,
        /// <summary>
        /// Centered on a maneuver node and oriented with the burn vector.
        /// </summary>
        Maneuver,
        /// <summary>
        /// Centered on a maneuver node and oriented with the burn vector.
        /// </summary>
        ManeuverOrbital,
        /// <summary>
        /// Centered on and oriented with a part.
        /// </summary>
        Part,
        /// <summary>
        /// Centered on and oriented with a parts center of mass.
        /// </summary>
        PartCenterOfMass,
        /// <summary>
        /// Centered on a docking port node and oriented with the
        /// direction the node points.
        /// </summary>
        DockingPort,
        /// <summary>
        /// Centered on a thruster (e.g. an engine nozzle) and
        /// oriented with the direction of thrust.
        /// </summary>
        Thrust,
        /// <summary>
        /// Centered and rotated by fixed quantities relative to another reference frame.
        /// </summary>
        Relative,
        /// <summary>
        /// Centered and rotated based on quantities from other reference frames.
        /// </summary>
        Hybrid
    }
}

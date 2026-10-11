using KRPC.Service.Attributes;

namespace KRPC.Drawing
{
    /// <summary>
    /// The icon a <see cref="MapMarker" /> is drawn with, from the game's own map icons.
    /// </summary>
    [KRPCEnum (Service = "Drawing")]
    public enum MapMarkerIcon
    {
        /// <summary>
        /// A plain marker.
        /// </summary>
        Generic,
        /// <summary>
        /// A celestial body.
        /// </summary>
        CelestialBody,
        /// <summary>
        /// An apoapsis.
        /// </summary>
        Apoapsis,
        /// <summary>
        /// A periapsis.
        /// </summary>
        Periapsis,
        /// <summary>
        /// An ascending node.
        /// </summary>
        AscendingNode,
        /// <summary>
        /// A descending node.
        /// </summary>
        DescendingNode,
        /// <summary>
        /// A maneuver node.
        /// </summary>
        ManeuverNode,
        /// <summary>
        /// An encounter with a sphere of influence.
        /// </summary>
        Encounter,
        /// <summary>
        /// An escape from a sphere of influence.
        /// </summary>
        Escape,
        /// <summary>
        /// An impact with the surface.
        /// </summary>
        Impact,
        /// <summary>
        /// A closest approach to a target.
        /// </summary>
        CloseApproach,
        /// <summary>
        /// An intersection with the orbit of a target.
        /// </summary>
        Intersect,
        /// <summary>
        /// A ship.
        /// </summary>
        Ship,
        /// <summary>
        /// A probe.
        /// </summary>
        Probe,
        /// <summary>
        /// A relay.
        /// </summary>
        Relay,
        /// <summary>
        /// A station.
        /// </summary>
        Station,
        /// <summary>
        /// A base.
        /// </summary>
        Base,
        /// <summary>
        /// A lander.
        /// </summary>
        Lander,
        /// <summary>
        /// A rover.
        /// </summary>
        Rover,
        /// <summary>
        /// A plane.
        /// </summary>
        Plane,
        /// <summary>
        /// A kerbal on EVA.
        /// </summary>
        EVA,
        /// <summary>
        /// A flag.
        /// </summary>
        Flag,
        /// <summary>
        /// Debris.
        /// </summary>
        Debris,
        /// <summary>
        /// An asteroid or comet.
        /// </summary>
        SpaceObject,
        /// <summary>
        /// Deployed science equipment.
        /// </summary>
        DeployedScience,
        /// <summary>
        /// A launch site.
        /// </summary>
        LaunchSite,
        /// <summary>
        /// A runway.
        /// </summary>
        Runway,
        /// <summary>
        /// The Kerbal Space Center.
        /// </summary>
        KSC
    }
}

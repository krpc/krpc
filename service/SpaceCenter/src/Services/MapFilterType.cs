using KRPC.Service.Attributes;
using System;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// The set of things that are visible in map mode.  These may be combined with bitwise logic.  Note this corresponds directly to KSP's MapViewFiltering.VesselTypeFilter enum.
    /// </summary>
    [Serializable]
    [Flags]
    [KRPCEnum(Service = "SpaceCenter")]
    public enum MapFilterType
    {
        /// <summary>
        /// Everything.
        /// </summary>
        All = MapViewFiltering.VesselTypeFilter.All,
        /// <summary>
        /// Nothing.
        /// </summary>
        None = MapViewFiltering.VesselTypeFilter.None,
        /// <summary>
        /// Debris.
        /// </summary>
        Debris = MapViewFiltering.VesselTypeFilter.Debris,
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = MapViewFiltering.VesselTypeFilter.Unknown,
        /// <summary>
        /// SpaceObjects.
        /// </summary>
        SpaceObjects = MapViewFiltering.VesselTypeFilter.SpaceObjects,
        /// <summary>
        /// Probes.
        /// </summary>
        Probes = MapViewFiltering.VesselTypeFilter.Probes,
        /// <summary>
        /// Rovers.
        /// </summary>
        Rovers = MapViewFiltering.VesselTypeFilter.Rovers,
        /// <summary>
        /// Landers.
        /// </summary>
        Landers = MapViewFiltering.VesselTypeFilter.Landers,
        /// <summary>
        /// Ships.
        /// </summary>
        Ships = MapViewFiltering.VesselTypeFilter.Ships,
        /// <summary>
        /// Stations.
        /// </summary>
        Stations = MapViewFiltering.VesselTypeFilter.Stations,
        /// <summary>
        /// Bases.
        /// </summary>
        Bases = MapViewFiltering.VesselTypeFilter.Bases,
        /// <summary>
        /// EVAs.
        /// </summary>
        EVAs = MapViewFiltering.VesselTypeFilter.EVAs,
        /// <summary>
        /// Flags.
        /// </summary>
        Flags = MapViewFiltering.VesselTypeFilter.Flags,
        /// <summary>
        /// Planes.
        /// </summary>
        Plane = MapViewFiltering.VesselTypeFilter.Plane,
        /// <summary>
        /// Relays.
        /// </summary>
        Relay = MapViewFiltering.VesselTypeFilter.Relay,
        /// <summary>
        /// Launch Sites.
        /// </summary>
        Site = MapViewFiltering.VesselTypeFilter.Site,
        /// <summary>
        /// Deployed Science Controllers.
        /// </summary>
        DeployedScienceController = MapViewFiltering.VesselTypeFilter.DeployedScienceController,
    }
}
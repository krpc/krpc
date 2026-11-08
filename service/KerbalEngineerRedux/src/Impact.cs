using KRPC.Service.Attributes;

namespace KRPC.KerbalEngineerRedux
{
    /// <summary>
    /// The point where the vessel's orbit meets the terrain.
    /// </summary>
    [KRPCStruct (Service = "KerbalEngineerRedux")]
    public struct Impact
    {
        /// <summary>
        /// The universal time of impact, in seconds.
        /// </summary>
        [KRPCProperty]
        public double UT { get; set; }

        /// <summary>
        /// The time until impact, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TimeTo { get; set; }

        /// <summary>
        /// The latitude of the impact site, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude of the impact site, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Longitude { get; set; }

        /// <summary>
        /// The elevation of the terrain at the impact site above sea level, in meters.
        /// </summary>
        /// <remarks>
        /// Zero for an impact in the ocean.
        /// </remarks>
        [KRPCProperty]
        public double Elevation { get; set; }

        /// <summary>
        /// The biome of the impact site.
        /// </summary>
        [KRPCProperty]
        public string Biome { get; set; }
    }
}

using KRPC.Service.Attributes;

namespace KRPC.KerbalEngineerRedux
{
    /// <summary>
    /// The latest burn at full thrust, against the surface velocity, that stops the vessel at
    /// the terrain.
    /// </summary>
    [KRPCStruct (Service = "KerbalEngineerRedux")]
    public struct SuicideBurn
    {
        /// <summary>
        /// The universal time at which the burn starts, in seconds.
        /// </summary>
        [KRPCProperty]
        public double UT { get; set; }

        /// <summary>
        /// The time until the burn starts, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TimeTo { get; set; }

        /// <summary>
        /// The altitude above sea level at which the burn starts, in meters.
        /// </summary>
        [KRPCProperty]
        public double Altitude { get; set; }

        /// <summary>
        /// The delta-v of the burn, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double DeltaV { get; set; }

        /// <summary>
        /// The distance along the orbit to the start of the burn, in meters.
        /// </summary>
        [KRPCProperty]
        public double Distance { get; set; }

        /// <summary>
        /// The duration of the burn, in seconds.
        /// </summary>
        [KRPCProperty]
        public double Duration { get; set; }
    }
}

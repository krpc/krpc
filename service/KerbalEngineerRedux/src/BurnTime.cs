using KRPC.Service.Attributes;

namespace KRPC.KerbalEngineerRedux
{
    /// <summary>
    /// The time to burn a delta-v at full thrust, staging as each stage runs dry.
    /// </summary>
    [KRPCStruct (Service = "KerbalEngineerRedux")]
    public struct BurnTime
    {
        /// <summary>
        /// The duration of the burn, in seconds.
        /// </summary>
        [KRPCProperty]
        public double Duration { get; set; }

        /// <summary>
        /// The time to burn half the delta-v, in seconds.
        /// </summary>
        /// <remarks>
        /// A burn centered on a maneuver node starts this long before it.
        /// </remarks>
        [KRPCProperty]
        public double HalfDuration { get; set; }
    }
}

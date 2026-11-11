using KRPC.Service.Attributes;

namespace KRPC.ScanSat
{
    /// <summary>
    /// An anomaly found by either anomaly scan. See <see cref="ScanSat.Anomalies" />.
    /// </summary>
    [KRPCStruct (Service = "ScanSat")]
    public struct Anomaly
    {
        /// <summary>
        /// The name of the anomaly, or <c>null</c> until the anomaly detail scan covers it.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public string Name { get; set; }

        /// <summary>
        /// The latitude of the anomaly, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude of the anomaly, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Longitude { get; set; }
    }
}

using System;
using KRPC.Service.Attributes;

namespace KRPC.ScanSat
{
    /// <summary>
    /// A set of scan types. Each scan type has its own coverage of a body.
    /// </summary>
    /// <remarks>
    /// The values may be combined.
    /// </remarks>
    [Flags]
    [KRPCEnum (Service = "ScanSat")]
    public enum ScanType
    {
        /// <summary>
        /// No scan types.
        /// </summary>
        None = 0,

        /// <summary>
        /// Low resolution altimetry, the terrain height in steps of 500 meters.
        /// </summary>
        AltimetryLoRes = 1 << 0,

        /// <summary>
        /// High resolution altimetry, the terrain height and slope.
        /// </summary>
        AltimetryHiRes = 1 << 1,

        /// <summary>
        /// Low resolution visual imaging.
        /// </summary>
        VisualLoRes = 1 << 2,

        /// <summary>
        /// Biomes.
        /// </summary>
        Biome = 1 << 3,

        /// <summary>
        /// The positions of anomalies.
        /// </summary>
        Anomaly = 1 << 4,

        /// <summary>
        /// The positions and names of anomalies, from close range.
        /// </summary>
        AnomalyDetail = 1 << 5,

        /// <summary>
        /// High resolution visual imaging.
        /// </summary>
        VisualHiRes = 1 << 6,

        /// <summary>
        /// Low resolution resource abundance, in steps of 5%.
        /// </summary>
        ResourceLoRes = 1 << 7,

        /// <summary>
        /// High resolution resource abundance.
        /// </summary>
        ResourceHiRes = 1 << 8,

        /// <summary>
        /// Both altimetry scans.
        /// </summary>
        Altimetry = AltimetryLoRes | AltimetryHiRes,

        /// <summary>
        /// Both visual scans.
        /// </summary>
        Visual = VisualLoRes | VisualHiRes,

        /// <summary>
        /// Both anomaly scans.
        /// </summary>
        Anomalies = Anomaly | AnomalyDetail,

        /// <summary>
        /// Both resource scans.
        /// </summary>
        Resources = ResourceLoRes | ResourceHiRes,

        /// <summary>
        /// Every scan type.
        /// </summary>
        All = Altimetry | Visual | Biome | Anomalies | Resources
    }
}

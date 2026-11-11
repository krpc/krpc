using KRPC.Service.Attributes;

namespace KRPC.ScanSat
{
    /// <summary>
    /// The type of a map. See <see cref="ScanSat.Map" />.
    /// </summary>
    [KRPCEnum (Service = "ScanSat")]
    public enum MapType
    {
        /// <summary>
        /// Terrain height, from the altimetry scans.
        /// </summary>
        Altimetry,

        /// <summary>
        /// Terrain slope, from the altimetry scans.
        /// </summary>
        Slope,

        /// <summary>
        /// Biomes, from the biome scan, over the terrain height.
        /// </summary>
        Biome,

        /// <summary>
        /// The surface as seen from orbit, from the visual scans.
        /// </summary>
        Visual
    }
}

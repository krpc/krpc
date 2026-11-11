using KRPC.Service.Attributes;

namespace KRPC.ScanSat
{
    /// <summary>
    /// The projection of a map. See <see cref="ScanSat.Map" />.
    /// </summary>
    [KRPCEnum (Service = "ScanSat")]
    public enum MapProjection
    {
        /// <summary>
        /// Equirectangular: longitude and latitude map linearly to the horizontal and vertical.
        /// </summary>
        Rectangular,

        /// <summary>
        /// Kavrayskiy VII, a compromise projection with less distortion at the poles.
        /// </summary>
        KavrayskiyVII,

        /// <summary>
        /// Two polar views side by side, the southern hemisphere on the left.
        /// </summary>
        Polar
    }
}

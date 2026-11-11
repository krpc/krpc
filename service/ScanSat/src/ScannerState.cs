using KRPC.Service.Attributes;

namespace KRPC.ScanSat
{
    /// <summary>
    /// The state of a scanner. See <see cref="Scanner.State" />.
    /// </summary>
    [KRPCEnum (Service = "ScanSat")]
    public enum ScannerState
    {
        /// <summary>
        /// Switched off.
        /// </summary>
        Off,

        /// <summary>
        /// Switched on, and outside its minimum and maximum altitudes.
        /// </summary>
        OutOfRange,

        /// <summary>
        /// Switched on, and paused as it needs light and the ground below it is in darkness.
        /// </summary>
        InDarkness,

        /// <summary>
        /// Scanning between its minimum and best altitudes, with a reduced field of view.
        /// </summary>
        InRange,

        /// <summary>
        /// Scanning between its best and maximum altitudes, with its full field of view.
        /// </summary>
        InBestRange
    }
}

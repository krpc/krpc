using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// The time warp mode.
    /// Returned by <see cref="WarpMode"/>
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum WarpMode
    {
        /// <summary>
        /// Time warp is active, and in regular "on-rails" mode.
        /// </summary>
        Rails,
        /// <summary>
        /// Time warp is active, and in physical time warp mode.
        /// </summary>
        Physics,
        /// <summary>
        /// Time warp is not active.
        /// </summary>
        None
    }
}

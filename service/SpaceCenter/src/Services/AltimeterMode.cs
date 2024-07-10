using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The mode of the altitude reported on the altimeter.
    /// See <see cref="SpaceCenter.AltimeterMode"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum AltimeterMode
    {
        /// <summary>
        /// The altimeter is in the default mode.
        /// </summary>
        DEFAULT,
        /// <summary>
        /// The altimeter is in the "Above Sea Level" mode.
        /// </summary>
        ASL,
        /// <summary>
        /// The altimeter is in the "Above Ground Level" mode.
        /// </summary>
        AGL
    }
}

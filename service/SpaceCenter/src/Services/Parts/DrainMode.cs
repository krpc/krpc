using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Resource drain mode.
    /// See <see cref="ResourceDrain.DrainMode"/>.
    /// </summary>
    [KRPCEnum(Service = "SpaceCenter")]
    [Serializable]
    public enum DrainMode {
        /// <summary>
        /// Drains from the parent part.
        /// </summary>
        Part,
        /// <summary>
        /// Drains from all available parts.
        /// </summary>
        Vessel,
    }
}

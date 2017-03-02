using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The control source of a vessel.
    /// See <see cref="Control.Source"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ControlSource
    {
        /// <summary>
        /// Vessel is controlled by a Kerbal.
        /// </summary>
        Kerbal,
        /// <summary>
        /// Vessel is controlled by a probe core.
        /// </summary>
        Probe,
        /// <summary>
        /// Vessel is not controlled.
        /// </summary>
        None
    }
}

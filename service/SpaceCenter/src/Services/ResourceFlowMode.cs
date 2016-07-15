using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The way in which a resource flows between parts. See <see cref="Resources.FlowMode"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ResourceFlowMode
    {
        /// <summary>
        /// The resource flows to any part in the vessel. For example, electric charge.
        /// </summary>
        Vessel,
        /// <summary>
        /// The resource flows from parts in the first stage, followed by the second,
        /// and so on. For example, mono-propellant.
        /// </summary>
        Stage,
        /// <summary>
        /// The resource flows between adjacent parts within the vessel. For example,
        /// liquid fuel or oxidizer.
        /// </summary>
        Adjacent,
        /// <summary>
        /// The resource does not flow. For example, solid fuel.
        /// </summary>
        None
    }
}

using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of a resource converter. See <see cref="ResourceConverter.State"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ResourceConverterState
    {
        /// <summary>
        /// Converter is running.
        /// </summary>
        Running,
        /// <summary>
        /// Converter is idle.
        /// </summary>
        Idle,
        /// <summary>
        /// Converter is missing a required resource.
        /// </summary>
        MissingResource,
        /// <summary>
        /// No available storage for output resource.
        /// </summary>
        StorageFull,
        /// <summary>
        /// At preset resource capacity.
        /// </summary>
        Capacity,
        /// <summary>
        /// Unknown state. Possible with modified resource converters.
        /// In this case, check <see cref="ResourceConverter.StatusInfo"/> for more information.
        /// </summary>
        Unknown
    }
}

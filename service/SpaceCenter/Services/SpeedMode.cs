using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// See <see cref="Control.SpeedMode"/>.
    /// </summary>
    [KRPCEnum (Service = "SpaceCenter")]
    public enum SpeedMode
    {
        /// <summary>
        /// Speed is relative to the vessel's orbit.
        /// </summary>
        Orbit,
        /// <summary>
        /// Speed is relative to the surface of the body being orbited.
        /// </summary>
        Surface,
        /// <summary>
        /// Speed is relative to the current target.
        /// </summary>
        Target
    }
}

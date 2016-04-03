using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// See <see cref="SolarPanel.State"/>.
    /// </summary>
    [KRPCEnum (Service = "SpaceCenter")]
    public enum SolarPanelState
    {
        /// <summary>
        /// Solar panel is fully extended.
        /// </summary>
        Extended,
        /// <summary>
        /// Solar panel is fully retracted.
        /// </summary>
        Retracted,
        /// <summary>
        /// Solar panel is being extended.
        /// </summary>
        Extending,
        /// <summary>
        /// Solar panel is being retracted.
        /// </summary>
        Retracting,
        /// <summary>
        /// Solar panel is broken.
        /// </summary>
        Broken
    }
}

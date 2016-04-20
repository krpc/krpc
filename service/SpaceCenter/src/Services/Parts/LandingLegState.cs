using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// See <see cref="LandingLeg.State"/>.
    /// </summary>
    [KRPCEnum (Service = "SpaceCenter")]
    public enum LandingLegState
    {
        /// <summary>
        /// Landing leg is fully deployed.
        /// </summary>
        Deployed,
        /// <summary>
        /// Landing leg is fully retracted.
        /// </summary>
        Retracted,
        /// <summary>
        /// Landing leg is being deployed.
        /// </summary>
        Deploying,
        /// <summary>
        /// Landing leg is being retracted.
        /// </summary>
        Retracting,
        /// <summary>
        /// Landing leg is broken.
        /// </summary>
        Broken
    }
}

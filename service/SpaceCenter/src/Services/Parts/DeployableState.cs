using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of a deployable part.
    /// <see cref="Antenna.State"/>, <see cref="CargoBay.State"/>,
    /// <see cref="Leg.State"/>, <see cref="Radiator.State"/>,
    /// <see cref="ResourceHarvester.State"/>, <see cref="SolarPanel.State"/>,
    /// <see cref="Wheel.State"/>
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum DeployableState
    {
        /// <summary>
        /// The part is fully deployed. A cargo bay in this state is fully open.
        /// Parts that cannot be retracted, such as fixed radiators, solar panels,
        /// antennas, landing legs and wheels, are always in this state.
        /// </summary>
        Deployed,
        /// <summary>
        /// The part is fully retracted. A cargo bay in this state is closed and locked.
        /// </summary>
        Retracted,
        /// <summary>
        /// The part is being deployed. A cargo bay in this state is opening.
        /// </summary>
        Deploying,
        /// <summary>
        /// The part is being retracted. A cargo bay in this state is closing.
        /// </summary>
        Retracting,
        /// <summary>
        /// The part is broken. Cargo bays and resource harvesters never report
        /// this state, as the game does not track damage for them.
        /// </summary>
        Broken
    }
}

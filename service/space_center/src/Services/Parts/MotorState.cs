using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of the motor on a powered wheel. See <see cref="Wheel.MotorState"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum(Service = "SpaceCenter")]
    public enum MotorState
    {
        /// <summary>
        /// The motor is idle.
        /// </summary>
        Idle,
        /// <summary>
        /// The motor is running.
        /// </summary>
        Running,
        /// <summary>
        /// The motor is disabled.
        /// </summary>
        Disabled,
        /// <summary>
        /// The motor is inoperable.
        /// </summary>
        Inoperable,
        /// <summary>
        /// The motor does not have enough resources to run.
        /// </summary>
        NotEnoughResources
    }
}

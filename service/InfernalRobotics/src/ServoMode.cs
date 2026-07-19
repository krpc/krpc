using System;
using KRPC.Service.Attributes;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// The mode a servo is operating in. See <see cref="Servo.Mode"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "InfernalRobotics")]
    public enum ServoMode
    {
        /// <summary>
        /// The part acts as a servo, driving towards a target position.
        /// </summary>
        Servo = 1,
        /// <summary>
        /// The part acts as a rotor, spinning continuously.
        /// </summary>
        Rotor = 2
    }
}

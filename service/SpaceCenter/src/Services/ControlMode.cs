using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// See <see cref="Control.InputMode"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ControlInputMode
    {
        /// <summary>
        /// Control inputs are added to the vessels current control inputs.
        /// </summary>
        Additive,
        /// <summary>
        /// Control inputs (when they are non-zero) override the vessels current control inputs.
        /// </summary>
        Override
    }
}

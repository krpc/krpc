using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The state of a parachute. See <see cref="Parachute.State" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum ParachuteState
    {
        /// <summary>
        /// Packed away.
        /// </summary>
        Stowed,
        /// <summary>
        /// Waiting to deploy at <see cref="Parachute.DeployAltitude" />.
        /// </summary>
        Armed,
        /// <summary>
        /// Leaving the pack.
        /// </summary>
        LineStretch,
        /// <summary>
        /// Partly open, to limit the opening load.
        /// </summary>
        Reefed,
        /// <summary>
        /// Opening from reefed to full.
        /// </summary>
        Disreefing,
        /// <summary>
        /// Fully open.
        /// </summary>
        Full,
        /// <summary>
        /// Cut away.
        /// </summary>
        Cut,
        /// <summary>
        /// Torn by too much dynamic pressure.
        /// </summary>
        Ripped,
        /// <summary>
        /// Lost after its lines broke under too much load.
        /// </summary>
        SeveredLines
    }
}

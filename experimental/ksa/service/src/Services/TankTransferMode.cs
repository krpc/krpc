using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The direction a tank transfers propellant in. See <see cref="Tank.TransferMode" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum TankTransferMode
    {
        /// <summary>
        /// No transfer.
        /// </summary>
        None,
        /// <summary>
        /// Into the tank, from tanks transferring out.
        /// </summary>
        In,
        /// <summary>
        /// Out of the tank, to tanks transferring in.
        /// </summary>
        Out
    }
}

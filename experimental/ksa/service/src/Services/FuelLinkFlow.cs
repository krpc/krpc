using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The direction propellant flows through a fuel link. See <see cref="FuelLink.Flow" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum FuelLinkFlow
    {
        /// <summary>
        /// From <see cref="FuelLink.PartA"/> to <see cref="FuelLink.PartB"/>.
        /// </summary>
        AToB,
        /// <summary>
        /// From <see cref="FuelLink.PartB"/> to <see cref="FuelLink.PartA"/>.
        /// </summary>
        BToA,
        /// <summary>
        /// Either way.
        /// </summary>
        Both
    }
}

using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The situation the game's own delta-v readout shows figures for.
    /// See <see cref="Editor.DeltaVSituation"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum DeltaVSituation
    {
        /// <summary>
        /// At sea level on the selected body.
        /// </summary>
        SeaLevel = DeltaVSituationOptions.SeaLevel,
        /// <summary>
        /// At the selected altitude on the selected body.
        /// </summary>
        Altitude = DeltaVSituationOptions.Altitude,
        /// <summary>
        /// In vacuum.
        /// </summary>
        Vacuum = DeltaVSituationOptions.Vaccum,
    }
}

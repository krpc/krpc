using System;
using KRPC.Service.Attributes;
using KSPEditorFacility = EditorFacility;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Editor facility.
    /// See <see cref="LaunchSite.EditorFacility"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum EditorFacility
    {
        /// <summary>
        /// Vehicle Assembly Building.
        /// </summary>
        VAB = KSPEditorFacility.VAB,
        /// <summary>
        /// Space Plane Hanger.
        /// </summary>
        SPH = KSPEditorFacility.SPH,
        /// <summary>
        /// None.
        /// </summary>
        None = KSPEditorFacility.None,
    }
}

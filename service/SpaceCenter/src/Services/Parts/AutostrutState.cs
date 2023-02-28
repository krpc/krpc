using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of a Autostrut. <see cref="Part.AutoStrutMode"/>
    /// </summary>
    [Serializable]
    [KRPCEnum(Service = "SpaceCenter")]
    public enum AutostrutState
    {
        /// <summary>
        /// Off
        /// </summary>
        Off,
        /// <summary>
        /// Root
        /// </summary>
        Root,
        /// <summary>
        /// Heaviest
        /// </summary>
        Heaviest,
        /// <summary>
        /// Grandparent
        /// </summary>
        Grandparent,
        /// <summary>
        /// ForceRoot
        /// </summary>
        ForceRoot,
        /// <summary>
        /// ForceHeaviest
        /// </summary>
        ForceHeaviest,
        /// <summary>
        /// ForceGrandparent
        /// </summary>
        ForceGrandparent
    }
}

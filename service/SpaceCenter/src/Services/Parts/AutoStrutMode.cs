using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of an auto-strut. <see cref="Part.AutoStrutMode"/>
    /// </summary>
    [Serializable]
    [KRPCEnum(Service = "SpaceCenter")]
    public enum AutoStrutMode
    {
        /// <summary>
        /// Off
        /// </summary>
        Off = global::Part.AutoStrutMode.Off,
        /// <summary>
        /// Root
        /// </summary>
        Root = global::Part.AutoStrutMode.Root,
        /// <summary>
        /// Heaviest
        /// </summary>
        Heaviest = global::Part.AutoStrutMode.Heaviest,
        /// <summary>
        /// Grandparent
        /// </summary>
        Grandparent = global::Part.AutoStrutMode.Grandparent,
        /// <summary>
        /// ForceRoot
        /// </summary>
        ForceRoot = global::Part.AutoStrutMode.ForceRoot,
        /// <summary>
        /// ForceHeaviest
        /// </summary>
        ForceHeaviest = global::Part.AutoStrutMode.ForceHeaviest,
        /// <summary>
        /// ForceGrandparent
        /// </summary>
        ForceGrandparent = global::Part.AutoStrutMode.ForceGrandparent
    }
}

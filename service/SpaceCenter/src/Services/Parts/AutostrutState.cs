using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of a radiator. <see cref="RadiatorState"/>
    /// </summary>
    [Serializable]
    [KRPCEnum(Service = "SpaceCenter")]
    public enum AutostrutState
    {
        Off, Root, Heaviest, Grandparent,ForceRoot,ForceHeaviest,ForceGrandparent
    }


}
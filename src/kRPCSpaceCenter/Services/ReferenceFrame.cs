using KRPC.Service.Attributes;

namespace KRPCSpaceCenter.Services
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ReferenceFrame
    {
        Orbital,
        Surface,
        Target,
        Maneuver,
        Docking
    }
}


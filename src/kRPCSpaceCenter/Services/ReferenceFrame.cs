using KRPC.Service.Attributes;

namespace KRPCSpaceCenter.Services
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ReferenceFrame
    {
        Orbital,
        Surface,
        SurfaceVelocity,
        Target,
        TargetVelocity,
        Maneuver,
        Docking
    }
}


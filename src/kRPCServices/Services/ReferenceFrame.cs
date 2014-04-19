using KRPC.Service.Attributes;

namespace KRPCServices.Services
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


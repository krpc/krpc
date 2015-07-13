using KRPC.Service.Attributes;

namespace KRPCKerbalAlarmClock.Services
{
    [KRPCEnum (Service = "KerbalAlarmClock")]
    public enum AlarmType
    {
        Apoapsis,
        AscendingNode,
        Closest,
        Contract,
        ContractAuto,
        Crew,
        DescendingNode,
        Distance,
        EarthTime,
        LaunchRendevous,
        Maneuver,
        ManeuverAuto,
        Periapsis,
        Raw,
        SOIChange,
        SOIChangeAuto,
        Transfer,
        TransferModelled
    }
}

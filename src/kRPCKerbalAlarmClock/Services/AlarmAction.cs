using KRPC.Service.Attributes;

namespace KRPCKerbalAlarmClock.Services
{
    [KRPCEnum (Service = "KerbalAlarmClock")]
    public enum AlarmAction
    {
        DoNothing,
        DoNothingDeleteWhenPassed,
        KillWarp,
        KillWarpOnly,
        MessageOnly,
        PauseGame
    }
}

using System;

namespace KRPC.KerbalAlarmClock.ExtensionMethods
{
    static class AlarmActionExtensions
    {
        public static KRPC.KerbalAlarmClock.Services.AlarmAction ToAlarmAction (this KACWrapper.KACAPI.AlarmActionEnum action)
        {
            switch (action) {
            case KACWrapper.KACAPI.AlarmActionEnum.DoNothing:
                return KRPC.KerbalAlarmClock.Services.AlarmAction.DoNothing;
            case KACWrapper.KACAPI.AlarmActionEnum.DoNothingDeleteWhenPassed:
                return KRPC.KerbalAlarmClock.Services.AlarmAction.DoNothingDeleteWhenPassed;
            case KACWrapper.KACAPI.AlarmActionEnum.KillWarp:
                return KRPC.KerbalAlarmClock.Services.AlarmAction.KillWarp;
            case KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly:
                return KRPC.KerbalAlarmClock.Services.AlarmAction.KillWarpOnly;
            case KACWrapper.KACAPI.AlarmActionEnum.MessageOnly:
                return KRPC.KerbalAlarmClock.Services.AlarmAction.MessageOnly;
            case KACWrapper.KACAPI.AlarmActionEnum.PauseGame:
                return KRPC.KerbalAlarmClock.Services.AlarmAction.PauseGame;
            default:
                throw new ArgumentException ("Unsupported alarm action");
            }
        }

        public static KACWrapper.KACAPI.AlarmActionEnum FromAlarmAction (this KRPC.KerbalAlarmClock.Services.AlarmAction action)
        {
            switch (action) {
            case KRPC.KerbalAlarmClock.Services.AlarmAction.DoNothing:
                return KACWrapper.KACAPI.AlarmActionEnum.DoNothing;
            case KRPC.KerbalAlarmClock.Services.AlarmAction.DoNothingDeleteWhenPassed:
                return KACWrapper.KACAPI.AlarmActionEnum.DoNothingDeleteWhenPassed;
            case KRPC.KerbalAlarmClock.Services.AlarmAction.KillWarp:
                return KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
            case KRPC.KerbalAlarmClock.Services.AlarmAction.KillWarpOnly:
                return KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
            case KRPC.KerbalAlarmClock.Services.AlarmAction.MessageOnly:
                return KACWrapper.KACAPI.AlarmActionEnum.MessageOnly;
            case KRPC.KerbalAlarmClock.Services.AlarmAction.PauseGame:
                return KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
            default:
                throw new ArgumentException ("Unsupported alarm action");
            }
        }
    }
}

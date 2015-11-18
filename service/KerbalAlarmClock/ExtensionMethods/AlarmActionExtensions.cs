using System;

namespace KRPCKerbalAlarmClock.ExtensionMethods
{
    static class AlarmActionExtensions
    {
        public static KRPCKerbalAlarmClock.Services.AlarmAction ToAlarmAction (this KACWrapper.KACAPI.AlarmActionEnum action)
        {
            switch (action) {
            case KACWrapper.KACAPI.AlarmActionEnum.DoNothing:
                return KRPCKerbalAlarmClock.Services.AlarmAction.DoNothing;
            case KACWrapper.KACAPI.AlarmActionEnum.DoNothingDeleteWhenPassed:
                return KRPCKerbalAlarmClock.Services.AlarmAction.DoNothingDeleteWhenPassed;
            case KACWrapper.KACAPI.AlarmActionEnum.KillWarp:
                return KRPCKerbalAlarmClock.Services.AlarmAction.KillWarp;
            case KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly:
                return KRPCKerbalAlarmClock.Services.AlarmAction.KillWarpOnly;
            case KACWrapper.KACAPI.AlarmActionEnum.MessageOnly:
                return KRPCKerbalAlarmClock.Services.AlarmAction.MessageOnly;
            case KACWrapper.KACAPI.AlarmActionEnum.PauseGame:
                return KRPCKerbalAlarmClock.Services.AlarmAction.PauseGame;
            default:
                throw new ArgumentException ("Unsupported alarm action");
            }
        }

        public static KACWrapper.KACAPI.AlarmActionEnum FromAlarmAction (this KRPCKerbalAlarmClock.Services.AlarmAction action)
        {
            switch (action) {
            case KRPCKerbalAlarmClock.Services.AlarmAction.DoNothing:
                return KACWrapper.KACAPI.AlarmActionEnum.DoNothing;
            case KRPCKerbalAlarmClock.Services.AlarmAction.DoNothingDeleteWhenPassed:
                return KACWrapper.KACAPI.AlarmActionEnum.DoNothingDeleteWhenPassed;
            case KRPCKerbalAlarmClock.Services.AlarmAction.KillWarp:
                return KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
            case KRPCKerbalAlarmClock.Services.AlarmAction.KillWarpOnly:
                return KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
            case KRPCKerbalAlarmClock.Services.AlarmAction.MessageOnly:
                return KACWrapper.KACAPI.AlarmActionEnum.MessageOnly;
            case KRPCKerbalAlarmClock.Services.AlarmAction.PauseGame:
                return KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
            default:
                throw new ArgumentException ("Unsupported alarm action");
            }
        }
    }
}

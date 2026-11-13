using System;

namespace KRPC.KerbalAlarmClock.ExtensionMethods
{
    static class AlarmActionExtensions
    {
        public static AlarmAction ToAlarmAction (this API.AlarmActionEnum action)
        {
            switch (action) {
            case API.AlarmActionEnum.DoNothing:
                return AlarmAction.DoNothing;
            case API.AlarmActionEnum.DoNothingDeleteWhenPassed:
                return AlarmAction.DoNothingDeleteWhenPassed;
            case API.AlarmActionEnum.KillWarp:
                return AlarmAction.KillWarp;
            case API.AlarmActionEnum.KillWarpOnly:
                return AlarmAction.KillWarpOnly;
            case API.AlarmActionEnum.MessageOnly:
                return AlarmAction.MessageOnly;
            case API.AlarmActionEnum.PauseGame:
                return AlarmAction.PauseGame;
            case API.AlarmActionEnum.Custom:
                return AlarmAction.Custom;
            case API.AlarmActionEnum.Converted:
                return AlarmAction.Converted;
            default:
                throw new ArgumentException ("Unsupported alarm action");
            }
        }

        public static API.AlarmActionEnum FromAlarmAction (this AlarmAction action)
        {
            switch (action) {
            case AlarmAction.DoNothing:
                return API.AlarmActionEnum.DoNothing;
            case AlarmAction.DoNothingDeleteWhenPassed:
                return API.AlarmActionEnum.DoNothingDeleteWhenPassed;
            case AlarmAction.KillWarp:
                return API.AlarmActionEnum.KillWarp;
            case AlarmAction.KillWarpOnly:
                return API.AlarmActionEnum.KillWarpOnly;
            case AlarmAction.MessageOnly:
                return API.AlarmActionEnum.MessageOnly;
            case AlarmAction.PauseGame:
                return API.AlarmActionEnum.PauseGame;
            case AlarmAction.Custom:
                return API.AlarmActionEnum.Custom;
            case AlarmAction.Converted:
                return API.AlarmActionEnum.Converted;
            default:
                throw new ArgumentException ("Unsupported alarm action");
            }
        }
    }
}

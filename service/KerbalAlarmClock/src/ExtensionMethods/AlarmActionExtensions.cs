using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.KerbalAlarmClock.ExtensionMethods
{
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
    static class AlarmActionExtensions
    {
        public static AlarmAction ToAlarmAction (this KACWrapper.KACAPI.AlarmActionEnum action)
        {
            switch (action) {
            case KACWrapper.KACAPI.AlarmActionEnum.DoNothing:
                return AlarmAction.DoNothing;
            case KACWrapper.KACAPI.AlarmActionEnum.DoNothingDeleteWhenPassed:
                return AlarmAction.DoNothingDeleteWhenPassed;
            case KACWrapper.KACAPI.AlarmActionEnum.KillWarp:
                return AlarmAction.KillWarp;
            case KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly:
                return AlarmAction.KillWarpOnly;
            case KACWrapper.KACAPI.AlarmActionEnum.MessageOnly:
                return AlarmAction.MessageOnly;
            case KACWrapper.KACAPI.AlarmActionEnum.PauseGame:
                return AlarmAction.PauseGame;
            default:
                throw new ArgumentException ("Unsupported alarm action");
            }
        }

        [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        public static KACWrapper.KACAPI.AlarmActionEnum FromAlarmAction (this AlarmAction action)
        {
            switch (action) {
            case AlarmAction.DoNothing:
                return KACWrapper.KACAPI.AlarmActionEnum.DoNothing;
            case AlarmAction.DoNothingDeleteWhenPassed:
                return KACWrapper.KACAPI.AlarmActionEnum.DoNothingDeleteWhenPassed;
            case AlarmAction.KillWarp:
                return KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
            case AlarmAction.KillWarpOnly:
                return KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
            case AlarmAction.MessageOnly:
                return KACWrapper.KACAPI.AlarmActionEnum.MessageOnly;
            case AlarmAction.PauseGame:
                return KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
            default:
                throw new ArgumentException ("Unsupported alarm action");
            }
        }
    }
}

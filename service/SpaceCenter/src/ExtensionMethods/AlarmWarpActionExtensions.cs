using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class AlarmWarpActionExtensions
    {
        public static Services.AlarmWarpAction ToAlarmWarpAction (this AlarmActions.WarpEnum warp)
        {
            switch (warp) {
            case AlarmActions.WarpEnum.DoNothing:
                return Services.AlarmWarpAction.NoChange;
            case AlarmActions.WarpEnum.KillWarp:
                return Services.AlarmWarpAction.StopWarp;
            case AlarmActions.WarpEnum.PauseGame:
                return Services.AlarmWarpAction.PauseGame;
            default:
                throw new ArgumentOutOfRangeException (nameof (warp));
            }
        }

        public static AlarmActions.WarpEnum FromAlarmWarpAction (this Services.AlarmWarpAction warp)
        {
            switch (warp) {
            case Services.AlarmWarpAction.NoChange:
                return AlarmActions.WarpEnum.DoNothing;
            case Services.AlarmWarpAction.StopWarp:
                return AlarmActions.WarpEnum.KillWarp;
            case Services.AlarmWarpAction.PauseGame:
                return AlarmActions.WarpEnum.PauseGame;
            default:
                throw new ArgumentOutOfRangeException (nameof (warp));
            }
        }
    }
}

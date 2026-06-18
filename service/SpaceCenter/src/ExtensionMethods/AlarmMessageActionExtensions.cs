using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class AlarmMessageActionExtensions
    {
        public static Services.AlarmMessageAction ToAlarmMessageAction (this AlarmActions.MessageEnum message)
        {
            switch (message) {
            case AlarmActions.MessageEnum.No:
                return Services.AlarmMessageAction.NoMessage;
            case AlarmActions.MessageEnum.Yes:
                return Services.AlarmMessageAction.Message;
            case AlarmActions.MessageEnum.YesIfOtherVessel:
                return Services.AlarmMessageAction.MessageIfNotActiveVessel;
            default:
                throw new ArgumentOutOfRangeException (nameof (message));
            }
        }

        public static AlarmActions.MessageEnum FromAlarmMessageAction (this Services.AlarmMessageAction message)
        {
            switch (message) {
            case Services.AlarmMessageAction.NoMessage:
                return AlarmActions.MessageEnum.No;
            case Services.AlarmMessageAction.Message:
                return AlarmActions.MessageEnum.Yes;
            case Services.AlarmMessageAction.MessageIfNotActiveVessel:
                return AlarmActions.MessageEnum.YesIfOtherVessel;
            default:
                throw new ArgumentOutOfRangeException (nameof (message));
            }
        }
    }
}

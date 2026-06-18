using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The on-screen message action taken when an alarm fires.
    /// See <see cref="Alarm.MessageAction"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum AlarmMessageAction
    {
        /// <summary>
        /// Do not display a message when the alarm fires.
        /// </summary>
        NoMessage,
        /// <summary>
        /// Display a message when the alarm fires.
        /// </summary>
        Message,
        /// <summary>
        /// Display a message only if the alarm's vessel is not the currently active vessel.
        /// </summary>
        MessageIfNotActiveVessel
    }
}

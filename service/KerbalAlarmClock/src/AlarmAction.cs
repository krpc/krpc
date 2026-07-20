using System;
using KRPC.Service.Attributes;

namespace KRPC.KerbalAlarmClock
{
    /// <summary>
    /// The action performed by an alarm when it fires.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KerbalAlarmClock")]
    public enum AlarmAction
    {
        /// <summary>
        /// Don't do anything at all...
        /// </summary>
        DoNothing,
        /// <summary>
        /// Don't do anything, and delete the alarm.
        /// </summary>
        DoNothingDeleteWhenPassed,
        /// <summary>
        /// Drop out of time warp and display a message.
        /// </summary>
        KillWarp,
        /// <summary>
        /// Drop out of time warp, without displaying a message.
        /// </summary>
        KillWarpOnly,
        /// <summary>
        /// Display a message, without affecting time warp.
        /// </summary>
        MessageOnly,
        /// <summary>
        /// Pause the game and display a message.
        /// </summary>
        PauseGame,
        /// <summary>
        /// A combination of actions configured in the Kerbal Alarm Clock user
        /// interface that does not correspond to any of the other values.
        /// Setting an alarm's action to this value has no effect.
        /// </summary>
        Custom,
        /// <summary>
        /// The alarm was converted from an older version of Kerbal Alarm Clock
        /// and its action has not been set since.
        /// Setting an alarm's action to this value has no effect.
        /// </summary>
        Converted
    }
}

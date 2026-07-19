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
        // TODO: what's the difference between KillWarp and KillWarpOnly?
        /// <summary>
        /// Drop out of time warp.
        /// </summary>
        KillWarp,
        /// <summary>
        /// Drop out of time warp.
        /// </summary>
        KillWarpOnly,
        /// <summary>
        /// Display a message.
        /// </summary>
        MessageOnly,
        /// <summary>
        /// Pause the game.
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

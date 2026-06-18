using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The warp action taken when an alarm fires.
    /// See <see cref="Alarm.WarpAction"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum AlarmWarpAction
    {
        /// <summary>
        /// Do not change time warp when the alarm fires.
        /// </summary>
        NoChange,
        /// <summary>
        /// Drop out of time warp when the alarm fires.
        /// </summary>
        StopWarp,
        /// <summary>
        /// Pause the game when the alarm fires.
        /// </summary>
        PauseGame
    }
}

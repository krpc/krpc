using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// When the auto-pilot's control loop runs within a server update, relative to the calls
    /// the update executes. See <see cref="AutoPilot.UpdateMode"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum AutoPilotUpdateMode
    {
        /// <summary>
        /// The default. The control loop runs once the update has executed the calls it
        /// received, so a target set during a tick is flown on that tick.
        /// </summary>
        AfterCalls,
        /// <summary>
        /// The control loop runs before the update executes any calls, so calls made during a
        /// tick see the attitude error and control output it computed for that tick.
        /// </summary>
        BeforeCalls,
        /// <summary>
        /// The control loop runs only when <see cref="AutoPilot.Update"/> is called, so a
        /// program chooses where among its own calls it runs.
        /// </summary>
        Manual
    }
}

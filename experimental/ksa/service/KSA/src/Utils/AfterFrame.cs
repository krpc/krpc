using System;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.KSA.Utils
{
    /// <summary>
    /// Actions run on the main thread at the end of the frame, where the game's menus load
    /// saves, after the vehicle solver step has been queued. The server runs them.
    /// </summary>
    public static class AfterFrame
    {
        static readonly Queue<Action> actions = new Queue<Action> ();

        /// <summary>
        /// Run an action at the end of the current frame.
        /// </summary>
        public static void Enqueue (Action action)
        {
            actions.Enqueue (action);
        }

        /// <summary>
        /// Run the queued actions. Called by the server at the end of each frame.
        /// </summary>
        public static void RunAll ()
        {
            while (actions.Count > 0) {
                try {
                    actions.Dequeue () ();
                } catch (Exception e) {
                    Logger.WriteLine ("After frame action failed: " + e, Logger.Severity.Error);
                }
            }
        }
    }
}

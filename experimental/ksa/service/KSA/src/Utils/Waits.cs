using System;
using KRPC.Service;

namespace KRPC.KSA.Utils
{
    static class Waits
    {
        /// <summary>
        /// Return once the condition holds, or after the given number of server updates.
        /// Changes queued through the game's input events apply at the start of the next
        /// frame, so a procedure that queues one waits for it with this.
        /// </summary>
        public static void Until (Func<bool> condition, int updates = 10)
        {
            if (updates > 0 && !condition ())
                throw new YieldException<Action> (() => Until (condition, updates - 1));
        }
    }
}

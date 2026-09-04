using System;

namespace KRPC.Client
{
    /// <summary>
    /// Markers the server side function compiler recognizes within a lambda
    /// expression. A program does not call these itself.
    /// </summary>
    public static class Function
    {
        /// <summary>
        /// Start a call to a procedure that pauses execution, such as
        /// <c>SpaceCenter.WarpTo</c>, without waiting for it. The call is started where
        /// it appears, the function carries on, and any value the procedure returns is
        /// discarded.
        /// </summary>
        /// <param name="call">A lambda containing the call to start.</param>
        public static void Defer (Action call)
        {
            throw new InvalidOperationException (
                "Function.Defer marks a call within a lambda compiled into a server " +
                "side function, and cannot be called directly");
        }
    }
}

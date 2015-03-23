using System;

namespace KRPC.Continuations
{
    /// <summary>
    /// Thrown by a continuation to indicate that there is more work to do later,
    /// represented by the new continuation in the exception.
    /// </summary>
    public class YieldException : Exception
    {
        /// <summary>
        /// The continuation to run to continue the work.
        /// </summary>
        public IContinuation Continuation { get; private set; }

        /// <summary>
        /// Create a yield exception, with a continuation
        /// representing the work to do later.
        /// </summary>
        public YieldException (IContinuation continuation)
        {
            Continuation = continuation;
        }
    }
}

using System;

namespace KRPC.Continuations
{
    /// <summary>
    /// Thrown by a continuation to indicate that there is more work to do,
    /// represented by the new continuation in the exception.
    /// </summary>
    public class YieldException : Exception
    {
        public IContinuation Continuation { get; private set; }

        public YieldException (IContinuation continuation)
        {
            Continuation = continuation;
        }
    }
}

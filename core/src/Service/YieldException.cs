using System;
using System.Reflection;

namespace KRPC.Service
{
    /// <summary>
    /// Base class for YieldException.
    /// </summary>
    public class YieldException : Exception {
        /// <summary>
        /// The continuation to run to continue the work.
        /// </summary>
        public object UntypedValue { get; set; }

        /// <summary>
        /// Call the continuation value as a zero-arg delegate, returning its return
        /// value as an object (or null if no return value).
        /// </summary>
        public object CallUntyped()
        {
            var value = UntypedValue;
            var action = value as Action;
            if (action != null)
            {
                action();
                return null;
            }
            else
            {
                try
                {
                    return ((Delegate)value).DynamicInvoke(null);
                }
                catch (TargetInvocationException e)
                {
                    if (e.InnerException is YieldException)
                        throw e.InnerException;
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Thrown by a continuation to indicate that there is more work to do later,
    /// represented by the new continuation in the exception.
    /// </summary>
    public sealed class YieldException<T> : YieldException
    {
        /// <summary>
        /// Create a yield exception, with a continuation representing the work to do later.
        /// </summary>
        public YieldException (T value)
        {
            Value = value;
        }

        /// <summary>
        /// The continuation to run to continue the work.
        /// </summary>
        public T Value {
          get { return (T)UntypedValue;  }
          private set { UntypedValue = value; }
        }
    }
}

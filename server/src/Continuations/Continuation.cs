namespace KRPC.Continuations
{
    /// <summary>
    /// A continuation that returns a result of type T
    /// </summary>
    public abstract class Continuation<T> : IContinuation
    {
        /// <summary>
        /// Run the continuation and return the result.
        /// </summary>
        public abstract T Run ();

        /// <summary>
        /// Run the continuation and return the result.
        /// </summary>
        public object RunUntyped ()
        {
            return Run ();
        }
    }

    /// <summary>
    /// A continuation that does not return a value
    /// </summary>
    public abstract class Continuation : IContinuation
    {
        /// <summary>
        /// Run the continuation and return the result.
        /// </summary>
        public abstract void Run ();

        /// <summary>
        /// Run the continuation and return the result.
        /// </summary>
        public object RunUntyped ()
        {
            Run ();
            return null;
        }
    }
}

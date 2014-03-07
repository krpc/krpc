namespace KRPC.Continuations
{
    /// <summary>
    /// A continuation that returns a result of type T
    /// </summary>
    public abstract class Continuation<T> : IContinuation
    {
        public abstract T Run ();

        public object RunUntyped ()
        {
            return Run ();
        }
    };

    /// <summary>
    /// A continuation that does not return a value
    /// </summary>
    public abstract class Continuation : IContinuation
    {
        public abstract void Run ();

        public object RunUntyped ()
        {
            Run ();
            return null;
        }
    };
}

namespace KRPC.Continuations
{
    /// <summary>
    /// Interface for continuations. Don't inherit from this directly.
    /// Use the type same abstract classes Continuation and Continuation<T>
    /// </summary>
    public interface IContinuation {
        object RunUntyped ();
    };
}

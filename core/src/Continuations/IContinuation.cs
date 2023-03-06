namespace KRPC.Continuations
{
    /// <summary>
    /// A continuation
    /// </summary>
    public interface IContinuation
    {
        /// <summary>
        /// Run the continuation and return the result.
        /// </summary>
        object RunUntyped ();
    }
}

namespace KRPC.Utils
{
    /// <summary>
    /// A collection of in-game state owned by RPC clients, which can release the state
    /// owned by clients that have disconnected.
    /// </summary>
    public interface IClientOwnedCollection
    {
        /// <summary>
        /// Release entries whose owning client has disconnected.
        /// </summary>
        void Sweep ();

        /// <summary>
        /// Release all entries.
        /// </summary>
        void Clear ();
    }
}

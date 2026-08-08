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
        /// Release all entries, whoever owns them.
        /// </summary>
        void Clear ();

        /// <summary>
        /// Take every entry out of the collection, holding them to be released by a later
        /// call to <see cref="ReleaseDetached" />. The collection is empty afterwards, so
        /// an entry added in the meantime is not caught up in the release.
        /// </summary>
        void Detach ();

        /// <summary>
        /// Release the entries taken out by <see cref="Detach" />.
        /// </summary>
        void ReleaseDetached ();
    }
}

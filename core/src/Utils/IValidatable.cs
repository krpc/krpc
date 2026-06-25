namespace KRPC.Utils
{
    /// <summary>
    /// Implemented by service objects that wrap a game object whose lifetime is
    /// controlled by the game (for example a part or a vessel). Allows the object
    /// store to discard objects whose underlying game object no longer exists, for
    /// example after a quickload or scene change.
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// Whether the underlying game object still exists. Must return
        /// <c>false</c> only when the object is definitively gone, not merely
        /// unloaded, so that objects a client legitimately still holds are not
        /// discarded.
        /// </summary>
        bool IsValid { get; }
    }
}

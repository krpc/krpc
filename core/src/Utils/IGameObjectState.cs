namespace KRPC.Utils
{
    /// <summary>
    /// Implemented by an object that stands for something in the game, which the game
    /// can destroy. The object store drops objects that report themselves destroyed; an
    /// object that does not implement this interface is kept until the server stops.
    /// </summary>
    public interface IGameObjectState
    {
        /// <summary>
        /// What the game holds for the thing this object stands for.
        /// </summary>
        /// <remarks>
        /// <see cref="GameObjectState.Destroyed" /> means the thing is definitively gone
        /// and the object can never work again. Anything less certain, including a thing
        /// the game has unloaded but can build again, is dormant: an object wrongly
        /// dropped cannot be recovered, whereas one wrongly kept only costs memory until
        /// it is checked again. Implementations must not throw, as this is called while
        /// sweeping the whole object store.
        /// </remarks>
        GameObjectState GameObjectState { get; }
    }
}

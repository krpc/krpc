namespace KRPC.Utils
{
    /// <summary>
    /// Implemented by an object that identifies something in the game, which the game
    /// can destroy. The object store drops objects that report themselves destroyed; an
    /// object that does not implement this interface is kept until the server stops.
    /// </summary>
    public interface IGameObjectState
    {
        /// <summary>
        /// The state of the game object this identifies.
        /// </summary>
        /// <remarks>
        /// <see cref="GameObjectState.Destroyed" /> means the game object is definitively
        /// gone and the object can never work again. Every less certain case is dormant,
        /// including a game object the game has unloaded and can build again: an object
        /// wrongly dropped cannot be recovered, whereas one wrongly kept only costs memory
        /// until it is checked again. Implementations must not throw, as this is called
        /// while sweeping the whole object store.
        /// </remarks>
        GameObjectState GameObjectState { get; }
    }
}

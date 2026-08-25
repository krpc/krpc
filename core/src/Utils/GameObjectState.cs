namespace KRPC.Utils
{
    /// <summary>
    /// The state of the game object that a service object identifies.
    /// </summary>
    /// <remarks>
    /// The values are ordered from most to least alive, so that an object built on
    /// several others can combine their states with <see cref="GameObjectStates" />.
    /// </remarks>
    public enum GameObjectState
    {
        /// <summary>
        /// The game object exists and can be used.
        /// </summary>
        Live,
        /// <summary>
        /// The game object does not exist, and the game holds what it needs to build it
        /// again. What the service object identifies is intact.
        /// </summary>
        Dormant,
        /// <summary>
        /// The game object is gone. It can never be used again.
        /// </summary>
        Destroyed
    }

    /// <summary>
    /// Combines the states of the objects a service object is built on.
    /// </summary>
    public static class GameObjectStates
    {
        /// <summary>
        /// The state of an object that needs both of the objects it is built on, which is
        /// the less alive of their two states.
        /// </summary>
        public static GameObjectState LeastAlive (this GameObjectState state, GameObjectState other)
        {
            return state > other ? state : other;
        }

        /// <summary>
        /// The state of an object that either of the objects it is built on is enough for,
        /// which is the more alive of their two states.
        /// </summary>
        public static GameObjectState MostAlive (this GameObjectState state, GameObjectState other)
        {
            return state < other ? state : other;
        }
    }
}

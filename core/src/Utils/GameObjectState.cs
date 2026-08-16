namespace KRPC.Utils
{
    /// <summary>
    /// What the game holds for the thing that a service object stands for.
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
        /// The game object does not exist, but the game still holds what it needs to
        /// build it again, so the thing the service object stands for is intact.
        /// </summary>
        Dormant,
        /// <summary>
        /// The game holds nothing for it. It can never be used again.
        /// </summary>
        Destroyed
    }

    /// <summary>
    /// Combines the states of the things a service object is built on.
    /// </summary>
    public static class GameObjectStates
    {
        /// <summary>
        /// The state of an object that needs both of the things it is built on, which is
        /// the less alive of their two states.
        /// </summary>
        public static GameObjectState LeastAlive (this GameObjectState state, GameObjectState other)
        {
            return state > other ? state : other;
        }

        /// <summary>
        /// The state of an object that either of the things it is built on is enough for,
        /// which is the more alive of their two states.
        /// </summary>
        public static GameObjectState MostAlive (this GameObjectState state, GameObjectState other)
        {
            return state < other ? state : other;
        }
    }
}

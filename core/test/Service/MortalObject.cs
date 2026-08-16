using System;
using KRPC.Utils;

namespace KRPC.Test.Service
{
    /// <summary>
    /// An object standing for a game object that the test can destroy.
    /// </summary>
    sealed class MortalObject : IGameObjectState
    {
        /// <summary>
        /// Whether reading the state raises, which the interface forbids and the store
        /// has to survive anyway.
        /// </summary>
        public bool StateThrows { get; set; }

        GameObjectState state = GameObjectState.Live;

        public GameObjectState GameObjectState {
            get {
                if (StateThrows)
                    throw new InvalidOperationException ("the game object cannot be read");
                return state;
            }
            set { state = value; }
        }
    }
}

using System;
using KRPC.Server;

namespace KRPC.Service
{
    /// <summary>
    /// Stores the context in which an RPC is executed.
    /// For example, used by an RPC to find out which client made the call.
    /// </summary>
    public static class CallContext
    {
        /// <summary>
        /// The current client
        /// </summary>
        public static IClient Client { get; private set; }

        /// <summary>
        /// The current game scene
        /// </summary>
        public static GameScene GameScene { get; set; }

        /// <summary>
        /// Delegate used to check whether the game is paused
        /// </summary>
        public static Func<bool> IsPaused { get; set; }

        /// <summary>
        /// Delegate used to pause the game
        /// </summary>
        public static Action Pause { get; set; }

        /// <summary>
        /// Delegate used to unpause the game
        /// </summary>
        public static Action Unpause { get; set; }

        /// <summary>
        /// A counter incremented each time the game state is (re)loaded, for example
        /// after a quickload or loading a save. Monotonic for the lifetime of the
        /// server process. Provides a boundary that consumers can use to detect that
        /// previously returned objects may now refer to a replaced game state.
        /// </summary>
        public static ulong StateGeneration { get; private set; }

        /// <summary>
        /// Raised when the game state has been (re)loaded, after
        /// <see cref="StateGeneration"/> has been incremented. Used to discard objects
        /// that reference the previous game state.
        /// </summary>
        public static event Action GameStateLoaded;

        /// <summary>
        /// Notify that the game state has been (re)loaded. Increments
        /// <see cref="StateGeneration"/> and raises <see cref="GameStateLoaded"/>.
        /// Should be called when the game loads or replaces the current game state.
        /// </summary>
        public static void NotifyGameStateLoaded ()
        {
            StateGeneration++;
            var handler = GameStateLoaded;
            if (handler != null)
                handler ();
        }

        internal static void Set (IClient client)
        {
            Client = client;
        }

        internal static void Clear ()
        {
            Client = null;
        }
    }
}

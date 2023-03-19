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

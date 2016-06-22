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
        public static GameScene GameScene { get; private set; }

        internal static void Set (IClient client)
        {
            Client = client;
        }

        internal static void Clear ()
        {
            Client = null;
        }

        internal static void SetGameScene (GameScene gameScene)
        {
            GameScene = gameScene;
        }
    }
}

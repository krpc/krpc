using System.Collections.Generic;
using KRPC.Server;
using UnityEngine;

namespace KRPC.Utils
{
    /// <summary>
    /// Connectivity checks for clients, cached per rendered frame so that many
    /// collections sweeping many entries do not repeatedly poll the same socket.
    /// </summary>
    public static class ClientConnections
    {
        static readonly IDictionary<IClient, bool> cache = new Dictionary<IClient, bool> ();
        static int cachedFrame = -1;

        /// <summary>
        /// Whether the given client has disconnected. A null client (for state set
        /// from in-game code rather than by an RPC) is never considered disconnected.
        /// </summary>
        public static bool Disconnected (IClient client)
        {
            if (client == null)
                return false;
            var frame = Time.frameCount;
            if (frame != cachedFrame) {
                cache.Clear ();
                cachedFrame = frame;
            }
            bool connected;
            if (!cache.TryGetValue (client, out connected)) {
                connected = client.Connected;
                cache [client] = connected;
            }
            return !connected;
        }
    }
}

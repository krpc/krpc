using System.Runtime.CompilerServices;
using KSA;

namespace KRPC.KSA.Utils
{
    /// <summary>
    /// The tank transfer modes set by clients. The game runs a tank's transfer only while its
    /// part window is open, and resets the mode otherwise. The server patches the game's
    /// window check to call <see cref="IsPartWindowOpen"/>, so a mode set by a client holds.
    /// </summary>
    public static class TankTransfers
    {
        /// <summary>
        /// Whether the server patched the game's window checks. Set before the server starts.
        /// </summary>
        public static bool Patched { get; set; }

        // The vehicle solver reads the modes on worker threads
        static readonly ConditionalWeakTable<Tank, object> held = new ConditionalWeakTable<Tank, object> ();

        /// <summary>
        /// Hold a transfer mode set by a client, or release the tank for None.
        /// </summary>
        public static void Hold (Tank tank, TankTransferMode mode)
        {
            if (mode == TankTransferMode.None)
                held.Remove (tank);
            else
                held.AddOrUpdate (tank, mode);
        }

        /// <summary>
        /// Whether the part window is open, or the part holds a tank in the transfer mode a
        /// client set. A mode the player changes since is not held.
        /// </summary>
        public static bool IsPartWindowOpen (Part part)
        {
            if (part.IsPartWindowOpen)
                return true;
            foreach (var tank in part.SubtreeModules.Get<Tank> ()) {
                if (held.TryGetValue (tank, out var mode) && (TankTransferMode)mode == tank.TransferMode)
                    return true;
            }
            return false;
        }
    }
}

using System;
using System.Runtime.CompilerServices;
using KRPC.Service;
using KSA;

namespace KRPC.KSA.Utils
{
    /// <summary>
    /// Removes service objects for game objects that no longer exist from the object store.
    /// </summary>
    public static class ObjectSweep
    {
        static int lastSignature;

        /// <summary>
        /// Sweep the object store if the vehicles or their parts have changed since the last
        /// call, or a save has loaded. Called by the server before each update.
        /// </summary>
        public static void Update ()
        {
            var signature = Signature ();
            if (signature == lastSignature && !GameState.SweepPending)
                return;
            lastSignature = signature;
            GameState.Sweep ();
        }

        // Changes when a vehicle is added or removed, a save replaces the vehicles, or a
        // vehicle gains or loses parts
        static int Signature ()
        {
            var system = Universe.CurrentSystem;
            if (system == null)
                return 0;
            var hash = new HashCode ();
            foreach (var astronomical in system.All.AsSpan ()) {
                if (astronomical is not global::KSA.Vehicle vehicle)
                    continue;
                hash.Add (RuntimeHelpers.GetHashCode (vehicle));
                hash.Add (vehicle.Parts.Count);
            }
            return hash.ToHashCode ();
        }
    }
}

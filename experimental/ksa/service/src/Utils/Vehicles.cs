using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Utils
{
    static class Vehicles
    {
        static CelestialSystem System {
            get {
                var system = Universe.CurrentSystem;
                if (system == null)
                    throw new InvalidOperationException ("No game is loaded");
                return system;
            }
        }

        /// <summary>
        /// The vehicles in the current system, excluding EVA kittens.
        /// </summary>
        public static List<Vehicle> All {
            get {
                var result = new List<Vehicle> ();
                var enumerator = System.All.OfType<Vehicle> ().GetEnumerator ();
                while (enumerator.MoveNext ()) {
                    if (enumerator.Current is not KittenEva)
                        result.Add (enumerator.Current);
                }
                return result;
            }
        }

        /// <summary>
        /// Return the vehicles that are not in <paramref name="before"/>, once the given
        /// number of server updates have passed.
        /// </summary>
        public static IList<Services.Vehicle> Separated (HashSet<Vehicle> before, int updates)
        {
            if (updates > 0)
                throw new YieldException<Func<IList<Services.Vehicle>>> (() => Separated (before, updates - 1));
            return All.Where (x => !before.Contains (x)).Select (x => new Services.Vehicle (x)).ToList ();
        }

        /// <summary>
        /// Return the vehicles that are not in <paramref name="before"/>, once the condition
        /// holds and a new vehicle exists, or after the given number of server updates.
        /// </summary>
        public static IList<Services.Vehicle> SeparatedWhen (Func<bool> condition, HashSet<Vehicle> before, int updates = 10)
        {
            var separated = All.Where (x => !before.Contains (x)).Select (x => new Services.Vehicle (x)).ToList ();
            if (updates > 0 && (!condition () || separated.Count == 0))
                throw new YieldException<Func<IList<Services.Vehicle>>> (() => SeparatedWhen (condition, before, updates - 1));
            return separated;
        }

        /// <summary>
        /// The bodies in the current system: planets, moons and stars.
        /// </summary>
        public static List<IParentBody> Bodies {
            get {
                var result = new List<IParentBody> ();
                var enumerator = System.All.OfType<IParentBody> ().GetEnumerator ();
                while (enumerator.MoveNext ()) {
                    if (enumerator.Current is not Barycenter)
                        result.Add (enumerator.Current);
                }
                return result;
            }
        }

        /// <summary>
        /// Live if the vehicle or body is in the current system, and destroyed otherwise.
        /// </summary>
        public static GameObjectState StateOf (object astronomical)
        {
            return astronomical != null && Exists (astronomical) ? GameObjectState.Live : GameObjectState.Destroyed;
        }

        /// <summary>
        /// Whether the vehicle or body is still in the current system. Docking, undocking and
        /// destruction remove the vehicle object.
        /// </summary>
        public static bool Exists (object astronomical)
        {
            var system = Universe.CurrentSystem;
            if (system == null)
                return false;
            foreach (var other in system.All.AsSpan ()) {
                if (ReferenceEquals (other, astronomical))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Rename the vehicle, and register it in the system under its new name. Returns
        /// false if the name is in use.
        /// </summary>
        public static bool Rename (Vehicle vehicle, string name)
        {
            // Checked here, as the game's rename also shows an alert when the name is taken
            if (System.All.TryGet (name, out Astronomical _))
                return false;
            return System.Rename (vehicle, name);
        }
    }
}

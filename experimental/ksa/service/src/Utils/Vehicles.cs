using System;
using System.Collections.Generic;
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
        /// Whether the vehicle is still in the current system. Docking, undocking and
        /// destruction remove the vehicle object.
        /// </summary>
        public static bool Exists (Vehicle vehicle)
        {
            var system = Universe.CurrentSystem;
            if (system == null)
                return false;
            foreach (var astronomical in system.All.AsSpan ()) {
                if (ReferenceEquals (astronomical, vehicle))
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

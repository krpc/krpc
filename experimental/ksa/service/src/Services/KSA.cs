using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.KSA.Utils;
using KRPC.Service;
using KRPC.Service.Attributes;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Provides functionality to interact with Kitten Space Agency.
    /// </summary>
    [KRPCService]
    public static class KSA
    {
        /// <summary>
        /// The vehicle under the player's control.
        /// </summary>
        /// <remarks>
        /// Setting it moves the camera to the vehicle, as the switch vehicle key does.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public static Vehicle ActiveVehicle {
            get {
                var vehicle = Program.ControlledVehicle;
                return vehicle == null ? null : new Vehicle (vehicle);
            }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException (nameof (ActiveVehicle));
                Universe.FollowInHoveredViewport (value.InternalVehicle);
            }
        }

        /// <summary>
        /// The vehicles in the current system, excluding kittens on EVA.
        /// </summary>
        [KRPCProperty]
        public static IList<Vehicle> Vehicles {
            get { return Utils.Vehicles.All.Select (vehicle => new Vehicle (vehicle)).ToList (); }
        }

        /// <summary>
        /// The planets, moons and stars in the current system, keyed by name.
        /// </summary>
        [KRPCProperty]
        public static IDictionary<string, Body> Bodies {
            get {
                var bodies = new Dictionary<string, Body> ();
                foreach (var body in Utils.Vehicles.Bodies)
                    bodies [body.Id] = new Body (body);
                return bodies;
            }
        }

        /// <summary>
        /// The universal time, in seconds.
        /// </summary>
        [KRPCProperty]
        public static double UT {
            get { return Universe.GetElapsedSeconds (); }
        }

        /// <summary>
        /// The simulation speed, as a multiple of real time. Zero when paused.
        /// </summary>
        [KRPCProperty]
        public static double SimulationSpeed {
            get { return Universe.SimulationSpeed; }
            set {
                if (!(value >= 0))
                    throw new ArgumentOutOfRangeException (nameof (SimulationSpeed));
                Universe.SetSimulationSpeed (value, alert: false);
            }
        }

        /// <summary>
        /// Warp to the given universal time, and return once the warp has finished.
        /// The game slows the warp down to normal speed toward the end.
        /// </summary>
        /// <param name="ut">The universal time to warp to, in seconds.</param>
        /// <remarks>
        /// Does nothing when the time is in the past. Returns early if the game stops the
        /// warp, for example when its menu pauses the game.
        /// </remarks>
        [KRPCProcedure]
        public static void WarpTo (double ut)
        {
            var seconds = ut - Universe.GetElapsedSeconds ();
            if (!(seconds > 0))
                return;
            Universe.AutoWarpTo (Universe.GetElapsedTime () + seconds);
            WaitForWarp ();
        }

        static void WaitForWarp ()
        {
            if (Universe.IsAutoWarpActive)
                throw new YieldException<Action> (WaitForWarp);
        }
    }
}

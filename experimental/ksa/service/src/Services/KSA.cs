using System;
using System.Collections.Generic;
using System.Linq;
using Brutal.Numerics;
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

        /// <summary>
        /// Launch a vehicle from a launch site, and make it the active vehicle. Returns once
        /// the vehicle is on the launch site.
        /// </summary>
        /// <param name="name">The name of the vehicle, one of the game's default vehicles or
        /// a vehicle saved by the player.</param>
        /// <param name="site">The name of the launch site. See <see cref="Body.LaunchSites"/>.</param>
        /// <returns>The vehicle. Its name is the vehicle's name, with a number appended if
        /// another vehicle has that name.</returns>
        /// <remarks>
        /// Throws an exception if there is no such vehicle or launch site.
        /// </remarks>
        [KRPCProcedure]
        public static Vehicle LaunchVehicle (string name, string site)
        {
            var save = DefaultVehicleSaves.FindSave (name);
            if (save == null) {
                foreach (var playerSave in VehicleSaves.AsSpan ()) {
                    if (playerSave.Id == name)
                        save = playerSave;
                }
            }
            if (save == null)
                throw new ArgumentException ("No vehicle named " + name);
            Celestial celestial = null;
            LocationReference location = null;
            foreach (var body in Utils.Vehicles.Bodies) {
                if (body is Celestial candidate && candidate.GetLocation (site) is LandmarkReference { IsLaunchPad: true } landmark) {
                    celestial = candidate;
                    location = landmark;
                }
            }
            if (location == null)
                throw new ArgumentException ("No launch site named " + site);
            var tree = save.Load (Program.MainViewport) ?? throw new InvalidOperationException ("Failed to load vehicle " + name);
            tree.ApplyTankAssignments ();
            var id = name;
            for (var i = 1; Universe.CurrentSystem.All.TryGet (id, out Astronomical _); i++)
                id = name + "_" + i;
            CrewAssignmentWindow.ValidateSeatAssignments (tree, id, hasLaunched: false);
            // As the game's launch menu places a vehicle
            var mass = tree.ComputeInertMassPropertiesAsmb () + tree.ComputePropellantMassPropertiesAsmb ();
            var (min, max) = tree.ComputeBoundingBoxAsmb ();
            var state = global::KSA.Vehicle.GetInitialKinematicStateForLocation (
                celestial, Universe.GetNextSimStep ().NextTime, location.Latitude.ToDegrees (), location.Longitude.ToDegrees (),
                min, max, double3.Unpack (in mass.Offset), new Random ().RandomColor ());
            InputEvents.CreateVehicleBuffer.Add (new InputEvents.CreateVehicleData {
                System = Universe.CurrentSystem,
                Parent = state.Orbit.Parent,
                Id = id,
                Body2Cce = state.Body2Cce,
                BodyRates = state.BodyRates,
                Orbit = state.Orbit,
                Root = tree.Root
            });
            return WaitForLaunch (id, 0);
        }

        static Vehicle WaitForLaunch (string id, int updates)
        {
            if (Universe.CurrentSystem.All.TryGet (id, out Astronomical created) && created is global::KSA.Vehicle vehicle) {
                Universe.FollowInHoveredViewport (vehicle);
                return new Vehicle (vehicle);
            }
            if (updates >= 10)
                throw new InvalidOperationException ("The vehicle was not created");
            throw new YieldException<Func<Vehicle>> (() => WaitForLaunch (id, updates + 1));
        }

        /// <summary>
        /// Convert a position from one reference frame to another.
        /// </summary>
        /// <param name="position">Position, in meters, in the frame <paramref name="from"/>.</param>
        /// <param name="from">The reference frame the position is in.</param>
        /// <param name="to">The reference frame to convert the position to.</param>
        /// <returns>The position in the frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple<double, double, double> TransformPosition (
            Tuple<double, double, double> position, ReferenceFrame from, ReferenceFrame to)
        {
            return Vectors.ToTuple (to.PositionFromEcl (from.PositionToEcl (Vectors.ToVector (position))));
        }

        /// <summary>
        /// Convert a direction from one reference frame to another.
        /// </summary>
        /// <param name="direction">Direction in the frame <paramref name="from"/>.</param>
        /// <param name="from">The reference frame the direction is in.</param>
        /// <param name="to">The reference frame to convert the direction to.</param>
        /// <returns>The direction in the frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple<double, double, double> TransformDirection (
            Tuple<double, double, double> direction, ReferenceFrame from, ReferenceFrame to)
        {
            return Vectors.ToTuple (to.DirectionFromEcl (from.DirectionToEcl (Vectors.ToVector (direction))));
        }

        /// <summary>
        /// Convert a rotation from one reference frame to another.
        /// </summary>
        /// <param name="rotation">Rotation in the frame <paramref name="from"/>.</param>
        /// <param name="from">The reference frame the rotation is in.</param>
        /// <param name="to">The reference frame to convert the rotation to.</param>
        /// <returns>The rotation in the frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple<double, double, double, double> TransformRotation (
            Tuple<double, double, double, double> rotation, ReferenceFrame from, ReferenceFrame to)
        {
            return Vectors.ToTuple (to.RotationFromEcl (from.RotationToEcl (Vectors.ToRotation (rotation))));
        }

        /// <summary>
        /// Convert a velocity from one reference frame to another. The velocity of a point
        /// depends on its position in a rotating frame, so the position is also needed.
        /// </summary>
        /// <param name="position">Position, in meters, in the frame <paramref name="from"/>.</param>
        /// <param name="velocity">Velocity, in meters per second, in the frame
        /// <paramref name="from"/>.</param>
        /// <param name="from">The reference frame the position and velocity are in.</param>
        /// <param name="to">The reference frame to convert the velocity to.</param>
        /// <returns>The velocity in the frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple<double, double, double> TransformVelocity (
            Tuple<double, double, double> position, Tuple<double, double, double> velocity,
            ReferenceFrame from, ReferenceFrame to)
        {
            var positionEcl = from.PositionToEcl (Vectors.ToVector (position));
            var velocityEcl = from.VelocityToEcl (positionEcl, Vectors.ToVector (velocity));
            return Vectors.ToTuple (to.VelocityFromEcl (positionEcl, velocityEcl));
        }
    }
}

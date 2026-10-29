using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Brutal.ImGuiApi.Abstractions;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service;
using KRPC.Service.Attributes;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Changes the state of the simulation directly, as the game's debug windows and console
    /// commands do: teleporting a vehicle, setting its attitude, refilling its tanks.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public sealed class Debug
    {
        // The class holds static methods only
        Debug ()
        {
        }

        /// <summary>
        /// Move a vehicle onto a circular orbit around a body, and return once it is there.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="body">The body to orbit.</param>
        /// <param name="altitude">The altitude of the orbit above the mean radius, in meters.</param>
        /// <remarks>
        /// Throws an exception if the game cannot compute the vehicle's trajectory on the
        /// orbit.
        /// </remarks>
        [KRPCMethod]
        public static void SetCircularOrbit (Vehicle vehicle, Body body, double altitude)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            var parent = body.InternalBody;
            Teleport (vehicle, global::KSA.Orbit.Create (parent, new OrbitData (
                Universe.GetElapsedTime (), parent.Mass, parent.MeanRadius + altitude, 0, 0, 0, 0), LineColor (vehicle)));
        }

        /// <summary>
        /// Move a vehicle onto the orbit with the given elements, and return once it is there.
        /// The angles are those of <see cref="Orbit"/>.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="body">The body to orbit.</param>
        /// <param name="semiMajorAxis">The semi-major axis, in meters.</param>
        /// <param name="eccentricity">The eccentricity.</param>
        /// <param name="inclination">The inclination, in radians.</param>
        /// <param name="longitudeOfAscendingNode">The longitude of the ascending node, in radians.</param>
        /// <param name="argumentOfPeriapsis">The argument of periapsis, in radians.</param>
        /// <param name="meanAnomalyAtEpoch">The mean anomaly at the epoch, in radians.</param>
        /// <param name="epoch">The universal time of the epoch, in seconds.</param>
        /// <remarks>
        /// Throws an exception if the game cannot compute the vehicle's trajectory on the
        /// orbit.
        /// </remarks>
        [KRPCMethod]
        public static void SetOrbit (
            Vehicle vehicle, Body body, double semiMajorAxis, double eccentricity, double inclination,
            double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            if (!(semiMajorAxis != 0 && eccentricity >= 0 && eccentricity != 1))
                throw new ArgumentException ("The orbit is not elliptical or hyperbolic");
            var parent = body.InternalBody;
            var periapsis = Math.Abs (semiMajorAxis * (1 - eccentricity));
            global::KSA.Orbit Create (UniverseTime timeAtPeriapsis) => global::KSA.Orbit.Create (parent, new OrbitData (
                timeAtPeriapsis, parent.Mass, periapsis, eccentricity, inclination, longitudeOfAscendingNode,
                argumentOfPeriapsis), LineColor (vehicle));
            // The game's elements give the time at periapsis, found from the mean motion
            var now = Universe.GetElapsedTime ();
            var meanMotion = Create (now).GetMeanMotion ();
            Teleport (vehicle, Create (now + (epoch - now.Seconds () - meanAnomalyAtEpoch / meanMotion)));
        }

        /// <summary>
        /// Place a vehicle on the surface of a body, as the game's teleport window does, and
        /// return once it is there.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="body">The body to land on.</param>
        /// <param name="latitude">The latitude, in degrees.</param>
        /// <param name="longitude">The longitude, in degrees.</param>
        /// <remarks>
        /// Throws an exception if the body has no surface.
        /// </remarks>
        [KRPCMethod]
        public static void SetLanded (Vehicle vehicle, Body body, double latitude, double longitude)
        {
            if (ReferenceEquals (vehicle, null))
                throw new ArgumentNullException (nameof (vehicle));
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            var celestial = body.InternalBody as Celestial ?? throw new ArgumentException ("The body has no surface");
            var v = vehicle.InternalVehicle;
            var orbit = v.Orbit;
            v.TeleportToLocation (celestial, latitude, longitude);
            WaitForTeleport (v, orbit, 10);
        }

        /// <summary>
        /// Move a vehicle to the given position, keeping its velocity, and return once it is
        /// there.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="position">The position of the vehicle, in meters.</param>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        /// <remarks>
        /// This is the inverse of <see cref="Vehicle.Position"/>. The vehicle keeps orbiting
        /// the same body.
        /// </remarks>
        [KRPCMethod]
        public static void SetPosition (Vehicle vehicle, Tuple<double, double, double> position, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (vehicle, null))
                throw new ArgumentNullException (nameof (vehicle));
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var v = vehicle.InternalVehicle;
            SetState (vehicle, referenceFrame.PositionToEcl (Vectors.ToVector (position)), v.GetVelocityEcl ());
        }

        /// <summary>
        /// Set the velocity of a vehicle, keeping its position, and return once it is set.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="velocity">The velocity of the vehicle, in meters per second.</param>
        /// <param name="referenceFrame">The reference frame of the velocity.</param>
        /// <remarks>
        /// This is the inverse of <see cref="Vehicle.Velocity"/>. The vehicle keeps orbiting
        /// the same body.
        /// </remarks>
        [KRPCMethod]
        public static void SetVelocity (Vehicle vehicle, Tuple<double, double, double> velocity, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (vehicle, null))
                throw new ArgumentNullException (nameof (vehicle));
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var positionEcl = vehicle.InternalVehicle.GetPositionEcl ();
            SetState (vehicle, positionEcl, referenceFrame.VelocityToEcl (positionEcl, Vectors.ToVector (velocity)));
        }

        static void SetState (Vehicle vehicle, double3 positionEcl, double3 velocityEcl)
        {
            var parent = vehicle.InternalVehicle.Orbit.Parent;
            var ecl2Cci = parent.GetCci2Cce ().Inverse ();
            Teleport (vehicle, global::KSA.Orbit.CreateFromStateCci (
                parent, Universe.GetElapsedTime (),
                (positionEcl - parent.GetPositionEcl ()).Transform (ecl2Cci),
                (velocityEcl - parent.GetVelocityEcl ()).Transform (ecl2Cci), LineColor (vehicle)));
        }

        /// <summary>
        /// Set the rotation of a vehicle, and return once it is set.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="rotation">The rotation of the vehicle, as a quaternion of the form
        /// <c>(x, y, z, w)</c>.</param>
        /// <param name="referenceFrame">The reference frame of the rotation.</param>
        /// <remarks>
        /// This is the inverse of <see cref="Vehicle.Rotation"/>.
        /// </remarks>
        [KRPCMethod]
        public static void SetRotation (Vehicle vehicle, Tuple<double, double, double, double> rotation, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (vehicle, null))
                throw new ArgumentNullException (nameof (vehicle));
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var v = vehicle.InternalVehicle;
            // The vehicle's rotation is that of its control part, the helm
            var helm2Cce = referenceFrame.RotationToEcl (Vectors.ToRotation (rotation));
            var body2Cce = doubleQuat.Concatenate (v.Helm2Body.Inverse (), helm2Cce);
            InputEvents.TeleportInputBuffer.Add (new InputEvents.TeleportInputData { Vehicle = v, Body2Cce = body2Cce });
            Waits.Until (() => Math.Abs (doubleQuat.Dot (v.Body2Cce, body2Cce)) > 1 - 1e-6);
        }

        /// <summary>
        /// Set the angular velocity of a vehicle, and return once it is set.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="angularVelocity">The angular velocity, in radians per second. The
        /// vector points along the axis of rotation, and its length is the rate.</param>
        /// <param name="referenceFrame">The reference frame of the angular velocity.</param>
        /// <remarks>
        /// This is the inverse of <see cref="Vehicle.AngularVelocity"/>.
        /// </remarks>
        [KRPCMethod]
        public static void SetAngularVelocity (Vehicle vehicle, Tuple<double, double, double> angularVelocity, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (vehicle, null))
                throw new ArgumentNullException (nameof (vehicle));
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var v = vehicle.InternalVehicle;
            var angularVelocityEcl = referenceFrame.AngularVelocityToEcl (Vectors.ToVector (angularVelocity));
            // The game gives the rates in the vehicle's body frame
            var bodyRates = angularVelocityEcl.Transform (v.Body2Cce.Inverse ());
            InputEvents.TeleportInputBuffer.Add (new InputEvents.TeleportInputData { Vehicle = v, BodyRates = bodyRates });
            Waits.Until (() => (v.BodyRates - bodyRates).Length () < 1e-6);
        }

        static byte4 LineColor (Vehicle vehicle)
        {
            if (ReferenceEquals (vehicle, null))
                throw new ArgumentNullException (nameof (vehicle));
            return vehicle.InternalVehicle.Orbit.OrbitLineColor;
        }

        static void Teleport (Vehicle vehicle, global::KSA.Orbit orbit)
        {
            if (ReferenceEquals (vehicle, null))
                throw new ArgumentNullException (nameof (vehicle));
            var v = vehicle.InternalVehicle;
            var previous = v.Orbit;
            InputEvents.TeleportInputBuffer.Add (new InputEvents.TeleportInputData { Vehicle = v, Orbit = orbit });
            WaitForTeleport (v, previous, 10);
        }

        static void WaitForTeleport (global::KSA.Vehicle vehicle, global::KSA.Orbit previous, int updates)
        {
            // The game replaces the vehicle's orbit, or logs an error and keeps it. The
            // vehicle's position and velocity follow at the start of the next frame.
            if (!ReferenceEquals (vehicle.Orbit, previous)) {
                Waits.Until (() => false, 1);
                return;
            }
            if (updates == 0)
                throw new InvalidOperationException ("The game could not place the vehicle on the orbit");
            throw new YieldException<Action> (() => WaitForTeleport (vehicle, previous, updates - 1));
        }

        /// <summary>
        /// Fill the propellant, batteries and other consumables of a vehicle, as the game's
        /// refill command does.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        [KRPCMethod]
        public static void FillResources (Vehicle vehicle)
        {
            ChangeResources (vehicle, refill: true);
        }

        /// <summary>
        /// Empty the propellant, batteries and other consumables of a vehicle, as the game's
        /// empty command does.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        [KRPCMethod]
        public static void EmptyResources (Vehicle vehicle)
        {
            ChangeResources (vehicle, refill: false);
        }

        static void ChangeResources (Vehicle vehicle, bool refill)
        {
            if (ReferenceEquals (vehicle, null))
                throw new ArgumentNullException (nameof (vehicle));
            var v = vehicle.InternalVehicle;
            var mass = v.PropellantMass;
            InputEvents.VehicleResourcesChangeBuffer.Add (new InputEvents.VehicleResourcesChangeData {
                Vehicle = v, Refill = refill, Empty = !refill
            });
            // The propellant mass is unchanged for a vehicle already full or empty
            Waits.Until (() => v.PropellantMass != mass, 2);
        }

        /// <summary>
        /// Create a copy of a vehicle beside it, as the game's duplicate command does, and
        /// return once it is created.
        /// </summary>
        /// <param name="vehicle">The vehicle to copy.</param>
        /// <param name="name">The name of the copy.</param>
        /// <returns>The copy.</returns>
        /// <remarks>
        /// Throws an exception if the name is in use.
        /// </remarks>
        [KRPCMethod]
        public static Vehicle DuplicateVehicle (Vehicle vehicle, string name)
        {
            if (ReferenceEquals (vehicle, null))
                throw new ArgumentNullException (nameof (vehicle));
            if (string.IsNullOrEmpty (name))
                throw new ArgumentException ("The name is empty");
            var v = vehicle.InternalVehicle;
            if (Universe.CurrentSystem.All.TryGet (name, out Astronomical _))
                throw new ArgumentException ("The name is in use");
            // As the game's duplicate command, the copy is placed outside the vehicle's bounding sphere
            var state = v.Orbit.StateVectors;
            var offset = v.Props.BoundingSphereRadiusBody * state.PositionCci.NormalizeOrZero ();
            InputEvents.CreateVehicleBuffer.Add (new InputEvents.CreateVehicleData {
                System = v.System,
                Parent = v.Parent,
                Id = name,
                Body2Cce = v.Body2Cce,
                BodyRates = v.BodyRates,
                Root = v.Parts.DeepCopy ().Root,
                Orbit = global::KSA.Orbit.CreateFromStateCci (
                    v.Orbit.Parent, state.StateTime, state.PositionCci + offset, state.VelocityCci, v.Orbit.OrbitLineColor)
            });
            return WaitForVehicle (name, 10);
        }

        static Vehicle WaitForVehicle (string name, int updates)
        {
            if (Universe.CurrentSystem.All.TryGet (name, out Astronomical created) && created is global::KSA.Vehicle vehicle)
                return new Vehicle (vehicle);
            if (updates == 0)
                throw new InvalidOperationException ("The vehicle was not created");
            throw new YieldException<Func<Vehicle>> (() => WaitForVehicle (name, updates - 1));
        }

        /// <summary>
        /// Destroy a vehicle in an explosion, as the game's explode command does, and return
        /// once the vehicle is gone. Its crew are killed.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        [KRPCMethod]
        public static void ExplodeVehicle (Vehicle vehicle)
        {
            if (ReferenceEquals (vehicle, null))
                throw new ArgumentNullException (nameof (vehicle));
            var v = vehicle.InternalVehicle;
            InputEvents.VehicleExplodeBuffer.Add (new InputEvents.VehicleExplodeData { Vehicle = v });
            Waits.Until (() => !Vehicles.Exists (v));
        }

        /// <summary>
        /// Show or hide the game's debug arrows for the velocities of the vehicles.
        /// </summary>
        /// <param name="visible">Whether to show the arrows.</param>
        [KRPCMethod]
        public static void ShowVelocityArrows (bool visible)
        {
            Universe.DisplayVelocityDebugArrows = visible;
        }

        // The console's commands, which the game does not expose
        static readonly FieldInfo staticCommands = typeof (TerminalInterface).GetField (
            "StaticCommandLibrary", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo objectCommands = typeof (TerminalInterface).GetField (
            "CommandLibrary", BindingFlags.NonPublic | BindingFlags.Instance);

        static bool HasCommand (TerminalInterface terminal, string name)
        {
            if (((IDictionary) staticCommands.GetValue (terminal)).Contains (name))
                return true;
            foreach (IDictionary commands in ((IDictionary) objectCommands.GetValue (terminal)).Values) {
                if (commands.Contains (name))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Run a command in the game's console, as typed in the console.
        /// </summary>
        /// <param name="command">The command and its arguments.</param>
        /// <returns>The console output of the command, one line per message.</returns>
        /// <remarks>
        /// Most commands write their messages to the game's log, not the returned output.
        /// Throws an exception if the console has no such command, or the arguments are not
        /// valid.
        /// </remarks>
        [KRPCMethod]
        public static IList<string> RunCommand (string command)
        {
            if (string.IsNullOrWhiteSpace (command))
                throw new ArgumentException ("The command is empty");
            var terminal = Program.TerminalInterface ?? throw new InvalidOperationException ("The game has no console");
            var name = command.Trim ().Split (' ') [0];
            if (!HasCommand (terminal, name))
                throw new ArgumentException ("No console command named " + name);
            var output = new List<string> ();
            void OnOutput (string text, TerminalInterfaceOutputType type) => output.Add (text);
            terminal.OnOutput += OnOutput;
            bool executed;
            try {
                executed = terminal.Execute (command);
            } finally {
                terminal.OnOutput -= OnOutput;
            }
            if (!executed)
                throw new ArgumentException ("The command failed: " + string.Join ("\n", output));
            return output;
        }
    }
}

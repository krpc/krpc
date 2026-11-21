using System;
using System.Collections.Generic;
using System.Linq;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A vehicle in the current system. Obtained from <see cref="KSA.ActiveVehicle"/>
    /// or <see cref="KSA.Vehicles"/>.
    /// </summary>
    /// <remarks>
    /// Docking and undocking replace the vehicles involved, so an object for one of them
    /// throws an exception afterwards.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Vehicle : Equatable<Vehicle>, IGameObjectState
    {
        // KSA vehicles have no permanent id: the id is the name, and changes on a rename.
        // The object is the identity
        readonly global::KSA.Vehicle vehicle;

        internal Vehicle (global::KSA.Vehicle vehicle)
        {
            this.vehicle = vehicle ?? throw new ArgumentNullException (nameof (vehicle));
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Vehicle other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (vehicle, other.vehicle);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Vehicles.StateOf (vehicle); }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vehicle.GetHashCode ();
        }

        /// <summary>
        /// The KSA vehicle. Throws an exception if it no longer exists.
        /// </summary>
        public global::KSA.Vehicle InternalVehicle {
            get {
                if (!Vehicles.Exists (vehicle))
                    throw new Service.KRPC.ObjectDestroyedException ("The vehicle no longer exists");
                return vehicle;
            }
        }

        /// <summary>
        /// The name of the vehicle, which is unique in the system.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalVehicle.Id; }
            set {
                if (!Vehicles.Rename (InternalVehicle, value))
                    throw new ArgumentException ("The name is in use");
            }
        }

        /// <summary>
        /// Destroy the vehicle, killing its crew, as abandoning the vehicle does. Returns once
        /// the vehicle is gone.
        /// </summary>
        /// <remarks>
        /// If the vehicle is the active vehicle, the game hands control to a nearby vehicle.
        /// </remarks>
        [KRPCMethod]
        public void Destroy ()
        {
            var v = InternalVehicle;
            InputEvents.VehicleDestroyBuffer.Add (new InputEvents.VehicleDestroyData { Vehicle = v });
            Waits.Until (() => !Vehicles.Exists (v));
        }

        /// <summary>
        /// Whether the vehicle can be recovered: at rest on the surface of the home planet.
        /// </summary>
        [KRPCProperty]
        public bool CanRecover {
            get { return InternalVehicle.CanRecover; }
        }

        /// <summary>
        /// Recover the vehicle, ending its crew's missions, as the game's recover button does.
        /// Returns once the vehicle is gone.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the vehicle cannot be recovered, see <see cref="CanRecover"/>.
        /// </remarks>
        [KRPCMethod]
        public void Recover ()
        {
            var v = InternalVehicle;
            if (!v.CanRecover)
                throw new InvalidOperationException ("The vehicle cannot be recovered");
            v.Recover ();
            Waits.Until (() => !Vehicles.Exists (v));
        }

        /// <summary>
        /// The situation of the vehicle.
        /// </summary>
        [KRPCProperty]
        public Situation Situation {
            get { return (Situation)InternalVehicle.Situation; }
        }

        /// <summary>
        /// Whether the game moves the vehicle without simulating the forces on it: along its
        /// orbit, or held on the surface. True in the freefall, landed, floating and bottomed
        /// situations.
        /// </summary>
        [KRPCProperty]
        public bool OnRails {
            get { return InternalVehicle.Situation.IsOnRails (); }
        }

        /// <summary>
        /// Whether the vehicle has launched.
        /// </summary>
        [KRPCProperty]
        public bool HasLaunched {
            get { return InternalVehicle.HasLaunched; }
        }

        /// <summary>
        /// The universal time the vehicle was created, in seconds.
        /// </summary>
        [KRPCProperty]
        public double LaunchTime {
            get { return InternalVehicle.LaunchGameTime.Seconds (); }
        }

        /// <summary>
        /// The total mass of the vehicle, including propellant, in kilograms.
        /// </summary>
        [KRPCProperty]
        public float Mass {
            get { return InternalVehicle.TotalMass; }
        }

        /// <summary>
        /// The mass of the propellant in the vehicle, in kilograms.
        /// </summary>
        [KRPCProperty]
        public float PropellantMass {
            get { return InternalVehicle.PropellantMass; }
        }

        /// <summary>
        /// The thrust of the active engines that have propellant, at full throttle and the
        /// current atmospheric pressure, in Newtons.
        /// </summary>
        [KRPCProperty]
        public float AvailableThrust {
            get {
                var v = InternalVehicle;
                return v.ComputeActiveThrust (v.PhysicsEnvironment.AtmosphericPressure);
            }
        }

        /// <summary>
        /// The thrust of the engines at their current throttle and the current atmospheric
        /// pressure, in Newtons.
        /// </summary>
        [KRPCProperty]
        public float Thrust {
            get { return (float)Utils.Engines.Performance (InternalVehicle, fullThrottle: false).thrust; }
        }

        /// <summary>
        /// The combined specific impulse of the active engines that have propellant, at the
        /// current atmospheric pressure, in seconds. Zero when no engine is active.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get {
                var (thrust, massFlowRate) = Utils.Engines.Performance (InternalVehicle, fullThrottle: true);
                return (float)(massFlowRate > 0 ? thrust / (massFlowRate * Utils.Engines.G0) : 0);
            }
        }

        /// <summary>
        /// The vehicle's parts.
        /// </summary>
        [KRPCProperty]
        public IList<Part> Parts {
            get { return InternalVehicle.Parts.Parts.ToArray ().Select (part => new Part (part)).ToList (); }
        }

        /// <summary>
        /// The vehicle's parts with the given template id. See <see cref="Part.Name"/>.
        /// </summary>
        /// <param name="name">The template id.</param>
        [KRPCMethod]
        public IList<Part> PartsWithName (string name)
        {
            return Parts.Where (part => part.Name == name).ToList ();
        }

        /// <summary>
        /// The vehicle's crew seats.
        /// </summary>
        [KRPCProperty]
        public IList<Seat> Seats {
            get { return Parts.SelectMany (part => part.Seats).ToList (); }
        }

        /// <summary>
        /// The kittens in the vehicle's seats.
        /// </summary>
        [KRPCProperty]
        public IList<Kitten> Crew {
            get { return Seats.Select (seat => seat.Kitten).Where (kitten => kitten != null).ToList (); }
        }

        /// <summary>
        /// The number of crew seats in the vehicle.
        /// </summary>
        [KRPCProperty]
        public int CrewCapacity {
            get { return InternalVehicle.Crew.Length; }
        }

        /// <summary>
        /// The vehicle's engines.
        /// </summary>
        [KRPCProperty]
        public IList<Engine> Engines {
            get { return Parts.Select (part => part.Engine).Where (engine => engine != null).ToList (); }
        }

        /// <summary>
        /// The vehicle's RCS thrusters.
        /// </summary>
        [KRPCProperty]
        public IList<RCS> RCS {
            get { return Parts.Select (part => part.RCS).Where (rcs => rcs != null).ToList (); }
        }

        /// <summary>
        /// The vehicle's decouplers.
        /// </summary>
        [KRPCProperty]
        public IList<Decoupler> Decouplers {
            get { return Parts.Select (part => part.Decoupler).Where (decoupler => decoupler != null).ToList (); }
        }

        /// <summary>
        /// The vehicle's parachutes.
        /// </summary>
        [KRPCProperty]
        public IList<Parachute> Parachutes {
            get { return Parts.Select (part => part.Parachute).Where (parachute => parachute != null).ToList (); }
        }

        /// <summary>
        /// The vehicle's propellant tanks.
        /// </summary>
        [KRPCProperty]
        public IList<Tank> Tanks {
            get { return Parts.SelectMany (part => part.Tanks).ToList (); }
        }

        /// <summary>
        /// The vehicle's fuel links.
        /// </summary>
        [KRPCProperty]
        public IList<FuelLink> FuelLinks {
            get { return InternalVehicle.Parts.FuelLinks.Links.ToArray ().Select (link => new FuelLink (link)).ToList (); }
        }

        /// <summary>
        /// The vehicle's batteries.
        /// </summary>
        [KRPCProperty]
        public IList<Battery> Batteries {
            get { return Parts.SelectMany (part => part.Batteries).ToList (); }
        }

        /// <summary>
        /// The vehicle's solar panels.
        /// </summary>
        [KRPCProperty]
        public IList<SolarPanel> SolarPanels {
            get { return Parts.SelectMany (part => part.SolarPanels).ToList (); }
        }

        /// <summary>
        /// The vehicle's generators.
        /// </summary>
        [KRPCProperty]
        public IList<Generator> Generators {
            get { return Parts.SelectMany (part => part.Generators).ToList (); }
        }

        /// <summary>
        /// The vehicle's power consumers.
        /// </summary>
        [KRPCProperty]
        public IList<PowerConsumer> PowerConsumers {
            get { return Parts.SelectMany (part => part.PowerConsumers).ToList (); }
        }

        /// <summary>
        /// The vehicle's lights.
        /// </summary>
        [KRPCProperty]
        public IList<Light> Lights {
            get { return Parts.SelectMany (part => part.Lights).ToList (); }
        }

        /// <summary>
        /// The vehicle's deployable sections.
        /// </summary>
        [KRPCProperty]
        public IList<Deployable> Deployables {
            get { return Parts.SelectMany (part => part.Deployables).ToList (); }
        }

        /// <summary>
        /// The vehicle's magnetic scoops.
        /// </summary>
        [KRPCProperty]
        public IList<MagneticScoop> MagneticScoops {
            get { return Parts.SelectMany (part => part.MagneticScoops).ToList (); }
        }

        /// <summary>
        /// The energy stored in the vehicle's batteries, in joules.
        /// </summary>
        [KRPCProperty]
        public float StoredEnergy {
            get { return Batteries.Sum (battery => battery.StoredEnergy); }
        }

        /// <summary>
        /// The energy the vehicle's batteries can store, in joules.
        /// </summary>
        [KRPCProperty]
        public float EnergyCapacity {
            get { return Batteries.Sum (battery => battery.EnergyCapacity); }
        }

        /// <summary>
        /// The power produced by the vehicle's solar panels and generators, less the power its
        /// parts use, in watts.
        /// </summary>
        [KRPCProperty]
        public float NetPower {
            get {
                return SolarPanels.Sum (panel => panel.Power) + Generators.Sum (generator => generator.Power) -
                    PowerConsumers.Sum (consumer => consumer.Power);
            }
        }

        /// <summary>
        /// The vehicle's docking ports.
        /// </summary>
        [KRPCProperty]
        public IList<DockingPort> DockingPorts {
            get { return Parts.Select (part => part.DockingPort).Where (port => port != null).ToList (); }
        }

        /// <summary>
        /// The vehicle's root part, which the other parts are attached to.
        /// </summary>
        [KRPCProperty]
        public Part RootPart {
            get { return new Part (InternalVehicle.Parts.Root); }
        }

        /// <summary>
        /// The resources the vehicle holds, one for each substance, totaled over its parts.
        /// </summary>
        [KRPCProperty]
        public IList<Resource> Resources {
            get { return Resource.Of (InternalVehicle, null); }
        }

        /// <summary>
        /// The change in velocity of all the vehicle's sequences, in meters per second.
        /// </summary>
        [KRPCProperty]
        public float DeltaV {
            get { return Utils.Sequences.TotalDeltaV (InternalVehicle); }
        }

        /// <summary>
        /// The vehicle's sequences, in activation order.
        /// </summary>
        [KRPCProperty]
        public IList<Sequence> Sequences {
            get {
                var v = InternalVehicle;
                return Utils.Sequences.Numbers (v).Select (number => new Sequence (v, number)).ToList ();
            }
        }

        /// <summary>
        /// The vehicle targeted by this vehicle, or <c>null</c>. Setting it replaces the
        /// target, and <c>null</c> clears it.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Vehicle TargetVehicle {
            get {
                var target = InternalVehicle.Target as global::KSA.Vehicle;
                return target == null || target is KittenEva ? null : new Vehicle (target);
            }
            set {
                var v = InternalVehicle;
                if (value != null && ReferenceEquals (value.InternalVehicle, v))
                    throw new ArgumentException ("A vehicle cannot target itself");
                v.SetTarget (value?.InternalVehicle);
            }
        }

        /// <summary>
        /// The body targeted by this vehicle, or <c>null</c>. Setting it replaces the target,
        /// and <c>null</c> clears it.
        /// </summary>
        /// <remarks>
        /// Setting a body that orbits nothing, such as the star, throws an exception.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public Body TargetBody {
            get {
                var target = InternalVehicle.Target as IParentBody;
                return target == null ? null : new Body (target);
            }
            set {
                var v = InternalVehicle;
                if (value == null) {
                    v.SetTarget (null);
                    return;
                }
                if (value.InternalBody is not IOrbiter orbiter)
                    throw new ArgumentException ("The body cannot be targeted");
                v.SetTarget (orbiter);
            }
        }

        /// <summary>
        /// The part targeted by this vehicle, on the vehicle in <see cref="TargetVehicle"/>,
        /// or <c>null</c>. Setting it targets the part and its vehicle, and <c>null</c> clears
        /// the target.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Part TargetPart {
            get {
                var v = InternalVehicle;
                var part = v.TargetPart;
                return part == null || v.Target is not global::KSA.Vehicle ? null : new Part (part);
            }
            set {
                var v = InternalVehicle;
                if (value == null) {
                    v.SetTarget (null);
                    return;
                }
                var target = value.InternalVehicle;
                if (ReferenceEquals (target, v))
                    throw new ArgumentException ("A vehicle cannot target its own part");
                v.SetTarget (target, value.InternalPart);
            }
        }

        /// <summary>
        /// Whether the vehicle is debris, such as a spent stage.
        /// </summary>
        [KRPCProperty]
        public bool IsDebris {
            get { return InternalVehicle.IsDebris; }
        }

        /// <summary>
        /// The highest smoothed g-load on the vehicle during the last physics step, as a
        /// multiple of standard gravity. Zero while the vehicle is <see cref="OnRails"/>.
        /// </summary>
        [KRPCProperty]
        public float PeakGLoad {
            get { return (float)InternalVehicle.StructuralLoad.PeakGLoad; }
        }

        /// <summary>
        /// The g-load the vehicle can take before it breaks up, as a multiple of standard
        /// gravity. Zero while the vehicle is <see cref="OnRails"/>.
        /// </summary>
        [KRPCProperty]
        public float MaxGLoad {
            get { return (float)InternalVehicle.StructuralLoad.MaxGLoad; }
        }

        /// <summary>
        /// The highest dynamic pressure on the vehicle during the last physics step, in
        /// pascals. In the sea, this is the dynamic pressure of the water. Zero while the
        /// vehicle is <see cref="OnRails"/>.
        /// </summary>
        [KRPCProperty]
        public float PeakDynamicPressure {
            get { return (float)InternalVehicle.StructuralLoad.PeakDynamicPressure; }
        }

        /// <summary>
        /// The dynamic pressure the vehicle can take before it breaks up, in pascals. Zero
        /// while the vehicle is <see cref="OnRails"/>.
        /// </summary>
        [KRPCProperty]
        public float MaxDynamicPressure {
            get { return (float)InternalVehicle.StructuralLoad.MaxDynamicPressure; }
        }

        /// <summary>
        /// The vehicle's planned burns, in time order.
        /// </summary>
        [KRPCProperty]
        public IList<Burn> Burns {
            get {
                var v = InternalVehicle;
                var plan = v.BurnPlan;
                var result = new List<Burn> ();
                for (var i = 0; i < plan.BurnCount; i++) {
                    if (plan.TryGetBurn (i, out var burn) && !burn.ParentDepartureBurn)
                        result.Add (new Burn (v, burn));
                }
                return result;
            }
        }

        /// <summary>
        /// Plan a burn, and add it to the vehicle's burn plan.
        /// </summary>
        /// <param name="ut">Universal time of the burn, in seconds.</param>
        /// <param name="prograde">Change in velocity in the prograde direction, in meters per
        /// second.</param>
        /// <param name="normal">Change in velocity along the orbit normal, in meters per
        /// second.</param>
        /// <param name="radial">Change in velocity radially outward, in meters per second.</param>
        /// <returns>The burn.</returns>
        /// <remarks>
        /// Returns once the flight computer has timed the burn, if it is the next one. Throws
        /// an exception if the time is in the past, or beyond the vehicle's predicted
        /// trajectory.
        /// </remarks>
        [KRPCMethod]
        public Burn AddBurn (double ut, float prograde = 0, float normal = 0, float radial = 0)
        {
            var v = InternalVehicle;
            if (!double.IsFinite (prograde) || !double.IsFinite (normal) || !double.IsFinite (radial))
                throw new ArgumentException ("The change in velocity is not finite");
            if (!(ut > Universe.GetElapsedSeconds ()))
                throw new ArgumentException ("The time of the burn is not in the future");
            var time = new UniverseTime (ut);
            var patch = v.FlightPlan.TryFindPatch (time)
                ?? throw new ArgumentException ("The time of the burn is beyond the predicted trajectory");
            var burn = global::KSA.Burn.Create (patch.Orbit.GetPointAt (time), ut, new double3 (prograde, normal, radial), patch, v);
            v.FlightComputer.AddBurn (burn);
            return WaitForTiming (v, burn, 0);
        }

        // The flight computer times the next burn in its solver step. We wait for that, so the
        // ignition time is known on return, unless the game is paused
        static Burn WaitForTiming (global::KSA.Vehicle vehicle, global::KSA.Burn burn, int updates)
        {
            var result = new Burn (vehicle, burn);
            if (updates < 10 && !Universe.IsPaused () && !result.IsTimed)
                throw new YieldException<Func<Burn>> (() => WaitForTiming (vehicle, burn, updates + 1));
            return result;
        }

        /// <summary>
        /// The current orbit of the vehicle.
        /// </summary>
        [KRPCProperty]
        public Orbit Orbit {
            get { return new Orbit (InternalVehicle); }
        }

        /// <summary>
        /// Flight telemetry for the vehicle, relative to the surface of the body it orbits.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public Flight Flight {
            get { return new Flight (InternalVehicle); }
        }

        /// <summary>
        /// The controls of the vehicle.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public Control Control {
            get { return new Control (InternalVehicle); }
        }

        /// <summary>
        /// The auto-pilot of the vehicle.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public AutoPilot AutoPilot {
            get { return new AutoPilot (InternalVehicle); }
        }

        /// <summary>
        /// The reference frame fixed to the vehicle, with its origin at the vehicle's position.
        /// The x axis points forward, the y axis to the right and the z axis down, relative to
        /// the vehicle's control point.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return new ReferenceFrame (ReferenceFrame.Kind.Vehicle, InternalVehicle); }
        }

        /// <summary>
        /// The reference frame with its origin at the vehicle's position, and axes aligned with
        /// the surface below it. The x axis points east, the y axis north and the z axis up.
        /// The frame rotates with the body.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame SurfaceReferenceFrame {
            get { return new ReferenceFrame (ReferenceFrame.Kind.VehicleSurface, InternalVehicle); }
        }

        /// <summary>
        /// The reference frame with its origin at the vehicle's position, and axes aligned with
        /// its orbit. The x axis points prograde, the y axis along the orbit normal and the z
        /// axis radially outward, perpendicular to the x axis.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame OrbitalReferenceFrame {
            get { return new ReferenceFrame (ReferenceFrame.Kind.VehicleOrbital, InternalVehicle); }
        }

        /// <summary>
        /// The position of the vehicle, in meters.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Position (ReferenceFrame referenceFrame)
        {
            return Vectors.ToTuple (referenceFrame.PositionFromEcl (InternalVehicle.GetPositionEcl ()));
        }

        /// <summary>
        /// The velocity of the vehicle, in meters per second.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the velocity.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Velocity (ReferenceFrame referenceFrame)
        {
            var v = InternalVehicle;
            return Vectors.ToTuple (referenceFrame.VelocityFromEcl (v.GetPositionEcl (), v.GetVelocityEcl ()));
        }

        /// <summary>
        /// The unit vector in which the vehicle points, the x axis of
        /// <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the direction.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Direction (ReferenceFrame referenceFrame)
        {
            var forward = double3.UnitX.Transform (InternalVehicle.GetHelm2Cce ());
            return Vectors.ToTuple (referenceFrame.DirectionFromEcl (forward));
        }

        /// <summary>
        /// The rotation of <see cref="ReferenceFrame"/> relative to the given frame.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the rotation.</param>
        [KRPCMethod]
        public Tuple<double, double, double, double> Rotation (ReferenceFrame referenceFrame)
        {
            return Vectors.ToTuple (referenceFrame.RotationFromEcl (InternalVehicle.GetHelm2Cce ()));
        }

        /// <summary>
        /// The angular velocity of the vehicle, in radians per second. The vector points along
        /// the axis of rotation, and its length is the rate of rotation.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the angular velocity.</param>
        [KRPCMethod]
        public Tuple<double, double, double> AngularVelocity (ReferenceFrame referenceFrame)
        {
            return Vectors.ToTuple (referenceFrame.AngularVelocityFromEcl (Vectors.AngularVelocityEcl (InternalVehicle)));
        }
    }
}

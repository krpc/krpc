using System;
using KRPC.KSA.Utils;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

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
    public class Vehicle : Equatable<Vehicle>
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
                    throw new InvalidOperationException ("The vehicle no longer exists");
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
        /// The situation of the vehicle.
        /// </summary>
        [KRPCProperty]
        public Situation Situation {
            get { return (Situation)InternalVehicle.Situation; }
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
        public double Thrust {
            get { return Engines.Performance (InternalVehicle, fullThrottle: false).thrust; }
        }

        /// <summary>
        /// The combined specific impulse of the active engines that have propellant, at the
        /// current atmospheric pressure, in seconds. Zero when no engine is active.
        /// </summary>
        [KRPCProperty]
        public double SpecificImpulse {
            get {
                var (thrust, massFlowRate) = Engines.Performance (InternalVehicle, fullThrottle: true);
                return massFlowRate > 0 ? thrust / (massFlowRate * Engines.G0) : 0;
            }
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
    }
}

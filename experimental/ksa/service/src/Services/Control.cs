using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KRPC.KSA.Utils;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The controls of a vehicle: engines, stabilization and sequences.
    /// Obtained from <see cref="Vehicle.Control"/>.
    /// </summary>
    [KRPCClass (Service = "KSA", GameScene = GameScene.Flight)]
    public class Control : Equatable<Control>
    {
        // The vehicle's held player inputs. A struct, with no public setter
        static readonly FieldInfo manualControlInputs = typeof (global::KSA.Vehicle).GetField (
            "_manualControlInputs", BindingFlags.NonPublic | BindingFlags.Instance);

        // The number of updates a sequence activation waits before collecting the vehicles
        // it split off. The game applies part activations in the input step of the next
        // frame, and the separated vehicles appear when that frame's solver results are applied
        const int SequenceSettleUpdates = 3;

        readonly global::KSA.Vehicle vehicle;

        internal Control (global::KSA.Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Control other)
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

        global::KSA.Vehicle InternalVehicle {
            get { return new Vehicle (vehicle).InternalVehicle; }
        }

        /// <summary>
        /// The manual throttle. A value between 0 and 1.
        /// </summary>
        /// <remarks>
        /// The game raises a value below the vehicle's minimum throttle to the minimum.
        /// The engines fire only while <see cref="EngineOn"/> is set.
        /// </remarks>
        [KRPCProperty]
        public float Throttle {
            get { return InternalVehicle.GetManualThrottle (); }
            set { SetInputs ((ref ManualControlInputs inputs) => inputs.EngineThrottle = Math.Clamp (value, 0f, 1f)); }
        }

        /// <summary>
        /// Whether the engines are ignited.
        /// </summary>
        [KRPCProperty]
        public bool EngineOn {
            get { return GetInputs ().EngineOn; }
            set { SetInputs ((ref ManualControlInputs inputs) => inputs.EngineOn = value); }
        }

        /// <summary>
        /// Whether the flight computer is controlling the vehicle's attitude.
        /// </summary>
        /// <remarks>
        /// Setting it to <c>true</c> holds the current attitude.
        /// </remarks>
        [KRPCProperty]
        public bool Stabilization {
            get { return InternalVehicle.IsStabilizationOn (); }
            set { InternalVehicle.SetStabilization (value); }
        }

        /// <summary>
        /// Whether the flight computer may use the reaction control thrusters for attitude
        /// control.
        /// </summary>
        [KRPCProperty]
        public bool RCS {
            get { return InternalVehicle.FlightComputer.RCSMode == FlightComputerRCSMode.Enabled; }
            set {
                InternalVehicle.FlightComputer.RCSMode =
                    value ? FlightComputerRCSMode.Enabled : FlightComputerRCSMode.Disabled;
            }
        }

        /// <summary>
        /// The number of the most recently activated sequence.
        /// </summary>
        [KRPCProperty]
        public int ActiveSequence {
            get { return InternalVehicle.Parts.SequenceList.ActiveSequence; }
        }

        /// <summary>
        /// Activates the next sequence, as the staging key does.
        /// </summary>
        /// <returns>The vehicles that separated from this one.</returns>
        /// <remarks>
        /// Throws an exception if the vehicle is not the <see cref="KSA.ActiveVehicle"/>.
        /// </remarks>
        [KRPCMethod]
        public IList<Vehicle> ActivateNextSequence ()
        {
            var v = InternalVehicle;
            if (!ReferenceEquals (v, Program.ControlledVehicle))
                throw new InvalidOperationException ("The vehicle is not the active vehicle");
            var preVehicles = new HashSet<global::KSA.Vehicle> (Vehicles.All);
            v.Parts.SequenceList.ActivateNextSequence (v);
            return CollectSeparated (preVehicles, SequenceSettleUpdates);
        }

        static IList<Vehicle> CollectSeparated (HashSet<global::KSA.Vehicle> preVehicles, int updates)
        {
            if (updates > 0)
                throw new YieldException<Func<IList<Vehicle>>> (() => CollectSeparated (preVehicles, updates - 1));
            return Vehicles.All.Where (x => !preVehicles.Contains (x)).Select (x => new Vehicle (x)).ToList ();
        }

        delegate void InputsSetter (ref ManualControlInputs inputs);

        ManualControlInputs GetInputs ()
        {
            return (ManualControlInputs)manualControlInputs.GetValue (InternalVehicle);
        }

        void SetInputs (InputsSetter setter)
        {
            var v = InternalVehicle;
            var inputs = (ManualControlInputs)manualControlInputs.GetValue (v);
            setter (ref inputs);
            manualControlInputs.SetValue (v, inputs);
        }
    }
}

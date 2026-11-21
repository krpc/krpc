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
    /// The controls of a vehicle: engines, manual inputs, stabilization and sequences.
    /// Obtained from <see cref="Vehicle.Control"/>.
    /// </summary>
    /// <remarks>
    /// The rotation and translation inputs act as held keys. Each is on or off, with no
    /// partial strength. A held rotation input overrides the auto-pilot on its axis. The game
    /// releases the inputs when its window gains or loses focus, or the active vehicle changes.
    /// </remarks>
    [KRPCClass (Service = "KSA", GameScene = GameScene.Flight)]
    public class Control : Equatable<Control>, IGameObjectState
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
        /// The pitch input: 1 for pitch up, -1 for pitch down, 0 for neither.
        /// </summary>
        [KRPCProperty]
        public int Pitch {
            get { return GetAxis (ThrusterMapFlags.PitchUp, ThrusterMapFlags.PitchDown); }
            set { SetAxis (value, ThrusterMapFlags.PitchUp, ThrusterMapFlags.PitchDown); }
        }

        /// <summary>
        /// The yaw input: 1 for yaw right, -1 for yaw left, 0 for neither.
        /// </summary>
        [KRPCProperty]
        public int Yaw {
            get { return GetAxis (ThrusterMapFlags.YawRight, ThrusterMapFlags.YawLeft); }
            set { SetAxis (value, ThrusterMapFlags.YawRight, ThrusterMapFlags.YawLeft); }
        }

        /// <summary>
        /// The roll input: 1 for roll right, -1 for roll left, 0 for neither.
        /// </summary>
        [KRPCProperty]
        public int Roll {
            get { return GetAxis (ThrusterMapFlags.RollRight, ThrusterMapFlags.RollLeft); }
            set { SetAxis (value, ThrusterMapFlags.RollRight, ThrusterMapFlags.RollLeft); }
        }

        /// <summary>
        /// The forward translation input: 1 for forward, -1 for backward, 0 for neither.
        /// </summary>
        [KRPCProperty]
        public int Forward {
            get { return GetAxis (ThrusterMapFlags.TranslateForward, ThrusterMapFlags.TranslateBackward); }
            set { SetAxis (value, ThrusterMapFlags.TranslateForward, ThrusterMapFlags.TranslateBackward); }
        }

        /// <summary>
        /// The right translation input: 1 for right, -1 for left, 0 for neither.
        /// </summary>
        [KRPCProperty]
        public int Right {
            get { return GetAxis (ThrusterMapFlags.TranslateRight, ThrusterMapFlags.TranslateLeft); }
            set { SetAxis (value, ThrusterMapFlags.TranslateRight, ThrusterMapFlags.TranslateLeft); }
        }

        /// <summary>
        /// The up translation input: 1 for up, -1 for down, 0 for neither.
        /// </summary>
        [KRPCProperty]
        public int Up {
            get { return GetAxis (ThrusterMapFlags.TranslateUp, ThrusterMapFlags.TranslateDown); }
            set { SetAxis (value, ThrusterMapFlags.TranslateUp, ThrusterMapFlags.TranslateDown); }
        }

        /// <summary>
        /// Whether the rotation and translation inputs fire the thrusters in short pulses.
        /// </summary>
        [KRPCProperty]
        public bool Pulse {
            get { return InternalVehicle.FlightComputer.ManualThrustMode == FlightComputerManualThrustMode.Pulse; }
            set {
                InternalVehicle.FlightComputer.ManualThrustMode =
                    value ? FlightComputerManualThrustMode.Pulse : FlightComputerManualThrustMode.Direct;
            }
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
        /// Whether the vehicle's lights are switched on, as the lights key does.
        /// </summary>
        [KRPCProperty]
        public bool Lights {
            get { return InternalVehicle.LightsOn; }
            set {
                var v = InternalVehicle;
                if (v.LightsOn != value)
                    v.ToggleLights ();
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
            return Vehicles.Separated (preVehicles, SequenceSettleUpdates);
        }

        // Opposing inputs cancel in the game, so both held reads as 0
        int GetAxis (ThrusterMapFlags positive, ThrusterMapFlags negative)
        {
            var flags = GetInputs ().ThrusterCommandFlags;
            return ((flags & positive) != 0 ? 1 : 0) - ((flags & negative) != 0 ? 1 : 0);
        }

        void SetAxis (int value, ThrusterMapFlags positive, ThrusterMapFlags negative)
        {
            if (value < -1 || value > 1)
                throw new ArgumentOutOfRangeException (nameof (value), "The input is not -1, 0 or 1");
            SetInputs ((ref ManualControlInputs inputs) => {
                var flags = inputs.ThrusterCommandFlags & ~(positive | negative);
                if (value > 0)
                    flags |= positive;
                else if (value < 0)
                    flags |= negative;
                inputs.ThrusterCommandFlags = flags;
            });
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

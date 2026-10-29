using System;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A sequence of a vehicle, the group of parts activated together, and its predicted
    /// performance. Obtained from <see cref="Vehicle.Sequences"/>.
    /// </summary>
    /// <remarks>
    /// The performance is the game's prediction at the current atmospheric pressure. It
    /// assumes the sequences activate in order, each once the one before has burned its
    /// propellant.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Sequence : Equatable<Sequence>, IGameObjectState
    {
        readonly global::KSA.Vehicle vehicle;

        internal Sequence (global::KSA.Vehicle vehicle, int number)
        {
            this.vehicle = vehicle;
            Number = number;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Sequence other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (vehicle, other.vehicle) && Number == other.Number;
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
            return HashCode.Combine (vehicle, Number);
        }

        /// <summary>
        /// The number of the sequence, as shown in the game.
        /// </summary>
        [KRPCProperty]
        public int Number { get; }

        SequencePerformance Performance {
            get { return Sequences.Performance (new Vehicle (vehicle).InternalVehicle, Number); }
        }

        /// <summary>
        /// Whether the sequence has been activated.
        /// </summary>
        [KRPCProperty]
        public bool Activated {
            get { return Sequences.Find (new Vehicle (vehicle).InternalVehicle, Number).Activated; }
        }

        /// <summary>
        /// The change in velocity the sequence provides, in meters per second.
        /// </summary>
        [KRPCProperty]
        public float DeltaV {
            get { return Performance.DeltaV; }
        }

        /// <summary>
        /// The thrust of the sequence's engines at full throttle, in Newtons.
        /// </summary>
        [KRPCProperty]
        public float Thrust {
            get { return Performance.Thrust; }
        }

        /// <summary>
        /// The combined specific impulse of the sequence's engines, in seconds.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get { return Performance.Isp; }
        }

        /// <summary>
        /// The time the sequence's engines take to burn its propellant at full throttle,
        /// in seconds.
        /// </summary>
        [KRPCProperty]
        public float BurnTime {
            get {
                var performance = Performance;
                return performance.MassFlowRate > 0 ? performance.BurnedFuelMass / performance.MassFlowRate : 0;
            }
        }

        /// <summary>
        /// The mass of the vehicle when the sequence starts to burn, in kilograms.
        /// </summary>
        [KRPCProperty]
        public float StartMass {
            get { return Performance.WetMass; }
        }

        /// <summary>
        /// The mass of the vehicle once the sequence has burned its propellant, in kilograms.
        /// </summary>
        [KRPCProperty]
        public float EndMass {
            get {
                var performance = Performance;
                return performance.WetMass - performance.BurnedFuelMass;
            }
        }

        /// <summary>
        /// The mass of propellant the sequence burns, in kilograms.
        /// </summary>
        [KRPCProperty]
        public float PropellantMass {
            get { return Performance.BurnedFuelMass; }
        }
    }
}

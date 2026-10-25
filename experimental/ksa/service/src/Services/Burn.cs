using System;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A planned burn, the game's equivalent of a maneuver node. Obtained from
    /// <see cref="Vehicle.Burns"/> or <see cref="Vehicle.AddBurn"/>.
    /// </summary>
    /// <remarks>
    /// The change in velocity is given along the vehicle's orbit at the time of the burn:
    /// prograde, along the orbit normal, and radially outward. An object for a burn throws an
    /// exception once the burn is removed. The flight computer removes a burn it completes
    /// when another burn follows it, and keeps the last one.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Burn : Equatable<Burn>
    {
        readonly global::KSA.Vehicle vehicle;
        readonly global::KSA.Burn burn;

        internal Burn (global::KSA.Vehicle vehicle, global::KSA.Burn burn)
        {
            this.vehicle = vehicle;
            this.burn = burn;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Burn other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (burn, other.burn);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return burn.GetHashCode ();
        }

        FlightComputer Computer {
            get { return new Vehicle (vehicle).InternalVehicle.FlightComputer; }
        }

        // The burn, which throws if it is no longer in the vehicle's burn plan
        global::KSA.Burn InternalBurn {
            get {
                var plan = Computer.BurnPlan;
                for (var i = 0; i < plan.BurnCount; i++) {
                    if (plan.TryGetBurn (i, out var planned) && ReferenceEquals (planned, burn))
                        return burn;
                }
                throw new InvalidOperationException ("The burn has been removed");
            }
        }

        // The flight computer's timing, for the burn it executes next once its solver step
        // has timed it
        BurnTarget Target {
            get {
                var computer = Computer;
                var next = computer.BurnPlan.FindFirstExecutableBurn ();
                if (!ReferenceEquals (next, InternalBurn) || computer.Burn == null)
                    return null;
                return computer.Burn.IgnitionTime >= UniverseTime.EndOfTime ? null : computer.Burn;
            }
        }

        internal bool IsTimed {
            get { return Target != null; }
        }

        /// <summary>
        /// The universal time of the burn, in seconds. The burn is centered on this time.
        /// </summary>
        [KRPCProperty]
        public double UT {
            get { return InternalBurn.Time.Seconds (); }
        }

        /// <summary>
        /// The change in velocity in the prograde direction, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double Prograde {
            get { return InternalBurn.DeltaVVlf.X; }
            set { SetDeltaV (0, value); }
        }

        /// <summary>
        /// The change in velocity along the orbit normal, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double Normal {
            get { return InternalBurn.DeltaVVlf.Y; }
            set { SetDeltaV (1, value); }
        }

        /// <summary>
        /// The change in velocity radially outward, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double Radial {
            get { return InternalBurn.DeltaVVlf.Z; }
            set { SetDeltaV (2, value); }
        }

        /// <summary>
        /// The magnitude of the change in velocity, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double DeltaV {
            get { return InternalBurn.DeltaVVlf.Length (); }
        }

        /// <summary>
        /// The change in velocity still to burn, in meters per second. Less than
        /// <see cref="DeltaV"/> once the vehicle has started the burn.
        /// </summary>
        /// <remarks>
        /// The flight computer tracks the progress of the next burn only. For later burns this
        /// is <see cref="DeltaV"/>.
        /// </remarks>
        [KRPCProperty]
        public double RemainingDeltaV {
            get {
                var target = Target;
                return target == null ? DeltaV : target.DeltaVToGoCci.Length ();
            }
        }

        /// <summary>
        /// The universal time at which to start the burn, in seconds. Not a number for a burn
        /// other than the next one, and until the flight computer has timed it.
        /// </summary>
        [KRPCProperty]
        public double IgnitionTime {
            get {
                var target = Target;
                return target == null ? double.NaN : target.IgnitionTime.Seconds ();
            }
        }

        /// <summary>
        /// The time the remaining burn takes with the active engines, in seconds. Not a number
        /// for a burn other than the next one, and until the flight computer has timed it.
        /// </summary>
        [KRPCProperty]
        public double BurnTime {
            get {
                var target = Target;
                return target == null ? double.NaN : target.BurnDuration;
            }
        }

        /// <summary>
        /// The direction of the change in velocity at the time of the burn, as a unit vector.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the direction.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Direction (ReferenceFrame referenceFrame)
        {
            var b = InternalBurn;
            var orbit = b.Patch.Orbit;
            var state = orbit.GetStateVectorsAt (b.Time);
            var vlf2Cci = VehicleReferenceFrameEx.GetVlf2Cci (state.PositionCci, state.VelocityCci)
                ?? throw new InvalidOperationException ("The burn has no orbital direction");
            var directionCci = b.DeltaVVlf.NormalizeOrZero ().Transform (vlf2Cci);
            return Vectors.ToTuple (referenceFrame.DirectionFromEcl (directionCci.Transform (orbit.Parent.GetCci2Cce ())));
        }

        /// <summary>
        /// Remove the burn from the vehicle's burn plan.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            Computer.RemoveBurn (InternalBurn);
        }

        void SetDeltaV (int axis, double value)
        {
            if (!double.IsFinite (value))
                throw new ArgumentException ("The change in velocity is not a finite number");
            var b = InternalBurn;
            var deltaV = b.DeltaVVlf;
            if (axis == 0)
                deltaV.X = value;
            else if (axis == 1)
                deltaV.Y = value;
            else
                deltaV.Z = value;
            b.DeltaVVlf = deltaV;
            Computer.BurnUpdated (b);
        }
    }
}

using System;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A parachute. Obtained from <see cref="Part.Parachute"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Parachute : Equatable<Parachute>
    {
        readonly global::KSA.Parachute parachute;

        internal Parachute (Part part, global::KSA.Parachute parachute)
        {
            Part = part;
            this.parachute = parachute;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Parachute other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (parachute, other.parachute);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return parachute.GetHashCode ();
        }

        /// <summary>
        /// The part the parachute is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        /// <summary>
        /// The state of the parachute.
        /// </summary>
        [KRPCProperty]
        public ParachuteState State {
            get {
                _ = Part.InternalPart;
                return (ParachuteState)parachute.State;
            }
        }

        /// <summary>
        /// How far the canopy is open, from 0 to 1.
        /// </summary>
        [KRPCProperty]
        public float Fill {
            get {
                _ = Part.InternalPart;
                return parachute.Fill;
            }
        }

        /// <summary>
        /// The height above the terrain at which an armed parachute deploys, in meters.
        /// </summary>
        [KRPCProperty]
        public float DeployAltitude {
            get {
                _ = Part.InternalPart;
                return parachute.Tuning.DeployAltitudeM;
            }
        }

        /// <summary>
        /// Whether the parachute can be cut away. Main parachutes cannot.
        /// </summary>
        [KRPCProperty]
        public bool CanCut {
            get {
                var cut = Part.InternalPart.SubtreeModules.Get<ParachuteCut> ();
                return cut.Length > 0 && cut[0].IsEnabled;
            }
        }

        /// <summary>
        /// Arm the parachute, so that it deploys at <see cref="DeployAltitude"/>. Returns once
        /// it is armed. Throws an exception if it is not stowed.
        /// </summary>
        [KRPCMethod]
        public void Deploy ()
        {
            var p = Part.InternalPart;
            var deploy = p.SubtreeModules.Get<ParachuteDeploy> ();
            if (deploy.Length == 0 || parachute.State != ChuteState.Stowed)
                throw new InvalidOperationException ("The parachute cannot deploy");
            deploy[0].SetIsActive (p.Tree.OwningVehicle, true);
            Waits.Until (() => parachute.State != ChuteState.Stowed);
        }

        /// <summary>
        /// Cut the parachute away. Returns once it is cut. Throws an exception if it cannot
        /// be cut. See <see cref="CanCut"/>.
        /// </summary>
        [KRPCMethod]
        public void Cut ()
        {
            if (!CanCut)
                throw new InvalidOperationException ("The parachute cannot be cut");
            var p = Part.InternalPart;
            var cut = p.SubtreeModules.Get<ParachuteCut> ();
            cut[0].SetIsActive (p.Tree.OwningVehicle, true);
            Waits.Until (() => parachute.State == ChuteState.Cut);
        }
    }
}

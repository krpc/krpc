using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A reaction wheel. Obtained by calling <see cref="Part.ReactionWheel"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class ReactionWheel : Equatable<ReactionWheel>
    {
        readonly ModuleReactionWheel reactionWheel;

        internal static bool Is (Part part)
        {
            return Is (part.InternalPart);
        }

        internal static bool Is (global::Part part)
        {
            return part.HasModule<ModuleReactionWheel> ();
        }

        internal ReactionWheel (Part part)
        {
            Part = part;
            reactionWheel = part.InternalPart.Module<ModuleReactionWheel> ();
            if (reactionWheel == null)
                throw new ArgumentException ("Part is not a reaction wheel");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ReactionWheel other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && reactionWheel == other.reactionWheel;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ reactionWheel.GetHashCode ();
        }

        /// <summary>
        /// The part object for this reaction wheel.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the reaction wheel is active.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return reactionWheel.State == ModuleReactionWheel.WheelState.Active; }
            set {
                var active = Active;
                if ((value && !active) || (!value && active))
                    reactionWheel.Toggle (new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate));
            }
        }

        /// <summary>
        /// Whether the reaction wheel is broken.
        /// </summary>
        [KRPCProperty]
        public bool Broken {
            get { return reactionWheel.State == ModuleReactionWheel.WheelState.Broken; }
        }

        /// <summary>
        /// The available torque in the pitch, roll and yaw axes of the vessel, in Newton meters.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// Returns zero if the reaction wheel is inactive or broken.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        public Tuple<Tuple3,Tuple3> AvailableTorque {
            get { return AvailableTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque the reaction wheel can provide, is it active,
        /// in the pitch, roll and yaw axes of the vessel, in Newton meters.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        public Tuple<Tuple3,Tuple3> MaxTorque {
            get { return MaxTorqueVectors.ToTuple (); }
        }

        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        internal Tuple<Vector3d,Vector3d> AvailableTorqueVectors {
            get {
                if (!Active || Broken)
                    return ITorqueProviderExtensions.zero;
                var torque = reactionWheel.GetPotentialTorque ();
                // Note: GetPotentialTorque returns negative torques with incorrect sign
                return new Tuple<Vector3d,Vector3d> (torque.Item1, -torque.Item2);
            }
        }

        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        internal Tuple<Vector3d,Vector3d> MaxTorqueVectors {
            get {
                var torque = new Vector3d (reactionWheel.PitchTorque, reactionWheel.RollTorque, reactionWheel.YawTorque) * 1000.0d;
                return new Tuple<Vector3d,Vector3d> (torque, -torque);
            }
        }
    }
}

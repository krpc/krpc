using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.ReactionWheel"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class ReactionWheel : Equatable<ReactionWheel>
    {
        readonly Part part;
        readonly ModuleReactionWheel reactionWheel;

        internal ReactionWheel (Part part)
        {
            this.part = part;
            reactionWheel = part.InternalPart.Module<ModuleReactionWheel> ();
            if (reactionWheel == null)
                throw new ArgumentException ("Part does not have a ModuleReactionWheel PartModule");
        }

        /// <summary>
        /// Check if reaction wheels are equal.
        /// </summary>
        public override bool Equals (ReactionWheel obj)
        {
            return part == obj.part && reactionWheel == obj.reactionWheel;
        }

        /// <summary>
        /// Hash the reaction wheel.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ reactionWheel.GetHashCode ();
        }

        /// <summary>
        /// The part object for this reaction wheel.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Whether the reaction wheel is active.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return reactionWheel.State == ModuleReactionWheel.WheelState.Active; }
            set {
                if ((value && !Active) || (!value && Active))
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
        /// The available torque in the pitch, yaw and roll axes of the vessel, in Newton meters.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// Returns zero if the reaction wheel is inactive or broken.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Torque {
            get {
                return TorqueVector ().ToTuple ();
            }
        }

        /// <summary>
        /// The available torque in the pitch, yaw and roll axes of the vessel, in Newton meters.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame" />.
        /// Returns zero if the reaction wheel is inactive or broken.
        /// </summary>
        public Vector3d TorqueVector ()
        {
            if (!Active || Broken)
                return Vector3d.zero;
            return (new Vector3d (reactionWheel.PitchTorque, reactionWheel.YawTorque, reactionWheel.RollTorque) * 1000f);
        }
    }
}

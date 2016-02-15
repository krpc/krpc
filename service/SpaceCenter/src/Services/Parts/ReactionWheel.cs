using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

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
            return part == obj.part;
        }

        /// <summary>
        /// Hash the reaction wheel.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode ();
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
        /// The torque in the pitch axis, in Newton meters.
        /// </summary>
        [KRPCProperty]
        public float PitchTorque {
            get { return reactionWheel.RollTorque * 1000f; }
        }

        /// <summary>
        /// The torque in the yaw axis, in Newton meters.
        /// </summary>
        [KRPCProperty]
        public float YawTorque {
            get { return reactionWheel.YawTorque * 1000f; }
        }

        /// <summary>
        /// The torque in the roll axis, in Newton meters.
        /// </summary>
        [KRPCProperty]
        public float RollTorque {
            get { return reactionWheel.PitchTorque * 1000f; }
        }
    }
}

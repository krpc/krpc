using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
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

        public override bool Equals (ReactionWheel obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        [KRPCProperty]
        public bool Active {
            get { return reactionWheel.State == ModuleReactionWheel.WheelState.Active; }
            set {
                if ((value && !Active) || (!value && Active))
                    reactionWheel.Toggle (new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate));
            }
        }

        [KRPCProperty]
        public bool Broken {
            get { return reactionWheel.State == ModuleReactionWheel.WheelState.Broken; }
        }

        [KRPCProperty]
        public float PitchTorque {
            get { return reactionWheel.RollTorque * 1000f; }
        }

        [KRPCProperty]
        public float YawTorque {
            get { return reactionWheel.YawTorque * 1000f; }
        }

        [KRPCProperty]
        public float RollTorque {
            get { return reactionWheel.PitchTorque * 1000f; }
        }
    }
}

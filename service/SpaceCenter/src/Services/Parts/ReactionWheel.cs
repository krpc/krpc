using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = System.Tuple<double, double, double>;
using TupleV3 = System.Tuple<Vector3d, Vector3d>;
using TupleT3 = System.Tuple<System.Tuple<double, double, double>, System.Tuple<double, double, double>>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A reaction wheel. Obtained by calling <see cref="Part.ReactionWheel"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class ReactionWheel : Equatable<ReactionWheel>, IGameObjectState
    {
        ModuleRef reactionWheelRef;

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
            reactionWheelRef = ModuleRef.ForType<ModuleReactionWheel> (part.InternalPart);
            if (reactionWheelRef.Find (part.InternalPart) == null)
                throw new ArgumentException ("Part is not a reaction wheel");
        }

        ModuleReactionWheel InternalReactionWheel {
            get { return (ModuleReactionWheel)reactionWheelRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the reaction wheel: the state of the part
        /// carrying it, or destroyed once that part no longer has the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return reactionWheelRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ReactionWheel other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && reactionWheelRef == other.reactionWheelRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Part).And (reactionWheelRef);
        }

        /// <summary>
        /// The part object for this reaction wheel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the reaction wheel is active.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return InternalReactionWheel.State == ModuleReactionWheel.WheelState.Active; }
            set {
                var active = Active;
                if ((value && !active) || (!value && active))
                    InternalReactionWheel.Toggle (new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate));
            }
        }

        /// <summary>
        /// Whether the reaction wheel is broken.
        /// </summary>
        [KRPCProperty]
        public bool Broken {
            get { return InternalReactionWheel.State == ModuleReactionWheel.WheelState.Broken; }
        }

        /// <summary>
        /// The authority limiter for the reaction wheel, as a percentage of maximum torque.
        /// A value between 0 and 1.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float AuthorityLimiter {
            get { return InternalReactionWheel.authorityLimiter / 100f; }
            set { InternalReactionWheel.authorityLimiter = (value * 100f).Clamp (0f, 100f); }
        }

        /// <summary>
        /// The available torque, in Newton meters, that can be produced by this reaction wheel,
        /// in the positive and negative pitch, roll and yaw axes of the vessel. These axes
        /// correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame"/>.
        /// Returns zero if the reaction wheel is inactive or broken.
        /// </summary>
        [KRPCProperty]
        public TupleT3 AvailableTorque {
            get { return AvailableTorqueVectors.ToTuple (); }
        }

        /// <summary>
        /// The maximum torque, in Newton meters, that can be produced by this reaction wheel,
        /// when it is active, in the positive and negative pitch, roll and yaw axes of the vessel.
        /// These axes correspond to the coordinate axes of the <see cref="Vessel.ReferenceFrame"/>.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public TupleT3 MaxTorque {
            get { return MaxTorqueVectors.ToTuple (); }
        }

        internal TupleV3 AvailableTorqueVectors {
            get {
                // Computed from the wheel's torque fields rather than ModuleReactionWheel's
                // ITorqueProvider implementation, whose handling of the authority limiter
                // varies between the stock module and modded replacements of it. The
                // conditions under which the wheel produces no torque match the stock ones:
                // the module must be enabled and active, and actuator mode 2 disables it.
                var reactionWheel = InternalReactionWheel;
                if (!reactionWheel.moduleIsEnabled || !Active || reactionWheel.actuatorModeCycle == 2)
                    return ITorqueProviderExtensions.zero;
                var torque = MaxTorqueVectors.Item1 * (reactionWheel.authorityLimiter / 100.0);
                return new TupleV3 (torque, -torque);
            }
        }

        internal TupleV3 MaxTorqueVectors {
            get {
                var torque = new Vector3d (InternalReactionWheel.PitchTorque, InternalReactionWheel.RollTorque, InternalReactionWheel.YawTorque) * 1000.0d;
                return new TupleV3 (torque, -torque);
            }
        }
    }
}

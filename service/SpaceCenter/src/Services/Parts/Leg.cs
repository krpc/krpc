using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A landing leg. Obtained by calling <see cref="Part.Leg"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Leg : Equatable<Leg>, IGameObjectState
    {
        ModuleRef wheelRef;
        ModuleRef deploymentRef;
        ModuleRef damageRef;

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            return internalPart.HasModule<ModuleWheelBase> () &&
                   internalPart.Module<ModuleWheelBase> ().wheelType == WheelType.LEG;
        }

        internal Leg (Part part)
        {
            if (!Is (part))
                throw new ArgumentException ("Part is not a landing leg");
            Part = part;
            var internalPart = part.InternalPart;
            wheelRef = ModuleRef.ForType<ModuleWheelBase> (internalPart);
            deploymentRef = ModuleRef.ForType<ModuleWheels.ModuleWheelDeployment> (internalPart);
            damageRef = ModuleRef.ForType<ModuleWheels.ModuleWheelDamage> (internalPart);
        }

        ModuleWheelBase InternalWheel {
            get { return (ModuleWheelBase)wheelRef.Get (Part.InternalPart); }
        }

        ModuleWheels.ModuleWheelDeployment InternalDeployment {
            get { return (ModuleWheels.ModuleWheelDeployment)deploymentRef.Find (Part.InternalPart); }
        }

        ModuleWheels.ModuleWheelDamage InternalDamage {
            get { return (ModuleWheels.ModuleWheelDamage)damageRef.Find (Part.InternalPart); }
        }

        /// <summary>
        /// The state of the part carrying the landing leg, or destroyed once that part
        /// loses the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return wheelRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Leg other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && wheelRef == other.wheelRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Part).And (wheelRef);
        }

        /// <summary>
        /// The part object for this landing leg.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// The current state of the landing leg.
        /// </summary>
        [KRPCProperty]
        public DeployableState State {
            get { return InternalDeployment.ToDeployableState (InternalDamage); }
        }

        /// <summary>
        /// Whether the leg is deployable.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool Deployable {
            get { return InternalDeployment != null; }
        }

        /// <summary>
        /// Whether the landing leg is deployed.
        /// </summary>
        /// <remarks>
        /// Fixed landing legs are always deployed.
        /// Returns an error if you try to deploy fixed landing gear.
        /// </remarks>
        [KRPCProperty]
        public bool Deployed {
            get { return State == DeployableState.Deployed; }
            set {
                if (InternalDeployment == null)
                    throw new InvalidOperationException ("Landing leg is not deployable");
                InternalDeployment.ActionToggle(new KSPActionParam(0, value ? KSPActionType.Activate : KSPActionType.Deactivate));
            }
        }

        /// <summary>
        /// Returns whether the leg is touching the ground.
        /// </summary>
        [KRPCProperty]
        public bool IsGrounded {
            get { return InternalWheel.isGrounded; }
        }
    }
}

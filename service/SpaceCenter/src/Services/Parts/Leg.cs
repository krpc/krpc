using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = System.Tuple<double, double, double>;

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
        ModuleRef suspensionRef;

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
            suspensionRef = ModuleRef.ForType<ModuleWheels.ModuleWheelSuspension> (internalPart);
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

        ModuleWheels.ModuleWheelSuspension InternalSuspension {
            get {
                var suspension = (ModuleWheels.ModuleWheelSuspension)suspensionRef.Find (Part.InternalPart);
                if (suspension == null)
                    throw new InvalidOperationException ("Landing leg does not have suspension");
                return suspension;
            }
        }

        ModuleWheels.ModuleWheelDamage CheckedDamage {
            get {
                var damage = InternalDamage;
                if (damage == null)
                    throw new InvalidOperationException ("Landing leg is not breakable");
                return damage;
            }
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
        /// Throws <see cref="InvalidOperationException"/> if set on a fixed landing leg.
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

        /// <summary>
        /// Suspension spring strength, as set in the editor.
        /// Throws an exception if the leg has no suspension.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float SuspensionSpringStrength {
            get { return InternalSuspension.springTweakable; }
        }

        /// <summary>
        /// Suspension damper strength, as set in the editor.
        /// Throws an exception if the leg has no suspension.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float SuspensionDamperStrength {
            get { return InternalSuspension.damperTweakable; }
        }

        /// <summary>
        /// The spring constant of the suspension, in <unit>kN/m</unit>, or <c>null</c> while
        /// the leg is not grounded.
        /// </summary>
        /// <remarks>
        /// The game updates it while the leg is grounded, from
        /// <see cref="SuspensionSpringStrength"/>, the vessel's mass, the leg's compression and
        /// the local gravity.
        /// </remarks>
        [KRPCProperty]
        public float? SuspensionSpring {
            get {
                return WheelPhysics.Spring (InternalWheel);
            }
        }

        /// <summary>
        /// The damping constant of the suspension, in <unit>kN.s/m</unit>, or <c>null</c>
        /// while the leg is not grounded.
        /// </summary>
        /// <remarks>
        /// The game updates it while the leg is grounded, from
        /// <see cref="SuspensionDamperStrength"/> and <see cref="SuspensionSpring"/>.
        /// </remarks>
        [KRPCProperty]
        public float? SuspensionDamper {
            get {
                return WheelPhysics.Damper (InternalWheel);
            }
        }

        /// <summary>
        /// The travel of the suspension, in meters.
        /// </summary>
        [KRPCProperty]
        public float SuspensionTravel {
            get { return WheelPhysics.Collider (InternalWheel).suspensionDistance; }
        }

        /// <summary>
        /// The force pressing the leg's foot into the ground, in <unit>kN</unit>.
        /// </summary>
        /// <remarks>
        /// The game raises the load of a grounded leg to at least 10 kN.
        /// <see cref="ImpactTolerance"/> applies to the rise in this value.
        /// </remarks>
        [KRPCProperty]
        public float Load {
            get { return WheelPhysics.Load (InternalWheel); }
        }

        /// <summary>
        /// The rise in <see cref="Load"/> over one physics tick that breaks the leg, in
        /// <unit>kN</unit>.
        /// Throws an exception if the leg is not breakable.
        /// </summary>
        /// <remarks>
        /// The game scales the rise by the local gravity divided by standard gravity before
        /// comparing it with this tolerance.
        /// </remarks>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float ImpactTolerance {
            get { return CheckedDamage.impactTolerance; }
        }

        /// <summary>
        /// The multiple of <see cref="ImpactTolerance"/> that destroys the leg, in place of
        /// breaking it.
        /// Throws an exception if the leg is not breakable.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float ExplodeMultiplier {
            get { return CheckedDamage.explodeMultiplier; }
        }

        /// <summary>
        /// The position of the bottom of the leg's foot, in the given reference frame. While
        /// the leg is grounded, this is where it touches the ground.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// position vector is in.</param>
        [KRPCMethod]
        public Tuple3 ContactPosition (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.PositionFromWorldSpace (WheelPhysics.ContactPosition (InternalWheel)).ToTuple ();
        }
    }
}

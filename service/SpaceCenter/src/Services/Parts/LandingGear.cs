using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Landing gear with wheels. Obtained by calling <see cref="Part.LandingGear"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class LandingGear : Equatable<LandingGear>
    {
        readonly ModuleWheelBase wheel;
        readonly ModuleWheels.ModuleWheelDeployment deployment;
        readonly ModuleWheels.ModuleWheelDamage damage;

        internal static bool Is (Part part)
        {
            // TODO: is WheelType.FREE correct? Landing gear are the only stock parts with this wheel type. Rover wheels are WheelType.MOTORIZED
            var internalPart = part.InternalPart;
            return
            internalPart.HasModule<ModuleWheelBase> () &&
            internalPart.Module<ModuleWheelBase> ().wheelType == WheelType.FREE;
        }

        internal LandingGear (Part part)
        {
            if (!Is (part))
                throw new ArgumentException ("Part is not landing gear");
            Part = part;
            var internalPart = part.InternalPart;
            wheel = internalPart.Module<ModuleWheelBase> ();
            deployment = internalPart.Module<ModuleWheels.ModuleWheelDeployment> ();
            damage = internalPart.Module<ModuleWheels.ModuleWheelDamage> ();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (LandingGear other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && wheel == other.wheel;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ wheel.GetHashCode ();
        }

        /// <summary>
        /// The part object for this landing gear.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the landing gear is deployable.
        /// </summary>
        [KRPCProperty]
        public bool Deployable {
            get { return deployment != null; }
        }

        /// <summary>
        /// Gets the current state of the landing gear.
        /// </summary>
        /// <remarks>
        /// Fixed landing gear are always deployed.
        /// </remarks>
        [KRPCProperty]
        public LandingGearState State {
            get {
                if (damage != null && damage.isDamaged)
                    return LandingGearState.Broken;
                if (deployment != null) {
                    if (Math.Abs (deployment.position - deployment.deployedPosition) < 0.0001)
                        return LandingGearState.Deployed;
                    else if (Math.Abs (deployment.position - deployment.retractedPosition) < 0.0001)
                        return LandingGearState.Retracted;
                    else if (deployment.stateString.Equals (deployment.st_deploying.name))
                        return LandingGearState.Deploying;
                    else if (deployment.stateString.Equals (deployment.st_retracting.name))
                        return LandingGearState.Retracting;
                    throw new InvalidOperationException ();
                }
                return LandingGearState.Deployed;
            }
        }

        /// <summary>
        /// Whether the landing gear is deployed.
        /// </summary>
        /// <remarks>
        /// Fixed landing gear are always deployed.
        /// Returns an error if you try to deploy fixed landing gear.
        /// </remarks>
        [KRPCProperty]
        public bool Deployed {
            get { return State == LandingGearState.Deployed; }
            set {
                if (deployment == null)
                    throw new InvalidOperationException ("Landing gear is not deployable");
                if (value) {
                    var extend = deployment.Events.FirstOrDefault (x => x.guiName == "Extend");
                    if (extend != null)
                        extend.Invoke ();
                } else {
                    var retract = deployment.Events.FirstOrDefault (x => x.guiName == "Retract");
                    if (retract != null)
                        retract.Invoke ();
                }
            }
        }

        /// <summary>
        /// Returns whether the gear is touching the ground.
        /// </summary>
        [KRPCProperty]
        public bool IsGrounded {
            get { return wheel.isGrounded; }
        }
    }
}

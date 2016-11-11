using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A landing leg. Obtained by calling <see cref="Part.LandingLeg"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class LandingLeg : Equatable<LandingLeg>
    {
        readonly ModuleWheelBase wheel;
        readonly ModuleWheels.ModuleWheelDeployment deployment;
        readonly ModuleWheels.ModuleWheelDamage damage;

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            return
            internalPart.HasModule<ModuleWheelBase> () &&
            internalPart.Module<ModuleWheelBase> ().wheelType == WheelType.LEG;
        }

        internal LandingLeg (Part part)
        {
            if (!Is (part))
                throw new ArgumentException ("Part is not a landing leg");
            Part = part;
            var internalPart = part.InternalPart;
            wheel = internalPart.Module<ModuleWheelBase> ();
            deployment = internalPart.Module<ModuleWheels.ModuleWheelDeployment> ();
            damage = internalPart.Module<ModuleWheels.ModuleWheelDamage> ();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (LandingLeg other)
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
        /// The part object for this landing leg.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// The current state of the landing leg.
        /// </summary>
        [KRPCProperty]
        public LandingLegState State {
            get {
                if (damage != null && damage.isDamaged)
                    return LandingLegState.Broken;
                if (deployment != null) {
                    if (Math.Abs (deployment.position - deployment.deployedPosition) < 0.0001)
                        return LandingLegState.Deployed;
                    else if (Math.Abs (deployment.position - deployment.retractedPosition) < 0.0001)
                        return LandingLegState.Retracted;
                    else if (deployment.stateString.Equals (deployment.st_deploying.name))
                        return LandingLegState.Deploying;
                    else if (deployment.stateString.Equals (deployment.st_retracting.name))
                        return LandingLegState.Retracting;
                    throw new InvalidOperationException ();
                }
                return LandingLegState.Deployed;
            }
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
            get { return State == LandingLegState.Deployed; }
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
        /// Returns whether the leg is touching the ground.
        /// </summary>
        [KRPCProperty]
        public bool IsGrounded {
            get { return wheel.isGrounded; }
        }
    }
}

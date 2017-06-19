using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A landing leg. Obtained by calling <see cref="Part.Leg"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class Leg : Equatable<Leg>
    {
        readonly ModuleWheelBase wheel;
        readonly ModuleWheels.ModuleWheelDeployment deployment;
        readonly ModuleWheels.ModuleWheelDamage damage;

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
            wheel = internalPart.Module<ModuleWheelBase> ();
            deployment = internalPart.Module<ModuleWheels.ModuleWheelDeployment> ();
            damage = internalPart.Module<ModuleWheels.ModuleWheelDamage> ();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Leg other)
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
        public LegState State {
            get {
                if (damage != null && damage.isDamaged)
                    return LegState.Broken;
                if (deployment != null) {
                    if (Math.Abs (deployment.position - deployment.deployedPosition) < 0.0001)
                        return LegState.Deployed;
                    else if (Math.Abs (deployment.position - deployment.retractedPosition) < 0.0001)
                        return LegState.Retracted;
                    else if (deployment.stateString.Equals (deployment.st_deploying.name))
                        return LegState.Deploying;
                    else if (deployment.stateString.Equals (deployment.st_retracting.name))
                        return LegState.Retracting;
                    throw new InvalidOperationException ();
                }
                return LegState.Deployed;
            }
        }

        /// <summary>
        /// Whether the leg is deployable.
        /// </summary>
        [KRPCProperty]
        public bool Deployable {
            get { return deployment != null; }
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
            get { return State == LegState.Deployed; }
            set {
                if (deployment == null)
                    throw new InvalidOperationException ("Landing leg is not deployable");
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

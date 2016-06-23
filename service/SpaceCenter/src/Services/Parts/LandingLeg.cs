using System;
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
    public class LandingLeg : Equatable<LandingLeg>
    {
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
            deployment = internalPart.Module<ModuleWheels.ModuleWheelDeployment> ();
            damage = internalPart.Module<ModuleWheels.ModuleWheelDamage> ();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (LandingLeg other)
        {
            return
            !ReferenceEquals (other, null) &&
            Part == other.Part &&
            (deployment == other.deployment || deployment.Equals (other.deployment)) &&
            (damage == other.damage || damage.Equals (other.damage));
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            var hash = Part.GetHashCode ();
            if (deployment != null)
                hash ^= deployment.GetHashCode ();
            if (damage != null)
                hash ^= damage.GetHashCode ();
            return hash;
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
                    if (deployment.stateString.Contains ("Deployed"))
                        return LandingLegState.Deployed;
                    else if (deployment.stateString.Contains ("Retracted"))
                        return LandingLegState.Retracted;
                    else if (deployment.stateString.Contains ("Deploying"))
                        return LandingLegState.Deploying;
                    else if (deployment.stateString.Contains ("Retracting"))
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
    }
}

using System;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.LandingLeg"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class LandingLeg : Equatable<LandingLeg>
    {
        readonly Part part;
        readonly ModuleWheels.ModuleWheelDeployment deployment;
        readonly ModuleWheels.ModuleWheelDamage damage;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleWheelBase> () && part.InternalPart.Module<ModuleWheelBase> ().wheelType == WheelType.LEG;
        }

        internal LandingLeg (Part part)
        {
            if (!Is (part))
                throw new ArgumentException ("Part is not a landing leg");
            this.part = part;
            deployment = part.InternalPart.Module<ModuleWheels.ModuleWheelDeployment> ();
            damage = part.InternalPart.Module<ModuleWheels.ModuleWheelDamage> ();
        }

        /// <summary>
        /// Check if the landing legs are equal.
        /// </summary>
        public override bool Equals (LandingLeg obj)
        {
            return part == obj.part && deployment == obj.deployment && damage == obj.damage;
        }

        /// <summary>
        /// Hash the landing leg.
        /// </summary>
        public override int GetHashCode ()
        {
            var hash = part.GetHashCode ();
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
        public Part Part {
            get { return part; }
        }

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
                    throw new ArgumentException ("Unknown landing leg state");
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

using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
    /// <summary>
    /// See <see cref="LandingLeg.State"/>.
    /// </summary>
    [KRPCEnum (Service = "SpaceCenter")]
    public enum LandingLegState
    {
        /// <summary>
        /// Landing leg is fully deployed.
        /// </summary>
        Deployed,
        /// <summary>
        /// Landing leg is fully retracted.
        /// </summary>
        Retracted,
        /// <summary>
        /// Landing leg is being deployed.
        /// </summary>
        Deploying,
        /// <summary>
        /// Landing leg is being retracted.
        /// </summary>
        Retracting,
        /// <summary>
        /// Landing leg is broken.
        /// </summary>
        Broken,
        /// <summary>
        /// Landing leg is being repaired.
        /// </summary>
        Repairing
    }

    /// <summary>
    /// Obtained by calling <see cref="Part.LandingLeg"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class LandingLeg : Equatable<LandingLeg>
    {
        readonly Part part;
        readonly ModuleLandingLeg leg;

        internal LandingLeg (Part part)
        {
            this.part = part;
            leg = part.InternalPart.Module<ModuleLandingLeg> ();
            if (leg == null)
                throw new ArgumentException ("Part does not have a ModuleLandingLeg PartModule");
        }

        public override bool Equals (LandingLeg obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
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
                switch (leg.legState) {
                case ModuleLandingLeg.LegStates.DEPLOYED:
                    return LandingLegState.Deployed;
                case ModuleLandingLeg.LegStates.RETRACTED:
                    return LandingLegState.Retracted;
                case ModuleLandingLeg.LegStates.DEPLOYING:
                    return LandingLegState.Deploying;
                case ModuleLandingLeg.LegStates.RETRACTING:
                    return LandingLegState.Retracting;
                case ModuleLandingLeg.LegStates.BROKEN:
                    return LandingLegState.Broken;
                case ModuleLandingLeg.LegStates.REPAIRING:
                    return LandingLegState.Repairing;
                default:
                    throw new ArgumentException ("Unknown landing leg state");
                }
            }
        }

        /// <summary>
        /// Whether the landing leg is deployed.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get { return State == LandingLegState.Deployed; }
            set {
                if (value)
                    leg.LowerLeg ();
                else
                    leg.RaiseLeg ();
            }
        }
    }
}

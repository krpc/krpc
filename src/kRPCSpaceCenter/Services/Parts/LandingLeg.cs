using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum LandingLegState
    {
        Deployed,
        Retracted,
        Deploying,
        Retracting,
        Broken,
        Repairing
    }

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

        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

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

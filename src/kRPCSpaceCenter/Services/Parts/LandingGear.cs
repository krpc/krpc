using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum LandingGearState
    {
        Deployed,
        Retracted,
        Deploying,
        Retracting
    }

    [KRPCClass (Service = "SpaceCenter")]
    public sealed class LandingGear : Equatable<LandingGear>
    {
        readonly Part part;
        readonly ModuleLandingGear gear;

        internal LandingGear (Part part)
        {
            this.part = part;
            gear = part.InternalPart.Module<ModuleLandingGear> ();
            if (gear == null)
                throw new ArgumentException ("Part does not have a ModuleLandingGear PartModule");
        }

        public override bool Equals (LandingGear obj)
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
        public LandingGearState State {
            get {
                switch (gear.gearState) {
                case ModuleLandingGear.GearStates.DEPLOYED:
                    return LandingGearState.Deployed;
                case ModuleLandingGear.GearStates.RETRACTED:
                    return LandingGearState.Retracted;
                case ModuleLandingGear.GearStates.DEPLOYING:
                    return LandingGearState.Deploying;
                case ModuleLandingGear.GearStates.RETRACTING:
                    return LandingGearState.Retracting;
                default:
                    throw new ArgumentException ("Unknown landing gear state");
                }
            }
        }

        [KRPCProperty]
        public bool Deployed {
            get { return State == LandingGearState.Deployed; }
            set {
                if (value)
                    gear.LowerLandingGear ();
                else
                    gear.RaiseLandingGear ();
            }
        }
    }
}

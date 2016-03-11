using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.LandingGear"/>.
    /// </summary>
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

        /// <summary>
        /// Check the landing gear are equal.
        /// </summary>
        public override bool Equals (LandingGear obj)
        {
            return part == obj.part && gear == obj.gear;
        }

        /// <summary>
        /// Hash the landing gear.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ gear.GetHashCode ();
        }

        /// <summary>
        /// The part object for this landing gear.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Gets the current state of the landing gear.
        /// </summary>
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

        /// <summary>
        /// Whether the landing gear is deployed.
        /// </summary>
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

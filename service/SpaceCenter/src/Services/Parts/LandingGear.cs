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
        readonly ModuleAdvancedLandingGear advGear;
        readonly ModuleLandingGearFixed fixedGear;

        internal LandingGear (Part part)
        {
            this.part = part;
            gear = part.InternalPart.Module<ModuleLandingGear> ();
            advGear = part.InternalPart.Module<ModuleAdvancedLandingGear> ();
            fixedGear = part.InternalPart.Module<ModuleLandingGearFixed> ();
            if (gear == null && advGear == null && fixedGear == null)
                throw new ArgumentException ("Part does not have a ModuleLandingGear, ModuleLandingGearFixed or ModuleAdvancedLandingGear PartModule");
        }

        /// <summary>
        /// Check the landing gear are equal.
        /// </summary>
        public override bool Equals (LandingGear obj)
        {
            return part == obj.part && gear == obj.gear && advGear == obj.advGear && fixedGear == obj.fixedGear;
        }

        /// <summary>
        /// Hash the landing gear.
        /// </summary>
        public override int GetHashCode ()
        {
            int hash = part.GetHashCode ();
            if (gear != null)
                hash ^= gear.GetHashCode ();
            if (advGear != null)
                hash ^= advGear.GetHashCode ();
            if (fixedGear != null)
                hash ^= fixedGear.GetHashCode ();
            return hash;
        }

        /// <summary>
        /// The part object for this landing gear.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Whether the landing gear is deployable.
        /// </summary>
        [KRPCProperty]
        public bool Deployable {
            get { return fixedGear == null; }
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
                if (gear != null) {
                    switch (gear.gearState) {
                    case ModuleLandingGear.GearStates.DEPLOYED:
                        return LandingGearState.Deployed;
                    case ModuleLandingGear.GearStates.RETRACTED:
                        return LandingGearState.Retracted;
                    case ModuleLandingGear.GearStates.DEPLOYING:
                        return LandingGearState.Deploying;
                    case ModuleLandingGear.GearStates.RETRACTING:
                        return LandingGearState.Retracting;
                    }
                } else if (advGear != null) {
                    switch (advGear.gearState) {
                    case ModuleAdvancedLandingGear.GearStates.DEPLOYED:
                        return LandingGearState.Deployed;
                    case ModuleAdvancedLandingGear.GearStates.RETRACTED:
                        return LandingGearState.Retracted;
                    case ModuleAdvancedLandingGear.GearStates.DEPLOYING:
                        return LandingGearState.Deploying;
                    case ModuleAdvancedLandingGear.GearStates.RETRACTING:
                        return LandingGearState.Retracting;
                    }
                } else if (fixedGear != null) {
                    return LandingGearState.Deployed;
                }
                throw new ArgumentException ("Unknown landing gear state");
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
                if (fixedGear != null)
                    throw new InvalidOperationException ("Gear is not deployable");
                if (gear != null && value)
                    gear.LowerLandingGear ();
                else if (gear != null && !value)
                    gear.RaiseLandingGear ();
                else if (advGear != null && value)
                    advGear.LowerLandingGear ();
                else if (advGear != null && !value)
                    advGear.RaiseLandingGear ();
            }
        }
    }
}

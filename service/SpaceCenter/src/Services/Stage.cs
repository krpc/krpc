using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A single stage of a vessel. Obtain activation (burn) stages from
    /// <see cref="Vessel.Stages"/> or <see cref="Vessel.StageAt"/>, and decouple
    /// stages from <see cref="Vessel.DecoupleStages"/> or
    /// <see cref="Vessel.DecoupleStageAt"/>.
    /// </summary>
    /// <remarks>
    /// Delta-v, thrust, TWR, specific impulse, burn time and mass members are only
    /// available on activation stages. On decouple stages those members throw
    /// InvalidOperationException because stock delta-v data does not
    /// apply. Thrust is reported in newtons and masses in kilograms (stock values
    /// are converted from kilonewtons and tonnes).
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public class Stage : Equatable<Stage>
    {
        readonly global::Vessel internalVessel;
        readonly int stageNumber;
        readonly bool decoupleStage;

        internal Stage (global::Vessel vessel, int number, bool decouple)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            internalVessel = vessel;
            stageNumber = number;
            decoupleStage = decouple;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Stage other)
        {
            return !ReferenceEquals (other, null) &&
                   internalVessel.id == other.internalVessel.id &&
                   stageNumber == other.stageNumber &&
                   decoupleStage == other.decoupleStage;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return internalVessel.id.GetHashCode () ^ stageNumber.GetHashCode () ^ decoupleStage.GetHashCode ();
        }

        /// <summary>
        /// The stage number (activation stage for burn stages, decouple stage otherwise).
        /// </summary>
        [KRPCProperty]
        public int Number => stageNumber;

        /// <summary>
        /// The parts that belong to this stage.
        /// </summary>
        [KRPCProperty]
        public IList<KRPC.SpaceCenter.Services.Parts.Part> Parts
        {
            get
            {
                var vesselParts = new Parts.Parts (internalVessel);
                if (decoupleStage)
                    return vesselParts.All.Where (part => part.DecoupleStage == stageNumber).ToList ();
                return vesselParts.All.Where (part => part.Stage == stageNumber).ToList ();
            }
        }

        /// <summary>
        /// Returns a <see cref="Resources"/> object for this stage.
        /// </summary>
        /// <param name="cumulative">When <c>false</c>, only resources assigned to this stage. When <c>true</c>, resources for this stage and all later stage numbers returned by <see cref="Vessel.Stages"/> or <see cref="Vessel.DecoupleStages"/> are included. On activation stages, unstaged resource containers (for example fuel tanks) are grouped with the first higher activation stage before they are detached, so the stage reflects what can be consumed while it burns. Defaults to <c>true</c> so decouple-stage calls match <see cref="Vessel.ResourcesInDecoupleStage"/>.</param>
        [KRPCMethod]
        public Resources Resources (bool cumulative = true)
        {
            return new Resources (internalVessel, stageNumber, cumulative, decoupleStage);
        }

        /// <summary>
        /// Delta-v for this stage in the current situation, in m/s.
        /// </summary>
        [KRPCProperty]
        public float DeltaV => RequireBurnStage ().deltaVActual;

        /// <summary>
        /// Vacuum delta-v for this stage, in m/s.
        /// </summary>
        [KRPCProperty]
        public float VacuumDeltaV => RequireBurnStage ().deltaVinVac;

        /// <summary>
        /// Sea-level delta-v for this stage, in m/s.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelDeltaV => RequireBurnStage ().deltaVatASL;

        /// <summary>
        /// Thrust in the current situation, in newtons.
        /// </summary>
        [KRPCProperty]
        public float Thrust => RequireBurnStage ().thrustActual * 1000f;

        /// <summary>
        /// Vacuum thrust, in newtons.
        /// </summary>
        [KRPCProperty]
        public float VacuumThrust => RequireBurnStage ().thrustVac * 1000f;

        /// <summary>
        /// Sea-level thrust, in newtons.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelThrust => RequireBurnStage ().thrustASL * 1000f;

        /// <summary>
        /// Thrust-to-weight ratio in the current situation.
        /// </summary>
        [KRPCProperty]
        public float Twr => RequireBurnStage ().TWRActual;

        /// <summary>
        /// Vacuum thrust-to-weight ratio.
        /// </summary>
        [KRPCProperty]
        public float VacuumTwr => RequireBurnStage ().TWRVac;

        /// <summary>
        /// Sea-level thrust-to-weight ratio.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelTwr => RequireBurnStage ().TWRASL;

        /// <summary>
        /// Specific impulse in the current situation, in seconds.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse => (float)RequireBurnStage ().ispActual;

        /// <summary>
        /// Vacuum specific impulse, in seconds.
        /// </summary>
        [KRPCProperty]
        public float VacuumSpecificImpulse => (float)RequireBurnStage ().ispVac;

        /// <summary>
        /// Sea-level specific impulse, in seconds.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelSpecificImpulse => (float)RequireBurnStage ().ispASL;

        /// <summary>
        /// Burn time for this stage, in seconds.
        /// </summary>
        [KRPCProperty]
        public float BurnTime => (float)RequireBurnStage ().stageBurnTime;

        /// <summary>
        /// Start mass for this stage, in kg.
        /// </summary>
        [KRPCProperty]
        public float StartMass => RequireBurnStage ().startMass * 1000f;

        /// <summary>
        /// End mass for this stage, in kg.
        /// </summary>
        [KRPCProperty]
        public float EndMass => RequireBurnStage ().endMass * 1000f;

        /// <summary>
        /// Dry mass for this stage, in kg.
        /// </summary>
        [KRPCProperty]
        public float DryMass => RequireBurnStage ().dryMass * 1000f;

        /// <summary>
        /// Fuel mass for this stage, in kg.
        /// </summary>
        [KRPCProperty]
        public float FuelMass => RequireBurnStage ().fuelMass * 1000f;

        DeltaVStageInfo RequireBurnStage ()
        {
            if (decoupleStage)
                throw new InvalidOperationException ("Delta-v information is not available for a decouple stage.");
            var dv = internalVessel.VesselDeltaV;
            if (dv == null || !dv.IsReady)
                throw new InvalidOperationException ("Delta-v has not been calculated for this vessel yet.");
            var stageInfo = dv.GetStage (stageNumber);
            if (stageInfo == null)
                throw new InvalidOperationException (
                    string.Format (
                        "Delta-v information is not available for activation stage {0}.",
                        stageNumber));
            return stageInfo;
        }
    }
}

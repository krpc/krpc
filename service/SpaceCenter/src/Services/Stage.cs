using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Parts = KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A single stage of a vessel. Obtain activation (burn) stages from
    /// <c>Vessel.Stages</c> / <c>Vessel.StageAt</c>, and decouple stages from
    /// <c>Vessel.DecoupleStages</c> / <c>Vessel.DecoupleStageAt</c>.
    /// </summary>
    /// <remarks>
    /// Delta-v, thrust, TWR, specific impulse, burn time and mass members are only
    /// available on activation stages. On decouple stages those members throw
    /// InvalidOperationException because stock delta-v data does not
    /// apply. Thrust is reported in newtons and masses in kilograms (stock values
    /// are converted from kilonewtons and tonnes).
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public class Stage : Equatable<Stage>, IGameObjectState
    {
        /// <summary>
        /// The id of the vessel the stage belongs to. Unused when the stage belongs to
        /// the vessel in the editor.
        /// </summary>
        readonly Guid vesselId;

        /// <summary>
        /// Whether the stage belongs to the vessel under construction in the editor.
        /// The editor only ever holds one vessel, so it needs no identifier.
        /// </summary>
        readonly bool editorVessel;

        /// <summary>
        /// The stage number.
        /// </summary>
        readonly int stageNumber;

        /// <summary>
        /// Whether this is a decouple stage.
        /// </summary>
        readonly bool decoupleStage;

        /// <summary>
        /// Initializes a stage object by storing the vessel id, stage number,
        /// and whether it is a decouple stage, while rejecting a null vessel.
        /// </summary>
        internal Stage (global::Vessel vessel, int number, bool decouple)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            vesselId = vessel.id;
            stageNumber = number;
            decoupleStage = decouple;
        }

        /// <summary>
        /// Initializes a stage object for the vessel under construction in the editor.
        /// </summary>
        internal Stage (int number, bool decouple)
        {
            editorVessel = true;
            stageNumber = number;
            decoupleStage = decouple;
        }

        /// <summary>
        /// What the game holds for the vessel the stage belongs to.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                return editorVessel
                    ? EditorExtensions.ShipState
                    : FlightGlobalsExtensions.VesselState (vesselId);
            }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Stage other)
        {
            return !ReferenceEquals (other, null) &&
                   vesselId == other.vesselId &&
                   editorVessel == other.editorVessel &&
                   stageNumber == other.stageNumber &&
                   decoupleStage == other.decoupleStage;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vesselId.GetHashCode () ^ editorVessel.GetHashCode () ^
                   stageNumber.GetHashCode () ^ decoupleStage.GetHashCode ();
        }

        /// <summary>
        /// The KSP vessel. Looked up by id on each use, so a stage keeps working across a
        /// scene reload that destroys the vessel and recreates it under the same id.
        /// </summary>
        public global::Vessel InternalVessel {
            get {
                if (editorVessel)
                    throw new InvalidOperationException (
                        "This stage belongs to the vessel in the editor, not to a vessel in flight.");
                return FlightGlobalsExtensions.GetVesselById (vesselId);
            }
        }

        /// <summary>
        /// The stock delta-v simulation the stage's figures come from, for either the
        /// vessel in flight or the vessel in the editor.
        /// </summary>
        VesselDeltaV InternalDeltaV {
            get {
                return editorVessel
                    ? EditorExtensions.GetShip ().vesselDeltaV
                    : InternalVessel.VesselDeltaV;
            }
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
        public IList<Parts.Part> Parts
        {
            get
            {
                var parts = editorVessel ? new Parts.Parts () : new Parts.Parts (InternalVessel);
                if (decoupleStage)
                    return parts.All.Where (part => part.DecoupleStage == stageNumber).ToList ();
                return parts.All.Where (part => part.Stage == stageNumber).ToList ();
            }
        }

        /// <summary>
        /// Returns a <see cref="Resources"/> object for this stage.
        /// </summary>
        /// <param name="cumulative">
        /// When <c>false</c>, only resources assigned to this stage. 
        /// When <c>true</c>, resources for this stage and all later activation or decouple stage 
        /// numbers are included. On activation stages, unstaged resource containers (for example 
        /// fuel tanks) are grouped with the first higher activation stage before they are detached. 
        /// Defaults to <c>true</c> so decouple-stage calls match the legacy 
        /// <c>Vessel.ResourcesInDecoupleStage</c> RPC.
        /// </param>
        [KRPCMethod]
        public Resources Resources (bool cumulative = true)
        {
            if (editorVessel)
                return new Resources (stageNumber, cumulative, decoupleStage);
            return new Resources (InternalVessel, stageNumber, cumulative, decoupleStage);
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
        public float TWR => RequireBurnStage ().TWRActual;

        /// <summary>
        /// Vacuum thrust-to-weight ratio.
        /// </summary>
        [KRPCProperty]
        public float VacuumTWR => RequireBurnStage ().TWRVac;

        /// <summary>
        /// Sea-level thrust-to-weight ratio.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelTWR => RequireBurnStage ().TWRASL;

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

        /// <summary>
        /// Validates that a stage can provide burn-related delta-v data 
        /// before returning it. It throws an exception for decouple stages, 
        /// for vessels whose delta-v calculations are not ready, or when 
        /// the requested activation stage has no available delta-v 
        /// information.
        /// </summary>
        DeltaVStageInfo RequireBurnStage ()
        {
            if (decoupleStage)
                throw new InvalidOperationException ("Delta-v information is not available for a decouple stage.");
            var dv = InternalDeltaV;
            if (dv == null || !dv.IsReady || (editorVessel && !EditorDeltaV.Ready))
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

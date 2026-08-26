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
        /// The state of the vessel the stage belongs to.
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
            return Hash.Of (vesselId).And (editorVessel).And (stageNumber).And (decoupleStage);
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
        /// The stock delta-v simulation the figures of a stage of the vessel under
        /// construction come from. A stage in flight goes through
        /// <see cref="FlightDeltaV" />, which also asks the game to run the simulation.
        /// </summary>
        VesselDeltaV EditorInternalDeltaV {
            get { return EditorExtensions.GetShip ().vesselDeltaV; }
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
        public float DeltaV => Figure (stage => stage.deltaVActual);

        /// <summary>
        /// Vacuum delta-v for this stage, in m/s.
        /// </summary>
        [KRPCProperty]
        public float VacuumDeltaV => Figure (stage => stage.deltaVinVac);

        /// <summary>
        /// Sea-level delta-v for this stage, in m/s.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelDeltaV => Figure (stage => stage.deltaVatASL);

        /// <summary>
        /// Thrust in the current situation, in newtons.
        /// </summary>
        [KRPCProperty]
        public float Thrust => Figure (stage => stage.thrustActual * 1000f);

        /// <summary>
        /// Vacuum thrust, in newtons.
        /// </summary>
        [KRPCProperty]
        public float VacuumThrust => Figure (stage => stage.thrustVac * 1000f);

        /// <summary>
        /// Sea-level thrust, in newtons.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelThrust => Figure (stage => stage.thrustASL * 1000f);

        /// <summary>
        /// Thrust-to-weight ratio in the current situation.
        /// </summary>
        [KRPCProperty]
        public float TWR => Figure (stage => stage.TWRActual);

        /// <summary>
        /// Vacuum thrust-to-weight ratio.
        /// </summary>
        [KRPCProperty]
        public float VacuumTWR => Figure (stage => stage.TWRVac);

        /// <summary>
        /// Sea-level thrust-to-weight ratio.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelTWR => Figure (stage => stage.TWRASL);

        /// <summary>
        /// Specific impulse in the current situation, in seconds.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse => Figure (stage => (float)stage.ispActual);

        /// <summary>
        /// Vacuum specific impulse, in seconds.
        /// </summary>
        [KRPCProperty]
        public float VacuumSpecificImpulse => Figure (stage => (float)stage.ispVac);

        /// <summary>
        /// Sea-level specific impulse, in seconds.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelSpecificImpulse => Figure (stage => (float)stage.ispASL);

        /// <summary>
        /// Burn time for this stage, in seconds.
        /// </summary>
        [KRPCProperty]
        public float BurnTime => Figure (stage => (float)stage.stageBurnTime);

        /// <summary>
        /// Start mass for this stage, in kg.
        /// </summary>
        [KRPCProperty]
        public float StartMass => Figure (stage => stage.startMass * 1000f);

        /// <summary>
        /// End mass for this stage, in kg.
        /// </summary>
        [KRPCProperty]
        public float EndMass => Figure (stage => stage.endMass * 1000f);

        /// <summary>
        /// Dry mass for this stage, in kg.
        /// </summary>
        [KRPCProperty]
        public float DryMass => Figure (stage => stage.dryMass * 1000f);

        /// <summary>
        /// Fuel mass for this stage, in kg.
        /// </summary>
        [KRPCProperty]
        public float FuelMass => Figure (stage => stage.fuelMass * 1000f);

        /// <summary>
        /// Read a figure off the stage's entry in the stock delta-v simulation. In flight
        /// the game is asked for a run when the figures are out of date, and the read
        /// yields until it has finished.
        /// </summary>
        T Figure<T> (Func<DeltaVStageInfo, T> read)
        {
            if (decoupleStage)
                throw new InvalidOperationException (
                    "Delta-v information is not available for a decouple stage.");
            if (!editorVessel)
                return FlightDeltaV.Read (vesselId, deltaV => read (BurnStage (deltaV)));
            var ship = EditorInternalDeltaV;
            if (ship == null || !ship.IsReady || !EditorDeltaV.Ready)
                throw new InvalidOperationException (
                    "Delta-v has not been calculated for this vessel yet.");
            return read (BurnStage (ship));
        }

        /// <summary>
        /// The stage's entry in the simulation, or an error when the simulation holds none
        /// for its number.
        /// </summary>
        DeltaVStageInfo BurnStage (VesselDeltaV deltaV)
        {
            var stageInfo = deltaV.GetStage (stageNumber);
            if (stageInfo == null)
                throw new InvalidOperationException (
                    string.Format (
                        "Delta-v information is not available for activation stage {0}.",
                        stageNumber));
            return stageInfo;
        }
    }
}

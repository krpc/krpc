using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Experiment.Data"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class ScienceData : Equatable<ScienceData>, IGameObjectState
    {
        readonly Part part;
        ModuleRef experimentRef;
        // The game's own record of the data. It is a plain object rather than something in
        // the scene, and the game offers nothing to identify it by, so it is the one thing
        // here that is held on to rather than found again. When the game replaces it, which
        // it does whenever it rebuilds the experiment, this object is reclaimed instead.
        readonly global::ScienceData data;

        internal ScienceData (Part dataPart, ModuleScienceExperiment experimentModule, global::ScienceData scienceData)
        {
            part = dataPart;
            experimentRef = ModuleRef.ForModule (experimentModule);
            data = scienceData;
        }

        ModuleScienceExperiment InternalExperiment {
            get { return (ModuleScienceExperiment)experimentRef.Get (part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the record. It is as live, dormant or destroyed as the
        /// experiment holding it until that experiment is there to look in, and destroyed
        /// once the experiment no longer holds this record.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = experimentRef.StateOn (part);
                if (state != GameObjectState.Live)
                    return state;
                var container = experimentRef.Find (part.InternalPart) as IScienceDataContainer;
                return container != null && container.GetData ().Contains (data)
                    ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ScienceData other)
        {
            return !ReferenceEquals (other, null) && part == other.part &&
            experimentRef == other.experimentRef && data == other.data;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ experimentRef.GetHashCode () ^ data.GetHashCode ();
        }

        /// <summary>
        /// Data amount.
        /// </summary>
        [KRPCProperty]
        public float DataAmount {
            get { return data.dataAmount; }
        }

        /// <summary>
        /// Science value.
        /// </summary>
        [KRPCProperty]
        public float ScienceValue {
            get {
                var subject = ResearchAndDevelopment.GetSubjectByID (data.subjectID);
                if (subject == null)
                    return 0;
                return ResearchAndDevelopment.GetScienceValue (data.dataAmount, subject, 1) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
            }
        }

        /// <summary>
        /// Transmit value.
        /// </summary>
        [KRPCProperty]
        public float TransmitValue {
            get {
                // Use ExperimentResultDialogPage to compute the science value
                ExperimentResultDialogPage page = new ExperimentResultDialogPage(
                    InternalExperiment.part, data, data.baseTransmitValue, data.transmitBonus,
                    false, string.Empty, false,
                    new ScienceLabSearch(InternalExperiment.part.vessel, data),
                    null, null, null, null);
                return page.baseTransmitValue * page.TransmitBonus;
            }
        }

    }
}

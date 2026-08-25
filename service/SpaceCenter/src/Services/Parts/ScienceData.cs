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
        // The game's own record of the data. It is a plain object with nothing to identify
        // it by, so it is held rather than found again. The game replaces it whenever it
        // rebuilds the experiment, and this object is then reclaimed
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
        /// The state of the record. It takes the state of the experiment holding it, and is
        /// destroyed once a live experiment stops holding it.
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
            return Hash.Of (part).And (experimentRef).And (data);
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

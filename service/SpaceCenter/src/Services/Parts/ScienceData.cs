using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Experiment.Data"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class ScienceData : Equatable<ScienceData>
    {
        readonly Part part;
        readonly ModuleRef<ModuleScienceExperiment> experimentRef;
        readonly global::ScienceData data;

        internal ScienceData (ModuleScienceExperiment experimentModule, global::ScienceData scienceData)
        {
            part = new Part (experimentModule.part);
            experimentRef = new ModuleRef<ModuleScienceExperiment> (experimentModule.part, experimentModule);
            data = scienceData;
        }

        // The experiment module, re-derived from the live part on each access, so no
        // reference to the destroyed module (and its vessel graph) is retained.
        ModuleScienceExperiment experiment {
            get { return experimentRef.Resolve (part.InternalPart); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ScienceData other)
        {
            return !ReferenceEquals (other, null) && part == other.part &&
                   experimentRef.Occurrence == other.experimentRef.Occurrence && data == other.data;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ experimentRef.Occurrence ^ (data == null ? 0 : data.GetHashCode ());
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
                    experiment.part, data, data.baseTransmitValue, data.transmitBonus,
                    false, string.Empty, false,
                    new ScienceLabSearch(experiment.part.vessel, data),
                    null, null, null, null);
                return page.baseTransmitValue * page.TransmitBonus;
            }
        }

    }
}

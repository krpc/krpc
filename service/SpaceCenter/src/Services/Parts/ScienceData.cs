using System.Linq;
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
        readonly int experimentIndex;
        readonly global::ScienceData data;

        internal ScienceData (ModuleScienceExperiment experimentModule, global::ScienceData scienceData)
        {
            part = new Part (experimentModule.part);
            experimentIndex = experimentModule.part.Modules.OfType<ModuleScienceExperiment> ().ToList ().IndexOf (experimentModule);
            data = scienceData;
        }

        // The experiment module, re-derived from the live part on each access, so no
        // reference to the destroyed module (and its vessel graph) is retained.
        ModuleScienceExperiment experiment {
            get {
                var experiments = part.InternalPart.Modules.OfType<ModuleScienceExperiment> ().ToList ();
                if (experimentIndex >= experiments.Count)
                    throw new PartDestroyedException ("The experiment no longer exists.");
                return experiments [experimentIndex];
            }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ScienceData other)
        {
            return !ReferenceEquals (other, null) && part == other.part &&
                   experimentIndex == other.experimentIndex && data == other.data;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ experimentIndex ^ (data == null ? 0 : data.GetHashCode ());
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

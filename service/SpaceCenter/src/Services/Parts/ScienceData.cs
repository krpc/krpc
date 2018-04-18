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
        readonly ModuleScienceExperiment experiment;
        readonly global::ScienceData data;

        internal ScienceData (ModuleScienceExperiment experimentModule, global::ScienceData scienceData)
        {
            experiment = experimentModule;
            data = scienceData;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ScienceData other)
        {
            return !ReferenceEquals (other, null) && experiment == other.experiment && data == other.data;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return experiment.GetHashCode () ^ data.GetHashCode ();
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

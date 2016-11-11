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
            get { return GetScienceValue (1); }
        }

        /// <summary>
        /// Transmit value.
        /// </summary>
        [KRPCProperty]
        public float TransmitValue {
            get { return GetScienceValue (data.baseTransmitValue); }
        }

        float GetScienceValue (float transmitValue)
        {
            var subject = ResearchAndDevelopment.GetSubjectByID (data.subjectID);
            if (subject == null)
                return 0;
            return ResearchAndDevelopment.GetScienceValue (data.dataAmount, subject, transmitValue) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
        }
    }
}

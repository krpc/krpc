using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Experiment.Data"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class ScienceData : Equatable<ScienceData>
    {
        readonly ModuleScienceExperiment experiment;
        readonly global::ScienceData data;

        internal ScienceData (ModuleScienceExperiment experiment, global::ScienceData data)
        {
            this.experiment = experiment;
            this.data = data;
        }

        /// <summary>
        /// Check if data are equal.
        /// </summary>
        public override bool Equals (ScienceData obj)
        {
            return experiment == obj.experiment && data == obj.data;
        }

        /// <summary>
        /// Hash the data.
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
            get { return GetScienceValue (data.transmitValue); }
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

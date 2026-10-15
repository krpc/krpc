using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Experiment.Data"/>.
    /// </summary>
    [KRPCStruct (Service = "SpaceCenter")]
    public struct ScienceData
    {
        internal ScienceData (global::Part part, global::ScienceData data) : this ()
        {
            DataAmount = data.dataAmount;
            var subject = ResearchAndDevelopment.GetSubjectByID (data.subjectID);
            if (subject == null)
                return;
            var gain = HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
            ScienceValue = ResearchAndDevelopment.GetScienceValue (data.dataAmount, subject, 1) * gain;
            TransmitValue = ComputeTransmitValue (part, data, subject, gain);
        }

        // The transmit value as ExperimentResultDialogPage computes it
        static float ComputeTransmitValue (
            global::Part part, global::ScienceData data, global::ScienceSubject subject, float gain)
        {
            var scienceValue = gain * ResearchAndDevelopment.GetScienceValue (
                data.dataAmount, data.scienceValueRatio, subject, 1);
            var transmitValue = gain * ResearchAndDevelopment.GetScienceValue (
                data.dataAmount, data.scienceValueRatio, subject, data.baseTransmitValue);
            if (scienceValue <= 0)
                return transmitValue;
            return Math.Min (transmitValue * (1 + CommNetBonus (part.vessel)), scienceValue);
        }

        // The bonus the signal strength of the vessel's control path adds to transmitted science
        static float CommNetBonus (global::Vessel vessel)
        {
            if (!CommNet.CommNetScenario.CommNetEnabled || vessel == null || vessel.connection == null)
                return 0;
            var node = vessel.connection.Comm;
            if (node == null || node.scienceCurve == null)
                return 0;
            return (float)node.scienceCurve.Evaluate (vessel.connection.ControlPath.signalStrength);
        }

        /// <summary>
        /// Data amount.
        /// </summary>
        [KRPCProperty]
        public float DataAmount { get; set; }

        /// <summary>
        /// Science value.
        /// </summary>
        [KRPCProperty]
        public float ScienceValue { get; set; }

        /// <summary>
        /// Transmit value.
        /// </summary>
        [KRPCProperty]
        public float TransmitValue { get; set; }
    }
}

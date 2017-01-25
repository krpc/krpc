using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Experiment.ScienceSubject"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class ScienceSubject
    {
        readonly global::ScienceSubject data;

        readonly float gainMultiplier = HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;

        internal ScienceSubject (global::ScienceSubject subject)
        {
            data = subject;
        }

        /// <summary>
        /// Amount of science already earned from this subject, not updated until after transmission/recovery.
        /// </summary>
        [KRPCProperty]
        public float Science {
            get { return data.science * gainMultiplier; }
        }

        /// <summary>
        /// Total science allowable for this subject.
        /// </summary>
        [KRPCProperty]
        public float ScienceCap {
            get { return data.scienceCap * gainMultiplier; }
        }

        /// <summary>
        /// Whether the experiment has been completed.
        /// </summary>
        [KRPCProperty]
        public bool IsComplete {
            get { return Math.Abs (ScienceCap - Science) < 0.0001; }
        }

        /// <summary>
        /// Multiply science value by this to determine data amount in mits.
        /// </summary>
        [KRPCProperty]
        public float DataScale {
            get { return data.dataScale; }
        }

        /// <summary>
        /// Diminishing value multiplier for decreasing the science value returned from repeated experiments.
        /// </summary>
        [KRPCProperty]
        public float ScientificValue {
            get { return data.scientificValue; }
        }

        /// <summary>
        /// Multiplier for specific Celestial Body/Experiment Situation combination.
        /// </summary>
        [KRPCProperty]
        public float SubjectValue {
            get { return data.subjectValue; }
        }

        /// <summary>
        /// Title of science subject, displayed in science archives
        /// </summary>
        [KRPCProperty]
        public string Title {
            get { return data.title; }
        }
    }
}

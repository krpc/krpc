using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Experiment.ScienceSubject"/>.
    /// </summary>
    [KRPCStruct (Service = "SpaceCenter")]
    public struct ScienceSubject
    {
        internal ScienceSubject (global::ScienceSubject subject) : this ()
        {
            if (ReferenceEquals (subject, null))
                throw new ArgumentNullException (nameof (subject));
            Id = subject.id;
            var gainMultiplier = HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
            Science = subject.science * gainMultiplier;
            ScienceCap = subject.scienceCap * gainMultiplier;
            IsComplete = Math.Abs (ScienceCap - Science) < 0.0001;
            DataScale = subject.dataScale;
            ScientificValue = subject.scientificValue;
            SubjectValue = subject.subjectValue;
            Title = subject.title;
        }

        /// <summary>
        /// The identifier of the subject, e.g. <c>crewReport@KerbinSrfLandedLaunchPad</c>.
        /// </summary>
        [KRPCProperty]
        public string Id { get; set; }

        /// <summary>
        /// The title of the subject, as shown in the science archives.
        /// </summary>
        [KRPCProperty]
        public string Title { get; set; }

        /// <summary>
        /// Whether the subject has been fully researched. This is true once the science banked for
        /// the subject reaches the science cap. As the banked science (see <see cref="Science"/>) is
        /// only updated after transmission/recovery, this reflects fully mining the subject over
        /// repeated experiments, not whether a single run has produced data. To check whether a run
        /// has produced data, and how valuable it is, use <see cref="Experiment.HasData"/> and
        /// <see cref="ScienceData.TransmitValue"/>.
        /// </summary>
        [KRPCProperty]
        public bool IsComplete { get; set; }

        /// <summary>
        /// Amount of science already earned from this subject, not updated until after
        /// transmission/recovery.
        /// </summary>
        [KRPCProperty]
        public float Science { get; set; }

        /// <summary>
        /// Total science allowable for this subject.
        /// </summary>
        [KRPCProperty]
        public float ScienceCap { get; set; }

        /// <summary>
        /// Multiply science value by this to determine data amount in mits.
        /// </summary>
        [KRPCProperty]
        public float DataScale { get; set; }

        /// <summary>
        /// Multiplier for specific Celestial Body/Experiment Situation combination.
        /// </summary>
        [KRPCProperty]
        public float SubjectValue { get; set; }

        /// <summary>
        /// Diminishing value multiplier for decreasing the science value returned from repeated
        /// experiments.
        /// </summary>
        [KRPCProperty]
        public float ScientificValue { get; set; }
    }
}

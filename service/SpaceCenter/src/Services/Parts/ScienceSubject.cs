using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Experiment.ScienceSubject"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class ScienceSubject : Equatable<ScienceSubject>, IGameObjectState
    {
        // The game's own science subject. A subject the game has recorded science for is
        // named by its id, but one that has not yet been researched is built on the spot
        // and the game keeps nothing to look it up by, so the object itself is held. It
        // belongs to the game state it was taken from, and goes when that state does.
        readonly global::ScienceSubject data;
        readonly string subjectId;
        readonly uint generation;

        internal ScienceSubject (global::ScienceSubject subject)
        {
            if (ReferenceEquals (subject, null))
                throw new ArgumentNullException (nameof (subject));
            data = subject;
            subjectId = subject.id;
            generation = GameState.Generation;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        /// <remarks>
        /// The game state the subject was taken from is part of what this stands for:
        /// the science recorded against a subject belongs to the game that recorded it,
        /// so subjects with the same id taken from two game states are not the same one.
        /// </remarks>
        public override bool Equals (ScienceSubject other)
        {
            return !ReferenceEquals (other, null) &&
            subjectId == other.subjectId && generation == other.generation;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return subjectId.GetHashCode () ^ (int)generation;
        }

        /// <summary>
        /// What the game holds for the subject. It belongs to the game state it was taken
        /// from, so it is destroyed once another state is loaded, with no dormant state to
        /// return to.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                return generation == GameState.Generation
                    ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        /// <summary>
        /// The KSP science subject, checked to belong to the game state that is loaded.
        /// </summary>
        /// <remarks>
        /// The game keeps a subject only once science has been banked against it, and
        /// builds one that has none on the spot, so this asks for the game's own record
        /// and falls back to what it was made from. Without that, a subject read before
        /// any science was banked would go on reporting none, as the object that stands
        /// for it is shared by every later read of the same subject.
        /// </remarks>
        global::ScienceSubject InternalSubject {
            get {
                if (generation != GameState.Generation)
                    throw new ObjectDestroyedException (
                        "The science subject no longer exists, as it was taken from a game " +
                        "state that has since been replaced.");
                return ResearchAndDevelopment.GetSubjectByID (subjectId) ?? data;
            }
        }

        /// <summary>
        /// How much the game scales the science earned from a subject, which a game's
        /// difficulty settings fix and the player can change while it is running.
        /// </summary>
        static float GainMultiplier {
            get { return HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier; }
        }

        /// <summary>
        /// Amount of science already earned from this subject, not updated until after
        /// transmission/recovery.
        /// </summary>
        [KRPCProperty]
        public float Science {
            get { return InternalSubject.science * GainMultiplier; }
        }

        /// <summary>
        /// Total science allowable for this subject.
        /// </summary>
        [KRPCProperty]
        public float ScienceCap {
            get { return InternalSubject.scienceCap * GainMultiplier; }
        }

        /// <summary>
        /// Whether the subject has been fully researched. This is true once the science banked for
        /// the subject reaches the science cap. As the banked science (see <see cref="Science"/>) is
        /// only updated after transmission/recovery, this reflects fully mining the subject over
        /// repeated experiments, not whether a single run has produced data. To check whether a run
        /// has produced data, and how valuable it is, use <see cref="Experiment.HasData"/> and
        /// <see cref="ScienceData.TransmitValue"/>.
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
            get { return InternalSubject.dataScale; }
        }

        /// <summary>
        /// Diminishing value multiplier for decreasing the science value returned from repeated
        /// experiments.
        /// </summary>
        [KRPCProperty]
        public float ScientificValue {
            get { return InternalSubject.scientificValue; }
        }

        /// <summary>
        /// Multiplier for specific Celestial Body/Experiment Situation combination.
        /// </summary>
        [KRPCProperty]
        public float SubjectValue {
            get { return InternalSubject.subjectValue; }
        }

        /// <summary>
        /// Title of science subject, displayed in science archives
        /// </summary>
        [KRPCProperty]
        public string Title {
            get { return InternalSubject.title; }
        }
    }
}

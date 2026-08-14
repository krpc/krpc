using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.Experiment"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Experiment : Equatable<Experiment>, IGameObjectState
    {
        ModuleRef experimentRef;

        internal Experiment(Part part, ModuleScienceExperiment experiment_)
        {
            if (experiment_ == null)
                throw new ArgumentException ("Part is not a science experiment");
            Part = part;
            experimentRef = ModuleRef.ForModule (experiment_);
        }

        ModuleScienceExperiment InternalExperiment {
            get { return (ModuleScienceExperiment)experimentRef.Get (Part.InternalPart); }
        }

        // Note: IScienceDataContainer needs to be used for some methods, rather than
        // ModuleScienceExperiment, as method dispatch uses the wrong method implementation for
        // DMagic science experiments
        IScienceDataContainer InternalDataContainer {
            get { return InternalExperiment; }
        }

        /// <summary>
        /// What the game holds for the experiment: the state of the part
        /// carrying it, or destroyed once that part no longer has the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return experimentRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Experiment other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && experimentRef == other.experimentRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ experimentRef.GetHashCode ();
        }

        /// <summary>
        /// The part object for this experiment.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }


        /// <summary>
        /// Internal name of the experiment, as used in
        /// <a href="https://wiki.kerbalspaceprogram.com/wiki/CFG_File_Documentation">part cfg files</a>.
        /// </summary>
        [KRPCProperty]
        public string Name
        {
            get { return InternalExperiment.experimentID; }
        }

        /// <summary>
        /// Title of the experiment, as shown on the in-game UI.
        /// </summary>
        [KRPCProperty]
        public string Title
        {
            get { return InternalExperiment.experiment.experimentTitle; }
        }

        /// <summary>
        /// Run the experiment.
        /// </summary>
        [KRPCMethod]
        public void Run ()
        {
            if (HasData)
                throw new InvalidOperationException ("Experiment already contains data");
            if (Inoperable)
                throw new InvalidOperationException ("Experiment is inoperable");
            // Stock experiments. KSP has no public method that runs an experiment without
            // also showing the results dialog: DeployExperiment, DeployAction and
            // DeployExperimentExternal all start gatherData(showDialog: true), and the only
            // dialog-free caller, OnActive, is gated on staging configuration. So invoke the
            // private gatherData coroutine directly, mirroring KSP's own staging path.
            var gatherData = InternalExperiment.GetType ().GetMethod ("gatherData", BindingFlags.NonPublic | BindingFlags.Instance);
            if (gatherData != null)
            {
                var result = (IEnumerator)gatherData.Invoke(InternalExperiment, new object[] { false });
                InternalExperiment.StartCoroutine(result);
                return;
            }
            // DMagic experiments
            gatherData = InternalExperiment.GetType().GetMethod("gatherScienceData", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (gatherData != null) {
                gatherData.Invoke(InternalExperiment, new object[] { true });
                return;
            }
            throw new InvalidOperationException("Failed to find a gather data method for this experiment");
        }

        /// <summary>
        /// Transmit all experimental data contained by this part.
        /// </summary>
        [KRPCMethod]
        public void Transmit ()
        {
            var data = InternalDataContainer.GetData ();
            for (int i = 0; i < data.Length; i++) {
                // Use ExperimentResultDialogPage to compute the science value
                // This object creation modifies the data object
                new ExperimentResultDialogPage(
                    InternalExperiment.part, data[i], data[i].baseTransmitValue, data[i].transmitBonus,
                    false, string.Empty, false,
                    new ScienceLabSearch(InternalExperiment.part.vessel, data[i]),
                    null, null, null, null);
            }
            var transmitter = ScienceUtil.GetBestTransmitter(InternalExperiment.vessel);
            if (transmitter == null)
                throw new InvalidOperationException ("No transmitters available to transmit the data");
            transmitter.TransmitData (data.ToList ());
            for (int i = 0; i < data.Length; i++)
                InternalDataContainer.DumpData(data[i]);
            if (InternalExperiment.useCooldown)
                InternalExperiment.cooldownToGo = InternalExperiment.cooldownTimer;
        }

        /// <summary>
        /// Dump the experimental data contained by the experiment.
        /// </summary>
        [KRPCMethod]
        public void Dump ()
        {
            foreach (var data in InternalDataContainer.GetData ())
                InternalDataContainer.DumpData (data);
        }

        /// <summary>
        /// Reset the experiment.
        /// </summary>
        [KRPCMethod]
        public void Reset ()
        {
            if (Inoperable)
                throw new InvalidOperationException ("Experiment is inoperable");
            InternalExperiment.ResetExperiment ();
            // ResetExperiment only clears the base ModuleScienceExperiment slot. DMagic science
            // experiments keep their results in their own IScienceDataContainer, which the base
            // reset leaves untouched, so dump the container through the interface reference too.
            foreach (var data in InternalDataContainer.GetData ())
                InternalDataContainer.DumpData (data);
        }

        /// <summary>
        /// Whether the experiment is inoperable.
        /// </summary>
        [KRPCProperty]
        public bool Inoperable {
            get { return InternalExperiment.Inoperable; }
        }

        /// <summary>
        /// Whether the experiment has been deployed.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get { return InternalExperiment.Deployed; }
        }

        /// <summary>
        /// Whether the experiment can be re-run.
        /// </summary>
        [KRPCProperty]
        public bool Rerunnable {
            get { return InternalDataContainer.IsRerunnable (); }
        }

        /// <summary>
        /// Whether the experiment contains data.
        /// </summary>
        [KRPCProperty]
        public bool HasData {
            get { return InternalDataContainer.GetData().Any (); }
        }

        /// <summary>
        /// The data contained in this experiment.
        /// </summary>
        [KRPCProperty]
        public IList<ScienceData> Data {
            get { return InternalDataContainer.GetData().Select (data => new ScienceData (InternalExperiment, data)).ToList (); }
        }

        /// <summary>
        /// Determines if the experiment is available given the current conditions.
        /// </summary>
        [KRPCProperty]
        public bool Available {
            get {
                var vessel = Part.InternalPart.vessel;
                var situation = ScienceUtil.GetExperimentSituation (vessel);
                var rndExperiment = ResearchAndDevelopment.GetExperiment (InternalExperiment.experimentID);
                return rndExperiment.IsAvailableWhile (situation, vessel.mainBody);
            }
        }

        /// <summary>
        /// The name of the biome the experiment is currently in.
        /// </summary>
        [KRPCProperty]
        public string Biome {
            get {
                var vessel = Part.InternalPart.vessel;
                var biome = vessel.LandedInKSC ? getKSCBiome (vessel)
                    : ScienceUtil.GetExperimentBiome (vessel.mainBody, vessel.latitude, vessel.longitude);
                return biome.Replace (" ", string.Empty);
            }
        }

        static string getKSCBiome (global::Vessel vessel)
        {
            var at = vessel.landedAt;
            return at == "KSC" ? at : at.Replace ("KSC", string.Empty).Replace ("Grounds", string.Empty).Replace ("_", string.Empty);
        }

        /// <summary>
        /// Containing information on the corresponding specific science result for the current
        /// conditions. Returns <c>null</c> if the experiment is unavailable.
        /// </summary>
        [KRPCProperty]
        public ScienceSubject ScienceSubject {
            get {
                if (!Available)
                    return null;
                var id = InternalExperiment.experimentID;
                var vessel = Part.InternalPart.vessel;
                var bodyName = vessel.mainBody.name;
                var situation = ScienceUtil.GetExperimentSituation (vessel);
                var rndExperiment = ResearchAndDevelopment.GetExperiment (id);
                var biome = rndExperiment.BiomeIsRelevantWhile (situation) ? Biome : string.Empty;
                var subjectId = string.Format ("{0}@{1}{2}{3}", id, bodyName, situation, biome);
                var subject = ResearchAndDevelopment.GetSubjectByID (subjectId);
                subject = subject ?? new global::ScienceSubject (rndExperiment, situation, vessel.mainBody, biome);
                return new ScienceSubject (subject);
            }
        }
    }
}

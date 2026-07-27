#pragma warning disable 0618

using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using UnityEngine;

namespace TestingTools
{
    /// <summary>
    /// kRPC testing tools.
    /// </summary>
    [KRPCService]
    public static class TestingTools
    {
        /// <summary>
        /// Get the name of the current save game.
        /// </summary>
        [KRPCProperty]
        public static string CurrentSave {
            get {
                var title = HighLogic.CurrentGame.Title.Split (' ');
                var name = title.Take (title.Length - 1).ToArray ();
                return string.Join (" ", name);
            }
        }

        /// <summary>
        /// Whether a part with the given name is present in the loaded part catalog. Used by the
        /// test framework to detect mods that add parts but no dedicated kRPC service (e.g.
        /// RealChute, wrapped by the SpaceCenter Parachute class).
        /// </summary>
        /// <param name="name">The internal name of the part, e.g. "RC_stack".</param>
        [KRPCProcedure]
        public static bool PartAvailable (string name)
        {
            return PartLoader.getPartInfoByName (name) != null;
        }

        /// <summary>
        /// Whether any loaded part prefab has a part module with the given name. Used by the test
        /// framework to detect mods that add no part and no dedicated kRPC service, but patch a
        /// module onto existing parts (e.g. Action Groups Extended, whose ModuleManager patch adds
        /// a ModuleAGX module to every part; wrapped by the SpaceCenter Control class).
        /// </summary>
        /// <param name="name">The part module class name, e.g. "ModuleAGX".</param>
        [KRPCProcedure]
        public static bool PartModuleAvailable (string name)
        {
            foreach (var part in PartLoader.LoadedPartsList) {
                var prefab = part.partPrefab;
                if (prefab == null)
                    continue;
                foreach (PartModule module in prefab.Modules)
                    if (module.moduleName == name)
                        return true;
            }
            return false;
        }

        /// <summary>
        /// Quit the game, closing Kerbal Space Program and returning to the desktop.
        /// Works from any scene, including in-flight, and skips the confirmation dialog
        /// that the main-menu quit button normally shows.
        /// </summary>
        [KRPCProcedure]
        public static void Quit ()
        {
            Application.Quit ();
        }

        /// <summary>
        /// Load an existing save game.
        /// </summary>
        [KRPCProcedure]
        public static void LoadSave (string directory, string name)
        {
            HighLogic.SaveFolder = directory;
            var game = GamePersistence.LoadGame (name, HighLogic.SaveFolder, true, false);
            if (game == null || game.flightState == null || !game.compatible)
                throw new ArgumentException ("Failed to load save '" + name + "'");
            FlightDriver.StartAndFocusVessel (game, game.flightState.activeVesselIdx);
            throw new YieldException<Action> (() => WaitForVesselSwitch(0));
        }

        /// <summary>
        /// The number of parts alive in the scene, across every vessel.
        /// </summary>
        [KRPCProperty]
        public static int LoadedPartCount {
            get { return UnityEngine.Object.FindObjectsOfType<Part> ().Length; }
        }

        /// <summary>
        /// The number of vessels the game's flight state lists.
        /// </summary>
        [KRPCProperty]
        public static int FlightStateVesselCount {
            get { return HighLogic.CurrentGame.flightState.protoVessels.Count; }
        }

        /// <summary>
        /// The number of mission summary dialogs open. The game puts one up when a vessel is
        /// recovered, and it stays up until it is dismissed by hand.
        /// </summary>
        [KRPCProperty]
        public static int RecoveryDialogCount {
            get {
                return UnityEngine.Object.FindObjectsOfType<
                    KSP.UI.Screens.MissionRecoveryDialog> ().Length;
            }
        }

        /// <summary>
        /// The game's own staging lock flag, which Alt+L toggles alongside the "manualStageLock"
        /// input lock and then reads to decide whether the next press locks or unlocks. Nothing
        /// else in the game reads it, so it is only reachable from here.
        /// </summary>
        [KRPCProperty]
        public static bool FlightInputStageLock {
            get { return FlightInputHandler.fetch != null && FlightInputHandler.fetch.stageLock; }
        }

        /// <summary>
        /// Remove all vessels except the active vessel.
        /// </summary>
        [KRPCProcedure]
        public static void RemoveOtherVessels ()
        {
            var vessels = FlightGlobals.Vessels.Where (v => v != FlightGlobals.ActiveVessel).ToList ();
            foreach (var vessel in vessels)
                vessel.Die ();
        }

        /// <summary>
        /// Destroy a part, the way the game does when it is blown up or overheats.
        /// Used by tests that need a part to stop existing under a client's part object.
        /// </summary>
        /// <param name="part">The part to destroy.</param>
        [KRPCProcedure]
        public static void DestroyPart (KRPC.SpaceCenter.Services.Parts.Part part)
        {
            if (part == null)
                throw new ArgumentNullException (nameof (part));
            part.InternalPart.Die ();
        }

        /// <summary>
        /// The number of objects the server is holding on behalf of its clients. Used by tests
        /// that check the server reclaims the objects whose game objects are gone.
        /// </summary>
        [KRPCProperty]
        public static int ObjectStoreSize {
            get { return ObjectStore.Instance.Count; }
        }

        static Quaternion ZeroRotation {
            get {
                var vessel = FlightGlobals.ActiveVessel;
                var vesselCoM = vessel.CoM;
                var right = vesselCoM - vessel.mainBody.position;
                var northPole = vessel.mainBody.position + ((Vector3d)vessel.mainBody.transform.up) * vessel.mainBody.Radius - vesselCoM;
                northPole.Normalize ();
                var up = Vector3.Exclude (right, northPole);
                var forward = Vector3.Cross (right, northPole);
                Vector3.OrthoNormalize (ref forward, ref up);
                var rotation = Quaternion.LookRotation (forward, up);
                return Quaternion.AngleAxis (90, new Vector3 (0, -1, 0)) * rotation;
            }
        }

        /// <summary>
        /// Point the given vessel at the fixed reference attitude the tests start from, and stop it
        /// rotating. The attitude itself is arbitrary; what matters is that every test that calls
        /// this begins from the same pose.
        /// </summary>
        /// <param name="vessel">Vessel.</param>
        [KRPCProcedure]
        public static void ClearRotation (KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            var serviceVessel = vessel ?? new KRPC.SpaceCenter.Services.Vessel (FlightGlobals.ActiveVessel);
            serviceVessel.InternalVessel.SetRotation (ZeroRotation);
            KRPC.Debug.Debug.SetAngularVelocity (
                new Tuple<double,double,double> (0, 0, 0), null, serviceVessel);
        }

        /// <summary>
        /// Reassign every crew member of the given vessel (default: the active vessel) to the Pilot
        /// profession at full experience level. The save's auto-crew fills the pod with whichever
        /// kerbal is next in the roster (often an engineer/scientist), which leaves the vessel on
        /// "partial control" — no in-game SAS and, after a rails warp, an unreliable control source.
        /// Overwriting the trait to Pilot gives deterministic full control for every test run without
        /// changing the craft (a kerbal's mass is the same for any profession, so the calibrated MOI
        /// and torque are unaffected).
        /// </summary>
        /// <param name="vessel">Vessel.</param>
        [KRPCProcedure]
        public static void SetCrewToPilot (KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            Vessel internalVessel = vessel == null ? FlightGlobals.ActiveVessel : vessel.InternalVessel;
            foreach (var crew in internalVessel.GetVesselCrew ()) {
                KerbalRoster.SetExperienceTrait (crew, KerbalRoster.pilotTrait);
                KerbalRoster.SetExperienceLevel (crew, 5);
            }
            internalVessel.CrewListSetDirty ();
        }

        static void WaitForVesselSwitch (int tick)
        {
            if (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.packed)
                throw new YieldException<Action> (() => WaitForVesselSwitch(0));
            if (tick < 10)
                throw new YieldException<Action> (() => WaitForVesselSwitch(tick + 1));
        }
    }
}

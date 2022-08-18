using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using KRPC.Continuations;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using KSP.UI;
using KSP.UI.Screens.Flight;
using PreFlightTests;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Provides functionality to interact with Kerbal Space Program. This includes controlling
    /// the active vessel, managing its resources, planning maneuver nodes and auto-piloting.
    /// </summary>
    [KRPCService (Id = 2)]
    public static class SpaceCenter
    {
        /// <summary>
        /// The current mode the game is in.
        /// </summary>
        [KRPCProperty]
        public static GameMode GameMode {
            get { return HighLogic.CurrentGame.Mode.ToGameMode(); }
        }

        /// <summary>
        /// The current amount of science.
        /// </summary>
        [KRPCProperty]
        public static float Science
        {
            get {
                if (ReferenceEquals(ResearchAndDevelopment.Instance, null))
                    throw new InvalidOperationException("Science not available");
                return ResearchAndDevelopment.Instance.Science;
            }
        }

        /// <summary>
        /// The current amount of funds.
        /// </summary>
        [KRPCProperty]
        public static double Funds {
            get {
                if (ReferenceEquals(Funding.Instance, null))
                    throw new InvalidOperationException("Funding not available");
                return Funding.Instance.Funds;
            }
        }

        /// <summary>
        /// The current amount of reputation.
        /// </summary>
        [KRPCProperty]
        public static float Reputation
        {
            get {
                if (ReferenceEquals(global::Reputation.Instance, null))
                    throw new InvalidOperationException("Reputation not available");
                return global::Reputation.Instance.reputation;
            }
        }

        /// <summary>
        /// The currently active vessel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public static Vessel ActiveVessel {
            get { return new Vessel (FlightGlobals.ActiveVessel); }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException ("ActiveVessel");
                FlightGlobals.ForceSetActiveVessel (value.InternalVessel);
                throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
            }
        }

        /// <summary>
        /// Wait until 10 frames after the active vessel is unpacked.
        /// </summary>
        static void WaitForVesselSwitch (int tick)
        {
            if (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.packed)
                throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
            if (tick < 25)
                throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, tick + 1));
        }

        /// <summary>
        /// A list of all the vessels in the game.
        /// </summary>
        [KRPCProperty]
        public static IList<Vessel> Vessels {
            get {
                var vessels = new List<Vessel> ();
                foreach (var vessel in FlightGlobals.Vessels) {
                    if (vessel.vesselType == global::VesselType.EVA ||
                        vessel.vesselType == global::VesselType.Flag ||
                        vessel.vesselType == global::VesselType.SpaceObject ||
                        vessel.vesselType == global::VesselType.Unknown)
                        continue;
                    vessels.Add (new Vessel (vessel));
                }
                return vessels;
            }
        }

        /// <summary>
        /// Gets a list of available launchsites.
        /// </summary>
        [KRPCProperty]
        public static IList<LaunchSite> LaunchSites
        {
            get {
                List<LaunchSite> list = new List<LaunchSite>(PSystemSetup.Instance.LaunchSites.Count + PSystemSetup.Instance.SpaceCenterFacilities.Length);
                foreach (var launchSite in PSystemSetup.Instance.LaunchSites) {
                    list.Add(new LaunchSite(launchSite.name, new CelestialBody(launchSite.Body), launchSite.editorFacility));
                }
                foreach (var facility in PSystemSetup.Instance.SpaceCenterFacilities) {
                    if (facility.IsLaunchFacility()) {
                        list.Add(new LaunchSite(facility.facilityName, new CelestialBody(facility.hostBody), facility.editorFacility));
                    }
                }
                return list;
            }
        }

        /// <summary>
        /// A dictionary of all celestial bodies (planets, moons, etc.) in the game,
        /// keyed by the name of the body.
        /// </summary>
        [KRPCProperty]
        public static IDictionary<string,CelestialBody> Bodies {
            get {
                var bodies = new Dictionary<string, CelestialBody> ();
                foreach (var body in FlightGlobals.Bodies)
                    bodies [body.name] = new CelestialBody (body);
                return bodies;
            }
        }

        /// <summary>
        /// The currently targeted celestial body.
        /// </summary>
        [KRPCProperty (Nullable = true, GameScene = GameScene.Flight)]
        public static CelestialBody TargetBody {
            get {
                var target = FlightGlobals.fetch.VesselTarget as global::CelestialBody;
                return target != null ? new CelestialBody (target) : null;
            }
            set { FlightGlobals.fetch.SetVesselTarget (ReferenceEquals (value, null) ? null : value.InternalBody, true); }
        }

        /// <summary>
        /// The currently targeted vessel.
        /// </summary>
        [KRPCProperty (Nullable = true, GameScene = GameScene.Flight)]
        public static Vessel TargetVessel {
            get {
                var target = FlightGlobals.fetch.VesselTarget as global::Vessel;
                return target != null ? new Vessel (target) : null;
            }
            set { FlightGlobals.fetch.SetVesselTarget (ReferenceEquals (value, null) ? null : value.InternalVessel, true); }
        }

        /// <summary>
        /// The currently targeted docking port.
        /// </summary>
        [KRPCProperty (Nullable = true, GameScene = GameScene.Flight)]
        public static Parts.DockingPort TargetDockingPort {
            get {
                var target = FlightGlobals.fetch.VesselTarget as ModuleDockingNode;
                return target != null ? new Parts.DockingPort (new Parts.Part (target.part)) : null;
            }
            set { FlightGlobals.fetch.SetVesselTarget (ReferenceEquals (value, null) ? null : value.InternalPort, true); }
        }

        /// <summary>
        /// Clears the current target.
        /// </summary>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void ClearTarget ()
        {
            FlightGlobals.fetch.SetVesselTarget (null, true);
        }

        /// <summary>
        /// The waypoint manager.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public static WaypointManager WaypointManager {
            get { return new WaypointManager (); }
        }

        /// <summary>
        /// The contract manager.
        /// </summary>
        [KRPCProperty]
        public static ContractManager ContractManager {
            get { return new ContractManager(); }
        }

        /// <summary>
        /// The Alarm Clock Module.
        /// </summary>
        [KRPCProperty]
        public static AlarmClock AlarmClock
        {
            get { return new AlarmClock(); }
        }

        static string GetFullCraftDirectory (string craftDirectory)
        {
            return KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/Ships/" + craftDirectory;
        }

        /// <summary>
        /// Returns a list of vessels from the given <paramref name="craftDirectory"/>
        /// that can be launched.
        /// </summary>
        /// <param name="craftDirectory">Name of the directory in the current saves
        /// "Ships" directory. For example <c>"VAB"</c> or <c>"SPH"</c>.</param>
        [KRPCProcedure]
        public static IList<string> LaunchableVessels (string craftDirectory)
        {
            try {
                var directory = new DirectoryInfo (GetFullCraftDirectory (craftDirectory));
                return directory.GetFiles ("*.craft").Select (file => Path.GetFileNameWithoutExtension (file.Name)).ToList ();
            } catch (DirectoryNotFoundException) {
                return new List<string> ();
            }
        }

        /// <summary>
        /// Helper class for launching a new vessel.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
        sealed class LaunchConfig {
            public LaunchConfig(string craftDirectory, string name, string launchSite, bool recover, string crew, string flagUrl) {
                LaunchSite = launchSite;
                Recover = recover;
                FlagUrl = string.IsNullOrEmpty(flagUrl) ? EditorLogic.FlagURL : flagUrl;
                // Load the vessel and its default crew
                if (craftDirectory == "VAB")
                    EditorDriver.editorFacility = EditorFacility.VAB;
                else if (craftDirectory == "SPH")
                    EditorDriver.editorFacility = EditorFacility.SPH;
                else
                    throw new ArgumentException("Invalid craftDirectory, should be VAB or SPH");
                Path = GetFullCraftDirectory(craftDirectory) + "/" + name + ".craft";
                template = ShipConstruction.LoadTemplate(Path);
                if (template == null)
                    throw new InvalidOperationException("Failed to load template for vessel");
                manifest = VesselCrewManifest.FromConfigNode(template.config);
                if (string.IsNullOrEmpty(crew)) {
                    manifest = HighLogic.CurrentGame.CrewRoster.DefaultCrewForVessel(template.config, manifest, true, false);
                }
                else {
                    string[] crewNames = crew.Split(';');
                    KerbalRoster crewRoster = new KerbalRoster(HighLogic.CurrentGame.Mode);
                    foreach (var crewName in crewNames) {
                        CrewMember kerbal = GetKerbal(crewName);
                        if (kerbal != null && kerbal.InternalCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Available) {
                            crewRoster.AddCrewMember(kerbal.InternalCrewMember);
                        }
                    }
                    manifest = crewRoster.DefaultCrewForVessel(template.config, manifest, true, false);
                    if (manifest.CrewCount < crewRoster.Count)
                    {
                        // Debug.Log("=========manifest is missing crew.  before:==============");
                        // manifest.DebugManifest();
                        foreach (ProtoCrewMember crewMember in crewRoster.Crew) {
                            if (!manifest.Contains(crewMember)) {
                                // Debug.Log($"{crewMember.name} is missing from crew");
                                if (!AddCrewToManifest(manifest, crewMember)) {
                                    Debug.LogError($"failed to add {crewMember.name} to a seat");
                                }
                            }
                            // Debug.Log("===========manifest after updates:=============");
                            // manifest.DebugManifest();
                        }
                    }
                }

                facility = (craftDirectory == "SPH") ? SpaceCenterFacility.SpaceplaneHangar : SpaceCenterFacility.VehicleAssemblyBuilding;
                facilityLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(facility);
                site = (launchSite == "Runway") ? SpaceCenterFacility.Runway : SpaceCenterFacility.LaunchPad;
                siteLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(site);
                isPad = (site == SpaceCenterFacility.LaunchPad);
            }

            bool AddCrewToManifest(VesselCrewManifest manifest, ProtoCrewMember pcm)
            {
                foreach (PartCrewManifest partManifest in manifest.PartManifests) {
                    for (int seatIndex = 0; seatIndex < partManifest.partCrew.Length; seatIndex++) {
                        if (string.IsNullOrEmpty(partManifest.partCrew[seatIndex])) {
                            partManifest.AddCrewToSeat(pcm, seatIndex);
                            return true;
                        }
                    }
                }
                return false;
            }

            public void RunPreFlightChecks()
            {
                var checks = new PreFlightCheck(
                    () => { preFlightChecksComplete = true; },
                    () => error = "Failed to launch vessel. Did not pass pre-flight checks.");
                var gameVars = GameVariables.Instance;
                checks.AddTest(new CraftWithinPartCountLimit(template, facility, gameVars.GetPartCountLimit(facilityLevel, isPad)));
                checks.AddTest(new CraftWithinSizeLimits(template, site, gameVars.GetCraftSizeLimit(siteLevel, isPad)));
                checks.AddTest(new CraftWithinMassLimits(template, site, gameVars.GetCraftMassLimit(siteLevel, isPad)));
                checks.AddTest(new ExperimentalPartsAvailable(manifest));
                checks.AddTest(new CanAffordLaunchTest(template, Funding.Instance));
                var launchSite = LaunchSite;
                checks.AddTest(new FacilityOperational(launchSite, launchSite));
                checks.AddTest(new NoControlSources(manifest));
                checks.RunTests();
            }

            public string LaunchSite { get; private set; }
            public bool Recover { get; private set; }
            public string Path { get; private set; }
            public string FlagUrl { get; private set; }

            readonly ShipTemplate template;
            readonly public VesselCrewManifest manifest;
            readonly SpaceCenterFacility facility;
            readonly float facilityLevel;
            readonly SpaceCenterFacility site;
            readonly float siteLevel;
            readonly bool isPad;

            public bool preFlightChecksComplete;
            public string error;
        };

        /// <summary>
        /// Launch a vessel.
        /// </summary>
        /// <param name="craftDirectory">Name of the directory in the current saves
        /// "Ships" directory, that contains the craft file.
        /// For example <c>"VAB"</c> or <c>"SPH"</c>.</param>
        /// <param name="name">Name of the vessel to launch. This is the name of the ".craft" file
        /// in the save directory, without the ".craft" file extension.</param>
        /// <param name="launchSite">Name of the launch site. For example <c>"LaunchPad"</c> or
        /// <c>"Runway"</c>.</param>
        /// <param name="recover">If true and there is a vessel on the launch site,
        /// recover it before launching.</param>
        /// <param name="crew">if not null, a semicolon-delimited list of crew names of kerbals to place in the craft.  Otherwise the crew will use default assignments.</param>
        /// <param name="flagUrl">If not null, the asset url of the mission flag to use for the launch.</param>
        /// <remarks>
        /// Throws an exception if any of the games pre-flight checks fail.
        /// </remarks>
        [KRPCProcedure]
        public static void LaunchVessel (string craftDirectory, string name, string launchSite, bool recover = true, string crew = null, string flagUrl = null)
        {
            CloseDialogs();
            var config = new LaunchConfig(craftDirectory, name, launchSite, recover, crew, flagUrl);
            config.RunPreFlightChecks();
            throw new YieldException (new ParameterizedContinuationVoid<LaunchConfig> (WaitForVesselPreFlightChecks, config));
        }

        /// <summary>
        /// Wait until pre-flight checks for new vessel are complete.
        /// </summary>
        /// <param name="config">Config.</param>
        static void WaitForVesselPreFlightChecks(LaunchConfig config)
        {
            if (config.error != null)
                throw new InvalidOperationException(config.error);
            if (!config.preFlightChecksComplete)
                throw new YieldException(new ParameterizedContinuationVoid<LaunchConfig>(WaitForVesselPreFlightChecks, config));
            // Check launch site clear
            var vesselsToRecover = ShipConstruction.FindVesselsLandedAt(HighLogic.CurrentGame.flightState, config.LaunchSite);
            if (vesselsToRecover.Any()) {
                // Recover existing vessels if the launch site is not clear
                if (!config.Recover)
                    throw new InvalidOperationException("Launch site not clear");
                foreach (var vessel in vesselsToRecover)
                    ShipConstruction.RecoverVesselFromFlight(vessel, HighLogic.CurrentGame.flightState, true);
            }
            // Do the actual launch - passed pre-flight checks, and launch site is clear.
            FlightDriver.StartWithNewLaunch(config.Path, config.FlagUrl, config.LaunchSite, config.manifest);
            throw new YieldException(new ParameterizedContinuationVoid<int>(WaitForVesselSwitch, 0));
        }

        /// <summary>
        /// Launch a new vessel from the VAB onto the launchpad.
        /// </summary>
        /// <param name="name">Name of the vessel to launch.</param>
        /// <param name="recover">If true and there is a vessel on the launch pad,
        /// recover it before launching.</param>
        /// <remarks>
        /// This is equivalent to calling <see cref="LaunchVessel"/> with the craft directory
        /// set to "VAB" and the launch site set to "LaunchPad".
        /// Throws an exception if any of the games pre-flight checks fail.
        /// </remarks>
        [KRPCProcedure]
        public static void LaunchVesselFromVAB (string name, bool recover = true)
        {
            LaunchVessel ("VAB", name, "LaunchPad", recover);
        }

        /// <summary>
        /// Launch a new vessel from the SPH onto the runway.
        /// </summary>
        /// <param name="name">Name of the vessel to launch.</param>
        /// <param name="recover">If true and there is a vessel on the runway,
        /// recover it before launching.</param>
        /// <remarks>
        /// This is equivalent to calling <see cref="LaunchVessel"/> with the craft directory
        /// set to "SPH" and the launch site set to "Runway".
        /// Throws an exception if any of the games pre-flight checks fail.
        /// </remarks>
        [KRPCProcedure]
        public static void LaunchVesselFromSPH (string name, bool recover = true)
        {
            LaunchVessel ("SPH", name, "Runway", recover);
        }

        /// <summary>
        /// Save the game with a given name.
        /// This will create a save file called <c>name.sfs</c> in the folder of the
        /// current save game.
        /// </summary>
        [KRPCProcedure]
        public static void Save (string name)
        {
            GamePersistence.SaveGame (name, HighLogic.SaveFolder, SaveMode.OVERWRITE);
        }

        /// <summary>
        /// Load the game with the given name.
        /// This will create a load a save file called <c>name.sfs</c> from the folder of the
        /// current save game.
        /// </summary>
        [KRPCProcedure]
        public static void Load (string name)
        {
            CloseDialogs ();
            var game = GamePersistence.LoadGame (name, HighLogic.SaveFolder, true, false);
            if (game == null || game.flightState == null || !game.compatible)
                throw new ArgumentException ("Failed to load " + name);
            FlightDriver.StartAndFocusVessel (game, game.flightState.activeVesselIdx);
            throw new YieldException (new ParameterizedContinuationVoid<int> (WaitForVesselSwitch, 0));
        }

        /// <summary>
        /// Save a quicksave.
        /// </summary>
        /// <remarks>
        /// This is the same as calling <see cref="Save"/> with the name "quicksave".
        /// </remarks>
        [KRPCProcedure]
        public static void Quicksave ()
        {
            Save ("quicksave");
        }

        /// <summary>
        /// Load a quicksave.
        /// </summary>
        /// <remarks>
        /// This is the same as calling <see cref="Load"/> with the name "quicksave".
        /// </remarks>
        [KRPCProcedure]
        public static void Quickload ()
        {
            Load ("quicksave");
        }

        /// <summary>
        /// Indicates whether the current flight can be reverted.
        /// </summary>
        /// <returns>True if RevertToLaunch will succeed.</returns>
        [KRPCProcedure]
        public static bool CanRevertToLaunch()
		{
            return FlightDriver.CanRevert;
		}

        /// <summary>
        /// Reverts the current flight to launch.  Call CanRevertToLaunch first - some conditions may prevent reverting.
        /// </summary>
        [KRPCProcedure]
        public static void RevertToLaunch()
        {
            if (FlightDriver.CanRevert)
            {
                CloseDialogs();
                FlightDriver.RevertToLaunch();
            }
        }

        private static void CloseDialogs()
        {
			KSP.UI.Dialogs.FlightResultsDialog.Close();
            var recoveryDialog = UnityEngine.Object.FindObjectOfType<KSP.UI.Screens.MissionRecoveryDialog>();
            if (recoveryDialog != null)
			{
                recoveryDialog.gameObject.DestroyGameObject();
			}
        }

        /// <summary>
        /// Tranfsers a crew member to a different part.
        /// </summary>
        /// <param name="crewMember">The crew member to transfer.</param>
        /// <param name="targetPart">The part to move them to.</param>
        [KRPCProcedure(GameScene = GameScene.Flight)]
        public static void TransferCrew(CrewMember crewMember, KRPC.SpaceCenter.Services.Parts.Part targetPart)
        {
            CrewTransfer transfer = CrewTransfer.Create(crewMember.InternalCrewMember.seat.part, crewMember.InternalCrewMember, delegate {});
            transfer.crew = crewMember.InternalCrewMember;
            transfer.tgtPart = targetPart.InternalPart;
            transfer.MoveCrewTo(targetPart.InternalPart);
        }

        /// <summary>
        /// An object that can be used to control the camera.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public static Camera Camera {
            get { return new Camera (); }
        }

        /// <summary>
        /// Whether the UI is visible.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public static bool UIVisible
        {
            get { return UIMasterController.Instance.mainCanvas.enabled; }
            set {
                var visible = UIVisible;
                if (value && !visible)
                    GameEvents.onShowUI.Fire();
                else if (!value && visible)
                    GameEvents.onHideUI.Fire();
            }
        }

        /// <summary>
        /// Whether the navball is visible.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public static bool Navball
        {
            get { return NavBallToggle.Instance.panel.expanded; }
            set {
                if (value)
                    NavBallToggle.Instance.panel.Expand();
                else
                    NavBallToggle.Instance.panel.Collapse();
            }
        }

        /// <summary>
        /// The current universal time in seconds.
        /// </summary>
        [KRPCProperty]
        public static double UT {
            get { return Planetarium.GetUniversalTime (); }
        }

        /// <summary>
        /// The value of the <a href="https://en.wikipedia.org/wiki/Gravitational_constant">
        /// gravitational constant</a> G in <math>N(m/kg)^2</math>.
        /// </summary>
        [KRPCProperty]
        public static double G {
            get { return 6.67408e-11; }
        }

        // TODO: warp functionality should be available in other game scenes? not just flight?

        /// <summary>
        /// The current time warp mode. Returns <see cref="WarpMode.None"/> if time
        /// warp is not active, <see cref="WarpMode.Rails"/> if regular "on-rails" time warp
        /// is active, or <see cref="WarpMode.Physics"/> if physical time warp is active.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public static WarpMode WarpMode {
            get {
                if (TimeWarp.CurrentRateIndex == 0)
                    return WarpMode.None;
                if (TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
                    return WarpMode.Rails;
                return WarpMode.Physics;
            }
        }

        /// <summary>
        /// The current warp rate. This is the rate at which time is passing for
        /// either on-rails or physical time warp. For example, a value of 10 means
        /// time is passing 10x faster than normal. Returns 1 if time warp is not
        /// active.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public static float WarpRate {
            get { return TimeWarp.CurrentRate; }
        }

        /// <summary>
        /// The current warp factor. This is the index of the rate at which time
        /// is passing for either regular "on-rails" or physical time warp. Returns 0
        /// if time warp is not active. When in on-rails time warp, this is equal to
        /// <see cref="RailsWarpFactor"/>, and in physics time warp, this is equal to
        /// <see cref="PhysicsWarpFactor"/>.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public static float WarpFactor {
            get { return TimeWarp.CurrentRateIndex; }
        }

        /// <summary>
        /// The time warp rate, using regular "on-rails" time warp. A value between
        /// 0 and 7 inclusive. 0 means no time warp. Returns 0 if physical time warp
        /// is active.
        ///
        /// If requested time warp factor cannot be set, it will be set to the next
        /// lowest possible value. For example, if the vessel is too close to a
        /// planet. See <a href="https://wiki.kerbalspaceprogram.com/wiki/Time_warp">
        /// the KSP wiki</a> for details.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public static int RailsWarpFactor {
            get { return WarpMode == WarpMode.Rails ? TimeWarp.CurrentRateIndex : 0; }
            set { SetWarpFactor (TimeWarp.Modes.HIGH, value.Clamp (0, MaximumRailsWarpFactor)); }
        }

        /// <summary>
        /// The physical time warp rate. A value between 0 and 3 inclusive. 0 means
        /// no time warp. Returns 0 if regular "on-rails" time warp is active.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public static int PhysicsWarpFactor {
            get { return WarpMode == WarpMode.Physics ? TimeWarp.CurrentRateIndex : 0; }
            set { SetWarpFactor (TimeWarp.Modes.LOW, value.Clamp (0, 3)); }
        }

        /// <summary>
        /// Returns <c>true</c> if regular "on-rails" time warp can be used, at the specified warp
        /// <paramref name="factor"/>. The maximum time warp rate is limited by various things,
        /// including how close the active vessel is to a planet. See
        /// <a href="https://wiki.kerbalspaceprogram.com/wiki/Time_warp">the KSP wiki</a>
        /// for details.
        /// </summary>
        /// <param name="factor">The warp factor to check.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static bool CanRailsWarpAt (int factor = 1)
        {
            if (factor == 0)
                return true;
            // Not a valid factor
            if (factor < 0 || factor >= TimeWarp.fetch.warpRates.Length)
                return false;
            // Landed
            var vessel = ActiveVessel.InternalVessel;
            if (vessel.LandedOrSplashed)
                return true;
            // Below altitude limit
            var altitude = vessel.mainBody.GetAltitude (vessel.CoM);
            var altitudeLimit = TimeWarp.fetch.GetAltitudeLimit (factor, vessel.mainBody);
            if (altitude < altitudeLimit)
                return false;
            // Throttle is non-zero
            if (FlightInputHandler.state.mainThrottle > 0f)
                return false;
            return true;
        }

        /// <summary>
        /// The current maximum regular "on-rails" warp factor that can be set.
        /// A value between 0 and 7 inclusive. See
        /// <a href="https://wiki.kerbalspaceprogram.com/wiki/Time_warp">the KSP wiki</a>
        /// for details.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight)]
        public static int MaximumRailsWarpFactor {
            get {
                for (int i = TimeWarp.fetch.warpRates.Length - 1; i > 1; i--) {
                    if (CanRailsWarpAt (i))
                        return i;
                }
                return 0;
            }
        }

        /// <summary>
        /// Uses time acceleration to warp forward to a time in the future, specified
        /// by universal time <paramref name="ut"/>. This call blocks until the desired
        /// time is reached. Uses regular "on-rails" or physical time warp as appropriate.
        /// For example, physical time warp is used when the active vessel is traveling
        /// through an atmosphere. When using regular "on-rails" time warp, the warp
        /// rate is limited by <paramref name="maxRailsRate"/>, and when using physical
        /// time warp, the warp rate is limited by <paramref name="maxPhysicsRate"/>.
        /// </summary>
        /// <param name="ut">The universal time to warp to, in seconds.</param>
        /// <param name="maxRailsRate">The maximum warp rate in regular "on-rails" time warp.
        /// </param>
        /// <param name="maxPhysicsRate">The maximum warp rate in physical time warp.</param>
        /// <returns>When the time warp is complete.</returns>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void WarpTo (double ut, float maxRailsRate = 100000, float maxPhysicsRate = 2)
        {
            float rate = Mathf.Clamp ((float)(ut - Planetarium.GetUniversalTime ()), 1f, maxRailsRate);

            if (CanRailsWarpAt ())
                RailsWarpAtRate (rate);
            else
                PhysicsWarpAtRate (Mathf.Min (rate, Math.Min (maxRailsRate, maxPhysicsRate)));

            if (Planetarium.GetUniversalTime () < ut)
                throw new YieldException (new ParameterizedContinuationVoid<double,float,float> (WarpTo, ut, maxRailsRate, maxPhysicsRate));
            if (TimeWarp.CurrentRateIndex > 0)
                SetWarpFactor (TimeWarp.Modes.HIGH, 0);
        }

        static void SetWarpMode (TimeWarp.Modes mode)
        {
            if (TimeWarp.WarpMode != mode) {
                TimeWarp.fetch.Mode = mode;
                TimeWarp.SetRate (0, true);
            }
        }

        static void SetWarpFactor (TimeWarp.Modes mode, int factor)
        {
            SetWarpMode (mode);
            TimeWarp.SetRate (factor, false);
        }

        /// <summary>
        /// Warp using regular "on-rails" time warp at the given rate.
        /// </summary>
        static void RailsWarpAtRate (float rate)
        {
            SetWarpMode (TimeWarp.Modes.HIGH);
            if (rate < TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex])
                DecreaseRailsWarp ();
            else if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.warpRates.Length &&
                     rate >= TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex + 1])
                IncreaseRailsWarp ();
        }

        /// <summary>
        /// Decrease the regular "on-rails" time warp factor.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        static void DecreaseRailsWarp ()
        {
            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH)
                throw new InvalidOperationException ("Not in on-rails time warp");
            if (TimeWarp.CurrentRateIndex > 0)
                TimeWarp.SetRate (TimeWarp.CurrentRateIndex - 1, false);
        }

        /// <summary>
        /// Increase the regular "on-rails" time warp factor.
        /// </summary>
        static void IncreaseRailsWarp ()
        {
            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH)
                throw new InvalidOperationException ("Not in on-rails time warp");
            // Check if we're already warping at the maximum rate
            if (TimeWarp.CurrentRateIndex >= MaximumRailsWarpFactor)
                return;
            // Check that the previous rate update has taken effect
            float currentRate = TimeWarp.fetch.warpRates [TimeWarp.CurrentRateIndex];
            if (Math.Abs (currentRate - TimeWarp.CurrentRate) > 0.01)
                return;
            // Increase the rate
            TimeWarp.SetRate (TimeWarp.CurrentRateIndex + 1, false);
        }

        /// <summary>
        /// Warp using physics time warp at the given rate.
        /// </summary>
        static void PhysicsWarpAtRate (float rate)
        {
            SetWarpMode (TimeWarp.Modes.LOW);
            if (rate < TimeWarp.fetch.physicsWarpRates [TimeWarp.CurrentRateIndex])
                DecreasePhysicsWarp ();
            else if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.physicsWarpRates.Length &&
                     rate >= TimeWarp.fetch.physicsWarpRates [TimeWarp.CurrentRateIndex + 1])
                IncreasePhysicsWarp ();
        }

        /// <summary>
        /// Decrease the physics time warp factor.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        static void DecreasePhysicsWarp ()
        {
            if (TimeWarp.WarpMode != TimeWarp.Modes.LOW)
                throw new InvalidOperationException ("Not in physical time warp");
            if (TimeWarp.CurrentRateIndex > 0)
                TimeWarp.SetRate (TimeWarp.CurrentRateIndex - 1, false);
        }

        /// <summary>
        /// Decrease the physics time warp factor.
        /// </summary>
        static void IncreasePhysicsWarp ()
        {
            if (TimeWarp.WarpMode != TimeWarp.Modes.LOW)
                throw new InvalidOperationException ("Not in physical time warp");
            // Check if we're already warping at the maximum rate
            if (TimeWarp.CurrentRateIndex + 1 >= TimeWarp.fetch.physicsWarpRates.Length)
                return;
            // Check that the previous rate update has taken effect
            var currentRate = TimeWarp.fetch.physicsWarpRates [TimeWarp.CurrentRateIndex];
            if (Math.Abs (currentRate - TimeWarp.CurrentRate) > 0.01)
                return;
            // Increase the rate
            TimeWarp.SetRate (TimeWarp.CurrentRateIndex + 1, false);
        }

        /// <summary>
        /// Converts a position from one reference frame to another.
        /// </summary>
        /// <param name="position">Position, as a vector, in reference frame
        /// <paramref name="from"/>.</param>
        /// <param name="from">The reference frame that the position is in.</param>
        /// <param name="to">The reference frame to covert the position to.</param>
        /// <returns>The corresponding position, as a vector, in reference frame
        /// <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple3 TransformPosition (Tuple3 position, ReferenceFrame from, ReferenceFrame to)
        {
            CheckReferenceFrames (from, to);
            return to.PositionFromWorldSpace (from.PositionToWorldSpace (position.ToVector ())).ToTuple ();
        }

        /// <summary>
        /// Converts a direction from one reference frame to another.
        /// </summary>
        /// <param name="direction">Direction, as a vector, in reference frame
        /// <paramref name="from"/>. </param>
        /// <param name="from">The reference frame that the direction is in.</param>
        /// <param name="to">The reference frame to covert the direction to.</param>
        /// <returns>The corresponding direction, as a vector, in reference frame
        /// <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple3 TransformDirection (Tuple3 direction, ReferenceFrame from, ReferenceFrame to)
        {
            CheckReferenceFrames (from, to);
            return to.DirectionFromWorldSpace (from.DirectionToWorldSpace (direction.ToVector ())).ToTuple ();
        }

        /// <summary>
        /// Converts a rotation from one reference frame to another.
        /// </summary>
        /// <param name="rotation">Rotation, as a quaternion of the form <math>(x, y, z, w)</math>,
        /// in reference frame <paramref name="from"/>.</param>
        /// <param name="from">The reference frame that the rotation is in.</param>
        /// <param name="to">The reference frame to covert the rotation to.</param>
        /// <returns>The corresponding rotation, as a quaternion of the form
        /// <math>(x, y, z, w)</math>, in reference frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple4 TransformRotation (Tuple4 rotation, ReferenceFrame from, ReferenceFrame to)
        {
            CheckReferenceFrames (from, to);
            return to.RotationFromWorldSpace (from.RotationToWorldSpace (rotation.ToQuaternion ())).ToTuple ();
        }

        /// <summary>
        /// Converts a velocity (acting at the specified position) from one reference frame
        /// to another. The position is required to take the relative angular velocity of the
        /// reference frames into account.
        /// </summary>
        /// <param name="position">Position, as a vector, in reference frame
        /// <paramref name="from"/>.</param>
        /// <param name="velocity">Velocity, as a vector that points in the direction of travel and
        /// whose magnitude is the speed in meters per second, in reference frame
        /// <paramref name="from"/>.</param>
        /// <param name="from">The reference frame that the position and velocity are in.</param>
        /// <param name="to">The reference frame to covert the velocity to.</param>
        /// <returns>The corresponding velocity, as a vector, in reference frame
        /// <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple3 TransformVelocity (Tuple3 position, Tuple3 velocity, ReferenceFrame from, ReferenceFrame to)
        {
            CheckReferenceFrames (from, to);
            var worldPosition = from.PositionToWorldSpace (position.ToVector ());
            var worldVelocity = from.VelocityToWorldSpace (position.ToVector (), velocity.ToVector ());
            return to.VelocityFromWorldSpace (worldPosition, worldVelocity).ToTuple ();
        }

        static void CheckReferenceFrames (ReferenceFrame from, ReferenceFrame to)
        {
            if (ReferenceEquals (from, null))
                throw new ArgumentNullException (nameof (from));
            if (ReferenceEquals (to, null))
                throw new ArgumentNullException (nameof (to));
        }

        /// <summary>
        /// Cast a ray from a given position in a given direction, and return the distance to the hit point.
        /// If no hit occurs, returns infinity.
        /// </summary>
        /// <param name="position">Position, as a vector, of the origin of the ray.</param>
        /// <param name="direction">Direction of the ray, as a unit vector.</param>
        /// <param name="referenceFrame">The reference frame that the position and direction are in.</param>
        /// <returns>The distance to the hit, in meters, or infinity if there was no hit.</returns>
        [KRPCProcedure]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public static double RaycastDistance (Tuple3 position, Tuple3 direction, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var worldPosition = referenceFrame.PositionToWorldSpace (position.ToVector ());
            var worldDirection = referenceFrame.DirectionToWorldSpace (direction.ToVector ());
            RaycastHit hitInfo;
            bool hit = Physics.Raycast(worldPosition, worldDirection, out hitInfo);
            return hit ? hitInfo.distance : Double.PositiveInfinity;
        }

        /// <summary>
        /// Cast a ray from a given position in a given direction, and return the part that it hits.
        /// If no hit occurs, returns <c>null</c>.
        /// </summary>
        /// <param name="position">Position, as a vector, of the origin of the ray.</param>
        /// <param name="direction">Direction of the ray, as a unit vector.</param>
        /// <param name="referenceFrame">The reference frame that the position and direction are in.</param>
        /// <returns>The part that was hit or <c>null</c> if there was no hit.</returns>
        [KRPCProcedure (Nullable = true, GameScene = GameScene.Flight)]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public static Parts.Part RaycastPart (Tuple3 position, Tuple3 direction, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var worldPosition = referenceFrame.PositionToWorldSpace (position.ToVector ());
            var worldDirection = referenceFrame.DirectionToWorldSpace (direction.ToVector ());
            RaycastHit hitInfo;
            bool hit = Physics.Raycast(worldPosition, worldDirection, out hitInfo);
            if (!hit)
                return null;
            var part = hitInfo.collider.gameObject.GetComponentInParent<global::Part>();
            return part == null ? null : new Parts.Part (part);
        }

        /// <summary>
        /// Whether <a href="https://forum.kerbalspaceprogram.com/index.php?/topic/19321-130-ferram-aerospace-research-v0159-liebe-82117/">Ferram Aerospace Research</a> is installed.
        /// </summary>
        [KRPCProperty]
        public static bool FARAvailable {
            get { return ExternalAPI.FAR.IsAvailable; }
        }

        /// <summary>
        /// Creates a kerbal.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="job"></param>
        /// <param name="male"></param>
        [KRPCProcedure]
        public static void CreateKerbal(string name, string job, bool male)
        {
            ProtoCrewMember val = new ProtoCrewMember(ProtoCrewMember.KerbalType.Crew, name);
            val.gender = male ? ProtoCrewMember.Gender.Male : ProtoCrewMember.Gender.Female;
            KerbalRoster.SetExperienceTrait(val, job);
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                Funding.Instance.AddFunds(-GameVariables.Instance.GetRecruitHireCost(HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount()), TransactionReasons.CrewRecruited);
            }
            
            HighLogic.CurrentGame.CrewRoster.AddCrewMember(val);
        }

        /// <summary>
        /// Finds a kerbal by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [KRPCProcedure(Nullable = true)]
        public static CrewMember GetKerbal(string name)
        {
            ProtoCrewMember val = HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault((ProtoCrewMember pcm) => pcm.name == name);
            if (val != null)
            {
                return new CrewMember(val);
            }
            return null;
        }

        /// <summary>
        /// Returns to the spacecenter view.
        /// </summary>
        [KRPCProcedure]
        public static void LoadSpaceCenter()
        {
            HighLogic.LoadScene(GameScenes.SPACECENTER);
        }

        /// <summary>
        /// Gets or sets the visible objects in map mode.
        /// </summary>
        [KRPCProperty(GameScene = GameScene.All)]
        public static MapFilterType MapFilter
        {
            get { return (MapFilterType)MapViewFiltering.GetFilterState(); }
            set { MapViewFiltering.SetFilter((MapViewFiltering.VesselTypeFilter)value); }
        }

        /// <summary>
        /// Saves a screenshot.
        /// </summary>
        /// <param name="fileName">The path to the file to save.</param>
        /// <param name="superSize">Resolution scaling factor (1 = default)</param>
        [KRPCProcedure(GameScene = GameScene.Flight)]
        public static void Screenshot(string fileName, int superSize = 1)
        {
            ScreenCapture.CaptureScreenshot(fileName, superSize);
        }
    }
}

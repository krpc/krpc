using System;
using System.Collections.Generic;
using System.IO;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Provides access to the vehicle assembly building and space plane hangar.
    /// Obtained by calling <see cref="SpaceCenter.Editor"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Editor)]
    public class Editor : Equatable<Editor>
    {
        internal Editor ()
        {
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Editor other)
        {
            return !ReferenceEquals (other, null);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return 0;
        }

        /// <summary>
        /// The editor that is currently open.
        /// </summary>
        [KRPCProperty]
        public EditorFacility Facility {
            get { return (EditorFacility)EditorDriver.editorFacility; }
        }

        /// <summary>
        /// The vessel that is being constructed.
        /// </summary>
        /// <remarks>
        /// An editor that has not had a vessel loaded into it holds an empty vessel, with
        /// no parts, rather than nothing at all.
        /// </remarks>
        [KRPCProperty]
        public EditorVessel Vessel {
            get { return new EditorVessel (); }
        }

        /// <summary>
        /// The body whose gravity and atmosphere the vessel's delta-v, thrust, TWR and
        /// specific impulse figures assume.
        /// </summary>
        /// <remarks>
        /// This and <see cref="DeltaVAltitude"/> are what <see cref="EditorVessel.DeltaV"/>,
        /// <see cref="Stage.DeltaV"/> and the other current-situation figures are
        /// computed for. The body sets the gravity the thrust-to-weight ratios are
        /// against, and, with the altitude, the atmospheric pressure and density the
        /// specific impulses and thrusts are against.
        /// This is global game state rather than editor state: the same setting backs
        /// the game's delta-v readout in flight, and it persists across scene changes.
        /// Setting it asks the game to re-run the simulation and returns straight away;
        /// the figures are out of date until <see cref="EditorVessel.DeltaVReady"/> is
        /// true again.
        /// </remarks>
        [KRPCProperty]
        public CelestialBody DeltaVBody {
            get {
                var body = AppValues.body;
                if (body == null)
                    throw new InvalidOperationException (
                        "The game has not chosen a body for its delta-v figures yet.");
                return new CelestialBody (body);
            }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException (nameof (value));
                AppValues.body = value.InternalBody;
                EditorDeltaV.Recalculate ();
            }
        }

        /// <summary>
        /// The altitude above <see cref="DeltaVBody"/>, in meters, that the vessel's
        /// delta-v, thrust, TWR and specific impulse figures assume. The atmospheric
        /// pressure and density at this altitude are what the figures are computed for,
        /// so raising it from zero to above the body's atmosphere moves them from the
        /// vessel's sea-level figures to its vacuum ones.
        /// Setting it asks the game to re-run the simulation and returns straight away;
        /// the figures are out of date until <see cref="EditorVessel.DeltaVReady"/> is
        /// true again.
        /// </summary>
        [KRPCProperty]
        public double DeltaVAltitude {
            get { return AppValues.altitude; }
            set {
                AppValues.altitude = value;
                EditorDeltaV.Recalculate ();
            }
        }

        /// <summary>
        /// Which situation the game's own delta-v readout shows figures for.
        /// </summary>
        /// <remarks>
        /// This selects which column the game's own delta-v app displays, and nothing
        /// else. It does not affect anything kRPC reports: the current-situation
        /// figures follow <see cref="DeltaVBody"/> and <see cref="DeltaVAltitude"/>
        /// whatever this is set to, and the sea-level and vacuum figures are reported
        /// directly as <see cref="EditorVessel.SeaLevelDeltaV"/> and
        /// <see cref="EditorVessel.VacuumDeltaV"/>. It is here for completeness with the
        /// app.
        /// </remarks>
        [KRPCProperty]
        public DeltaVSituation DeltaVSituation {
            get { return (DeltaVSituation)AppValues.situation; }
            set {
                AppValues.situation = (DeltaVSituationOptions)value;
            }
        }

        /// <summary>
        /// The game-wide settings the delta-v readout works from.
        /// </summary>
        static DeltaVAppValues AppValues {
            get {
                var values = DeltaVGlobals.DeltaVAppValues;
                if (values == null)
                    throw new InvalidOperationException (
                        "The game's delta-v settings are not available.");
                return values;
            }
        }

        /// <summary>
        /// Load a vessel from a craft file into the editor, replacing the vessel that
        /// is currently being constructed.
        /// </summary>
        /// <param name="craftDirectory">Name of the directory in the current saves
        /// "Ships" directory, that contains the craft file.
        /// For example <c>"VAB"</c> or <c>"SPH"</c>.</param>
        /// <param name="name">Name of the vessel to load. This is the name of the ".craft"
        /// file in the save directory, without the ".craft" file extension.</param>
        /// <remarks>
        /// The vessel is loaded into whichever editor is open, regardless of which
        /// directory it came from. Its delta-v figures are recalculated in the
        /// background afterwards; see <see cref="EditorVessel.DeltaVReady"/>.
        /// </remarks>
        [KRPCMethod]
        public void LoadVessel (string craftDirectory, string name)
        {
            var path = SpaceCenter.GetFullCraftDirectory (craftDirectory) + "/" + name + ".craft";
            if (!File.Exists (path))
                throw new ArgumentException ("Craft file not found: " + path, nameof (name));
            throw new YieldException<Action> (() => StartVesselLoad (path, 0));
        }

        /// <summary>
        /// Launch the vessel that is being constructed, from the given launch site.
        /// </summary>
        /// <param name="launchSite">Name of the launch site. For example <c>"LaunchPad"</c> or
        /// <c>"Runway"</c>.</param>
        /// <param name="crew">The Kerbals to place in the vessel, by name. Controls how the
        /// vessel is crewed, as described in the remarks.</param>
        /// <param name="recover">If true and there is a vessel on the launch site,
        /// recover it before launching.</param>
        /// <param name="flagUrl">If not <c>null</c>, the asset URL of the mission flag to use for
        /// the launch.</param>
        /// <remarks>
        /// The vessel being constructed is not a craft file, so it is written to the editor's
        /// auto-saved craft file and that is launched, as the game's own launch button does.
        /// The vessel's own craft file, if it was loaded from one, is left alone.
        /// The <paramref name="crew"/> parameter works as it does for
        /// <see cref="SpaceCenter.LaunchVessel"/>, and takes precedence over the crew assigned
        /// in the editor.
        /// Throws an exception if any of the games pre-flight checks fail.
        /// </remarks>
        [KRPCMethod]
        public void LaunchVessel (
            string launchSite, IList<string> crew = null, bool recover = true, string flagUrl = "")
        {
            var ship = EditorLogic.fetch == null ? null : EditorLogic.fetch.ship;
            if (ReferenceEquals (ship, null) || ship.Count == 0)
                throw new InvalidOperationException (
                    "There is no vessel in the editor to launch.");
            var craftDirectory = Facility == EditorFacility.SPH ? "SPH" : "VAB";
            var name = KSPUtil.SanitizeString (EditorLogic.autoShipName, '_', true);
            ShipConstruction.SaveShip (ship, name);
            SpaceCenter.LaunchVessel (craftDirectory, name, launchSite, crew, recover, flagUrl);
        }

        /// <summary>
        /// How many consecutive frames the editor must have looked ready for before a
        /// vessel is loaded into it. Loading one into an editor that is still settling
        /// crashes the game, and the editor looks ready some way before it is.
        /// </summary>
        const int SettleFrames = 60;

        /// <summary>
        /// Whether the editor looks ready to have a vessel loaded into it. The scene
        /// reports itself as an editor well before this is true.
        /// </summary>
        static bool Ready {
            get {
                if (!HighLogic.LoadedSceneIsEditor)
                    return false;
                var driver = EditorDriver.fetch;
                if (driver == null || driver.restartingEditor)
                    return false;
                return !ReferenceEquals (EditorExtensions.Ship, null);
            }
        }

        /// <summary>
        /// Rebuild the editor around the vessel in the craft file at the given path,
        /// once it has looked ready for <see cref="SettleFrames"/> frames in a row. The
        /// rebuild itself is what the game does when a craft file is chosen in its own
        /// craft browser.
        /// </summary>
        static void StartVesselLoad (string path, int settled)
        {
            if (!Ready)
                throw new YieldException<Action> (() => StartVesselLoad (path, 0));
            if (settled < SettleFrames)
                throw new YieldException<Action> (() => StartVesselLoad (path, settled + 1));
            EditorLogic.LoadShipFromFile (path);
            // The game leaves itself set to read this file again the next time the editor
            // starts up, which would load the vessel on a later entry into the editor.
            // Point it back at the vessel the editor is holding, as it is when a vessel is
            // edited in place
            EditorDriver.StartupBehaviour = EditorDriver.StartupBehaviours.LOAD_FROM_CACHE;
        }
    }
}

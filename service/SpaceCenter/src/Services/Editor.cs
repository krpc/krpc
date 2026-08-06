using System;
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
        /// directory it came from.
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
                var logic = EditorLogic.fetch;
                return logic != null && !ReferenceEquals (logic.ship, null);
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
            // The game leaves itself set to read this file again the next time the
            // editor starts up, which would spring the vessel on a later entry into the
            // editor that did not ask for it. Point it back at the vessel the editor is
            // holding, as it is when a vessel is edited in place.
            EditorDriver.StartupBehaviour = EditorDriver.StartupBehaviours.LOAD_FROM_CACHE;
        }
    }
}

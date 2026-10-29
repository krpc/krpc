using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KRPC.Service;
using KRPC.Service.Attributes;
using KSA;

namespace KRPC.KSA.Testing
{
    /// <summary>
    /// Procedures used by the KSA integration tests. Installed only for testing.
    /// </summary>
    [KRPCService]
    public static class TestingTools
    {
        // The game's save list, with no public accessor
        static readonly PropertyInfo saves = typeof (GameSaves).GetProperty (
            "Saves", BindingFlags.NonPublic | BindingFlags.Static);

        static GameSave FindSave (string name)
        {
            var collection = (LookupCollection<GameSave>)saves.GetValue (null);
            return collection.TryGet (name, out GameSave save) ? save : null;
        }

        /// <summary>
        /// Add a vehicle to the game's default vehicles, from a folder holding its
        /// <c>vehicle.xml</c> and <c>meta.toml</c>, so that it can be launched.
        /// </summary>
        /// <param name="directory">Path of the folder.</param>
        /// <returns>The name of the vehicle.</returns>
        [KRPCProcedure]
        public static string RegisterVehicle (string directory)
        {
            var save = UncompressedVehicleSave.FromDirectory (directory, defaultVehicle: true);
            if (save == null)
                throw new ArgumentException ("No vehicle in '" + directory + "'");
            return save.Id;
        }

        /// <summary>
        /// Whether a save with the given name exists.
        /// </summary>
        /// <param name="name">Name of the save.</param>
        [KRPCProcedure]
        public static bool SaveExists (string name)
        {
            return FindSave (name) != null;
        }

        /// <summary>
        /// Save the game, replacing any save with the same name.
        /// </summary>
        /// <param name="name">Name of the save.</param>
        [KRPCProcedure]
        public static void SaveGame (string name)
        {
            if (UncompressedSave.Make (name) == null)
                throw new InvalidOperationException ("Failed to write save '" + name + "'");
        }

        /// <summary>
        /// Load a save, and return once it has loaded. The game is paused after loading.
        /// Throws an exception if the save does not exist.
        /// </summary>
        /// <param name="name">Name of the save.</param>
        [KRPCProcedure]
        public static void LoadSave (string name)
        {
            var save = FindSave (name);
            if (save == null)
                throw new ArgumentException ("Save '" + name + "' not found");
            var loaded = false;
            Exception error = null;
            // The load replaces every vehicle, so it runs once the vehicle solver step that
            // follows the server update has been queued
            Addon.RunAfterFrame (() => {
                try {
                    save.Load ();
                } catch (Exception e) {
                    error = e;
                }
                loaded = true;
            });
            WaitForLoad (() => loaded, () => error);
        }

        static void WaitForLoad (Func<bool> loaded, Func<Exception> error)
        {
            if (!loaded ())
                throw new YieldException<Action> (() => WaitForLoad (loaded, error));
            if (error () != null)
                throw new InvalidOperationException ("Failed to load save: " + error ().Message);
        }

        /// <summary>
        /// Close the game's update available pop-up, and any shown later.
        /// </summary>
        /// <remarks>
        /// The pop-up holds the keyboard, and the game clears the control inputs of the
        /// controlled vehicle each frame while the keyboard is held.
        /// </remarks>
        [KRPCProcedure]
        public static void SuppressUpdatePopup ()
        {
            if (harmony == null) {
                harmony = new Harmony ("kRPC.TestingTools");
                harmony.Patch (AccessTools.Method (typeof (Popup), nameof (Popup.DrawAll)),
                    prefix: new HarmonyMethod (typeof (TestingTools), nameof (CloseUpdatePopups)));
            }
            CloseUpdatePopups ();
        }

        static Harmony harmony;

        // The open pop-ups, which the game does not expose
        static readonly FieldInfo popups = AccessTools.Field (typeof (Popup), "Popups");

        static void CloseUpdatePopups ()
        {
            ((List<Popup>)popups.GetValue (null)).RemoveAll (popup => popup is UpdateAvailablePopup);
        }

        /// <summary>
        /// The RPCs that use game members missing from the running game, found whatever the
        /// game version.
        /// </summary>
        [KRPCProperty]
        public static IList<string> CompatibilityBreakages {
            get { return GameCompatibility.FindBreakages (); }
        }

        /// <summary>
        /// Quit the game at the end of the frame.
        /// </summary>
        [KRPCProcedure]
        public static void Quit ()
        {
            Program.Instance.RequestExit (0);
        }
    }
}

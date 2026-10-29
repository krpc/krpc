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
        /// The number of objects in the server's object store.
        /// </summary>
        [KRPCProperty]
        public static int ObjectCount {
            get { return (int)objectCount.GetValue (objectStore.GetValue (null)); }
        }

        // The object store, which the core does not expose
        static readonly Type objectStoreType = typeof (GameState).Assembly.GetType ("KRPC.Service.ObjectStore");
        static readonly PropertyInfo objectStore = objectStoreType.GetProperty ("Instance");
        static readonly PropertyInfo objectCount = objectStoreType.GetProperty ("Count");

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

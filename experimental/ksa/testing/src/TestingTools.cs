using System;
using System.Reflection;
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
        /// Quit the game at the end of the frame.
        /// </summary>
        [KRPCProcedure]
        public static void Quit ()
        {
            Program.Instance.RequestExit (0);
        }
    }
}

using System;
using System.Collections.Generic;

namespace KRPC.Utils
{
    /// <summary>
    /// Every collection of client-owned state in the mod, so that all of it can be
    /// released at once when there is no game left to hold it.
    /// </summary>
    /// <remarks>
    /// A collection is a static field of the addon that owns it, and an addon only exists
    /// in the scenes it is declared for, so an addon cannot be asked to give its state up
    /// once it is gone. Collections therefore add themselves here as they are created, and
    /// releasing goes through the collections rather than through the addons.
    /// </remarks>
    public static class ClientOwnedState
    {
        static readonly List<IClientOwnedCollection> collections =
            new List<IClientOwnedCollection> ();

        /// <summary>
        /// Add a collection, which is done by the collection itself as it is created.
        /// </summary>
        public static void Register (IClientOwnedCollection collection)
        {
            collections.Add (collection);
        }

        /// <summary>
        /// Release every collection's state, whoever owns it, including state that has
        /// been taken out of a collection but not yet released: a scene left before its
        /// first sweep leaves the previous scene's state waiting for a sweep that will
        /// not come.
        /// </summary>
        /// <remarks>
        /// One collection failing to release must not leave the rest holding their state,
        /// so each is released on its own and a failure is logged and stepped over. The
        /// collections belong to different services, and the order they are released in
        /// is the order they happened to be created in, so without this the state a
        /// failure strands would be whatever came after it.
        /// </remarks>
        public static void ReleaseAll ()
        {
            foreach (var collection in collections) {
                Release (collection.Clear);
                Release (collection.ReleaseDetached);
            }
        }

        static void Release (Action release)
        {
            try {
                release ();
            } catch (Exception exn) {
                // Anything at all, as the release actions are supplied by the services and
                // run here against state whose subject the game may already have destroyed.
                Logger.WriteLine (
                    "Failed to release client-owned state; " + exn, Logger.Severity.Error);
            }
        }
    }
}

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
        public static void ReleaseAll ()
        {
            foreach (var collection in collections) {
                collection.Clear ();
                collection.ReleaseDetached ();
            }
        }
    }
}

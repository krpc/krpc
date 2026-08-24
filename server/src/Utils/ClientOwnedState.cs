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
                Guarded (collection.Clear, "Failed to release client-owned state");
                Guarded (collection.ReleaseDetached, "Failed to release client-owned state");
            }
        }

        /// <summary>
        /// Drop from every collection the entries whose object stands for something the
        /// game has destroyed, so that a collection holds no more than the object store
        /// does.
        /// </summary>
        /// <remarks>
        /// Called from the object store's sweep, which is the one point where classifying
        /// an object is both meaningful and already being paid for: the game has finished
        /// building the state it moved to, and the store is asking everything it holds the
        /// same question. A collection cannot ask it every frame on its own account,
        /// because classifying is allowed to search as widely as it needs to, and a
        /// collection whose objects are not acted on every frame has nothing else to do
        /// there.
        /// </remarks>
        public static void RemoveDestroyed ()
        {
            foreach (var collection in collections)
                Guarded (
                    collection.RemoveDestroyed,
                    "Failed to drop destroyed client-owned state");
        }

        static void Guarded (Action action, string failure)
        {
            try {
                action ();
            } catch (Exception exn) {
                // Anything at all, as what runs here is supplied by the services and acts
                // on state whose subject the game may already have destroyed.
                Logger.WriteLine (failure + "; " + exn, Logger.Severity.Error);
            }
        }
    }
}

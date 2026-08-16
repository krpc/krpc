using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// The state of the game that service objects stand for - the vessels, parts and
    /// everything else a loaded game consists of.
    /// </summary>
    public static class GameState
    {
        /// <summary>
        /// The number of times the game has replaced its state since the server started.
        /// </summary>
        /// <remarks>
        /// A service object that caches something it looked up in the game records the
        /// generation it looked it up in, and looks it up again when the generation has
        /// moved on, because the game may have rebuilt what the cached value refers to.
        /// </remarks>
        public static uint Generation { get; private set; }

        /// <summary>
        /// Whether the object store is waiting to be swept.
        /// </summary>
        public static bool SweepPending { get; private set; }

        static bool settled = true;

        /// <summary>
        /// Whether the game has finished building the state it moved to.
        /// </summary>
        /// <remarks>
        /// False from the moment the game replaces its state until the sweep that follows
        /// it, which is the window in which the game is still filling in the vessels, parts
        /// and everything else that state consists of. Nothing follows from one of them
        /// being absent while this is false: it may be one the game has not built yet. It
        /// starts out true, as nothing has said the game is between states.
        /// </remarks>
        public static bool Settled {
            get { return settled; }
        }

        /// <summary>
        /// Called when the game replaces the state that service objects stand for: a
        /// game is loaded, quickloaded or reverted, or the scene changes. Moves the
        /// generation on, so that anything cached from the previous state is looked up
        /// again, asks for the object store to be swept, and records that the state is
        /// being built, so that nothing is declared gone for being absent from a state
        /// that is not finished.
        /// </summary>
        public static void Changed ()
        {
            Generation++;
            settled = false;
            RequestSweep ();
        }

        /// <summary>
        /// Called when the game destroys something that service objects may stand for, such
        /// as a part or a vessel. Asks for the object store to be swept without moving the
        /// generation on: what was destroyed is gone, but nothing else has been rebuilt, so
        /// there is nothing for anyone to look up again.
        /// </summary>
        /// <remarks>
        /// Several things can be destroyed in the same moment, a vessel breaking up being
        /// the obvious case, and this only records that a sweep is due; one sweep then
        /// covers all of them.
        /// </remarks>
        public static void RequestSweep ()
        {
            SweepPending = true;
        }

        /// <summary>
        /// Remove objects the game no longer has from the object store.
        /// </summary>
        /// <remarks>
        /// Called once the game has finished building the state it moved to, rather than
        /// at the moment it started: while a state is still being built, the objects it
        /// consists of do not exist yet and every object in the store would look gone.
        /// Being called is therefore what says the state is finished, which is what
        /// <see cref="Settled" /> reports.
        /// </remarks>
        public static void Sweep ()
        {
            SweepPending = false;
            settled = true;
            var removed = ObjectStore.Instance.Sweep ();
            if (removed > 0)
                Logger.WriteLine ("Removed " + removed + " destroyed object(s) from the object store");
        }
    }
}

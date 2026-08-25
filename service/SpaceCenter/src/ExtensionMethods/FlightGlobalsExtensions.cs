using System;
using KRPC.Service;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter
{
    static class FlightGlobalsExtensions
    {
        /// <summary>
        /// Whether the game currently knows which vessels exist. It does not while it is
        /// between game states, when nothing can be concluded from a vessel being absent.
        /// </summary>
        /// <remarks>
        /// A game that has replaced its state fills its vessel list one vessel at a time,
        /// so the list holding some vessels does not mean it holds them all. The game is
        /// only taken to know what exists once that state has settled, which is the same
        /// point at which the object store is swept; before then a vessel that cannot be
        /// found may be one the game has not rebuilt yet.
        /// </remarks>
        public static bool VesselsKnown {
            get {
                return HighLogic.CurrentGame != null && GameState.Settled &&
                FlightGlobals.Vessels != null && FlightGlobals.Vessels.Count > 0;
            }
        }

        /// <summary>
        /// The vessel with the given id, loaded or not,
        /// or null if the game has no such vessel.
        /// </summary>
        public static Vessel FindVesselById (Guid id)
        {
            var active = FlightGlobals.ActiveVessel;
            if (active != null && active.id == id)
                return active;
            // A game that is not listing vessels has none to find. Return an empty list,
            // which leaves VesselsKnown to say what the absence means: the object store
            // checks every vessel it holds in one pass, and one that raises must not stop
            // the rest
            var vessels = FlightGlobals.Vessels;
            if (vessels == null)
                return null;
            foreach (var vessel in vessels)
                if (vessel != null && vessel.id == id)
                    return vessel;
            return null;
        }

        /// <summary>
        /// The state of the vessel with the given id. A vessel is live whether it is loaded
        /// or not, and destroyed once the game holds no vessel with the id. A game between
        /// states holds no list of vessels, and leaves the vessel dormant.
        /// </summary>
        public static GameObjectState VesselState (Guid id)
        {
            if (FindVesselById (id) != null)
                return GameObjectState.Live;
            return VesselsKnown ? GameObjectState.Destroyed : GameObjectState.Dormant;
        }

        /// <summary>
        /// The vessel with the given id, loaded or not. Throws if the game has no such
        /// vessel, saying whether it is gone for good or whether the game is merely
        /// between states and cannot say what exists.
        /// </summary>
        public static Vessel GetVesselById (Guid id)
        {
            var vessel = FindVesselById (id);
            if (vessel != null)
                return vessel;
            throw NotResolvable (id);
        }

        /// <summary>
        /// The error to raise when a vessel cannot be looked up, which
        /// <see cref="VesselState" /> decides between so that one rule says what the
        /// absence of a vessel means.
        /// </summary>
        static Exception NotResolvable (Guid id)
        {
            if (VesselState (id) == GameObjectState.Destroyed)
                return new ObjectDestroyedException (
                    "The vessel " + id + " no longer exists. " +
                    "It was destroyed or recovered, or belongs to a game that is no longer loaded.");
            return new InvalidOperationException (
                "The vessel " + id + " is not loaded, as the game is between states and " +
                "does not currently know which vessels exist. " +
                "It can be used again once the game has finished loading.");
        }
    }
}

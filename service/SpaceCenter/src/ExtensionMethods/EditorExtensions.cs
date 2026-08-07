using System;
using KRPC.Utils;

namespace KRPC.SpaceCenter
{
    static class EditorExtensions
    {
        /// <summary>
        /// The vessel the editor has open, or null if it has none.
        /// </summary>
        public static ShipConstruct Ship {
            get {
                var logic = EditorLogic.fetch;
                return ReferenceEquals (logic, null) ? null : logic.ship;
            }
        }

        /// <summary>
        /// The vessel the editor has open. Throws if it has none, which is the case
        /// outside the editor and while the editor is still starting up.
        /// </summary>
        public static ShipConstruct GetShip ()
        {
            var ship = Ship;
            if (ReferenceEquals (ship, null))
                throw new InvalidOperationException ("The editor does not contain a vessel.");
            return ship;
        }

        /// <summary>
        /// What the game holds for the vessel in the editor. The editor holds one vessel
        /// while it is open and takes it with it when it is left, so anything named
        /// against that vessel is live until the game leaves the editor, and gone
        /// afterwards. An editor that is still starting up has no vessel yet, and nothing
        /// is concluded from that.
        /// </summary>
        public static GameObjectState ShipState {
            get {
                if (!HighLogic.LoadedSceneIsEditor)
                    return GameObjectState.Destroyed;
                return ReferenceEquals (Ship, null)
                    ? GameObjectState.Dormant
                    : GameObjectState.Live;
            }
        }
    }
}

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

        // The vessel the editor had open when the generation was last taken, held weakly
        // so that a vessel the editor has finished with is not kept alive by this.
        static WeakReference lastShip;
        static uint shipGeneration;

        /// <summary>
        /// The number of vessels the editor has held since the game started.
        /// </summary>
        /// <remarks>
        /// A part in the editor is named by its craft id, which the game keeps in the
        /// craft file so that it survives the vessel being built again. That id is only
        /// unique within one vessel, and a craft file saved from another carries its ids
        /// over, so two vessels routinely give the same id to different parts. Anything
        /// naming a part records which vessel it meant, and a part of any earlier one is
        /// gone rather than whatever now answers to its id.
        ///
        /// Which vessel the editor has is taken from the vessel itself rather than from
        /// an event announcing a new one: the vessel object being a different object is
        /// the fact any such event would be reporting, and it can be read at any time.
        ///
        /// An undo is the one thing that changes the vessel object without making a new
        /// vessel, so it is the one thing that has to be heard; EditorShipAddon does that
        /// and calls ShipRestored.
        /// </remarks>
        public static uint ShipGeneration {
            get {
                var ship = Ship;
                if (!ReferenceEquals (ship, null) &&
                    (lastShip == null || !ReferenceEquals (lastShip.Target, ship))) {
                    lastShip = new WeakReference (ship);
                    shipGeneration++;
                }
                return shipGeneration;
            }
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

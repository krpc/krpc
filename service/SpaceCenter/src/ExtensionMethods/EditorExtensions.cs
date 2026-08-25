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
                    // An undo or a redo hands back a vessel object of its own, built from
                    // the game's backup, so the object changing is not on its own a new
                    // vessel. The parts it builds carry the craft ids they had, so what
                    // a part object names is unchanged; one whose part the undo took away
                    // is simply no longer there to be found.
                    var wasRestored = restoredShip != null &&
                                      ReferenceEquals (restoredShip.Target, ship);
                    restoredShip = null;
                    if (!wasRestored)
                        shipGeneration++;
                }
                return shipGeneration;
            }
        }

        // The vessel the editor last restored from its own backup, held weakly. Set from
        // EditorShipAddon, which hears the game say so. It is the vessel rather than the
        // fact that one was restored, because a vessel is only adopted once and nothing
        // says when that has happened: a flag would still be set when the next craft was
        // loaded, and would take that craft for the vessel the editor already had.
        static WeakReference restoredShip;

        /// <summary>
        /// Called when the editor restores the vessel it had open from its own backup,
        /// which is what an undo and a redo are.
        /// </summary>
        public static void ShipRestored (ShipConstruct ship)
        {
            restoredShip = ReferenceEquals (ship, null) ? null : new WeakReference (ship);
        }

        /// <summary>
        /// The state of the vessel in the editor. The editor holds one vessel while it is
        /// open and destroys it on leaving, so anything identified against that vessel is
        /// live until the game leaves the editor. An editor that is still starting up
        /// leaves the vessel dormant.
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

using System;
using System.Linq;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// A reference to a part that keeps working when the game destroys the part and
    /// recreates it. Holds the identifier the game gave the part in the scene the
    /// reference was made in, and which kind of identifier that is: a part is not given
    /// a flight id until the vessel it belongs to is launched, and the id it is given
    /// in the editor is only unique within one vessel, so neither works in both scenes.
    /// A reference made in the editor also holds which loaded vessel it was made in,
    /// as a craft id says nothing on its own about which vessel it belongs to; see
    /// <see cref="EditorExtensions.ShipGeneration" />.
    /// </summary>
    struct PartId : IEquatable<PartId>
    {
        readonly uint id;
        readonly bool inEditor;
        readonly uint generation;

        internal PartId (Part part)
        {
            if (ReferenceEquals (part, null))
                throw new ArgumentNullException (nameof (part));
            inEditor = HighLogic.LoadedSceneIsEditor;
            id = inEditor ? part.craftID : part.flightID;
            generation = inEditor ? EditorExtensions.ShipGeneration : 0;
        }

        /// <summary>
        /// A reference to the part in flight with the given flight identifier, which the
        /// game need not currently have.
        /// </summary>
        internal PartId (uint flightId)
        {
            inEditor = false;
            id = flightId;
            generation = 0;
        }

        /// <summary>
        /// Whether the reference names a part of the vessel the editor has open now. A
        /// craft id belongs to the vessel it was read from, so one from a vessel the
        /// editor has since loaded over names nothing, however many parts answer to it.
        /// </summary>
        bool OfLoadedShip {
            get { return !inEditor || generation == EditorExtensions.ShipGeneration; }
        }

        /// <summary>
        /// Whether the reference names a part in an editor rather than one in flight.
        /// </summary>
        internal bool InEditor {
            get { return inEditor; }
        }

        /// <summary>
        /// The part the reference refers to now, or null if the game does not have it.
        /// </summary>
        internal Part Find ()
        {
            if (!inEditor)
                return FlightGlobals.FindPartByID (id);
            if (!OfLoadedShip)
                return null;
            var editorId = id;
            var construct = EditorExtensions.Ship;
            return ReferenceEquals (construct, null)
                ? null
                : construct.Parts.FirstOrDefault (x => x.craftID == editorId);
        }

        /// <summary>
        /// The part the reference refers to now. Throws if the game does not have it.
        /// </summary>
        internal Part Resolve ()
        {
            var part = Find ();
            if (ReferenceEquals (part, null))
                throw NotResolvable ();
            return part;
        }

        /// <summary>
        /// What the game holds for the part. A part in flight is live while the vessel
        /// carrying it is loaded, dormant while that vessel is unloaded, as the game then
        /// keeps only a snapshot of each of its parts, and destroyed once the game holds
        /// neither. A part in the editor is only ever in the vessel the editor has open,
        /// so once it is not there it is gone, and leaving the editor takes it with it.
        /// </summary>
        /// <remarks>
        /// Only reached once looking the part up has already failed, so it may take as
        /// long as it needs to; searching every unloaded vessel is the price of never
        /// declaring a part that is merely out of range to be gone.
        /// </remarks>
        internal GameObjectState GameObjectState {
            get {
                if (Find () != null)
                    return GameObjectState.Live;
                // The editor holds nothing that is not in the vessel it has open, so a
                // part it does not have is gone as soon as it is known to have one, and a
                // part of a vessel it has loaded over is gone whatever it has open now.
                if (inEditor) {
                    if (!OfLoadedShip)
                        return GameObjectState.Destroyed;
                    var ship = EditorExtensions.ShipState;
                    return ship == GameObjectState.Live ? GameObjectState.Destroyed : ship;
                }
                if (!FlightGlobalsExtensions.VesselsKnown)
                    return GameObjectState.Dormant;
                foreach (var vessel in FlightGlobals.Vessels) {
                    if (vessel == null || vessel.loaded || vessel.protoVessel == null)
                        continue;
                    foreach (var snapshot in vessel.protoVessel.protoPartSnapshots)
                        if (snapshot.flightID == id)
                            return GameObjectState.Dormant;
                }
                return GameObjectState.Destroyed;
            }
        }

        /// <summary>
        /// The error to raise when the part cannot be looked up, which says whether it is
        /// gone for good or is only waiting for the game to get back to it.
        /// </summary>
        internal Exception NotResolvable ()
        {
            if (GameObjectState != GameObjectState.Destroyed)
                return new InvalidOperationException (
                    inEditor
                    ? "The vessel in the editor is not ready to be read. " +
                      "The part can be used again once the editor has it open."
                    : "The part is not loaded, as the vessel carrying it is unloaded. " +
                      "It can be used again once the game loads that vessel.");
            if (!OfLoadedShip)
                return new ObjectDestroyedException (
                    "The part belongs to a vessel that the editor has loaded another " +
                    "vessel over. Parts of the vessel the editor has open now have to " +
                    "be obtained again, as a part is only named within its own vessel.");
            return new ObjectDestroyedException (
                inEditor
                ? "The part is no longer in the vessel in the editor. It was removed " +
                  "from the vessel, or the editor was left."
                : "The part no longer exists. It was destroyed, or the vessel " +
                  "carrying it was destroyed or recovered.");
        }

        /// <summary>
        /// Returns true if the references are to the same part.
        /// </summary>
        public bool Equals (PartId other)
        {
            return id == other.id && inEditor == other.inEditor &&
                   generation == other.generation;
        }

        /// <summary>
        /// Returns true if the references are to the same part.
        /// </summary>
        public override bool Equals (object obj)
        {
            return obj is PartId && Equals ((PartId)obj);
        }

        /// <summary>
        /// Hash code for the reference.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (id).And (inEditor).And (generation);
        }

        /// <summary>
        /// Returns true if the references are to the same part.
        /// </summary>
        public static bool operator == (PartId lhs, PartId rhs)
        {
            return lhs.Equals (rhs);
        }

        /// <summary>
        /// Returns true if the references are to different parts.
        /// </summary>
        public static bool operator != (PartId lhs, PartId rhs)
        {
            return !lhs.Equals (rhs);
        }
    }
}

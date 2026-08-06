using System;
using System.Linq;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// A reference to a part that keeps working when the game destroys the part and
    /// recreates it. Holds the identifier the game gave the part in the scene the
    /// reference was made in, and which kind of identifier that is: a part is not given
    /// a flight id until the vessel it belongs to is launched, and the id it is given
    /// in the editor is only unique within one vessel, so neither works in both scenes.
    /// </summary>
    struct PartId : IEquatable<PartId>
    {
        readonly uint id;
        readonly bool inEditor;

        internal PartId (Part part)
        {
            if (ReferenceEquals (part, null))
                throw new ArgumentNullException (nameof (part));
            inEditor = HighLogic.LoadedSceneIsEditor;
            id = inEditor ? part.craftID : part.flightID;
        }

        /// <summary>
        /// The part the reference refers to now.
        /// </summary>
        internal Part Resolve ()
        {
            if (!inEditor)
                return FlightGlobals.FindPartByID (id);
            var editorId = id;
            var logic = EditorLogic.fetch;
            var construct = ReferenceEquals (logic, null) ? null : logic.ship;
            var part = ReferenceEquals (construct, null)
                ? null
                : construct.Parts.FirstOrDefault (x => x.craftID == editorId);
            if (ReferenceEquals (part, null))
                throw new InvalidOperationException (
                    "The part is no longer in the vessel in the editor.");
            return part;
        }

        /// <summary>
        /// Returns true if the references are to the same part.
        /// </summary>
        public bool Equals (PartId other)
        {
            return id == other.id && inEditor == other.inEditor;
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
            return id.GetHashCode () ^ inEditor.GetHashCode ();
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

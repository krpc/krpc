using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents crew in a vessel. Can be obtained using <see cref="Vessel.Crew"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class CrewMember : Equatable<CrewMember>
    {
        /// <summary>
        /// Construct a crew member from a KSP crew member.
        /// </summary>
        public CrewMember (ProtoCrewMember crewMember)
        {
            InternalCrewMember = crewMember;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (CrewMember other)
        {
            return !ReferenceEquals (other, null) && InternalCrewMember == other.InternalCrewMember;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return InternalCrewMember.GetHashCode ();
        }

        /// <summary>
        /// The KSP crew member.
        /// </summary>
        public ProtoCrewMember InternalCrewMember { get; private set; }

        /// <summary>
        /// The crew members name.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalCrewMember.name; }
            set { InternalCrewMember.ChangeName (value); }
        }

        /// <summary>
        /// The type of crew member.
        /// </summary>
        [KRPCProperty]
        public CrewMemberType Type {
            get { return InternalCrewMember.type.ToCrewMemberType(); }
        }

        /// <summary>
        /// Whether the crew member is on a mission.
        /// </summary>
        [KRPCProperty]
        public bool OnMission {
            get { return !InternalCrewMember.inactive; }
        }

        /// <summary>
        /// The crew members courage.
        /// </summary>
        [KRPCProperty]
        public float Courage {
            get { return InternalCrewMember.courage; }
            set { InternalCrewMember.courage = value; }
        }

        /// <summary>
        /// The crew members stupidity.
        /// </summary>
        [KRPCProperty]
        public float Stupidity {
            get { return InternalCrewMember.stupidity; }
            set { InternalCrewMember.stupidity = value; }
        }

        /// <summary>
        /// The crew members experience.
        /// </summary>
        [KRPCProperty]
        public float Experience {
            get { return InternalCrewMember.experience; }
            set { InternalCrewMember.experience = value; }
        }

        /// <summary>
        /// Whether the crew member is a badass.
        /// </summary>
        [KRPCProperty]
        public bool Badass {
            get { return InternalCrewMember.isBadass; }
            set { InternalCrewMember.isBadass = value; }
        }

        /// <summary>
        /// Whether the crew member is a veteran.
        /// </summary>
        [KRPCProperty]
        public bool Veteran {
            get { return InternalCrewMember.veteran; }
            set { InternalCrewMember.veteran = value; }
        }
    }
}

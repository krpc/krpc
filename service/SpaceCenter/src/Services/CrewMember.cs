using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// The crewmember's job.
        /// </summary>
        [KRPCProperty]
        public string Trait => InternalCrewMember.trait;

        /// <summary>
        /// The crewmember's gender.
        /// </summary>
        [KRPCProperty]
        public string Gender {
            get { return InternalCrewMember.gender.ToString(); }
        }

        /// <summary>
        /// The crew member's current roster status.  One of "Available", "Assigned", "dead", or "Missing"
        /// </summary>
        [KRPCProperty]
        public string RosterStatus {
            get { return InternalCrewMember.rosterStatus.ToString(); }
        }

        /// <summary>
        /// The crew member's current suit type.  One of "Default", "Vintage", "Future", "Slim"
        /// </summary>
        [KRPCProperty]
        public string SuitType {
            get { return InternalCrewMember.suit.ToString(); }
            set { InternalCrewMember.suit = (ProtoCrewMember.KerbalSuit)Enum.Parse(typeof(ProtoCrewMember.KerbalSuit), value, true); }
        }

        /// <summary>
        /// The flight IDs for each entry in the career flight log.
        /// </summary>
        [KRPCProperty]
        public IList<int> CareerLogFlights => InternalCrewMember.careerLog.Entries.Select((FlightLog.Entry entry) => entry.flight).ToList();

        /// <summary>
        /// The type for each entry in the career flight log.
        /// </summary>
        [KRPCProperty]
        public IList<string> CareerLogTypes => InternalCrewMember.careerLog.Entries.Select((FlightLog.Entry entry) => entry.type).ToList();

        /// <summary>
        /// The body name for each entry in the career flight log.
        /// </summary>
        [KRPCProperty]
        public IList<string> CareerLogTargets => InternalCrewMember.careerLog.Entries.Select((FlightLog.Entry entry) => entry.target).ToList();
    }
}

using KRPC.Service;
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
    public class CrewMember : Equatable<CrewMember>, IGameObjectState
    {
        /// <summary>
        /// Construct a crew member from a KSP crew member.
        /// </summary>
        public CrewMember (ProtoCrewMember crewMember)
        {
            m_protoCrewMemberName = crewMember.name;
        }

        /// <summary>
        /// What the game holds for the crew member, who is live while the game still
        /// has them on its roster. A game that has not been loaded has no roster to look
        /// in, which says nothing about anyone.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var roster = HighLogic.CurrentGame?.CrewRoster;
                if (roster == null)
                    return GameObjectState.Dormant;
                return roster [m_protoCrewMemberName] != null
                    ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        /// <remarks>
        /// The kerbal's name is what this stands for, and comparing it is all this may do.
        /// The object store compares and hashes the objects it holds, including while removing
        /// one whose kerbal the game no longer has, so this has to agree with the hash code
        /// wherever the hash code can be taken. Looking the kerbal up here instead would make
        /// two objects standing for different kerbals the game no longer has, which both look
        /// up to nothing, compare equal while hashing differently.
        /// </remarks>
        public override bool Equals (CrewMember other)
        {
            return !ReferenceEquals (other, null) &&
            m_protoCrewMemberName == other.m_protoCrewMemberName;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return m_protoCrewMemberName.GetHashCode ();
        }

        /// <summary>
        /// The KSP crew member.
        /// </summary>
        public ProtoCrewMember InternalCrewMember
        {
            get
            {
                return HighLogic.CurrentGame?.CrewRoster?[m_protoCrewMemberName];
            }
        }
        readonly string m_protoCrewMemberName;

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
        /// The crew member's job.
        /// </summary>
        [KRPCProperty]
        public string Trait => InternalCrewMember.trait;

        /// <summary>
        /// The crew member's gender.
        /// </summary>
        [KRPCProperty]
        public CrewMemberGender Gender {
            get { return (CrewMemberGender)InternalCrewMember.gender; }
        }

        /// <summary>
        /// The crew member's current roster status.
        /// </summary>
        [KRPCProperty]
        public RosterStatus RosterStatus {
            get { return (RosterStatus)InternalCrewMember.rosterStatus; }
        }

        /// <summary>
        /// The part containing the crew member. Returns <c>null</c> if the crew member is not
        /// occupying a part, or the vessel containing the part is not loaded.
        /// </summary>
        [KRPCProperty (Nullable = true, GameScene = GameScene.Flight)]
        public Parts.Part Part {
            get {
                var part = InternalPart;
                return part == null ? null : new Parts.Part (part);
            }
        }

        /// <summary>
        /// The KSP part the crew member is occupying, or <c>null</c> if it is not in one or
        /// the vessel containing it is not loaded.
        /// </summary>
        global::Part InternalPart {
            get {
                var crewMember = InternalCrewMember;
                if (crewMember == null)
                    return null;
                var seat = crewMember.seat;
                var part = seat == null ? null : seat.part;
                if (part == null || !part.protoModuleCrew.Contains (crewMember)) {
                    // The seat is null when the part's IVA model is not instantiated, but the
                    // part's crew manifest remains available.
                    part = FlightGlobals.VesselsLoaded
                        .SelectMany (vessel => vessel.parts)
                        .FirstOrDefault (candidate => candidate.protoModuleCrew.Contains (crewMember));
                }
                return part;
            }
        }

        /// <summary>
        /// Send the crew member outside on EVA, through the hatch of the part it is in, and
        /// return the vessel it becomes. The game switches to that vessel, which can then be
        /// walked around and flown using its <see cref="Vessel.Control"/>, and brought back
        /// in with <see cref="Control.Board"/>.
        /// </summary>
        /// <remarks>
        /// Throws an exception if EVA is disabled for the game, if the crew member is not in
        /// a loaded vessel, if it is already on EVA, or if every hatch out of the part it is
        /// in is obstructed.
        /// </remarks>
        [KRPCMethod (GameScene = GameScene.Flight)]
        public Vessel EVA ()
        {
            if (!HighLogic.CurrentGame.Parameters.Flight.CanEVA)
                throw new InvalidOperationException ("EVA is disabled for this game");
            var crewMember = InternalCrewMember;
            var part = InternalPart;
            if (part == null)
                throw new InvalidOperationException ("The crew member is not in a loaded vessel");
            if (part.vessel.isEVA)
                throw new InvalidOperationException ("The crew member is already on EVA");
            var eva = FlightEVA.fetch.spawnEVA (crewMember, part, part.airlock, true);
            if (eva == null)
                throw new InvalidOperationException (
                    "Failed to send " + crewMember.name + " on EVA; every hatch out of " +
                    part.partInfo.title + " may be obstructed");
            return WaitForEVA (eva, 0);
        }

        static Vessel WaitForEVA (KerbalEVA eva, int tick)
        {
            // The game brings the kerbal into being over the following frames and then
            // switches to it, so wait for it to be flyable rather than hand back a vessel
            // that cannot yet be controlled.
            if (eva == null || eva.vessel == null)
                throw new InvalidOperationException ("The kerbal was destroyed while leaving the vessel");
            if (FlightGlobals.ActiveVessel == eva.vessel && eva.vessel.loaded && !eva.vessel.packed)
                return new Vessel (eva.vessel);
            if (tick > 500)
                throw new InvalidOperationException ("The kerbal failed to leave the vessel");
            throw new YieldException<Func<Vessel>> (() => WaitForEVA (eva, tick + 1));
        }

        /// <summary>
        /// The crew member's suit type.
        /// </summary>
        [KRPCProperty]
        public SuitType SuitType {
            get { return (SuitType)InternalCrewMember.suit; }
            set { InternalCrewMember.suit = (ProtoCrewMember.KerbalSuit)value; }
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

using System;
using KRPC.SpaceCenter.Services;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class KerbalTypeExtensions
    {
        public static CrewMemberType ToCrewMemberType (this ProtoCrewMember.KerbalType type)
        {
            switch (type) {
            case ProtoCrewMember.KerbalType.Applicant:
                return CrewMemberType.Applicant;
            case ProtoCrewMember.KerbalType.Crew:
                return CrewMemberType.Crew;
            case ProtoCrewMember.KerbalType.Tourist:
                return CrewMemberType.Tourist;
            case ProtoCrewMember.KerbalType.Unowned:
                return CrewMemberType.Unowned;
            default:
                throw new ArgumentOutOfRangeException (nameof (type));
            }
        }
    }
}

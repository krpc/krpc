using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class HopTypeExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static CommLinkType ToCommLinkType (this CommNet.HopType type)
        {
            switch (type) {
            case CommNet.HopType.Home:
                return CommLinkType.Home;
            case CommNet.HopType.ControlPoint:
                return CommLinkType.Control;
            case CommNet.HopType.Relay:
                return CommLinkType.Relay;
            default:
                throw new ArgumentOutOfRangeException (nameof (type));
            }
        }
    }
}

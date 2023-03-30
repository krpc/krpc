using System;
using KRPC.SpaceCenter.Services;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class HopTypeExtensions
    {
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

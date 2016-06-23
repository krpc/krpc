using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.KerbalAlarmClock.ExtensionMethods
{
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
    static class AlarmTypeExtensions
    {
        public static AlarmType ToAlarmType (this KACWrapper.KACAPI.AlarmTypeEnum type)
        {
            switch (type) {
            case KACWrapper.KACAPI.AlarmTypeEnum.Apoapsis:
                return AlarmType.Apoapsis;
            case KACWrapper.KACAPI.AlarmTypeEnum.AscendingNode:
                return AlarmType.AscendingNode;
            case KACWrapper.KACAPI.AlarmTypeEnum.Closest:
                return AlarmType.Closest;
            case KACWrapper.KACAPI.AlarmTypeEnum.Contract:
                return AlarmType.Contract;
            case KACWrapper.KACAPI.AlarmTypeEnum.ContractAuto:
                return AlarmType.ContractAuto;
            case KACWrapper.KACAPI.AlarmTypeEnum.Crew:
                return AlarmType.Crew;
            case KACWrapper.KACAPI.AlarmTypeEnum.DescendingNode:
                return AlarmType.DescendingNode;
            case KACWrapper.KACAPI.AlarmTypeEnum.Distance:
                return AlarmType.Distance;
            case KACWrapper.KACAPI.AlarmTypeEnum.EarthTime:
                return AlarmType.EarthTime;
            case KACWrapper.KACAPI.AlarmTypeEnum.LaunchRendevous:
                return AlarmType.LaunchRendevous;
            case KACWrapper.KACAPI.AlarmTypeEnum.Maneuver:
                return AlarmType.Maneuver;
            case KACWrapper.KACAPI.AlarmTypeEnum.ManeuverAuto:
                return AlarmType.ManeuverAuto;
            case KACWrapper.KACAPI.AlarmTypeEnum.Periapsis:
                return AlarmType.Periapsis;
            case KACWrapper.KACAPI.AlarmTypeEnum.Raw:
                return AlarmType.Raw;
            case KACWrapper.KACAPI.AlarmTypeEnum.SOIChange:
                return AlarmType.SOIChange;
            case KACWrapper.KACAPI.AlarmTypeEnum.SOIChangeAuto:
                return AlarmType.SOIChangeAuto;
            case KACWrapper.KACAPI.AlarmTypeEnum.Transfer:
                return AlarmType.Transfer;
            case KACWrapper.KACAPI.AlarmTypeEnum.TransferModelled:
                return AlarmType.TransferModelled;
            default:
                throw new ArgumentException ("Unsupported alarm type");
            }
        }

        [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        public static KACWrapper.KACAPI.AlarmTypeEnum FromAlarmType (this AlarmType type)
        {
            switch (type) {
            case AlarmType.Apoapsis:
                return KACWrapper.KACAPI.AlarmTypeEnum.Apoapsis;
            case AlarmType.AscendingNode:
                return KACWrapper.KACAPI.AlarmTypeEnum.AscendingNode;
            case AlarmType.Closest:
                return KACWrapper.KACAPI.AlarmTypeEnum.Closest;
            case AlarmType.Contract:
                return KACWrapper.KACAPI.AlarmTypeEnum.Contract;
            case AlarmType.ContractAuto:
                return KACWrapper.KACAPI.AlarmTypeEnum.ContractAuto;
            case AlarmType.Crew:
                return KACWrapper.KACAPI.AlarmTypeEnum.Crew;
            case AlarmType.DescendingNode:
                return KACWrapper.KACAPI.AlarmTypeEnum.DescendingNode;
            case AlarmType.Distance:
                return KACWrapper.KACAPI.AlarmTypeEnum.Distance;
            case AlarmType.EarthTime:
                return KACWrapper.KACAPI.AlarmTypeEnum.EarthTime;
            case AlarmType.LaunchRendevous:
                return KACWrapper.KACAPI.AlarmTypeEnum.LaunchRendevous;
            case AlarmType.Maneuver:
                return KACWrapper.KACAPI.AlarmTypeEnum.Maneuver;
            case AlarmType.ManeuverAuto:
                return KACWrapper.KACAPI.AlarmTypeEnum.ManeuverAuto;
            case AlarmType.Periapsis:
                return KACWrapper.KACAPI.AlarmTypeEnum.Periapsis;
            case AlarmType.Raw:
                return KACWrapper.KACAPI.AlarmTypeEnum.Raw;
            case AlarmType.SOIChange:
                return KACWrapper.KACAPI.AlarmTypeEnum.SOIChange;
            case AlarmType.SOIChangeAuto:
                return KACWrapper.KACAPI.AlarmTypeEnum.SOIChangeAuto;
            case AlarmType.Transfer:
                return KACWrapper.KACAPI.AlarmTypeEnum.Transfer;
            case AlarmType.TransferModelled:
                return KACWrapper.KACAPI.AlarmTypeEnum.TransferModelled;
            default:
                throw new ArgumentException ("Unsupported alarm type");
            }
        }
    }
}

using System;

namespace KRPCKerbalAlarmClock.ExtensionMethods
{
    static class AlarmTypeExtensions
    {
        public static KRPCKerbalAlarmClock.Services.AlarmType ToAlarmType (this KACWrapper.KACAPI.AlarmTypeEnum type)
        {
            switch (type) {
            case KACWrapper.KACAPI.AlarmTypeEnum.Apoapsis:
                return KRPCKerbalAlarmClock.Services.AlarmType.Apoapsis;
            case KACWrapper.KACAPI.AlarmTypeEnum.AscendingNode:
                return KRPCKerbalAlarmClock.Services.AlarmType.AscendingNode;
            case KACWrapper.KACAPI.AlarmTypeEnum.Closest:
                return KRPCKerbalAlarmClock.Services.AlarmType.Closest;
            case KACWrapper.KACAPI.AlarmTypeEnum.Contract:
                return KRPCKerbalAlarmClock.Services.AlarmType.Contract;
            case KACWrapper.KACAPI.AlarmTypeEnum.ContractAuto:
                return KRPCKerbalAlarmClock.Services.AlarmType.ContractAuto;
            case KACWrapper.KACAPI.AlarmTypeEnum.Crew:
                return KRPCKerbalAlarmClock.Services.AlarmType.Crew;
            case KACWrapper.KACAPI.AlarmTypeEnum.DescendingNode:
                return KRPCKerbalAlarmClock.Services.AlarmType.DescendingNode;
            case KACWrapper.KACAPI.AlarmTypeEnum.Distance:
                return KRPCKerbalAlarmClock.Services.AlarmType.Distance;
            case KACWrapper.KACAPI.AlarmTypeEnum.EarthTime:
                return KRPCKerbalAlarmClock.Services.AlarmType.EarthTime;
            case KACWrapper.KACAPI.AlarmTypeEnum.LaunchRendevous:
                return KRPCKerbalAlarmClock.Services.AlarmType.LaunchRendevous;
            case KACWrapper.KACAPI.AlarmTypeEnum.Maneuver:
                return KRPCKerbalAlarmClock.Services.AlarmType.Maneuver;
            case KACWrapper.KACAPI.AlarmTypeEnum.ManeuverAuto:
                return KRPCKerbalAlarmClock.Services.AlarmType.ManeuverAuto;
            case KACWrapper.KACAPI.AlarmTypeEnum.Periapsis:
                return KRPCKerbalAlarmClock.Services.AlarmType.Periapsis;
            case KACWrapper.KACAPI.AlarmTypeEnum.Raw:
                return KRPCKerbalAlarmClock.Services.AlarmType.Raw;
            case KACWrapper.KACAPI.AlarmTypeEnum.SOIChange:
                return KRPCKerbalAlarmClock.Services.AlarmType.SOIChange;
            case KACWrapper.KACAPI.AlarmTypeEnum.SOIChangeAuto:
                return KRPCKerbalAlarmClock.Services.AlarmType.SOIChangeAuto;
            case KACWrapper.KACAPI.AlarmTypeEnum.Transfer:
                return KRPCKerbalAlarmClock.Services.AlarmType.Transfer;
            case KACWrapper.KACAPI.AlarmTypeEnum.TransferModelled:
                return KRPCKerbalAlarmClock.Services.AlarmType.TransferModelled;
            default:
                throw new ArgumentException ("Unsupported alarm type");
            }
        }

        public static KACWrapper.KACAPI.AlarmTypeEnum FromAlarmType (this KRPCKerbalAlarmClock.Services.AlarmType type)
        {
            switch (type) {
            case KRPCKerbalAlarmClock.Services.AlarmType.Apoapsis:
                return KACWrapper.KACAPI.AlarmTypeEnum.Apoapsis;
            case KRPCKerbalAlarmClock.Services.AlarmType.AscendingNode:
                return KACWrapper.KACAPI.AlarmTypeEnum.AscendingNode;
            case KRPCKerbalAlarmClock.Services.AlarmType.Closest:
                return KACWrapper.KACAPI.AlarmTypeEnum.Closest;
            case KRPCKerbalAlarmClock.Services.AlarmType.Contract:
                return KACWrapper.KACAPI.AlarmTypeEnum.Contract;
            case KRPCKerbalAlarmClock.Services.AlarmType.ContractAuto:
                return KACWrapper.KACAPI.AlarmTypeEnum.ContractAuto;
            case KRPCKerbalAlarmClock.Services.AlarmType.Crew:
                return KACWrapper.KACAPI.AlarmTypeEnum.Crew;
            case KRPCKerbalAlarmClock.Services.AlarmType.DescendingNode:
                return KACWrapper.KACAPI.AlarmTypeEnum.DescendingNode;
            case KRPCKerbalAlarmClock.Services.AlarmType.Distance:
                return KACWrapper.KACAPI.AlarmTypeEnum.Distance;
            case KRPCKerbalAlarmClock.Services.AlarmType.EarthTime:
                return KACWrapper.KACAPI.AlarmTypeEnum.EarthTime;
            case KRPCKerbalAlarmClock.Services.AlarmType.LaunchRendevous:
                return KACWrapper.KACAPI.AlarmTypeEnum.LaunchRendevous;
            case KRPCKerbalAlarmClock.Services.AlarmType.Maneuver:
                return KACWrapper.KACAPI.AlarmTypeEnum.Maneuver;
            case KRPCKerbalAlarmClock.Services.AlarmType.ManeuverAuto:
                return KACWrapper.KACAPI.AlarmTypeEnum.ManeuverAuto;
            case KRPCKerbalAlarmClock.Services.AlarmType.Periapsis:
                return KACWrapper.KACAPI.AlarmTypeEnum.Periapsis;
            case KRPCKerbalAlarmClock.Services.AlarmType.Raw:
                return KACWrapper.KACAPI.AlarmTypeEnum.Raw;
            case KRPCKerbalAlarmClock.Services.AlarmType.SOIChange:
                return KACWrapper.KACAPI.AlarmTypeEnum.SOIChange;
            case KRPCKerbalAlarmClock.Services.AlarmType.SOIChangeAuto:
                return KACWrapper.KACAPI.AlarmTypeEnum.SOIChangeAuto;
            case KRPCKerbalAlarmClock.Services.AlarmType.Transfer:
                return KACWrapper.KACAPI.AlarmTypeEnum.Transfer;
            case KRPCKerbalAlarmClock.Services.AlarmType.TransferModelled:
                return KACWrapper.KACAPI.AlarmTypeEnum.TransferModelled;
            default:
                throw new ArgumentException ("Unsupported alarm type");
            }
        }
    }
}

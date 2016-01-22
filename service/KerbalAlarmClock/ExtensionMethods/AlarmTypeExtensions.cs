using System;

namespace KRPC.KerbalAlarmClock.ExtensionMethods
{
    static class AlarmTypeExtensions
    {
        public static KRPC.KerbalAlarmClock.Services.AlarmType ToAlarmType (this KACWrapper.KACAPI.AlarmTypeEnum type)
        {
            switch (type) {
            case KACWrapper.KACAPI.AlarmTypeEnum.Apoapsis:
                return KRPC.KerbalAlarmClock.Services.AlarmType.Apoapsis;
            case KACWrapper.KACAPI.AlarmTypeEnum.AscendingNode:
                return KRPC.KerbalAlarmClock.Services.AlarmType.AscendingNode;
            case KACWrapper.KACAPI.AlarmTypeEnum.Closest:
                return KRPC.KerbalAlarmClock.Services.AlarmType.Closest;
            case KACWrapper.KACAPI.AlarmTypeEnum.Contract:
                return KRPC.KerbalAlarmClock.Services.AlarmType.Contract;
            case KACWrapper.KACAPI.AlarmTypeEnum.ContractAuto:
                return KRPC.KerbalAlarmClock.Services.AlarmType.ContractAuto;
            case KACWrapper.KACAPI.AlarmTypeEnum.Crew:
                return KRPC.KerbalAlarmClock.Services.AlarmType.Crew;
            case KACWrapper.KACAPI.AlarmTypeEnum.DescendingNode:
                return KRPC.KerbalAlarmClock.Services.AlarmType.DescendingNode;
            case KACWrapper.KACAPI.AlarmTypeEnum.Distance:
                return KRPC.KerbalAlarmClock.Services.AlarmType.Distance;
            case KACWrapper.KACAPI.AlarmTypeEnum.EarthTime:
                return KRPC.KerbalAlarmClock.Services.AlarmType.EarthTime;
            case KACWrapper.KACAPI.AlarmTypeEnum.LaunchRendevous:
                return KRPC.KerbalAlarmClock.Services.AlarmType.LaunchRendevous;
            case KACWrapper.KACAPI.AlarmTypeEnum.Maneuver:
                return KRPC.KerbalAlarmClock.Services.AlarmType.Maneuver;
            case KACWrapper.KACAPI.AlarmTypeEnum.ManeuverAuto:
                return KRPC.KerbalAlarmClock.Services.AlarmType.ManeuverAuto;
            case KACWrapper.KACAPI.AlarmTypeEnum.Periapsis:
                return KRPC.KerbalAlarmClock.Services.AlarmType.Periapsis;
            case KACWrapper.KACAPI.AlarmTypeEnum.Raw:
                return KRPC.KerbalAlarmClock.Services.AlarmType.Raw;
            case KACWrapper.KACAPI.AlarmTypeEnum.SOIChange:
                return KRPC.KerbalAlarmClock.Services.AlarmType.SOIChange;
            case KACWrapper.KACAPI.AlarmTypeEnum.SOIChangeAuto:
                return KRPC.KerbalAlarmClock.Services.AlarmType.SOIChangeAuto;
            case KACWrapper.KACAPI.AlarmTypeEnum.Transfer:
                return KRPC.KerbalAlarmClock.Services.AlarmType.Transfer;
            case KACWrapper.KACAPI.AlarmTypeEnum.TransferModelled:
                return KRPC.KerbalAlarmClock.Services.AlarmType.TransferModelled;
            default:
                throw new ArgumentException ("Unsupported alarm type");
            }
        }

        public static KACWrapper.KACAPI.AlarmTypeEnum FromAlarmType (this KRPC.KerbalAlarmClock.Services.AlarmType type)
        {
            switch (type) {
            case KRPC.KerbalAlarmClock.Services.AlarmType.Apoapsis:
                return KACWrapper.KACAPI.AlarmTypeEnum.Apoapsis;
            case KRPC.KerbalAlarmClock.Services.AlarmType.AscendingNode:
                return KACWrapper.KACAPI.AlarmTypeEnum.AscendingNode;
            case KRPC.KerbalAlarmClock.Services.AlarmType.Closest:
                return KACWrapper.KACAPI.AlarmTypeEnum.Closest;
            case KRPC.KerbalAlarmClock.Services.AlarmType.Contract:
                return KACWrapper.KACAPI.AlarmTypeEnum.Contract;
            case KRPC.KerbalAlarmClock.Services.AlarmType.ContractAuto:
                return KACWrapper.KACAPI.AlarmTypeEnum.ContractAuto;
            case KRPC.KerbalAlarmClock.Services.AlarmType.Crew:
                return KACWrapper.KACAPI.AlarmTypeEnum.Crew;
            case KRPC.KerbalAlarmClock.Services.AlarmType.DescendingNode:
                return KACWrapper.KACAPI.AlarmTypeEnum.DescendingNode;
            case KRPC.KerbalAlarmClock.Services.AlarmType.Distance:
                return KACWrapper.KACAPI.AlarmTypeEnum.Distance;
            case KRPC.KerbalAlarmClock.Services.AlarmType.EarthTime:
                return KACWrapper.KACAPI.AlarmTypeEnum.EarthTime;
            case KRPC.KerbalAlarmClock.Services.AlarmType.LaunchRendevous:
                return KACWrapper.KACAPI.AlarmTypeEnum.LaunchRendevous;
            case KRPC.KerbalAlarmClock.Services.AlarmType.Maneuver:
                return KACWrapper.KACAPI.AlarmTypeEnum.Maneuver;
            case KRPC.KerbalAlarmClock.Services.AlarmType.ManeuverAuto:
                return KACWrapper.KACAPI.AlarmTypeEnum.ManeuverAuto;
            case KRPC.KerbalAlarmClock.Services.AlarmType.Periapsis:
                return KACWrapper.KACAPI.AlarmTypeEnum.Periapsis;
            case KRPC.KerbalAlarmClock.Services.AlarmType.Raw:
                return KACWrapper.KACAPI.AlarmTypeEnum.Raw;
            case KRPC.KerbalAlarmClock.Services.AlarmType.SOIChange:
                return KACWrapper.KACAPI.AlarmTypeEnum.SOIChange;
            case KRPC.KerbalAlarmClock.Services.AlarmType.SOIChangeAuto:
                return KACWrapper.KACAPI.AlarmTypeEnum.SOIChangeAuto;
            case KRPC.KerbalAlarmClock.Services.AlarmType.Transfer:
                return KACWrapper.KACAPI.AlarmTypeEnum.Transfer;
            case KRPC.KerbalAlarmClock.Services.AlarmType.TransferModelled:
                return KACWrapper.KACAPI.AlarmTypeEnum.TransferModelled;
            default:
                throw new ArgumentException ("Unsupported alarm type");
            }
        }
    }
}

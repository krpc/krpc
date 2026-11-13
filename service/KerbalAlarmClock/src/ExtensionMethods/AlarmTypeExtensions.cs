using System;

namespace KRPC.KerbalAlarmClock.ExtensionMethods
{
    static class AlarmTypeExtensions
    {
        public static AlarmType ToAlarmType (this API.AlarmTypeEnum type)
        {
            switch (type) {
            case API.AlarmTypeEnum.Apoapsis:
                return AlarmType.Apoapsis;
            case API.AlarmTypeEnum.AscendingNode:
                return AlarmType.AscendingNode;
            case API.AlarmTypeEnum.Closest:
                return AlarmType.Closest;
            case API.AlarmTypeEnum.Contract:
                return AlarmType.Contract;
            case API.AlarmTypeEnum.ContractAuto:
                return AlarmType.ContractAuto;
            case API.AlarmTypeEnum.Crew:
                return AlarmType.Crew;
            case API.AlarmTypeEnum.DescendingNode:
                return AlarmType.DescendingNode;
            case API.AlarmTypeEnum.Distance:
                return AlarmType.Distance;
            case API.AlarmTypeEnum.EarthTime:
                return AlarmType.EarthTime;
            case API.AlarmTypeEnum.LaunchRendevous:
                return AlarmType.LaunchRendevous;
            case API.AlarmTypeEnum.Maneuver:
                return AlarmType.Maneuver;
            case API.AlarmTypeEnum.ManeuverAuto:
                return AlarmType.ManeuverAuto;
            case API.AlarmTypeEnum.Periapsis:
                return AlarmType.Periapsis;
            case API.AlarmTypeEnum.Raw:
                return AlarmType.Raw;
            case API.AlarmTypeEnum.SOIChange:
                return AlarmType.SOIChange;
            case API.AlarmTypeEnum.SOIChangeAuto:
                return AlarmType.SOIChangeAuto;
            case API.AlarmTypeEnum.Transfer:
                return AlarmType.Transfer;
            case API.AlarmTypeEnum.TransferModelled:
                return AlarmType.TransferModelled;
            case API.AlarmTypeEnum.ScienceLab:
                return AlarmType.ScienceLab;
            default:
                throw new ArgumentException ("Unsupported alarm type");
            }
        }

        public static API.AlarmTypeEnum FromAlarmType (this AlarmType type)
        {
            switch (type) {
            case AlarmType.Apoapsis:
                return API.AlarmTypeEnum.Apoapsis;
            case AlarmType.AscendingNode:
                return API.AlarmTypeEnum.AscendingNode;
            case AlarmType.Closest:
                return API.AlarmTypeEnum.Closest;
            case AlarmType.Contract:
                return API.AlarmTypeEnum.Contract;
            case AlarmType.ContractAuto:
                return API.AlarmTypeEnum.ContractAuto;
            case AlarmType.Crew:
                return API.AlarmTypeEnum.Crew;
            case AlarmType.DescendingNode:
                return API.AlarmTypeEnum.DescendingNode;
            case AlarmType.Distance:
                return API.AlarmTypeEnum.Distance;
            case AlarmType.EarthTime:
                return API.AlarmTypeEnum.EarthTime;
            case AlarmType.LaunchRendevous:
                return API.AlarmTypeEnum.LaunchRendevous;
            case AlarmType.Maneuver:
                return API.AlarmTypeEnum.Maneuver;
            case AlarmType.ManeuverAuto:
                return API.AlarmTypeEnum.ManeuverAuto;
            case AlarmType.Periapsis:
                return API.AlarmTypeEnum.Periapsis;
            case AlarmType.Raw:
                return API.AlarmTypeEnum.Raw;
            case AlarmType.SOIChange:
                return API.AlarmTypeEnum.SOIChange;
            case AlarmType.SOIChangeAuto:
                return API.AlarmTypeEnum.SOIChangeAuto;
            case AlarmType.Transfer:
                return API.AlarmTypeEnum.Transfer;
            case AlarmType.TransferModelled:
                return API.AlarmTypeEnum.TransferModelled;
            case AlarmType.ScienceLab:
                return API.AlarmTypeEnum.ScienceLab;
            default:
                throw new ArgumentException ("Unsupported alarm type");
            }
        }
    }
}

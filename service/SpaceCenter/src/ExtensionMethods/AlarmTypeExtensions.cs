namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class AlarmTypeExtensions
    {
        /// <summary>
        /// Map a KSP stock alarm to the corresponding kRPC <see cref="Services.AlarmType"/>.
        /// Dispatch is by concrete runtime type so that this is resilient to changes in
        /// <c>AlarmTypeBase.TypeName</c> formatting.
        /// </summary>
        public static Services.AlarmType ToAlarmType (this AlarmTypeBase alarm)
        {
            if (alarm is AlarmTypeRaw)
                return Services.AlarmType.Raw;
            if (alarm is AlarmTypeApoapsis)
                return Services.AlarmType.Apoapsis;
            if (alarm is AlarmTypePeriapsis)
                return Services.AlarmType.Periapsis;
            if (alarm is AlarmTypeManeuver)
                return Services.AlarmType.Maneuver;
            if (alarm is AlarmTypeSOI)
                return Services.AlarmType.SOIChange;
            if (alarm is AlarmTypeTransferWindow)
                return Services.AlarmType.TransferWindow;
            return Services.AlarmType.Unknown;
        }
    }
}

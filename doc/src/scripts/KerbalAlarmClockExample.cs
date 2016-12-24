using System;
using System.Net;
using KRPC.Client;
using KRPC.Client.Services.KerbalAlarmClock;
using KRPC.Client.Services.SpaceCenter;

class KerbalAlarmClockExample
{
    public static void Main ()
    {
        var connection = new Connection (name: "Kerbal Alarm Clock Example");
        var kac = connection.KerbalAlarmClock ();
        var alarm = kac.CreateAlarm (AlarmType.Raw, "My New Alarm", connection.SpaceCenter ().UT + 10);
        alarm.Notes = "10 seconds have now passed since the alarm was created.";
        alarm.Action = AlarmAction.MessageOnly;
    }
}

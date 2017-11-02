#include <krpc.h>
#include <krpc/services/space_center.h>
#include <krpc/services/kerbal_alarm_clock.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Kerbal Alarm Clock Example");

  double ut;
  krpc_SpaceCenter_UT(conn, &ut);
  krpc_KerbalAlarmClock_Alarm_t alarm;
  krpc_KerbalAlarmClock_CreateAlarm(
    conn, &alarm, KRPC_KERBALALARMCLOCK_ALARMTYPE_RAW, "My New Alarm", ut+10);

  krpc_KerbalAlarmClock_Alarm_set_Notes(
    conn, alarm, "10 seconds have now passed since the alarm was created.");
  krpc_KerbalAlarmClock_Alarm_set_Action(
    conn, alarm, KRPC_KERBALALARMCLOCK_ALARMACTION_MESSAGEONLY);
}

#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <krpc_cnano/services/kerbal_alarm_clock.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open(&conn, "COM0") != KRPC_OK)
    return 1;
  if (krpc_connect(conn, "Kerbal Alarm Clock Example") != KRPC_OK)
    return 1;

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

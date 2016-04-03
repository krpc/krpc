#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/kerbal_alarm_clock.hpp>
#include <iostream>

using namespace krpc::services;

int main() {
  krpc::Client conn = krpc::connect("Kerbal Alarm Clock Example");
  SpaceCenter sc(&conn);
  KerbalAlarmClock kac(&conn);

  auto alarm = kac.create_alarm(KerbalAlarmClock::AlarmType::raw,
                                "My New Alarm",
                                sc.ut()+10);

  alarm.set_notes("10 seconds have now passed since the alarm was created.");
  alarm.set_action(KerbalAlarmClock::AlarmAction::message_only);
}

#include "gtest/gtest.h"
// clang-format off: space_center.hpp must precede infernal_robotics.hpp, which
// uses SpaceCenter types.
#include "krpc/services/space_center.hpp"
#include "krpc/services/infernal_robotics.hpp"
#include "krpc/services/kerbal_alarm_clock.hpp"
// clang-format on
#include "server_test.hpp"

class test_services : public server_test {};

TEST_F(test_services, test_basic) {
  krpc::services::SpaceCenter sc(&conn);
  krpc::services::KerbalAlarmClock kac(&conn);
  krpc::services::InfernalRobotics ir(&conn);
}

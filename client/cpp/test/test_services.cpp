#include <gtest/gtest.h>

#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/infernal_robotics.hpp>  // NOLINT(build/include_alpha)
#include <krpc/services/kerbal_alarm_clock.hpp>

#include "server_test.hpp"

class test_services: public server_test {
};

TEST_F(test_services, test_basic) {
  krpc::services::SpaceCenter sc(&conn);
  krpc::services::KerbalAlarmClock kac(&conn);
  krpc::services::InfernalRobotics ir(&conn);
}

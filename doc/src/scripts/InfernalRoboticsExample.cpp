#include <iostream>
#include <vector>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/infernal_robotics.hpp>

using SpaceCenter = krpc::services::SpaceCenter;
using InfernalRobotics = krpc::services::InfernalRobotics;

int main() {
  auto conn = krpc::connect("InfernalRobotics Example");
  SpaceCenter space_center(&conn);
  InfernalRobotics infernal_robotics(&conn);

  InfernalRobotics::ServoGroup group = infernal_robotics.servo_group_with_name(space_center.active_vessel(), "MyGroup");
  if (group == InfernalRobotics::ServoGroup())
    std::cout << "Group not found" << std::endl;

  std::vector<InfernalRobotics::Servo> servos = group.servos();
  for (auto servo : servos)
    std::cout << servo.name() << " " << servo.position() << std::endl;

  group.move_right();
  sleep(1);
  group.stop();
}

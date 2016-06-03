#include <krpc.hpp>
#include <krpc/services/infernal_robotics.hpp>
#include <iostream>
#include <vector>

using namespace krpc::services;

int main() {
  auto conn = krpc::connect("InfernalRobotics Example");
  InfernalRobotics infernal_robotics(&conn);

  InfernalRobotics::ServoGroup group = infernal_robotics.servo_group_with_name("MyGroup");
  if (group == InfernalRobotics::ServoGroup())
    std::cout << "Group not found" << std::endl;

  std::vector<InfernalRobotics::Servo> servos = group.servos();
  for (auto servo : servos)
    std::cout << servo.name() << " " << servo.position() << std::endl;

  group.move_right();
  sleep(1);
  group.stop();
}

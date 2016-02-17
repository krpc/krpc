#include <krpc.hpp>
#include <krpc/services/infernal_robotics.hpp>
#include <iostream>
#include <vector>

using namespace krpc;
using namespace krpc::services;

int main() {
  Client conn = krpc::connect("InfernalRobotics Example");
  InfernalRobotics infernal_robotics(&conn);

  InfernalRobotics::ControlGroup group = infernal_robotics.servo_group_with_name("MyGroup");
  if (group == InfernalRobotics::ControlGroup()) {
    std::cout << "Group not found" << std::endl;
    return 1;
  }

  std::vector<InfernalRobotics::Servo> servos = group.servos();
  for (std::vector<InfernalRobotics::Servo>::iterator i = servos.begin();
       i != servos.end(); i++) {
    std::cout << i->name() << " " << i->position() << std::endl;
  }

  group.move_right();
  sleep(1); //Note: platform dependent
  group.stop();
}

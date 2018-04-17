#include <unistd.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <krpc_cnano/services/infernal_robotics.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "InfernalRobotics Example");
  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  krpc_InfernalRobotics_ServoGroup_t group;
  krpc_InfernalRobotics_ServoGroupWithName(conn, &group, vessel, "MyGroup");
  if (!group)
    printf("Group not found\n");

  krpc_list_object_t servos = KRPC_NULL_LIST;
  krpc_InfernalRobotics_ServoGroup_Servos(conn, &servos, group);
  for (size_t i = 0; i < servos.size; i++) {
    krpc_InfernalRobotics_Servo_t servo = servos.items[i];
    char * name = NULL;
    krpc_InfernalRobotics_Servo_Name(conn, &name, servo);
    float position;
    krpc_InfernalRobotics_Servo_Position(conn, &position, servo);
    printf("%s %.2f\n", name, position);
  }

  krpc_InfernalRobotics_ServoGroup_MoveRight(conn, group);
  sleep(1);
  krpc_InfernalRobotics_ServoGroup_Stop(conn, group);
}

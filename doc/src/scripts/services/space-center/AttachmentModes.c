#include <krpc.h>
#include <krpc/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "InfernalRobotics Example");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  krpc_SpaceCenter_Part_t root;
  krpc_SpaceCenter_Parts_Root(conn, &root, parts);

  typedef struct {
    krpc_SpaceCenter_Part_t part;
    int depth;
  } StackEntry;
  StackEntry stack[256];
  int stackPtr = 0;
  stack[stackPtr].part = root;
  stack[stackPtr].depth = 0;

  while (stackPtr >= 0) {
    krpc_SpaceCenter_Part_t part = stack[stackPtr].part;
    int depth = stack[stackPtr].depth;
    stackPtr--;  // Pop the stack
    bool axially_attached;
    krpc_SpaceCenter_Part_AxiallyAttached(conn, &axially_attached, part);
    const char * attach_mode = axially_attached ? "axial" : "radial";
    char * title = NULL;
    krpc_SpaceCenter_Part_Title(conn, &title, part);
    for (int i = 0; i < depth; i++)
      printf(" ");
    printf("%s - %s\n", title, attach_mode);

    krpc_list_object_t children = KRPC_NULL_LIST;
    krpc_SpaceCenter_Part_Children(conn, &children, part);
    for (size_t i = 0; i < children.size; i++) {
      // Push onto the stack
      stackPtr++;
      stack[stackPtr].part = children.items[i];
      stack[stackPtr].depth = depth+1;
    }
  }
}

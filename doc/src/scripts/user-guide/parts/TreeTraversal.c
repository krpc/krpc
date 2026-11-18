#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Tree traversal") != KRPC_OK) return 1;
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
    char* title = NULL;
    krpc_SpaceCenter_Part_Title(conn, &title, part);
    for (int i = 0; i < depth; i++) printf(" ");
    printf("%s\n", title);
    krpc_free(title);

    krpc_list_object_t children = KRPC_NULL_LIST;
    krpc_SpaceCenter_Part_Children(conn, &children, part);
    for (size_t i = 0; i < children.size; i++) {
      // Push onto the stack, unless it is full
      if (stackPtr == 255) break;
      stackPtr++;
      stack[stackPtr].part = children.items[i];
      stack[stackPtr].depth = depth + 1;
    }
    KRPC_FREE_LIST(children);
  }

  krpc_disconnect(conn);
}

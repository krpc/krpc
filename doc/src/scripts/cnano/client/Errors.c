#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Errors example");

  krpc_error_t error = krpc_SpaceCenter_Load(conn, "missing");
  if (error != KRPC_OK)
    printf("Failed to load the save: %s\n", krpc_get_error(error));

  krpc_disconnect(conn);
}

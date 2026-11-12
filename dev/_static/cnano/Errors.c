#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Errors example") != KRPC_OK) return 1;

  krpc_error_t error = krpc_SpaceCenter_Load(conn, "missing");
  if (error == KRPC_ERROR_SERVER &&
      KRPC_IS_EXCEPTION(KRPC_EXCEPTION_KRPC_ARGUMENTEXCEPTION))
    printf("No such save\n");
  else if (error != KRPC_OK)
    printf("Failed to load the save: %s\n", krpc_get_error(error));

  krpc_disconnect(conn);
}

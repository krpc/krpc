#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open(&conn, "/run/user/1000/krpc.sock") != KRPC_OK)
    return 1;
  if (krpc_connect(conn, "My Example Program") != KRPC_OK)
    return 1;
  krpc_KRPC_ServerStatus_t status = {0};
  krpc_KRPC_GetStatus(conn, &status);
  printf("%s\n", status.version);
  krpc_free(status.version);
  krpc_disconnect(conn);
}

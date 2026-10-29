#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "My Example Program");
  krpc_schema_Status status;
  krpc_KRPC_GetStatus(conn, &status);
  printf("%s\n", status.version);
  krpc_disconnect(conn);
}

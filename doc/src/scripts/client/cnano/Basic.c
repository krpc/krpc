#include <krpc.h>
#include <krpc/services/krpc.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Basic example");
  krpc_schema_Status status;
  krpc_KRPC_GetStatus(conn, &status);
  printf("Connected to kRPC server version %s\n", status.version);
}

#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>

int main() {
  krpc_connection_t conn;
  krpc_connection_config_t config;
  config.address = "127.0.0.1";
  config.port = 50000;
  if (krpc_open(&conn, &config) != KRPC_OK)
    return 1;
  if (krpc_connect(conn, "My Example Program") != KRPC_OK)
    return 1;
  krpc_KRPC_ServerStatus_t status = {0};
  krpc_KRPC_GetStatus(conn, &status);
  printf("%s\n", status.version);
  krpc_free(status.version);
  krpc_disconnect(conn);
}

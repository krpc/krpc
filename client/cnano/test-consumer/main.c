#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
#include <stdio.h>

int main(void) {
  /* Compiles and links against the installed package. Opening each transport pulls in its
     code, and the libraries it needs. Nothing is listening at any of the endpoints, so each
     open fails. */
  krpc_connection_t connection;
  if (krpc_open_serial(&connection, "/nonexistent/krpc-cnano-consumer-test-port") == KRPC_OK) {
    printf("opened a serial port that should not exist\n");
    return 1;
  }
  if (krpc_open_tcp(&connection, "127.0.0.1", 1) == KRPC_OK) {
    printf("opened a connection to a port nothing should be listening on\n");
    return 1;
  }
  if (krpc_open_localsocket(&connection, "/nonexistent/krpc-cnano-consumer-test.sock") == KRPC_OK) {
    printf("opened a connection to a socket nothing should be listening on\n");
    return 1;
  }
  printf("krpc_cnano library linked OK\n");
  return 0;
}

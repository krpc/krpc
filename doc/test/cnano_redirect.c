/* The examples' serial connection, opened as a TCP/IP connection to the game's server. The test
   build of each C-nano example renames its krpc_open_serial calls to this function. */

#include <krpc_cnano.h>

krpc_error_t krpc_example_open_serial(krpc_connection_t *connection,
                                      const char *port) {
  (void)port;
  return krpc_open_tcp(connection, "127.0.0.1", 50000);
}

#include <stdio.h>

#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>

int main(void) {
    /* Compiles and links against the installed package; there is no server to talk to. */
    /* Which transport the library was built for reaches a program using it through the package,
       so each of these is compiled only when that transport is the one in question. Opening a
       connection is what pulls in the socket code, and with it the libraries the transport
       needs; nothing is listening at either endpoint, so it fails. */
#ifdef KRPC_COMMUNICATION_TCP
    krpc_connection_t connection;
    krpc_connection_config_t config;
    config.address = "127.0.0.1";
    config.port = 1;
    if (krpc_open(&connection, &config) == KRPC_OK) {
        printf("opened a connection to a port nothing should be listening on\n");
        return 1;
    }
#endif
#ifdef KRPC_COMMUNICATION_LOCALSOCKET
    krpc_connection_t connection;
    if (krpc_open(&connection, "/nonexistent/krpc-cnano-consumer-test.sock") == KRPC_OK) {
        printf("opened a connection to a socket nothing should be listening on\n");
        return 1;
    }
#endif
    printf("krpc_cnano library linked OK\n");
    return 0;
}

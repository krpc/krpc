#include <stdio.h>

#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>

int main(void) {
    /* Compiles and links against the installed package; there is no server to talk to. */
#ifdef KRPC_COMMUNICATION_TCP
    /* Which transport the library was built for reaches a program using it through the package,
       so this is compiled only when that transport is TCP/IP. Opening a connection is what pulls
       in the socket code, and with it the libraries the transport needs; nothing is listening on
       the port, so it fails. */
    krpc_connection_t connection;
    krpc_connection_config_t config;
    config.address = "127.0.0.1";
    config.port = 1;
    if (krpc_open(&connection, &config) == KRPC_OK) {
        printf("opened a connection to a port nothing should be listening on\n");
        return 1;
    }
#endif
    printf("krpc_cnano library linked OK\n");
    return 0;
}

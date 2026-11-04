#include <krpc_cnano/communication.h>

krpc_error_t krpc_read(krpc_connection_t connection, uint8_t* buf, size_t count) {
  if (connection.transport == NULL) KRPC_RETURN_ERROR(IO, "connection is not open");
  return connection.transport->read(connection.handle, buf, count);
}

krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t* buf, size_t count) {
  if (connection.transport == NULL) KRPC_RETURN_ERROR(IO, "connection is not open");
  return connection.transport->write(connection.handle, buf, count);
}

krpc_error_t krpc_close(krpc_connection_t connection) {
  if (connection.transport == NULL) KRPC_RETURN_ERROR(IO, "connection is not open");
  if (connection.transport->close == NULL) return KRPC_OK;
  return connection.transport->close(connection.handle);
}

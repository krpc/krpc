#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
#include <stdio.h>
#ifdef _WIN32
#include <fcntl.h>
#include <io.h>
#endif

/* The program's own state for the link: here, a pair of files */
typedef struct {
  FILE *in;
  FILE *out;
} link_t;

static krpc_error_t link_read(uintptr_t handle, uint8_t *buf, size_t count) {
  link_t *link = (link_t *)handle;
  if (fread(buf, 1, count, link->in) != count)
    return KRPC_ERROR_EOF;
  return KRPC_OK;
}

static krpc_error_t link_write(uintptr_t handle, const uint8_t *buf, size_t count) {
  link_t *link = (link_t *)handle;
  if (fwrite(buf, 1, count, link->out) != count || fflush(link->out) != 0)
    return KRPC_ERROR_IO;
  return KRPC_OK;
}

static const krpc_transport_t link_transport = {link_read, link_write, NULL, false};

int main() {
#ifdef _WIN32
  /* Windows opens the standard streams in text mode, which rewrites newline bytes and stops
     reading at 0x1A, so a message only gets through them in binary mode */
  _setmode(_fileno(stdin), _O_BINARY);
  _setmode(_fileno(stdout), _O_BINARY);
#endif
  link_t link = {stdin, stdout};
  krpc_connection_t conn = {&link_transport, (uintptr_t)&link};
  if (krpc_connect(conn, "My Example Program") != KRPC_OK)
    return 1;
  krpc_KRPC_ServerStatus_t status = {0};
  krpc_KRPC_GetStatus(conn, &status);
  fprintf(stderr, "%s\n", status.version);
  krpc_free(status.version);
  krpc_disconnect(conn);
}

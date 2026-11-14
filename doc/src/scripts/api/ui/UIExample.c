#include <krpc_cnano.h>
#include <krpc_cnano/services/ui.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "User Interface Example") != KRPC_OK) return 1;

  krpc_StdLib_ColorRGBA_t color = {1, 0.92f, 0.016f, 1};
  krpc_UI_Message(conn, "Hello, World!", 10, KRPC_UI_MESSAGEPOSITION_TOPCENTER,
                  &color, 20);
  krpc_disconnect(conn);
}

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#define sleep(seconds) Sleep((seconds) * 1000)
#else
#include <unistd.h>
#endif
#include <krpc_cnano.h>
#include <krpc_cnano/services/drawing.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Drawing Example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_Vessel_ReferenceFrame(conn, &frame, vessel);
  krpc_tuple_double_double_double_t direction = {0, 1, 0};
  krpc_Drawing_Line_t line;
  krpc_Drawing_AddDirectionFromCom(conn, &line, &direction, frame, 10, true);
  sleep(10);
  krpc_disconnect(conn);
}

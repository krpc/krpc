#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <krpc_cnano/services/ui.h>
#include <krpc_cnano/services/drawing.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open(&conn, "COM0") != KRPC_OK)
    return 1;
  if (krpc_connect(conn, "Visual debugging") != KRPC_OK)
    return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  // Draw the axes of the surface reference frame
  krpc_SpaceCenter_ReferenceFrame_t surface_ref;
  krpc_SpaceCenter_Vessel_SurfaceReferenceFrame(conn, &surface_ref, vessel);
  krpc_Drawing_AddAxes(conn, NULL, surface_ref, 10, true);

  // Draw the direction of the surface velocity, as a labeled arrow
  krpc_SpaceCenter_ReferenceFrame_t ref_frame;
  krpc_SpaceCenter_Vessel_SurfaceVelocityReferenceFrame(conn, &ref_frame, vessel);
  krpc_tuple_double_double_double_t direction = { 0, 1, 0 };
  krpc_Drawing_Line_t line;
  krpc_Drawing_AddDirectionFromCom(conn, &line, &direction, ref_frame, 10, true);
  krpc_Drawing_Line_set_EndArrow(conn, line, true);
  krpc_Drawing_Line_set_Label(conn, line, "Surface velocity");
  while (true) {
  }
}

#include <unistd.h>
#include <math.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <krpc_cnano/services/ui.h>
#include <krpc_cnano/services/drawing.h>

typedef krpc_tuple_double_double_double_t vector3;
typedef krpc_tuple_double_double_double_double_t quaternion;

static double pi = 3.1415926535897;

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Landing site");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  krpc_SpaceCenter_ReferenceFrame_t body_ref;
  krpc_SpaceCenter_CelestialBody_ReferenceFrame(conn, &body_ref, body);

  // Define the landing site as the top of the VAB
  double landing_latitude = -(0.0+(5.0/60.0)+(48.38/60.0/60.0));
  double landing_longitude = -(74.0+(37.0/60.0)+(12.2/60.0/60.0));
  double landing_altitude = 111;

  // Determine landing site reference frame
  // (orientation: x=zenith, y=north, z=east)
  vector3 landing_position;
  krpc_SpaceCenter_CelestialBody_SurfacePosition(
    conn, &landing_position, body, landing_latitude, landing_longitude, body_ref);
  quaternion q_long = {
    0.0,
    sin(-landing_longitude * 0.5 * pi / 180.0),
    0.0,
    cos(-landing_longitude * 0.5 * pi / 180.0)
  };
  quaternion q_lat = {
    0.0,
    0.0,
    sin(landing_latitude * 0.5 * pi / 180.0),
    cos(landing_latitude * 0.5 * pi / 180.0)
  };

  krpc_SpaceCenter_ReferenceFrame_t landing_reference_frame;
  {
    vector3 zero = {0, 0, 0};
    quaternion q_zero = {0, 0, 0, 1};
    krpc_SpaceCenter_ReferenceFrame_t parent_ref;
    krpc_SpaceCenter_ReferenceFrame_CreateRelative(
      conn, &parent_ref, body_ref, &landing_position, &q_long, &zero, &zero);
    krpc_SpaceCenter_ReferenceFrame_CreateRelative(
      conn, &parent_ref, parent_ref, &zero, &q_lat, &zero, &zero);
    vector3 position = { landing_altitude, 0, 0 };
    krpc_SpaceCenter_ReferenceFrame_CreateRelative(
      conn, &landing_reference_frame, parent_ref, &position, &q_zero, &zero, &zero);
  }

  // Draw axes
  vector3 zero = {0, 0, 0};
  vector3 x_axis = {1, 0, 0};
  vector3 y_axis = {0, 1, 0};
  vector3 z_axis = {0, 0, 1};
  krpc_Drawing_AddLine(conn, NULL, &zero, &x_axis, landing_reference_frame, true);
  krpc_Drawing_AddLine(conn, NULL, &zero, &y_axis, landing_reference_frame, true);
  krpc_Drawing_AddLine(conn, NULL, &zero, &z_axis, landing_reference_frame, true);

  while (true)
    sleep(1);
}

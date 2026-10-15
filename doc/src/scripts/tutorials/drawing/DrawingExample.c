#include <stdio.h>
#include <string.h>
#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#define sleep(seconds) Sleep((seconds) * 1000)
#else
#include <unistd.h>
#endif
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <krpc_cnano/services/std_lib.h>
#include <krpc_cnano/services/ui.h>
#include <krpc_cnano/services/drawing.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Drawing Example");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_dictionary_string_object_t bodies = KRPC_NULL_DICTIONARY;
  krpc_SpaceCenter_Bodies(conn, &bodies);
  krpc_SpaceCenter_CelestialBody_t kerbin;
  for (size_t i = 0; i < bodies.size; i++)
    if (!strcmp(bodies.entries[i].key, "Kerbin"))
      kerbin = bodies.entries[i].value;
  krpc_SpaceCenter_ReferenceFrame_t kerbin_ref;
  krpc_SpaceCenter_CelestialBody_ReferenceFrame(conn, &kerbin_ref, kerbin);

  // Draw a circular orbit 250 km above Kerbin, as a dashed green path with
  // a label
  double radius;
  krpc_SpaceCenter_CelestialBody_EquatorialRadius(conn, &radius, kerbin);
  double ut;
  krpc_SpaceCenter_UT(conn, &ut);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Orbit_CreateFromOrbitalElements(
    conn, &orbit, kerbin, radius + 250000, 0, 0, 0, 0, 0, ut);
  krpc_Drawing_OrbitPath_t path;
  krpc_Drawing_AddOrbit(conn, &path, orbit, true);
  krpc_StdLib_ColorRGB_t green = {0, 1, 0};
  krpc_Drawing_OrbitPath_set_Color(conn, path, &green);
  krpc_Drawing_OrbitPath_set_Dashed(conn, path, true);
  krpc_Drawing_OrbitPath_set_Label(conn, path, "Target orbit");

  // Mark the north pole in the map view, with the game's flag icon
  krpc_tuple_double_double_double_t site;
  krpc_SpaceCenter_CelestialBody_SurfacePosition(
    conn, &site, kerbin, 90, 0, kerbin_ref);
  krpc_Drawing_AddMapMarker(
    conn, NULL, &site, kerbin_ref, KRPC_DRAWING_MAPMARKERICON_FLAG,
    "North pole", true);

  // Draw a yellow arrow from the vessel, pointing prograde
  krpc_SpaceCenter_ReferenceFrame_t orbital_ref;
  krpc_SpaceCenter_Vessel_OrbitalReferenceFrame(conn, &orbital_ref, vessel);
  krpc_tuple_double_double_double_t prograde = {0, 1, 0};
  krpc_Drawing_Line_t arrow;
  krpc_Drawing_AddDirectionFromCom(conn, &arrow, &prograde, orbital_ref, 10, true);
  krpc_StdLib_ColorRGB_t yellow = {1, 1, 0};
  krpc_Drawing_Line_set_Color(conn, arrow, &yellow);
  krpc_Drawing_Line_set_EndArrow(conn, arrow, true);

  // Label the arrow with the orbital speed of the vessel
  krpc_SpaceCenter_Orbit_t vessel_orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &vessel_orbit, vessel);
  while (true) {
    double speed;
    krpc_SpaceCenter_Orbit_Speed(conn, &speed, vessel_orbit);
    char label[32];
    snprintf(label, sizeof(label), "%d m/s", (int)speed);
    krpc_Drawing_Line_set_Label(conn, arrow, label);
    sleep(1);
  }
}

#include <krpc_cnano.h>
#include <krpc_cnano/services/docking_camera.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Docking Camera Example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  krpc_list_object_t cameras = KRPC_NULL_LIST;
  krpc_SpaceCenter_Parts_WithName(conn, &cameras, parts, "OnboardCamera");

  krpc_DockingCamera_Camera_t camera;
  krpc_DockingCamera_Camera(conn, &camera, cameras.items[0]);
  krpc_bytes_t image = KRPC_BYTES_NULL;
  krpc_DockingCamera_Camera_Image(conn, &image, camera,
                                  KRPC_STDLIB_IMAGEFORMAT_PNG, 75);

  FILE* file = fopen("camera.png", "wb");
  fwrite(image.data, 1, image.size, file);
  fclose(file);
  KRPC_FREE_BYTES(image);
  KRPC_FREE_LIST(cameras);
  krpc_disconnect(conn);
}

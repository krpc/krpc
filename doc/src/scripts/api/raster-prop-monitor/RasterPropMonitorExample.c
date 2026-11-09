#include <krpc_cnano.h>
#include <krpc_cnano/services/raster_prop_monitor.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Raster Prop Monitor Example") != KRPC_OK) return 1;
  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  // The first screen in the IVA, and the content of its krpc page
  krpc_list_object_t monitors = KRPC_NULL_LIST;
  krpc_RasterPropMonitor_Monitors(conn, &monitors, vessel);
  krpc_RasterPropMonitor_Monitor_t monitor = monitors.items[0];
  krpc_RasterPropMonitor_Display_t display;
  krpc_RasterPropMonitor_Monitor_Display(conn, &display, monitor);

  // Raster Prop Monitor formats the SAS variable, and keeps it up to date
  krpc_RasterPropMonitor_Display_set_Text(conn, display,
                                          "SAS: {0:ON;OFF;OFF} $&$ SAS");

  // A press of the NAV button shows the page
  krpc_RasterPropMonitor_Monitor_CaptureButton(conn, monitor, "buttonR1",
                                               "krpc");
}

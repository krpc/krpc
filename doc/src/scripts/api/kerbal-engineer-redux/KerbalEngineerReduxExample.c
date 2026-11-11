#include <krpc_cnano.h>
#include <krpc_cnano/services/kerbal_engineer_redux.h>
#include <krpc_cnano/services/space_center.h>
#include <stdbool.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Kerbal Engineer Redux Example") != KRPC_OK) return 1;
  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  krpc_list_KerbalEngineerRedux_Stage_t stages = KRPC_NULL_LIST;
  krpc_KerbalEngineerRedux_Stages(conn, &stages, vessel);
  for (size_t i = stages.size; i > 0; i--) {
    krpc_KerbalEngineerRedux_Stage_t* stage = &stages.items[i - 1];
    printf("Stage %d: %.0f m/s, TWR %.2f\n", (int)stage->number, stage->delta_v,
           stage->available_twr);
  }

  krpc_KerbalEngineerRedux_BurnTime_t burn;
  bool isNull;
  krpc_KerbalEngineerRedux_BurnTime(conn, &burn, &isNull, vessel, 500);
  if (isNull)
    printf("Not enough delta-v for the burn\n");
  else
    printf("A 500 m/s burn takes %.1f seconds\n", burn.duration);
}

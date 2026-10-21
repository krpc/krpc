#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "CombinedISP");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);

  krpc_list_object_t engines = KRPC_NULL_LIST;
  krpc_SpaceCenter_Parts_Engines(conn, &engines, parts);

  krpc_list_object_t active_engines = KRPC_NULL_LIST;
  active_engines.size = 0;
  active_engines.items = krpc_calloc(engines.size, sizeof(krpc_object_t));

  for (size_t i = 0; i < engines.size; i++) {
    krpc_SpaceCenter_Engine_t engine = engines.items[i];
    bool active;
    bool has_fuel;
    krpc_SpaceCenter_Engine_Active(conn, &active, engine);
    krpc_SpaceCenter_Engine_HasFuel(conn, &has_fuel, engine);
    if (active && has_fuel) {
      active_engines.items[active_engines.size] = engine;
      active_engines.size++;
    }
  }

  printf("Active engines:\n");
  for (size_t i = 0; i < active_engines.size; i++) {
    krpc_SpaceCenter_Engine_t engine = active_engines.items[i];
    krpc_SpaceCenter_Part_t part;
    krpc_SpaceCenter_Engine_Part(conn, &part, engine);
    char * title = NULL;
    int stage;
    krpc_SpaceCenter_Part_Title(conn, &title, part);
    krpc_SpaceCenter_Part_Stage(conn, &stage, part);
    printf("   %s in stage %d\n", title, stage);
  }

  double thrust = 0;
  double fuel_consumption = 0;
  for (size_t i = 0; i < active_engines.size; i++) {
    krpc_SpaceCenter_Engine_t engine = active_engines.items[i];
    float engine_thrust;
    float engine_isp;
    krpc_SpaceCenter_Engine_Thrust(conn, &engine_thrust, engine);
    krpc_SpaceCenter_Engine_SpecificImpulse(conn, &engine_isp, engine);
    thrust += engine_thrust;
    fuel_consumption += engine_thrust / engine_isp;
  }
  double isp = thrust / fuel_consumption;
  printf("Combined vacuum Isp = %.2f seconds\n", isp);

  KRPC_FREE_LIST(engines);
  KRPC_FREE_LIST(active_engines);
}

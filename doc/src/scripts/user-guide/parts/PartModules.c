#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>
#include <string.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "PartModules") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  // [docs]
  // Print the fields the right-click menu shows for each engine, and
  // limit the thrust of each engine to half
  krpc_list_object_t engines = KRPC_NULL_LIST;
  krpc_SpaceCenter_Parts_ModulesWithName(conn, &engines, parts,
                                         "ModuleEngines");
  for (size_t i = 0; i < engines.size; i++) {
    krpc_SpaceCenter_Module_t module = engines.items[i];
    krpc_SpaceCenter_Part_t part;
    krpc_SpaceCenter_Module_Part(conn, &part, module);
    char* title = NULL;
    krpc_SpaceCenter_Part_Title(conn, &title, part);
    krpc_list_object_t fields = KRPC_NULL_LIST;
    krpc_SpaceCenter_Module_FieldList(conn, &fields, module);
    for (size_t j = 0; j < fields.size; j++) {
      krpc_SpaceCenter_PartField_t field = fields.items[j];
      bool visible;
      krpc_SpaceCenter_PartField_Visible(conn, &visible, field);
      if (visible) {
        char* gui_name = NULL;
        char* value = NULL;
        krpc_SpaceCenter_PartField_GuiName(conn, &gui_name, field);
        krpc_SpaceCenter_PartField_Value(conn, &value, field);
        printf("%s %s %s\n", title, gui_name, value);
        krpc_free(gui_name);
        krpc_free(value);
      }
      char* name = NULL;
      if (krpc_SpaceCenter_PartField_Name(conn, &name, field) == KRPC_OK &&
          strcmp(name, "thrustPercentage") == 0)
        krpc_SpaceCenter_PartField_set_FloatValue(conn, field, 50);
      krpc_free(name);
    }
    KRPC_FREE_LIST(fields);
    krpc_free(title);
  }
  KRPC_FREE_LIST(engines);

  // Extend the solar panels, as their Extend button does
  krpc_list_object_t panels = KRPC_NULL_LIST;
  krpc_SpaceCenter_Parts_ModulesWithName(conn, &panels, parts,
                                         "ModuleDeployableSolarPanel");
  for (size_t i = 0; i < panels.size; i++) {
    krpc_list_object_t events = KRPC_NULL_LIST;
    krpc_SpaceCenter_Module_EventList(conn, &events, panels.items[i]);
    for (size_t j = 0; j < events.size; j++) {
      krpc_SpaceCenter_PartEvent_t event = events.items[j];
      char* name = NULL;
      bool active;
      if (krpc_SpaceCenter_PartEvent_Name(conn, &name, event) == KRPC_OK &&
          krpc_SpaceCenter_PartEvent_Active(conn, &active, event) ==
              KRPC_OK &&
          strcmp(name, "Extend") == 0 && active)
        krpc_SpaceCenter_PartEvent_Trigger(conn, event);
      krpc_free(name);
    }
    KRPC_FREE_LIST(events);
  }
  KRPC_FREE_LIST(panels);
  // [/docs]

  krpc_disconnect(conn);
}

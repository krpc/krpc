#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  // [docs]
  // Print the fields the right-click menu shows for each engine
  for (auto module : vessel.parts().modules_with_name("ModuleEngines"))
    for (auto field : module.field_list())
      if (field.visible())
        std::cout << module.part().title() << " " << field.gui_name() << " "
                  << field.value() << std::endl;

  // Limit the thrust of each engine to half
  for (auto module : vessel.parts().modules_with_name("ModuleEngines"))
    for (auto field : module.field_list())
      if (field.name() == "thrustPercentage") field.set_float_value(50);

  // Extend the solar panels, as their Extend button does
  for (auto module :
       vessel.parts().modules_with_name("ModuleDeployableSolarPanel"))
    for (auto event : module.event_list())
      if (event.name() == "Extend" && event.active()) event.trigger();
  // [/docs]
}

import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Module;
import krpc.client.services.SpaceCenter.PartEvent;
import krpc.client.services.SpaceCenter.PartField;
import krpc.client.services.SpaceCenter.Parts;

public class PartModules {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Parts parts =
          SpaceCenter.newInstance(connection).getActiveVessel().getParts();
      // [docs]
      // Print the fields the right-click menu shows for each engine
      for (Module engine : parts.modulesWithName("ModuleEngines")) {
        for (PartField field : engine.getFieldList()) {
          if (field.getVisible()) {
            System.out.println(engine.getPart().getTitle() + " "
                + field.getGuiName() + " " + field.getValue());
          }
        }
      }

      // Limit the thrust of each engine to half
      for (Module engine : parts.modulesWithName("ModuleEngines")) {
        for (PartField field : engine.getFieldList()) {
          if (field.getName().equals("thrustPercentage")) {
            field.setFloatValue(50);
          }
        }
      }

      // Extend the solar panels, as their Extend button does
      for (Module panel :
           parts.modulesWithName("ModuleDeployableSolarPanel")) {
        for (PartEvent event : panel.getEventList()) {
          if (event.getName().equals("Extend") && event.getActive()) {
            event.trigger();
          }
        }
      }
      // [/docs]
    }
  }
}

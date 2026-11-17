import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Part;
import krpc.client.services.SpaceCenter.Vessel;

public class PressureLimits {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      for (Part part : vessel.getParts().getAll()) {
        if (part.getStaticPressure() + part.getDynamicPressure()
            > part.getMaxPressure()) {
          System.out.println(
              part.getTitle() + " is over its pressure limit");
        }
      }
      // [/docs]
    }
  }
}

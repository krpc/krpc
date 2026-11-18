import krpc.client.Connection;
import krpc.client.services.KRPC;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Part;

public class DestroyedPart {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Part part = spaceCenter.getActiveVessel().getParts().getAll().get(0);
      // [docs]
      try {
        System.out.println(part.getTemperature());
      } catch (KRPC.ObjectDestroyedException exn) {
        System.out.println("that part is gone");
      }
      // [/docs]
    }
  }
}

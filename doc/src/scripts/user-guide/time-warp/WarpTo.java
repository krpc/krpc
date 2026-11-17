import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;

public class WarpTo {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel vessel = spaceCenter.getActiveVessel();
      // [docs]
      double apoapsisUT =
          spaceCenter.getUT() + vessel.getOrbit().getTimeToApoapsis();
      spaceCenter.warpTo(apoapsisUT - 10, 100000, 2);
      // [/docs]
    }
  }
}

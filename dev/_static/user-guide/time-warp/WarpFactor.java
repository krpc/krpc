import krpc.client.Connection;
import krpc.client.services.SpaceCenter;

public class WarpFactor {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      // [docs]
      System.out.println(spaceCenter.getMaximumRailsWarpFactor());
      spaceCenter.setRailsWarpFactor(3);
      // [/docs]
    }
  }
}

import krpc.client.Connection;
import krpc.client.services.SpaceCenter;

public class LaunchVessel {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      // [docs]
      for (String name : spaceCenter.launchableVessels("VAB")) {
        System.out.println(name);
      }
      spaceCenter.launchVesselFromVAB("Kerbal X", true);
      System.out.println(spaceCenter.getActiveVessel().getName());
      // [/docs]
    }
  }
}

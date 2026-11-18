import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Parachute;
import krpc.client.services.SpaceCenter.Vessel;

public class Quickload {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      // [docs]
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel vessel = spaceCenter.getActiveVessel();
      Parachute parachute = vessel.getParts()
          .withName("parachuteSingle").get(0).getParachute();
      spaceCenter.quickload();
      // reads the parachute of the loaded game
      System.out.println(parachute.getState().getValue());
      // [/docs]
    }
  }
}

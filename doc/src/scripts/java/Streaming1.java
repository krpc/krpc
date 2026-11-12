import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.SpaceCenter.Vessel;


public class Streaming1 {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel vessel = spaceCenter.getActiveVessel();
      ReferenceFrame refframe = vessel.getOrbit().getBody().getReferenceFrame();
      while (true) {
        System.out.println(vessel.position(refframe));
      }
    }
  }
}

import krpc.client.Connection;
import krpc.client.Stream;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;


public class Streaming2 {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel vessel = spaceCenter.getActiveVessel();
      ReferenceFrame refframe = vessel.getOrbit().getBody().getReferenceFrame();
      Stream<Triplet<Double, Double, Double>> position =
          connection.addStream(vessel, "position", refframe);
      while (true) {
        System.out.println(position.get());
      }
    }
  }
}

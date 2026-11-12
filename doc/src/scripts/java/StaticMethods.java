import krpc.client.Connection;
import krpc.client.Stream;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Orbit;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;


public class StaticMethods {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
      ReferenceFrame frame = vessel.getOrbit().getBody().getReferenceFrame();

      // Call a method on an object
      System.out.println(vessel.position(frame));

      // Call a static method, passing the connection first
      System.out.println(Orbit.referencePlaneDirection(connection, frame));

      // Stream it, with only its other arguments
      Stream<Triplet<Double, Double, Double>> direction = connection.addStream(
          Orbit.class, "referencePlaneDirection", frame);
      System.out.println(direction.get());
    }
  }
}

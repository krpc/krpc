import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Orbit;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

import java.io.IOException;

public class StaticMethods {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();

      // Call a static method, passing the connection first
      System.out.println(Orbit.referencePlaneDirection(connection, vessel.getReferenceFrame()));

      // Stream it, with only its other arguments
      Stream<Triplet<Double, Double, Double>> direction = connection.addStream(
          Orbit.class, "referencePlaneDirection", vessel.getReferenceFrame());
      System.out.println(direction.get());
    }
  }
}

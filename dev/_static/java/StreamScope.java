import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;

import java.io.IOException;

public class StreamScope {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flight = spaceCenter.getActiveVessel().flight(null);
      try (Stream<Double> altitude = connection.addStream(flight, "getMeanAltitude")) {
        for (int i = 0; i < 10; i++) {
          System.out.println(altitude.get());
        }
      }
      // The stream was removed at the end of the block
    }
  }
}

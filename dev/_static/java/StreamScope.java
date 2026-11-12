import krpc.client.Connection;
import krpc.client.Stream;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;


public class StreamScope {
  public static void main(String[] args) {
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

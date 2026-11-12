import krpc.client.Connection;
import krpc.client.Stream;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;


public class StreamRate {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flightInfo = spaceCenter.getActiveVessel().flight(null);
      Stream<Double> altitude = connection.addStream(flightInfo, "getMeanAltitude");
      // Update the stream at most 10 times a second
      altitude.setRate(10);
    }
  }
}

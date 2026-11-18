import krpc.client.Connection;
import krpc.client.Stream;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;

public class StreamRate {
  public static void main(String[] args) throws InterruptedException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flight = spaceCenter.getActiveVessel().flight(null);
      // [docs]
      Stream<Double> altitude =
          connection.addStream(flight, "getMeanAltitude");
      altitude.setRate(10);
      while (true) {
        System.out.println(altitude.get());
        Thread.sleep(100);
      }
      // [/docs]
    }
  }
}

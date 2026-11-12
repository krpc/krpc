import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;


public class EventCallback {
  public static void main(String[] args) throws InterruptedException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flight = spaceCenter.getActiveVessel().flight(null);

      Expression.Bound e = Expression.bind(connection);
      Expression expr = e.greaterThan(
          e.call(connection.getCall(flight, "getMeanAltitude")), e.constantDouble(1000));
      Event event = connection.addEvent(expr);

      event.addCallback(() -> System.out.println("Altitude reached 1000m"));

      // Keep the program running, while the callback is called
      while (true) {
        Thread.sleep(1000);
      }
    }
  }
}

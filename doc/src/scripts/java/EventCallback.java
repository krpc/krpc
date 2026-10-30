import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.RPCException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;

import java.io.IOException;

public class EventCallback {
  public static void main(String[] args)
      throws IOException, RPCException, InterruptedException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flight = spaceCenter.getActiveVessel().flight(null);

      Expression expr = Expression.greaterThan(
          connection,
          Expression.call(connection, connection.getCall(flight, "getMeanAltitude")),
          Expression.constantDouble(connection, 1000));
      Event event = connection.addEvent(expr);

      event.addCallback(() -> System.out.println("Altitude reached 1000m"));

      // Keep the program running, while the callback is called
      while (true) {
        Thread.sleep(1000);
      }
    }
  }
}
